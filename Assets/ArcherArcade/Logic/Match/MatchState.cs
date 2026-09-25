using System;
using System.Collections.Generic;

namespace ArcherArcade.Logic
{
    /// <summary>
    /// The whole match as a deterministic state machine (GAME_DESIGN §15). Runtime only renders it:
    /// it calls <see cref="ApplyShot"/> when a player releases and <see cref="Tick"/> while a player is aiming,
    /// then plays <see cref="Events"/> and <see cref="LastShot"/>. Nothing here reads the clock, the frame rate
    /// or UnityEngine; all randomness comes from the seeded <see cref="Rng"/>.
    /// </summary>
    public sealed class MatchState
    {
        readonly MatchSetup _setup;
        readonly Fighter[] _fighters;
        readonly Rng _rng;
        readonly CollisionWorld _world = new CollisionWorld();
        readonly ShotResult _shot = new ShotResult();
        readonly List<MatchEvent> _events = new List<MatchEvent>(32);
        int[] _order = new int[16];

        public MatchState(MatchSetup setup)
        {
            if (setup == null) throw new ArgumentNullException(nameof(setup));
            if (!HasFighter(setup, 0) || !HasFighter(setup, 1))
                throw new ArgumentException("A match needs at least one fighter on side 0 and on side 1.");

            _setup = setup;
            _rng = new Rng(setup.Seed);
            _fighters = new Fighter[setup.Fighters.Count];
            for (int i = 0; i < _fighters.Length; i++) _fighters[i] = new Fighter(i, setup.Fighters[i], setup.Tips, setup.Damage);

            Winner = -1;
            int first = setup.FirstTurn == FirstTurnRule.CoinFlip ? _rng.NextInt(2) : 0;
            BeginTurn(first);
        }

        public MatchSetup Setup => _setup;
        public MatchPhase Phase { get; private set; }

        /// <summary>Side whose turn it is (0 or 1).</summary>
        public int CurrentSide { get; private set; }

        /// <summary>1-based count of turns started.</summary>
        public int TurnNumber { get; private set; }

        public double TurnTimeLeft { get; private set; }

        /// <summary>Signed wind in bars; + blows toward +x (right).</summary>
        public int Wind { get; private set; }

        /// <summary>Winning side, −1 while playing.</summary>
        public int Winner { get; private set; }

        public int FighterCount => _fighters.Length;
        public Fighter GetFighter(int index) => _fighters[index];

        /// <summary>Fighter currently standing for a side, −1 if the side is out.</summary>
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

