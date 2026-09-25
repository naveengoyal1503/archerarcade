using System.Collections.Generic;
using ArcherArcade.Logic;
using ArcherArcade.Logic.Campaign;

namespace ArcherArcade.Tests
{
    /// <summary>
    /// A perfect-aim player for campaign tests: always finds an exact shot (weak spot / head / body / legs for
    /// archers, the next target / apple / rope for goals, a brute-force bounce search for trick shots) and uses
    /// the ability when it is ready. It proves a level can be won without boosters; it does not predict fun.
    /// </summary>
    public static class PerfectPlayer
    {
        static readonly HitZone[] ZoneOrder = { HitZone.WeakSpot, HitZone.Head, HitZone.Body, HitZone.Legs };

        /// <summary>Plays the player's turn: returns true if a shot was fired (false = let the clock run).</summary>
        public static bool PlayTurn(LevelRun run)
        {
            MatchState m = run.Match;
            Fighter me = m.CurrentFighter;
            bool ability = me.AbilityReady;
            ShotInput? shot = null;

            int foe = m.IsSolo ? -1 : m.ActiveFighter(1);
            if (run.Level.Goal == GoalKind.Rescue || !run.Level.IsDuelGoal)
            {
                int prop = NextGoalProp(run);
                if (prop >= 0)
                {
                    shot = AimProp(m, me, prop, false);
                    if (shot == null && run.Level.Goal == GoalKind.TrickShot) shot = SearchProp(m, me, prop);
                }
            }
            if (shot == null && foe >= 0)
            {
                ArrowTip tip = BestTip(me);
                shot = AimFighter(m, me, foe, ability, tip);
                if (shot == null && ability)
                {
                    ability = false;
                    shot = AimFighter(m, me, foe, false, tip);
                }
                if (shot == null && tip != ArrowTip.Normal) shot = AimFighter(m, me, foe, false, ArrowTip.Normal);
                if (shot == null) shot = ClearObstacle(m, me);
            }
            if (shot == null) return false;
            run.Shoot(shot.Value);
            return true;
        }

        static int NextGoalProp(LevelRun run)
        {
            MatchState m = run.Match;
            PropKind want = run.Level.Goal == GoalKind.AppleShot ? PropKind.Apple
                : run.Level.Goal == GoalKind.Rescue ? PropKind.Rope : PropKind.Target;
            for (int i = 0; i < m.PropCount; i++)
            {
                if (m.GetProp(i).Kind == want && m.PropPresent(i)) return i;
            }
            return -1;
        }

        /// <summary>Most damage per arrow among the tips with ammo left (Heavy 35 · Fire 22 + burn · Poison 10 + 16 · Normal 25).</summary>
        static ArrowTip BestTip(Fighter me)
        {
            foreach (ArrowTip t in new[] { ArrowTip.Heavy, ArrowTip.Fire, ArrowTip.Poison })
            {
                if (me.Ammo[(int)t] > 0) return t;
            }
            return ArrowTip.Normal;
        }

        static ShotInput? AimFighter(MatchState m, Fighter me, int foeIndex, bool ability, ArrowTip tip)
        {
            Fighter foe = m.GetFighter(foeIndex);
            TipDef arrow = m.BuildShotTip(me, tip, ability, me.MultiArrowLeft, new TipDef());
            CollisionWorld world = m.BuildWorld();
            foreach (HitZone zone in ZoneOrder)
            {
                if (zone == HitZone.WeakSpot && !foe.Def.HasWeakSpot) continue;
                var req = AimRequest.Create(me.BowPosition(m.Setup.Shot), me.Facing, m.Setup.Body.ZoneCenter(zone, foe), m.Wind, arrow.GravityScale);
                req.SpeedScale = arrow.SpeedScale;
                req.WindScale = arrow.WindScale;
                req.Clock = m.Clock;
                AimSolution sol;
                if (AimSolver.SolveValidated(req, m.Setup.Shot, world, m.Setup.Arena, me.Index, foeIndex, zone, out sol))
                {
                    ShotInput s = sol.ToInput(tip);
                    s.UseAbility = ability;
                    return s;
                }
            }
            return null;
        }

