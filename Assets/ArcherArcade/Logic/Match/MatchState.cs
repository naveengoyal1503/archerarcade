using System;
using System.Collections.Generic;

namespace ArcherArcade.Logic
{
    /// <summary>
    /// The whole match as a deterministic state machine (GAME_DESIGN §15). Runtime only renders it:
    /// it calls <see cref="ApplyShot"/> when a player releases and <see cref="Tick"/> while a player is aiming,
    /// then plays <see cref="Events"/> and <see cref="LastShot"/>. Nothing here reads the clock, the frame rate
    /// or UnityEngine; all randomness comes from the seeded <see cref="Rng"/>. The match clock (<see cref="Clock"/>)
    /// only advances through Tick and shot flight time, so moving targets are deterministic too.
    /// </summary>
    public sealed class MatchState
    {
        readonly MatchSetup _setup;
        readonly Fighter[] _fighters;
        readonly Prop[] _props;
        readonly Rng _rng;
        readonly bool _solo;
        readonly CollisionWorld _world = new CollisionWorld();
        readonly ShotResult _shot = new ShotResult();
        readonly List<MatchEvent> _events = new List<MatchEvent>(32);
        readonly Queue<int> _explosions = new Queue<int>();
        readonly TipDef _shotTip = new TipDef();
        readonly List<int> _chained = new List<int>(4);
        int[] _order = new int[16];
        int _shotsLeft;

        // Shot being resolved.
        Fighter _shooter;
        TipDef _tip;

        public MatchState(MatchSetup setup)
        {
            if (setup == null) throw new ArgumentNullException(nameof(setup));
            if (!HasFighter(setup, 0)) throw new ArgumentException("A match needs at least one fighter on side 0.");

            _setup = setup;
            _rng = new Rng(setup.Seed);
            _solo = !HasFighter(setup, 1);
            _fighters = new Fighter[setup.Fighters.Count];
            for (int i = 0; i < _fighters.Length; i++)
                _fighters[i] = new Fighter(i, setup.Fighters[i], setup.Tips, setup.Damage, setup.Boosters, setup.Rules);

            // Level props, then props that belong to archers: a Shield Bearer's shield, a boss's two rotating
            // shields and its (hidden until grown) vine wall.
            var extra = new List<PropSpec>();
            for (int i = 0; i < _fighters.Length; i++)
            {
                Fighter f = _fighters[i];
                if (f.Def.CarriesShield) extra.Add(ShieldFor(f, setup.PropRules));
                if (f.Def.RotatingShields)
                {
                    extra.Add(BossShieldFor(f, 0, setup.PropRules));
                    extra.Add(BossShieldFor(f, 1, setup.PropRules));
                }
                if (f.Def.VineWallEveryTurns > 0) extra.Add(VineWallFor(f, setup.PropRules));
            }
            _props = new Prop[setup.Props.Count + extra.Count];
            for (int i = 0; i < setup.Props.Count; i++) _props[i] = new Prop(i, setup.Props[i], setup.PropRules);
            for (int i = 0; i < extra.Count; i++)
            {
                int index = setup.Props.Count + i;
                _props[index] = new Prop(index, extra[i], setup.PropRules);
            }

            Winner = -1;
            int first = setup.FirstTurn == FirstTurnRule.CoinFlip && !_solo ? _rng.NextInt(2) : 0;
            BeginTurn(first);
        }

        static PropSpec ShieldFor(Fighter f, PropConfig pc)
        {
            return new PropSpec
            {
                Kind = PropKind.Shield,
                Shape = Shape.Box(new Vec2(f.Facing * pc.ShieldForward, pc.ShieldCenterY), new Vec2(pc.ShieldHalfWidth, pc.ShieldHalfHeight)),
                ShieldOwner = f.Index
            };
        }

        /// <summary>Boss shield: both start at pattern 0 (A covers the head, B the body).</summary>
        static PropSpec BossShieldFor(Fighter f, int slot, PropConfig pc)
        {
            double s = f.Def.BodyScale;
            double y = slot == 0 ? pc.BossShieldHeadY : pc.BossShieldBodyY;
            return new PropSpec
            {
                Kind = PropKind.Shield,
                Shape = Shape.Box(new Vec2(f.Facing * pc.BossShieldForward * s, y * s), new Vec2(pc.BossShieldHalfWidth * s, pc.BossShieldHalfHeight * s)),
                ShieldOwner = f.Index,
                RotatingSlot = slot
            };
        }

        /// <summary>Vine wall shape relative to its base point (bottom centre); placed when it grows.</summary>
        static PropSpec VineWallFor(Fighter f, PropConfig pc)
        {
            return new PropSpec
            {
                Kind = PropKind.VineWall,
                Shape = Shape.Box(new Vec2(0.0, pc.VineWallHeight * 0.5), new Vec2(pc.VineWallHalfWidth, pc.VineWallHeight * 0.5)),
                VineOwner = f.Index
            };
        }

        /// <summary>
        /// Ends the match because a level goal was reached or failed (targets, apples, rescue). Appends MatchOver to
        /// the current events, so the shot that reached the goal keeps its events.
        /// </summary>
        public void EndByGoal(int winnerSide)
        {
            if (Phase == MatchPhase.Over) return;
            EndMatch(winnerSide, _shot.Duration);
        }

        public MatchSetup Setup => _setup;
        public MatchPhase Phase { get; private set; }

        /// <summary>Aimed shots left in the current turn (Twin Shooter has 2).</summary>
        public int ShotsLeftThisTurn => _shotsLeft;

        /// <summary>Side whose turn it is (0 or 1).</summary>
        public int CurrentSide { get; private set; }

        /// <summary>1-based count of turns started.</summary>
        public int TurnNumber { get; private set; }

        public double TurnTimeLeft { get; private set; }

        /// <summary>Signed wind in bars; + blows toward +x (right).</summary>
        public int Wind { get; private set; }

        /// <summary>Winning side, −1 while playing.</summary>
        public int Winner { get; private set; }

        /// <summary>Match clock in seconds: aiming time (Tick) plus flight time of every shot.</summary>
        public double Clock { get; private set; }

        /// <summary>No opponent side: every turn is side 0's (target / apple / trick-shot levels).</summary>
        public bool IsSolo => _solo;

        public int FighterCount => _fighters.Length;
        public Fighter GetFighter(int index) => _fighters[index];