        /// <summary>Rebuilds the colliders for the fighters currently standing (also used by the AI).</summary>
        public CollisionWorld BuildWorld()
        {
            _world.Clear();
            _world.AddArena(_setup.Arena);
            for (int side = 0; side < 2; side++)
            {
                int f = ActiveFighter(side);
                if (f >= 0) _world.AddFighter(f, _fighters[f].Feet, _setup.Body);
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
            if (input.UseAbility) return Reject(ShotRejectReason.AbilityUnavailable);

            Fighter shooter = CurrentFighter;
            if (!shooter.HasAmmo(input.Tip)) return Reject(ShotRejectReason.NoAmmo);

            ShotConfig cfg = _setup.Shot;
            input.AngleDeg = DetMath.Clamp(input.AngleDeg, cfg.MinAngleDeg, cfg.MaxAngleDeg);
            input.Power = DetMath.Clamp01(input.Power);
            _shot.Input = input;
            _shot.Accepted = true;
            _shot.Shooter = shooter.Index;
            _shot.Wind = Wind;

            if (shooter.Ammo[(int)input.Tip] > 0) shooter.Ammo[(int)input.Tip]--;

            TipDef tip = _setup.Tips[input.Tip];
            Vec2 origin = shooter.BowPosition(cfg);
            Vec2 velocity = Ballistics.LaunchVelocity(input.AngleDeg, input.Power, shooter.Facing, cfg);
            Vec2 accel = Ballistics.Acceleration(Wind, tip.GravityScale, cfg);
            FlightSimulator.Simulate(origin, velocity, accel, tip.SplitCount, tip.SplitSpreadDeg, cfg, BuildWorld(),
                _setup.Arena, shooter.Index, _shot.Arrows);

            ResolveArrows(shooter, tip);

            if (!SideStanding(1 - shooter.Side))
            {
                EndMatch(shooter.Side, _shot.Duration);
            }
            else
            {
                shooter.Status.EndTurn();
                BeginTurn(1 - CurrentSide);
            }
            return _shot;
        }

        ShotResult Reject(ShotRejectReason reason)
        {
            _shot.Accepted = false;
            _shot.Reason = reason;
            return _shot;
        }

        void ResolveArrows(Fighter shooter, TipDef tip)
        {
            List<ArrowPath> arrows = _shot.Arrows;
            int n = arrows.Count;
            if (_order.Length < n) _order = new int[n * 2];

            // Resolve in landing order (ties by index) so knockouts happen on the right arrow.
            for (int i = 0; i < n; i++) _order[i] = i;
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

            int standingFoe = ActiveFighter(1 - shooter.Side);
            for (int k = 0; k < n; k++)
            {
                int ai = _order[k];
                ArrowPath path = arrows[ai];
                if (path.EndTime > _shot.Duration) _shot.Duration = path.EndTime;
                ResolveArrow(ai, path, shooter, tip);
            }

            int nowFoe = ActiveFighter(1 - shooter.Side);
            if (nowFoe >= 0 && nowFoe != standingFoe)
                Push(MatchEventKind.FighterEntered, nowFoe, shooter.Index, 0, _shot.Duration, _fighters[nowFoe].Feet, tip.Tip, -1);
        }

        void ResolveArrow(int arrow, ArrowPath path, Fighter shooter, TipDef tip)
        {
            DamageConfig dc = _setup.Damage;
            ArcherDef def = shooter.Def;
            double splashDamage = tip.SplashDamage + def.PassiveSplashDamage;
            double splashRadius = tip.SplashRadius > def.PassiveSplashRadius ? tip.SplashRadius : def.PassiveSplashRadius;
            int directTarget = -1;

            if (path.Contact == ContactKind.Fighter && _fighters[path.HitFighter].IsAlive)
            {
                Fighter target = _fighters[path.HitFighter];
                directTarget = target.Index;
                int dmg = Damage.Compute(tip.Damage, def, shooter.Level, dc.ZoneMultiplier(path.Zone), tip.Element, dc);
                Push(MatchEventKind.Hit, target.Index, shooter.Index, dmg, path.EndTime, path.EndPosition, tip.Tip, arrow, path.Zone);
                ApplyDamage(target, dmg, shooter.Index, path.EndTime, path.EndPosition, tip.Tip, arrow);
                if (target.IsAlive) ApplyStatuses(target, shooter, tip, path.EndTime, path.EndPosition, arrow);

                double chainFraction = tip.ChainFraction > def.PassiveChainFraction ? tip.ChainFraction : def.PassiveChainFraction;
                double chainRadius = tip.ChainRadius > def.PassiveChainRadius ? tip.ChainRadius : def.PassiveChainRadius;
                if (chainFraction > 0.0 && dmg > 0) Chain(target, shooter, dmg, chainFraction, chainRadius, path, tip, arrow);
            }
            else
            {
                Push(MatchEventKind.Miss, -1, shooter.Index, 0, path.EndTime, path.EndPosition, tip.Tip, arrow);
            }

            if (splashDamage > 0.0 && splashRadius > 0.0)
                Splash(shooter, directTarget, splashDamage, splashRadius, path, tip, arrow);
        }

        void ApplyStatuses(Fighter target, Fighter shooter, TipDef tip, double time, Vec2 point, int arrow)
        {
            ArcherDef def = shooter.Def;
            int burnPer = tip.BurnPerTurn > def.PassiveBurnPerTurn ? tip.BurnPerTurn : def.PassiveBurnPerTurn;
            int burnTurns = tip.BurnTurns > def.PassiveBurnTurns ? tip.BurnTurns : def.PassiveBurnTurns;
            if (burnPer > 0 && burnTurns > 0)
            {
                target.Status.AddBurn(burnPer, burnTurns);
                Push(MatchEventKind.BurnApplied, target.Index, shooter.Index, burnPer, time, point, tip.Tip, arrow);
            }
            if (tip.PoisonPerTurn > 0 && tip.PoisonTurns > 0)
            {
                target.Status.AddPoison(tip.PoisonPerTurn, tip.PoisonTurns);
                Push(MatchEventKind.PoisonApplied, target.Index, shooter.Index, tip.PoisonPerTurn, time, point, tip.Tip, arrow);
            }
            if (tip.StunSeconds > 0.0)
            {
                target.Status.AddStun(tip.StunSeconds);
                Push(MatchEventKind.Stunned, target.Index, shooter.Index, DetMath.RoundToInt(tip.StunSeconds), time, point, tip.Tip, arrow);
            }
            if (tip.FreezeDrawSlow > 0.0)
            {
                target.Status.AddFreeze(tip.FreezeDrawSlow);
                Push(MatchEventKind.Frozen, target.Index, shooter.Index, DetMath.RoundToInt(tip.FreezeDrawSlow * 100.0), time, point, tip.Tip, arrow);
            }
        }

        void Chain(Fighter from, Fighter shooter, int damage, double fraction, double radius, ArrowPath path, TipDef tip, int arrow)
        {
            int best = -1;
            double bestDist = double.MaxValue;
            Vec2 center = _setup.Body.ZoneCenter(HitZone.Body, from.Feet);
            for (int side = 0; side < 2; side++)
            {
                if (side == shooter.Side) continue;
                int f = ActiveFighter(side);
                if (f < 0 || f == from.Index) continue;
                double d = DistanceToFighter(_fighters[f], center);
                if (d <= radius && d < bestDist)
                {
                    best = f;
                    bestDist = d;
                }
            }
            if (best < 0) return;
            int dmg = DetMath.RoundToInt(damage * fraction);
            if (dmg < 1) dmg = 1;
            Fighter target = _fighters[best];
            Push(MatchEventKind.ChainHit, best, shooter.Index, dmg, path.EndTime, center, tip.Tip, arrow);
            ApplyDamage(target, dmg, shooter.Index, path.EndTime, center, tip.Tip, arrow);
        }

        void Splash(Fighter shooter, int directTarget, double damage, double radius, ArrowPath path, TipDef tip, int arrow)
        {
            DamageConfig dc = _setup.Damage;
            for (int side = 0; side < 2; side++)
            {
                if (side == shooter.Side) continue;
                int f = ActiveFighter(side);
                if (f < 0 || f == directTarget) continue;
                Fighter target = _fighters[f];
                if (DistanceToFighter(target, path.EndPosition) > radius) continue;
                int dmg = Damage.Compute(damage, shooter.Def, shooter.Level, 1.0, tip.Element, dc);
                Push(MatchEventKind.SplashHit, f, shooter.Index, dmg, path.EndTime, path.EndPosition, tip.Tip, arrow);
                ApplyDamage(target, dmg, shooter.Index, path.EndTime, path.EndPosition, tip.Tip, arrow);
            }
        }

        double DistanceToFighter(Fighter f, Vec2 point)
        {
            BodyConfig body = _setup.Body;
            double d = body.ZoneShape(HitZone.Head, f.Feet).DistanceTo(point);
            double b = body.ZoneShape(HitZone.Body, f.Feet).DistanceTo(point);
            double l = body.ZoneShape(HitZone.Legs, f.Feet).DistanceTo(point);
            if (b < d) d = b;
            if (l < d) d = l;
            return d;
        }

        void ApplyDamage(Fighter target, int amount, int source, double time, Vec2 point, ArrowTip tip, int arrow)
        {
            if (amount <= 0 || !target.IsAlive) return;
            target.Hp -= amount;
            if (target.Hp <= 0)
            {
                target.Hp = 0;
                target.Status = default;
                Push(MatchEventKind.Knockout, target.Index, source, 0, time, point, tip, arrow);
            }
        }

        // ------------------------------------------------------------------ turns

        /// <summary>
        /// Advances the turn timer while a player is aiming. Returns true if the turn timed out (the turn passes
        /// without a shot). Do not call while an arrow is flying or the game is paused.
        /// </summary>
        public bool Tick(double dt)
        {
            _events.Clear();
            if (Phase != MatchPhase.Aiming || dt <= 0.0) return false;
            TurnTimeLeft -= dt;
            if (TurnTimeLeft > 0.0) return false;
            TurnTimeLeft = 0.0;
            Fighter f = CurrentFighter;
            Push(MatchEventKind.TurnTimedOut, f.Index, -1, 0, 0.0, f.Feet, ArrowTip.Normal, -1);
            f.Status.EndTurn();
            BeginTurn(1 - CurrentSide);
            return true;
        }

        void BeginTurn(int side)
        {
            while (true)
            {
                CurrentSide = side;
                TurnNumber++;
                Wind = ArcherArcade.Logic.Wind.Roll(_setup.Wind, _rng);

                Fighter f = _fighters[ActiveFighter(side)];
                int burn, poison;
                f.Status.TickTurnStart(out burn, out poison);
                if (burn > 0)
                {
                    Push(MatchEventKind.BurnDamage, f.Index, -1, burn, 0.0, f.Feet, ArrowTip.Fire, -1);
                    ApplyDamage(f, burn, -1, 0.0, f.Feet, ArrowTip.Fire, -1);
                }
                if (poison > 0 && f.IsAlive)
                {
                    Push(MatchEventKind.PoisonDamage, f.Index, -1, poison, 0.0, f.Feet, ArrowTip.Poison, -1);
                    ApplyDamage(f, poison, -1, 0.0, f.Feet, ArrowTip.Poison, -1);
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
                    Push(MatchEventKind.FighterEntered, next, -1, 0, 0.0, _fighters[next].Feet, ArrowTip.Normal, -1);
                    TurnNumber--;
                    continue;
                }

                double stun = f.Status.ConsumeStun();
                double time = _setup.Rules.TurnSeconds - stun;
                TurnTimeLeft = time < _setup.Rules.MinTurnSeconds ? _setup.Rules.MinTurnSeconds : time;
                Phase = MatchPhase.Aiming;
                Push(MatchEventKind.WindChanged, f.Index, -1, Wind, 0.0, f.Feet, ArrowTip.Normal, -1);
                Push(MatchEventKind.TurnStarted, f.Index, -1, side, 0.0, f.Feet, ArrowTip.Normal, -1);
                return;
            }
        }

        void EndMatch(int winnerSide, double time)
        {
            Phase = MatchPhase.Over;
            Winner = winnerSide;
            TurnTimeLeft = 0.0;
            Push(MatchEventKind.MatchOver, -1, -1, winnerSide, time, Vec2.Zero, ArrowTip.Normal, -1);
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

        void Push(MatchEventKind kind, int fighter, int source, int amount, double time, Vec2 point, ArrowTip tip,
            int arrow, HitZone zone = HitZone.None)
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
                Tip = tip,
                Arrow = arrow
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
            h = Mix(h, unchecked((ulong)BitConverter.DoubleToInt64Bits(TurnTimeLeft)));
            h = Mix(h, _rng.State);
            for (int i = 0; i < _fighters.Length; i++)
            {
                Fighter f = _fighters[i];
                h = Mix(h, (ulong)f.Hp);
                h = Mix(h, (ulong)f.Status.BurnPerTurn);
                h = Mix(h, (ulong)f.Status.BurnTurns);
                h = Mix(h, (ulong)f.Status.PoisonPerTurn);
                h = Mix(h, (ulong)f.Status.PoisonTurns);
                h = Mix(h, unchecked((ulong)BitConverter.DoubleToInt64Bits(f.Status.PendingStunSeconds)));
                h = Mix(h, unchecked((ulong)BitConverter.DoubleToInt64Bits(f.Status.PendingDrawSlow)));
                for (int a = 0; a < f.Ammo.Length; a++) h = Mix(h, unchecked((ulong)f.Ammo[a]));
            }
            return h;
        }

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
