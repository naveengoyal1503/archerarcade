namespace ArcherArcade.Logic
{
    /// <summary>
    /// Computer opponent (GAME_DESIGN §6.1): picks a zone, solves the exact shot with the same flight code as real
    /// shots, adds its profile's noise, and shrinks that noise after every miss. Owns its own seeded Rng, so it
    /// never disturbs the match's random stream and the same match seed gives the same AI.
    /// </summary>
    public sealed class AiPlayer
    {
        readonly AiProfile _profile;
        readonly Rng _rng;
        readonly TipDef _arrow = new TipDef();

        public AiPlayer(AiProfile profile, ulong seed)
        {
            _profile = profile ?? AiProfile.Medium();
            _rng = new Rng(seed, 0xA1UL);
            ErrorScale = 1.0;
        }

        public AiProfile Profile => _profile;

        /// <summary>Multiplier on the profile's noise (1 at the start, smaller after each miss).</summary>
        public double ErrorScale { get; private set; }

        /// <summary>Decides the current fighter's shot. Always draws the same amount of randomness.</summary>
        public AiDecision Decide(MatchState match)
        {
            Fighter me = match.CurrentFighter;
            bool head = _rng.Chance(_profile.HeadAimChance);
            double think = _rng.Range(_profile.ThinkMinSeconds, _profile.ThinkMaxSeconds);
            double angleNoise = _rng.NextGaussian();
            double powerNoise = _rng.NextGaussian();
            double abilityRoll = _rng.NextDouble();

            bool useAbility = me.AbilityReady && WantsAbility(abilityRoll);
            TipDef arrow = match.BuildShotTip(me, ArrowTip.Normal, useAbility, me.MultiArrowLeft, _arrow);

            var decision = new AiDecision { ThinkSeconds = think * (1.0 + me.Status.ActiveDrawSlow) };
            ShotInput perfect;
            HitZone zone;
            if (!TrySolve(match, me, arrow, head ? HitZone.Head : HitZone.Body, out perfect, out zone))
            {
                perfect = new ShotInput(45.0, 0.75);
                zone = HitZone.None;
            }
            perfect.UseAbility = useAbility;

            ShotConfig cfg = match.Setup.Shot;
            ShotInput shot = perfect;
            shot.AngleDeg = DetMath.Clamp(perfect.AngleDeg + angleNoise * _profile.AngleSigmaDeg * ErrorScale,
                cfg.MinAngleDeg, cfg.MaxAngleDeg);
            shot.Power = DetMath.Clamp01(perfect.Power + powerNoise * _profile.PowerSigma * ErrorScale);

            decision.Input = shot;
            decision.PerfectInput = perfect;
            decision.AimZone = zone;
            return decision;
        }

        /// <summary>Call after this AI's shot resolves: a miss makes the next shot more accurate.</summary>
        public void Observe(MatchState match, ShotResult result)
        {
            if (result == null || !result.Accepted) return;
            int shooterSide = match.GetFighter(result.Shooter).Side;
            for (int i = 0; i < result.Arrows.Count; i++)
            {
                ArrowPath a = result.Arrows[i];
                if (a.Contact == ContactKind.Fighter && match.GetFighter(a.HitFighter).Side != shooterSide) return;
            }
            ErrorScale *= 1.0 - _profile.LearnPerMiss;
        }

        bool WantsAbility(double roll)
        {
            switch (_profile.AbilityUse)
            {
                case AbilityUse.Sometimes: return roll < _profile.AbilityChance;
                case AbilityUse.WhenCharged:
                case AbilityUse.Always:
                    return true;
                default:
                    return false;
            }
        }

        /// <summary>Wanted zone first, then any zone, then the zone's point ignoring cover.</summary>
        static bool TrySolve(MatchState match, Fighter me, TipDef arrow, HitZone wanted, out ShotInput input, out HitZone zone)
        {
            input = default;
            zone = wanted;
            int foeIndex = match.IsSolo ? -1 : match.ActiveFighter(1 - me.Side);
            if (foeIndex < 0) return false;
            Fighter foe = match.GetFighter(foeIndex);
            ShotConfig cfg = match.Setup.Shot;
            CollisionWorld world = match.BuildWorld();

            var req = AimRequest.Create(me.BowPosition(cfg), me.Facing, match.Setup.Body.ZoneCenter(wanted, foe.Feet), match.Wind,
                arrow.GravityScale);
            req.SpeedScale = arrow.SpeedScale;
            req.WindScale = arrow.WindScale;
            req.Clock = match.Clock;
            AimSolution sol;
            if (AimSolver.SolveValidated(req, cfg, world, match.Setup.Arena, me.Index, foeIndex, wanted, out sol))
            {
                input = sol.ToInput();
                return true;
            }

            HitZone other = wanted == HitZone.Head ? HitZone.Body : HitZone.Head;
            req.Target = match.Setup.Body.ZoneCenter(other, foe.Feet);
            if (AimSolver.SolveValidated(req, cfg, world, match.Setup.Arena, me.Index, foeIndex, HitZone.None, out sol))
            {
                input = sol.ToInput();
                zone = other;
                return true;
            }

            req.Target = match.Setup.Body.ZoneCenter(wanted, foe.Feet);
            req.PreferHighArc = true;
            if (AimSolver.SolveValidated(req, cfg, world, match.Setup.Arena, me.Index, foeIndex, HitZone.None, out sol) ||
                AimSolver.Solve(req, cfg, out sol))
            {
                input = sol.ToInput();
                return true;
            }
            return false;
        }
    }
}