        public int PropCount => _props.Length;
        public Prop GetProp(int index) => _props[index];

        /// <summary>Fighter currently standing for a side, −1 if the side is out (or empty).</summary>
        public int ActiveFighter(int side)
        {
            for (int i = 0; i < _fighters.Length; i++)
            {
                if (_fighters[i].Side == side && _fighters[i].IsAlive) return i;
            }
            return -1;
        }

        public Fighter CurrentFighter => _fighters[ActiveFighter(CurrentSide)];

        /// <summary>Events from the last constructor / ApplyShot / Tick call.</summary>
        public IReadOnlyList<MatchEvent> Events => _events;

        public ShotResult LastShot => _shot;

        /// <summary>Seed for other deterministic systems of this match (AI noise, visuals) without touching ours.</summary>
        public ulong DeriveSeed(ulong salt) => _setup.Seed * 0x9E3779B97F4A7C15UL ^ (salt + 0x632BE59BD9B4E019UL);

        /// <summary>Prop shape at rest this turn (tower settling, platform position, shield on its owner), before motion.</summary>
        public Shape PropRestShape(int index)
        {
            Prop p = _props[index];
            Shape s = p.Spec.Shape;
            if (p.Kind == PropKind.Shield && p.Spec.ShieldOwner >= 0) s = s.Translated(_fighters[p.Spec.ShieldOwner].Feet);
            return s.Translated(p.StackOffset + p.TurnOffset);
        }

        /// <summary>Prop shape at a given match clock (moving targets).</summary>
        public Shape PropShapeAt(int index, double clock) => _props[index].Spec.Motion.Apply(PropRestShape(index), clock);

        /// <summary>Is this prop solid right now?</summary>
        public bool PropPresent(int index)
        {
            Prop p = _props[index];
            if (!p.Alive) return false;
            if (p.Kind == PropKind.Shield)
            {
                if (p.ShieldDown) return false;
                int owner = p.Spec.ShieldOwner;
                if (owner >= 0 && (!_fighters[owner].IsAlive || ActiveFighter(_fighters[owner].Side) != owner)) return false;
            }
            return true;
        }

        /// <summary>Rebuilds the colliders for what is standing now (also used by the AI and level validation).</summary>
        public CollisionWorld BuildWorld()
        {
            _world.Clear();
            _world.AddArena(_setup.Arena);
            for (int i = 0; i < _props.Length; i++)
            {
                if (!PropPresent(i)) continue;
                _world.AddProp(i, PropRestShape(i), _props[i].Spec.Motion, _props[i].Kind == PropKind.BouncePad, Clock);
            }
            for (int side = 0; side < 2; side++)
            {
                int f = ActiveFighter(side);
                if (f >= 0) _world.AddFighter(_fighters[f], _setup.Body);
            }
            return _world;
        }

        // ------------------------------------------------------------------ shots

        public ShotResult ApplyShot(ShotInput input)
        {
            _events.Clear();
            _shot.Reset();
            _shot.Input = input;

            if (Phase != MatchPhase.Aiming) return Reject(ShotRejectReason.MatchOver);
            if (double.IsNaN(input.AngleDeg) || double.IsNaN(input.Power) || (int)input.Tip < 0 || (int)input.Tip >= TipTable.Count)
                return Reject(ShotRejectReason.InvalidInput);

            Fighter shooter = CurrentFighter;
            if (input.UseAbility && !shooter.AbilityReady) return Reject(ShotRejectReason.AbilityUnavailable);
            bool abilityReplacesTip = input.UseAbility && _setup.Abilities[shooter.Def.Ability].Arrow != null;
            if (!abilityReplacesTip && !shooter.HasAmmo(input.Tip)) return Reject(ShotRejectReason.NoAmmo);

            ShotConfig cfg = _setup.Shot;
            input.AngleDeg = DetMath.Clamp(input.AngleDeg, cfg.MinAngleDeg, cfg.MaxAngleDeg);
            input.Power = DetMath.Clamp01(input.Power);
            _shot.Input = input;
            _shot.Accepted = true;
            _shot.Shooter = shooter.Index;
            _shot.Wind = Wind;
            _shot.Clock = Clock;

            TipDef tip = BuildShotTip(shooter, input.Tip, input.UseAbility, shooter.MultiArrowLeft, _shotTip);
            if (!abilityReplacesTip && shooter.Ammo[(int)input.Tip] > 0) shooter.Ammo[(int)input.Tip]--;
            if (input.UseAbility)
            {
                shooter.AbilityCharge = 0;
                shooter.AbilityChargeNeeded = _setup.Rules.AbilityChargeTurns;
                shooter.AbilityUsedThisTurn = true;
                Push(MatchEventKind.AbilityUsed, shooter.Index, shooter.Index, (int)shooter.Def.Ability, 0.0, shooter.Feet, -1);
            }
            if (shooter.MultiArrowLeft)
            {
                shooter.MultiArrowLeft = false;
                Push(MatchEventKind.BoosterUsed, shooter.Index, shooter.Index, (int)BoosterKind.MultiArrow, 0.0, shooter.Feet, -1);
            }

            Vec2 accel = Ballistics.Acceleration(Wind, tip.GravityScale, cfg, tip.WindScale);
            CollisionWorld world = BuildWorld();
            AbilityDef used = input.UseAbility ? _setup.Abilities[shooter.Def.Ability] : null;
            if (used != null && used.RainCount > 0)
            {
                // Rain of Leaves: arrows appear above the opponent and fall; aim does not matter.
                int foe = ActiveFighter(1 - shooter.Side);
                Vec2 above = foe >= 0 ? _fighters[foe].Feet : shooter.Feet + new Vec2(shooter.Facing * 20.0, 0.0);
                Push(MatchEventKind.RainOfLeaves, foe, shooter.Index, used.RainCount, 0.0, above, -1);
                for (int i = 0; i < used.RainCount; i++)
                {
                    double x = above.X + (i - (used.RainCount - 1) * 0.5) * used.RainSpacing;
                    FlightSimulator.Simulate(new Vec2(x, above.Y + used.RainHeight), new Vec2(0.0, -used.RainSpeed), accel, 0, 0.0,
                        cfg, _setup.PropRules, world, _setup.Arena, shooter.Index, Clock, _shot.Arrows);
                }
            }
            else
            {
                Vec2 origin = shooter.BowPosition(cfg);
                Vec2 velocity = Ballistics.LaunchVelocity(input.AngleDeg, input.Power, shooter.Facing, cfg, tip.SpeedScale);
                FlightSimulator.Simulate(origin, velocity, accel, tip.SplitCount, tip.SplitSpreadDeg, cfg, _setup.PropRules,
                    world, _setup.Arena, shooter.Index, Clock, _shot.Arrows, tip.LaunchCount, tip.LaunchSpreadDeg);
            }

            _shooter = shooter;
            _tip = tip;
            ResolveArrows();
            _shooter = null;
            _tip = null;
            Clock += _shot.Duration;

            int foeSide = 1 - shooter.Side;
            if (!_solo && !SideStanding(foeSide))
            {
                EndMatch(shooter.Side, _shot.Duration);
            }
            else if (!SideStanding(shooter.Side))
            {
                EndMatch(foeSide, _shot.Duration);
            }
            else if (--_shotsLeft > 0)
            {
                // Twin Shooter: another aimed shot in the same turn, with a fresh timer.
                TurnTimeLeft = _setup.Rules.TurnSeconds;
                Push(MatchEventKind.ExtraShot, shooter.Index, -1, _shotsLeft, _shot.Duration, shooter.Feet, -1);
            }
            else
            {
                FinishTurn(shooter);
                BeginTurn(NextSide());
            }
            return _shot;
        }