        static ShotInput? AimProp(MatchState m, Fighter me, int prop, bool highArc)
        {
            var req = AimRequest.Create(me.BowPosition(m.Setup.Shot), me.Facing, m.PropShapeAt(prop, m.Clock).Center, m.Wind);
            req.Clock = m.Clock;
            req.PreferHighArc = highArc;
            AimSolution sol;
            if (AimSolver.SolveValidatedProp(req, m.Setup.Shot, m.BuildWorld(), m.Setup.Arena, me.Index, prop, out sol)) return sol.ToInput();
            return null;
        }

        /// <summary>Breaks whatever blocks every line (a vine wall, a crate) so the next turn has a clear shot.</summary>
        static ShotInput? ClearObstacle(MatchState m, Fighter me)
        {
            for (int i = 0; i < m.PropCount; i++)
            {
                Prop p = m.GetProp(i);
                if (!m.PropPresent(i) || !p.IsBreakable) continue;
                ShotInput? s = AimProp(m, me, i, false);
                if (s != null) return s;
            }
            return null;
        }

        /// <summary>Brute force over angle and power with the real flight (bounces included).</summary>
        public static ShotInput? SearchProp(MatchState m, Fighter me, int prop)
        {
            ShotConfig cfg = m.Setup.Shot;
            CollisionWorld world = m.BuildWorld();
            var paths = new List<ArrowPath>();
            Vec2 acc = Ballistics.Acceleration(m.Wind, 1.0, cfg);
            for (double angle = cfg.MinAngleDeg; angle <= cfg.MaxAngleDeg; angle += 0.5)
            {
                for (double power = 0.0; power <= 1.0; power += 0.01)
                {
                    paths.Clear();
                    FlightSimulator.Simulate(me.BowPosition(cfg), Ballistics.LaunchVelocity(angle, power, me.Facing, cfg), acc, 0, 0,
                        cfg, m.Setup.PropRules, world, m.Setup.Arena, me.Index, m.Clock, paths);
                    for (int i = 0; i < paths.Count; i++)
                    {
                        if (paths[i].Contact == ContactKind.Prop && paths[i].HitProp == prop) return new ShotInput(angle, power);
                    }
                }
            }
            return null;
        }

        /// <summary>
        /// What a player who cleared the levels before this one has, without boosters: a Ranger upgraded one level
        /// every 5 campaign levels (≈ the coins those levels give) and the 3 best tips unlocked so far.
        /// </summary>
        public static FighterSpec ExpectedProgression(int levelNumber)
        {
            var tips = new List<ArrowTip>();
            foreach (ArrowTip t in new[] { ArrowTip.Heavy, ArrowTip.Fire, ArrowTip.Poison, ArrowTip.Bomb, ArrowTip.Electric })
            {
                if (tips.Count < 3 && ArcherArcade.Logic.Meta.TipUnlocks.IsUnlocked(t, levelNumber - 1)) tips.Add(t);
            }
            return new FighterSpec { Def = ArcherTable.Ranger(), Level = 1 + levelNumber / 5, Tips = tips.ToArray() };
        }

        /// <summary>Plays a whole level: perfect player vs the level's computer opponents.</summary>
        public static LevelResult Play(LevelDef level, ulong seed, int maxTurns = 400)
        {
            var run = new LevelRun(level, ExpectedProgression(level.Number), seed, false);
            int guard = 0;
            while (run.Outcome == LevelOutcome.Playing && guard++ < maxTurns)
            {
                if (run.IsPlayerTurn)
                {
                    if (!PlayTurn(run)) run.Tick(0.25);
                }
                else
                {
                    run.Shoot(run.DecideAi().Input);
                }
            }
            return run.Result();
        }
    }
}