        /// <summary>
        /// The arrow a shot really fires: the picked tip, or the ability's own arrow, fanned out by Triple Shot /
        /// Multi Arrow, with the archer's shot style (Crossbow Scout) applied. Writes into <paramref name="into"/>.
        /// </summary>
        public TipDef BuildShotTip(Fighter shooter, ArrowTip picked, bool useAbility, bool multiArrow, TipDef into)
        {
            AbilityDef ability = useAbility ? _setup.Abilities[shooter.Def.Ability] : null;
            if (ability != null && ability.Arrow != null)
            {
                into.CopyFrom(ability.Arrow);
            }
            else
            {
                into.CopyFrom(_setup.Tips[picked]);
                if (ability != null && ability.LaunchCount > 1)
                {
                    into.LaunchCount = ability.LaunchCount;
                    into.LaunchSpreadDeg = ability.LaunchSpreadDeg;
                    into.Damage *= ability.DamageScale;
                    into.SplashDamage *= ability.DamageScale;
                }
            }
            if (multiArrow && into.LaunchCount < _setup.Boosters.MultiArrowCount)
            {
                BoosterConfig bc = _setup.Boosters;
                into.LaunchCount = bc.MultiArrowCount;
                into.LaunchSpreadDeg = bc.MultiArrowSpreadDeg;
                into.Damage *= bc.MultiArrowDamageScale;
                into.SplashDamage *= bc.MultiArrowDamageScale;
            }
            into.SpeedScale *= shooter.Def.ShotSpeedScale;
            into.GravityScale *= shooter.Def.ShotGravityScale;
            return into;
        }

        /// <summary>End of an archer's turn (shot or timeout): ability charge and status clean-up.</summary>
        void FinishTurn(Fighter f)
        {
            f.Status.EndTurn();
            if (!f.HasAbility || f.AbilityUsedThisTurn) return;
            bool wasReady = f.AbilityReady;
            f.AbilityCharge++;
            if (f.HeadshotThisTurn && f.AbilityChargeNeeded > _setup.Rules.HeadshotChargeTurns)
                f.AbilityChargeNeeded = _setup.Rules.HeadshotChargeTurns;
            if (!wasReady && f.AbilityReady)
                Push(MatchEventKind.AbilityReady, f.Index, -1, (int)f.Def.Ability, 0.0, f.Feet, -1);
        }

        ShotResult Reject(ShotRejectReason reason)
        {
            _shot.Accepted = false;
            _shot.Reason = reason;
            return _shot;
        }

        void ResolveArrows()
        {
            List<ArrowPath> arrows = _shot.Arrows;
            int n = arrows.Count;
            if (_order.Length < n) _order = new int[n * 2];

            // Resolve in landing order (ties by index) so knockouts happen on the right arrow.
            for (int i = 0; i < n; i++)
            {
                _order[i] = i;
                if (arrows[i].EndTime > _shot.Duration) _shot.Duration = arrows[i].EndTime;
            }
            for (int i = 1; i < n; i++)
            {
                int cur = _order[i];
                int j = i - 1;
                while (j >= 0 && arrows[_order[j]].EndTime > arrows[cur].EndTime)
                {
                    _order[j + 1] = _order[j];
                    j--;
                }
                _order[j + 1] = cur;
            }

            int standingFoe = ActiveFighter(1 - _shooter.Side);
            for (int k = 0; k < n; k++)
            {
                int ai = _order[k];
                ResolveArrow(ai, arrows[ai]);
            }

            SettleTowers();
            UpdateStanding(true, _shot.Duration);

            int nowFoe = ActiveFighter(1 - _shooter.Side);
            if (nowFoe >= 0 && nowFoe != standingFoe)
                Push(MatchEventKind.FighterEntered, nowFoe, _shooter.Index, 0, _shot.Duration, _fighters[nowFoe].Feet, -1);
        }

        void ResolveArrow(int arrow, ArrowPath path)
        {
            Fighter shooter = _shooter;
            TipDef tip = _tip;
            ArcherDef def = shooter.Def;
            DamageConfig dc = _setup.Damage;
            double time = path.EndTime;
            Vec2 point = path.EndPosition;
            double splashDamage = tip.SplashDamage + def.PassiveSplashDamage;
            double splashRadius = tip.SplashRadius > def.PassiveSplashRadius ? tip.SplashRadius : def.PassiveSplashRadius;
            bool explosive = splashDamage > 0.0 && splashRadius > 0.0;
            double chainFraction = tip.ChainFraction > def.PassiveChainFraction ? tip.ChainFraction : def.PassiveChainFraction;
            double chainRadius = tip.ChainRadius > def.PassiveChainRadius ? tip.ChainRadius : def.PassiveChainRadius;
            int directFighter = -1;
            int directProp = -1;

            if (path.Contact == ContactKind.Bounce)
            {
                Push(MatchEventKind.Bounce, -1, shooter.Index, 0, time, point, arrow, HitZone.None, path.HitProp);
                return;
            }

            int chainCount = tip.ChainFraction > 0.0 && tip.ChainCount > 1 ? tip.ChainCount : 1;

            if (path.Contact == ContactKind.Fighter && _fighters[path.HitFighter].IsAlive)
            {
                Fighter target = _fighters[path.HitFighter];
                directFighter = target.Index;
                bool absorbed = false;
                if (target.HasBubble)
                {
                    // A shield bubble eats one arrow; Electric pops it and still hits.
                    target.HasBubble = false;
                    absorbed = !tip.PopsBubbles;
                    Push(absorbed ? MatchEventKind.BubbleAbsorbed : MatchEventKind.BubblePopped, target.Index, shooter.Index, 0,
                        time, point, arrow);
                }
                if (!absorbed)
                {
                    HitZone zone = path.Zone;
                    if (zone == HitZone.Head && target.HelmetLeft)
                    {
                        target.HelmetLeft = false;
                        zone = HitZone.Body;
                        Push(MatchEventKind.HelmetSaved, target.Index, shooter.Index, 0, time, point, arrow, HitZone.Head);
                    }
                    if (zone == HitZone.Head) shooter.HeadshotThisTurn = true;
                    int dmg = ShotDamage(tip.Damage, dc.ZoneMultiplier(zone));
                    Push(MatchEventKind.Hit, target.Index, shooter.Index, dmg, time, point, arrow, zone);
                    ApplyDamage(target, dmg, time, point, arrow);
                    if (target.IsAlive)
                    {
                        ApplyStatuses(target, time, point, arrow);
                        if (tip.Knockback) Knockback(target, path.EndVelocity.X, time, arrow);
                    }
                    if (chainFraction > 0.0) Chain(point, directFighter, -1, dmg * chainFraction, chainRadius, chainCount, time, arrow);
                }
            }
            else if (path.Contact == ContactKind.Prop && _props[path.HitProp].Alive)
            {
                directProp = path.HitProp;
                HitProp(_props[directProp], explosive, tip.KnocksShields, true, time, point, arrow);
                if (chainFraction > 0.0)
                {
                    double bodyDamage = ShotDamage(tip.Damage, dc.BodyMultiplier);
                    Chain(point, -1, directProp, bodyDamage * chainFraction, chainRadius, chainCount, time, arrow);
                }
            }
            else
            {
                Push(MatchEventKind.Miss, -1, shooter.Index, 0, time, point, arrow);
            }

            if (explosive) Splash(point, directFighter, directProp, splashDamage, splashRadius, time, arrow);
            ProcessExplosions(time, arrow);
        }

        void ApplyStatuses(Fighter target, double time, Vec2 point, int arrow)
        {
            TipDef tip = _tip;
            ArcherDef def = _shooter.Def;
            int src = _shooter.Index;
            int burnPer = tip.BurnPerTurn > def.PassiveBurnPerTurn ? tip.BurnPerTurn : def.PassiveBurnPerTurn;
            int burnTurns = tip.BurnTurns > def.PassiveBurnTurns ? tip.BurnTurns : def.PassiveBurnTurns;
            if (burnPer > 0 && burnTurns > 0)
            {
                target.Status.AddBurn(burnPer, burnTurns);
                Push(MatchEventKind.BurnApplied, target.Index, src, burnPer, time, point, arrow);
            }
            if (tip.PoisonPerTurn > 0 && tip.PoisonTurns > 0)
            {
                target.Status.AddPoison(tip.PoisonPerTurn, tip.PoisonTurns);
                Push(MatchEventKind.PoisonApplied, target.Index, src, tip.PoisonPerTurn, time, point, arrow);
            }
            if (tip.StunSeconds > 0.0)
            {
                target.Status.AddStun(tip.StunSeconds);
                Push(MatchEventKind.Stunned, target.Index, src, DetMath.RoundToInt(tip.StunSeconds), time, point, arrow);
            }
            if (tip.FreezeDrawSlow > 0.0)
            {
                target.Status.AddFreeze(tip.FreezeDrawSlow);
                Push(MatchEventKind.Frozen, target.Index, src, DetMath.RoundToInt(tip.FreezeDrawSlow * 100.0), time, point, arrow);
            }
        }

        /// <summary>Damage of the current shot's arrow (ability arrows carry their own final numbers).</summary>
        int ShotDamage(double damage, double zoneMultiplier)
        {
            DamageConfig dc = _setup.Damage;
            return _tip.AbilityArrow
                ? Damage.ComputeFixed(damage, _shooter.Level, zoneMultiplier, dc)
                : Damage.Compute(damage, _shooter.Def, _shooter.Level, zoneMultiplier, _tip.Element, dc);
        }

        /// <summary>
        /// Sparks jump <paramref name="jumps"/> times, each to the nearest foe or breakable prop within the radius of
        /// the last thing hit (foes win ties, nothing is hit twice).
        /// </summary>
        void Chain(Vec2 from, int skipFighter, int skipProp, double damage, double radius, int jumps, double time, int arrow)
        {
            // Visited ids: fighters as index, props as -(index + 1).
            _chained.Clear();
            if (skipFighter >= 0) _chained.Add(skipFighter);
            if (skipProp >= 0) _chained.Add(-(skipProp + 1));
            double clock = Clock + time;

            for (int jump = 0; jump < jumps; jump++)
            {
                int bestFighter = -1;
                int bestProp = -1;
                double best = double.MaxValue;
                int foe = ActiveFighter(1 - _shooter.Side);
                if (foe >= 0 && !_chained.Contains(foe))
                {
                    double d = DistanceToFighter(_fighters[foe], from);
                    if (d <= radius)
                    {
                        bestFighter = foe;
                        best = d;
                    }
                }
                for (int i = 0; i < _props.Length; i++)
                {
                    if (_chained.Contains(-(i + 1)) || !PropPresent(i) || !_props[i].IsBreakable) continue;
                    double d = PropShapeAt(i, clock).DistanceTo(from);
                    if (d <= radius && d < best)
                    {
                        bestFighter = -1;
                        bestProp = i;
                        best = d;
                    }
                }

                if (bestFighter >= 0)
                {
                    int dmg = DetMath.RoundToInt(damage);
                    if (dmg < 1) dmg = 1;
                    Fighter target = _fighters[bestFighter];
                    Vec2 at = _setup.Body.ZoneCenter(HitZone.Body, target);
                    Push(MatchEventKind.ChainHit, bestFighter, _shooter.Index, dmg, time, at, arrow);
                    ApplyDamage(target, dmg, time, at, arrow);
                    _chained.Add(bestFighter);
                    from = at;
                }
                else if (bestProp >= 0)
                {
                    Vec2 at = PropShapeAt(bestProp, clock).Center;
                    Push(MatchEventKind.ChainHit, -1, _shooter.Index, 0, time, at, arrow, HitZone.None, bestProp);
                    HitProp(_props[bestProp], false, false, false, time, at, arrow);
                    _chained.Add(-(bestProp + 1));
                    from = at;
                }
                else
                {
                    return;
                }
            }
        }

        /// <summary>Bomb blast: foes near the impact (not the one hit directly) take splash; props get an explosive hit.</summary>
        void Splash(Vec2 point, int directFighter, int directProp, double damage, double radius, double time, int arrow)
        {
            DamageConfig dc = _setup.Damage;
            int foe = ActiveFighter(1 - _shooter.Side);
            if (foe >= 0 && foe != directFighter && DistanceToFighter(_fighters[foe], point) <= radius)
            {
                int dmg = ShotDamage(damage, dc.BodyMultiplier);
                Push(MatchEventKind.SplashHit, foe, _shooter.Index, dmg, time, point, arrow);
                ApplyDamage(_fighters[foe], dmg, time, point, arrow);
            }
            HitPropsInRadius(point, radius, directProp, time, arrow);
        }

        void HitPropsInRadius(Vec2 point, double radius, int skipProp, double time, int arrow)
        {
            double clock = Clock + time;
            for (int i = 0; i < _props.Length; i++)
            {
                if (i == skipProp || !PropPresent(i)) continue;
                if (PropShapeAt(i, clock).DistanceTo(point) > radius) continue;
                HitProp(_props[i], true, true, false, time, point, arrow);
            }
        }

        /// <summary>
        /// One hit on a prop. <paramref name="direct"/> = the arrow itself (walls and pads only react to that).
        /// Explosive hits break crates at once and topple towers.
        /// </summary>
        void HitProp(Prop prop, bool explosive, bool knocksShields, bool direct, double time, Vec2 point, int arrow)
        {
            if (!prop.Alive) return;
            int src = _shooter.Index;
            int i = prop.Index;
            switch (prop.Kind)
            {
                case PropKind.Wall:
                case PropKind.Platform:
                case PropKind.BouncePad:
                    if (direct) Push(MatchEventKind.PropHit, -1, src, 0, time, point, arrow, HitZone.None, i);
                    break;

                case PropKind.Crate:
                    if (prop.Spec.Tower >= 0)
                    {
                        if (explosive)
                        {
                            Topple(prop.Spec.Tower, time, point, arrow);
                        }
                        else
                        {
                            prop.Alive = false;
                            Push(MatchEventKind.CrateKnockedOff, -1, src, 0, time, point, arrow, HitZone.None, i);
                        }
                        break;
                    }
                    prop.HitsLeft = explosive ? 0 : prop.HitsLeft - 1;
                    if (prop.HitsLeft > 0)
                    {
                        Push(MatchEventKind.PropHit, -1, src, prop.HitsLeft, time, point, arrow, HitZone.None, i);
                    }
                    else
                    {
                        prop.Alive = false;
                        Push(MatchEventKind.CrateBroken, -1, src, 0, time, point, arrow, HitZone.None, i);
                    }
                    break;

                case PropKind.TntCrate:
                case PropKind.ExplosiveBarrel:
                    QueueExplosion(prop);
                    break;

                case PropKind.Target:
                    if (!prop.Spec.Durable) prop.Alive = false;
                    Push(MatchEventKind.TargetHit, -1, src, 0, time, point, arrow, HitZone.None, i);
                    break;

                case PropKind.Apple:
                    prop.Alive = false;
                    Push(MatchEventKind.AppleHit, -1, src, 0, time, point, arrow, HitZone.None, i);
                    break;

                case PropKind.Dummy:
                    Push(MatchEventKind.DummyHit, -1, src, 0, time, point, arrow, HitZone.None, i);
                    break;

                case PropKind.Rope:
                    prop.Alive = false;
                    Push(MatchEventKind.RopeCut, -1, src, 0, time, point, arrow, HitZone.None, i);
                    break;

                case PropKind.VineWall:
                    prop.HitsLeft = explosive || (_tip != null && _tip.Element == Element.Fire) ? 0 : prop.HitsLeft - 1;
                    if (prop.HitsLeft > 0)
                    {
                        Push(MatchEventKind.PropHit, -1, src, prop.HitsLeft, time, point, arrow, HitZone.None, i);
                    }
                    else
                    {
                        prop.Alive = false;
                        Push(MatchEventKind.VineWallBroken, -1, src, 0, time, point, arrow, HitZone.None, i);
                    }
                    break;

                case PropKind.Shield:
                    if (explosive || knocksShields)
                    {
                        prop.ShieldDownTurn = TurnNumber;
                        Push(MatchEventKind.ShieldKnockedDown, prop.Spec.ShieldOwner, src, 0, time, point, arrow, HitZone.None, i);
                    }
                    else
                    {
                        Push(MatchEventKind.ShieldBlocked, prop.Spec.ShieldOwner, src, 0, time, point, arrow, HitZone.None, i);
                    }
                    break;
            }
        }

        void QueueExplosion(Prop prop)
        {
            if (!prop.Alive) return;
            prop.Alive = false;
            _explosions.Enqueue(prop.Index);
        }

        /// <summary>Knocks a whole tower apart; TNT inside goes off.</summary>
        void Topple(int tower, double time, Vec2 point, int arrow)
        {
            bool any = false;
            for (int i = 0; i < _props.Length; i++)
            {
                Prop p = _props[i];
                if (!p.Alive || p.Spec.Tower != tower) continue;
                any = true;
                if (p.Kind == PropKind.TntCrate) QueueExplosion(p);
                else p.Alive = false;
            }
            if (any) Push(MatchEventKind.TowerToppled, -1, _shooter.Index, tower, time, point, arrow);
        }

        void ProcessExplosions(double time, int arrow)
        {
            PropConfig pc = _setup.PropRules;
            while (_explosions.Count > 0)
            {
                Prop p = _props[_explosions.Dequeue()];
                bool tnt = p.Kind == PropKind.TntCrate;
                int damage = tnt ? pc.TntDamage : pc.BarrelDamage;
                double radius = tnt ? pc.TntRadius : pc.BarrelRadius;
                Vec2 center = PropShapeAt(p.Index, Clock + time).Center;
                Push(MatchEventKind.Explosion, -1, _shooter.Index, damage, time, center, arrow, HitZone.None, p.Index);
                if (p.Spec.Tower >= 0) Topple(p.Spec.Tower, time, center, arrow);

                for (int side = 0; side < 2; side++)
                {
                    int f = ActiveFighter(side);
                    if (f < 0 || DistanceToFighter(_fighters[f], center) > radius) continue;
                    Push(MatchEventKind.ExplosionHit, f, _shooter.Index, damage, time, center, arrow, HitZone.None, p.Index);
                    ApplyDamage(_fighters[f], damage, time, center, arrow);
                }
                HitPropsInRadius(center, radius, p.Index, time, arrow);
            }
        }

        /// <summary>Bomb knockback: slide along the island away from the hit; at the edge the archer stumbles.</summary>
        void Knockback(Fighter target, double arrowVelocityX, double time, int arrow)
        {
            if (target.Spec.StandOnProp >= 0 || target.Spec.StandOnTower >= 0) return;
            PropConfig pc = _setup.PropRules;
            double dir = arrowVelocityX > 0.0 ? 1.0 : (arrowVelocityX < 0.0 ? -1.0 : -target.Facing);
            double x = target.Feet.X + dir * pc.KnockbackMeters;
            bool stumble = false;
            double left, right, top;
            if (GroundUnder(target.Feet.X, target.Feet.Y, out left, out right, out top))
            {
                double min = left + pc.EdgeMargin;
                double max = right - pc.EdgeMargin;
                if (x < min) { x = min; stumble = true; }
                if (x > max) { x = max; stumble = true; }
            }
            int cm = DetMath.RoundToInt(Math.Abs(x - target.Feet.X) * 100.0);
            target.Feet = new Vec2(x, target.Feet.Y);
            Push(MatchEventKind.Knockback, target.Index, _shooter.Index, cm, time, target.Feet, arrow);
            if (stumble) Push(MatchEventKind.Stumble, target.Index, _shooter.Index, 0, time, target.Feet, arrow);
        }

        /// <summary>Island (ground box) whose top is at or just below the feet and spans x.</summary>
        bool GroundUnder(double x, double feetY, out double left, out double right, out double top)
        {
            left = right = top = 0.0;
            bool found = false;
            double bestGap = double.MaxValue;
            List<Shape> grounds = _setup.Arena.Grounds;
            for (int i = 0; i < grounds.Count; i++)
            {
                Shape g = grounds[i];
                if (x < g.A.X - g.HalfSize.X || x > g.A.X + g.HalfSize.X) continue;
                double gTop = g.A.Y + g.HalfSize.Y;
                double gap = feetY - gTop;
                if (gap < -0.05 || gap >= bestGap) continue;
                bestGap = gap;
                left = g.A.X - g.HalfSize.X;
                right = g.A.X + g.HalfSize.X;
                top = gTop;
                found = true;
            }
            return found;
        }

        /// <summary>Crates above a knocked-off crate drop down to fill the gap.</summary>
        void SettleTowers()
        {
            for (int i = 0; i < _props.Length; i++)
            {
                Prop p = _props[i];
                if (!p.Alive || p.Spec.Tower < 0) continue;
                int rank = 0;
                for (int j = 0; j < _props.Length; j++)
                {
                    Prop q = _props[j];
                    if (q.Alive && q.Spec.Tower == p.Spec.Tower && q.Spec.StackIndex < p.Spec.StackIndex) rank++;
                }
                double size = p.Spec.Shape.HalfSize.Y * 2.0;
                p.StackOffset = new Vec2(0.0, (rank - p.Spec.StackIndex) * size);
            }
        }

        /// <summary>Archers on towers and platforms follow what they stand on.</summary>
        void UpdateStanding(bool emitDrops, double time)
        {
            for (int i = 0; i < _fighters.Length; i++)
            {
                Fighter f = _fighters[i];
                if (!f.IsAlive) continue;
                if (f.Spec.StandOnTower >= 0)
                {
                    double top = double.MinValue;
                    for (int j = 0; j < _props.Length; j++)
                    {
                        Prop p = _props[j];
                        if (!p.Alive || p.Spec.Tower != f.Spec.StandOnTower) continue;
                        Shape s = PropRestShape(j);
                        double t = s.A.Y + s.HalfSize.Y;
                        if (t > top) top = t;
                    }
                    if (top == double.MinValue)
                    {
                        double left, right, groundTop;
                        top = GroundUnder(f.Feet.X, f.Feet.Y, out left, out right, out groundTop) ? groundTop : f.Feet.Y;
                    }
                    if (top < f.Feet.Y - 1e-9)
                    {
                        int cm = DetMath.RoundToInt((f.Feet.Y - top) * 100.0);
                        f.Feet = new Vec2(f.Feet.X, top);
                        if (emitDrops) Push(MatchEventKind.FighterDropped, i, _shooter != null ? _shooter.Index : -1, cm, time, f.Feet, -1);
                    }
                }
                else if (f.Spec.StandOnProp >= 0)
                {
                    int pi = f.Spec.StandOnProp;
                    Shape rest = _props[pi].Spec.Shape;
                    Shape now = PropRestShape(pi);
                    f.Feet = new Vec2(f.Spec.Feet.X + (now.A.X - rest.A.X), now.A.Y + now.HalfSize.Y);
                }
            }
        }

        double DistanceToFighter(Fighter f, Vec2 point)
        {
            BodyConfig body = _setup.Body;
            double d = body.ZoneShape(HitZone.Head, f).DistanceTo(point);
            double b = body.ZoneShape(HitZone.Body, f).DistanceTo(point);
            double l = body.ZoneShape(HitZone.Legs, f).DistanceTo(point);
            if (b < d) d = b;
            if (l < d) d = l;
            return d;
        }

        void ApplyDamage(Fighter target, int amount, double time, Vec2 point, int arrow)
        {
            if (amount <= 0 || !target.IsAlive) return;
            if (_tip != null) target.Marks |= Injury.MarkFor(_tip.Element);
            target.Hp -= amount;
            if (target.Hp <= 0)
            {
                target.Hp = 0;
                target.Status = default;
                Push(MatchEventKind.Knockout, target.Index, _shooter != null ? _shooter.Index : -1, 0, time, point, arrow);
            }
        }

        // ------------------------------------------------------------------ turns

        /// <summary>
        /// Advances the turn timer (and the match clock) while a player is aiming. Returns true if the turn timed
        /// out (the turn passes without a shot). Do not call while an arrow is flying or the game is paused.
        /// </summary>
        public bool Tick(double dt)
        {
            _events.Clear();
            if (Phase != MatchPhase.Aiming || dt <= 0.0) return false;
            Clock += dt;
            TurnTimeLeft -= dt;
            if (TurnTimeLeft > 0.0) return false;
            TurnTimeLeft = 0.0;
            Fighter f = CurrentFighter;
            Push(MatchEventKind.TurnTimedOut, f.Index, -1, 0, 0.0, f.Feet, -1);
            FinishTurn(f);
            BeginTurn(NextSide());
            return true;
        }

        int NextSide() => _solo ? 0 : 1 - CurrentSide;

        void BeginTurn(int side)
        {
            while (true)
            {
                CurrentSide = side;
                TurnNumber++;
                Wind = _setup.FixedWind ?? ArcherArcade.Logic.Wind.Roll(_setup.Wind, _rng);
                MovePlatforms();
                RaiseShields();
                RotateShields();
                UpdateStanding(false, 0.0);

                Fighter f = _fighters[ActiveFighter(side)];
                int burn, poison;
                f.Status.TickTurnStart(out burn, out poison);
                if (burn > 0)
                {
                    Push(MatchEventKind.BurnDamage, f.Index, -1, burn, 0.0, f.Feet, -1, HitZone.None, -1, ArrowTip.Fire);
                    ApplyDamage(f, burn, 0.0, f.Feet, -1);
                }
                if (poison > 0 && f.IsAlive)
                {
                    Push(MatchEventKind.PoisonDamage, f.Index, -1, poison, 0.0, f.Feet, -1, HitZone.None, -1, ArrowTip.Poison);
                    ApplyDamage(f, poison, 0.0, f.Feet, -1);
                }

                if (!f.IsAlive)
                {
                    if (!SideStanding(side))
                    {
                        EndMatch(1 - side, 0.0);
                        return;
                    }
                    // Gauntlet: the next archer of this side steps in and takes this turn.
                    int next = ActiveFighter(side);
                    Push(MatchEventKind.FighterEntered, next, -1, 0, 0.0, _fighters[next].Feet, -1);
                    TurnNumber--;
                    continue;
                }

                f.OwnTurns++;
                f.HeadshotThisTurn = false;
                f.AbilityUsedThisTurn = false;
                ArcherDef def = f.Def;
                if (def.HealEveryTurns > 0 && def.HealAmount > 0 && f.OwnTurns % def.HealEveryTurns == 0 && f.Hp < f.MaxHp)
                {
                    int healed = f.Hp + def.HealAmount > f.MaxHp ? f.MaxHp - f.Hp : def.HealAmount;
                    f.Hp += healed;
                    Push(MatchEventKind.Healed, f.Index, f.Index, healed, 0.0, f.Feet, -1);
                }
                if (def.BubbleEveryTurns > 0 && f.OwnTurns % def.BubbleEveryTurns == 0 && !f.HasBubble)
                {
                    f.HasBubble = true;
                    Push(MatchEventKind.BubbleCast, f.Index, f.Index, 0, 0.0, f.Feet, -1);
                }
                if (def.VineWallEveryTurns > 0 && f.OwnTurns % def.VineWallEveryTurns == 0) GrowVineWall(f);
                _shotsLeft = def.ShotsPerTurn > 1 ? def.ShotsPerTurn : 1;
                if (def.EnrageBelow > 0.0 && f.Hp <= f.MaxHp * def.EnrageBelow)
                {
                    if (!f.Enraged)
                    {
                        f.Enraged = true;
                        Push(MatchEventKind.Enraged, f.Index, f.Index, 0, 0.0, f.Feet, -1);
                    }
                    if (_shotsLeft < 2) _shotsLeft = 2;
                }

                double stun = f.Status.ConsumeStun();
                double time = _setup.Rules.TurnSeconds - stun;
                TurnTimeLeft = time < _setup.Rules.MinTurnSeconds ? _setup.Rules.MinTurnSeconds : time;
                Phase = MatchPhase.Aiming;
                Push(MatchEventKind.WindChanged, f.Index, -1, Wind, 0.0, f.Feet, -1);
                Push(MatchEventKind.TurnStarted, f.Index, -1, side, 0.0, f.Feet, -1);
                return;
            }
        }

        /// <summary>Moving platforms step along their path once per turn (turn 1 = offset A).</summary>
        void MovePlatforms()
        {
            for (int i = 0; i < _props.Length; i++)
            {
                Prop p = _props[i];
                if (!p.Alive || p.Spec.TurnCycle <= 0) continue;
                double k = PropMotion.Triangle((double)(TurnNumber - 1) / p.Spec.TurnCycle);
                Vec2 offset = Vec2.Lerp(p.Spec.TurnOffsetA, p.Spec.TurnOffsetB, k);
                bool moved = offset.X != p.TurnOffset.X || offset.Y != p.TurnOffset.Y;
                p.TurnOffset = offset;
                if (moved && TurnNumber > 1) Push(MatchEventKind.PlatformMoved, -1, -1, 0, 0.0, PropRestShape(i).Center, -1, HitZone.None, i);
            }
        }

        /// <summary>
        /// Boss shields turn every turn through 3 patterns, leaving one opening each time:
        /// 0 = head + body covered (legs open), 1 = body + legs covered (head open), 2 = head + legs covered
        /// (body and the weak spot open).
        /// </summary>
        void RotateShields()
        {
            PropConfig pc = _setup.PropRules;
            int pattern = (TurnNumber - 1) % 3;
            bool any = false;
            for (int i = 0; i < _props.Length; i++)
            {
                Prop p = _props[i];
                if (p.Spec.RotatingSlot < 0 || !p.Alive) continue;
                double scale = _fighters[p.Spec.ShieldOwner].Def.BodyScale;
                double baseY = p.Spec.RotatingSlot == 0 ? pc.BossShieldHeadY : pc.BossShieldBodyY;
                double y;
                if (p.Spec.RotatingSlot == 0) y = pattern == 1 ? pc.BossShieldBodyY : pc.BossShieldHeadY;
                else y = pattern == 0 ? pc.BossShieldBodyY : pc.BossShieldLegsY;
                p.TurnOffset = new Vec2(0.0, (y - baseY) * scale);
                any = true;
            }
            if (any) Push(MatchEventKind.ShieldsRotated, -1, -1, pattern, 0.0, Vec2.Zero, -1);
        }

        /// <summary>Boss move: a vine wall grows in front of the opponent (regrows if broken).</summary>
        void GrowVineWall(Fighter owner)
        {
            int foe = ActiveFighter(1 - owner.Side);
            if (foe < 0) return;
            Fighter target = _fighters[foe];
            PropConfig pc = _setup.PropRules;
            for (int i = 0; i < _props.Length; i++)
            {
                Prop p = _props[i];
                if (p.Kind != PropKind.VineWall || p.Spec.VineOwner != owner.Index) continue;
                p.TurnOffset = new Vec2(target.Feet.X + target.Facing * pc.VineWallDistance, target.Feet.Y);
                p.HitsLeft = pc.VineWallHits;
                p.Alive = true;
                Push(MatchEventKind.VineWallGrown, owner.Index, owner.Index, 0, 0.0, PropRestShape(i).Center, -1, HitZone.None, i);
            }
        }

        void RaiseShields()
        {
            for (int i = 0; i < _props.Length; i++)
            {
                Prop p = _props[i];
                if (!p.Alive || !p.ShieldDown) continue;
                if (TurnNumber <= p.ShieldDownTurn + _setup.PropRules.ShieldDownTurns) continue;
                p.ShieldDownTurn = 0;
                Push(MatchEventKind.ShieldRaised, p.Spec.ShieldOwner, -1, 0, 0.0, PropRestShape(i).Center, -1, HitZone.None, i);
            }
        }

        void EndMatch(int winnerSide, double time)
        {
            Phase = MatchPhase.Over;
            Winner = winnerSide;
            TurnTimeLeft = 0.0;
            Push(MatchEventKind.MatchOver, -1, -1, winnerSide, time, Vec2.Zero, -1);
        }

        bool SideStanding(int side) => ActiveFighter(side) >= 0;

        static bool HasFighter(MatchSetup setup, int side)
        {
            for (int i = 0; i < setup.Fighters.Count; i++)
            {
                if (setup.Fighters[i] != null && setup.Fighters[i].Def != null && setup.Fighters[i].Side == side) return true;
            }
            return false;
        }

        void Push(MatchEventKind kind, int fighter, int source, int amount, double time, Vec2 point, int arrow,
            HitZone zone = HitZone.None, int prop = -1, ArrowTip? tip = null)
        {
            _events.Add(new MatchEvent
            {
                Kind = kind,
                Fighter = fighter,
                Source = source,
                Zone = zone,
                Amount = amount,
                Time = time,
                Point = point,
                Tip = tip ?? (_tip != null ? _tip.Tip : ArrowTip.Normal),
                Arrow = arrow,
                Prop = prop
            });
        }

        // ------------------------------------------------------------------ determinism

        /// <summary>FNV-1a hash of everything that decides the rest of the match (for replay / sync checks).</summary>
        public ulong ComputeHash()
        {
            ulong h = 14695981039346656037UL;
            h = Mix(h, (ulong)Phase);
            h = Mix(h, (ulong)CurrentSide);
            h = Mix(h, (ulong)TurnNumber);
            h = Mix(h, unchecked((ulong)Winner));
            h = Mix(h, unchecked((ulong)Wind));
            h = MixDouble(h, TurnTimeLeft);
            h = MixDouble(h, Clock);
            h = Mix(h, _rng.State);
            h = Mix(h, (ulong)_shotsLeft);
            for (int i = 0; i < _fighters.Length; i++)
            {
                Fighter f = _fighters[i];
                h = Mix(h, (ulong)f.Hp);
                h = MixDouble(h, f.Feet.X);
                h = MixDouble(h, f.Feet.Y);
                h = Mix(h, (ulong)f.Status.BurnPerTurn);
                h = Mix(h, (ulong)f.Status.BurnTurns);
                h = Mix(h, (ulong)f.Status.PoisonPerTurn);
                h = Mix(h, (ulong)f.Status.PoisonTurns);
                h = MixDouble(h, f.Status.PendingStunSeconds);
                h = MixDouble(h, f.Status.PendingDrawSlow);
                h = Mix(h, (ulong)f.AbilityCharge);
                h = Mix(h, (ulong)f.AbilityChargeNeeded);
                h = Mix(h, (ulong)f.OwnTurns);
                h = Mix(h, (ulong)((f.HasBubble ? 1 : 0) | (f.HelmetLeft ? 2 : 0) | (f.MultiArrowLeft ? 4 : 0)));
                h = Mix(h, (ulong)f.Marks);
                for (int a = 0; a < f.Ammo.Length; a++) h = Mix(h, unchecked((ulong)f.Ammo[a]));
            }
            for (int i = 0; i < _props.Length; i++)
            {
                Prop p = _props[i];
                h = Mix(h, p.Alive ? 1UL : 0UL);
                h = Mix(h, unchecked((ulong)p.HitsLeft));
                h = Mix(h, (ulong)p.ShieldDownTurn);
                h = MixDouble(h, p.StackOffset.Y);
                h = MixDouble(h, p.TurnOffset.X);
                h = MixDouble(h, p.TurnOffset.Y);
            }
            return h;
        }

        static ulong MixDouble(ulong h, double v) => Mix(h, unchecked((ulong)BitConverter.DoubleToInt64Bits(v)));

        static ulong Mix(ulong h, ulong v)
        {
            for (int i = 0; i < 8; i++)
            {
                h ^= (v >> (i * 8)) & 0xFF;
                h *= 1099511628211UL;
            }
            return h;
        }
    }
}
