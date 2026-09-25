using ArcherArcade.Logic;

namespace ArcherArcade.Tests
{
    /// <summary>Builds small duels for tests: player at x = 0 facing right, opponent at x = distance facing left.</summary>
    public static class TestArena
    {
        public static MatchSetup Duel(double distance, WindRange wind, ulong seed = 1UL, ArcherDef player = null,
            ArcherDef enemy = null, ArrowTip[] playerTips = null)
        {
            var s = new MatchSetup
            {
                Seed = seed,
                Arena = ArenaLayout.TwoIslands(0.0, distance),
                Wind = wind
            };
            s.Fighters.Add(new FighterSpec
            {
                Def = player ?? ArcherTable.Ranger(), Side = 0, Feet = new Vec2(0, 0), Facing = 1,
                Tips = playerTips ?? new ArrowTip[0]
            });
            s.Fighters.Add(new FighterSpec
            {
                Def = enemy ?? ArcherTable.Basic("bandit", 100), Side = 1, Feet = new Vec2(distance, 0), Facing = -1
            });
            return s;
        }

        /// <summary>Exact shot from the current fighter at the opponent's zone, solved in the match's current wind.</summary>
        public static ShotInput Aim(MatchState m, HitZone zone, ArrowTip tip = ArrowTip.Normal)
        {
            Fighter me = m.CurrentFighter;
            Fighter foe = m.GetFighter(m.ActiveFighter(1 - me.Side));
            TipDef def = m.Setup.Tips[tip];
            var req = AimRequest.Create(me.BowPosition(m.Setup.Shot), me.Facing, m.Setup.Body.ZoneCenter(zone, foe.Feet),
                m.Wind, def.GravityScale);
            AimSolution sol;
            bool ok = AimSolver.SolveValidated(req, m.Setup.Shot, m.BuildWorld(), m.Setup.Arena, me.Index, foe.Index, zone, out sol);
            NUnit.Framework.Assert.IsTrue(ok, "solver found no shot at " + zone);
            return sol.ToInput(tip);
        }

        /// <summary>Exact shot from the current fighter that first touches prop <paramref name="prop"/>.</summary>
        public static ShotInput AimAtProp(MatchState m, int prop, ArrowTip tip = ArrowTip.Normal, bool highArc = false)
        {
            Fighter me = m.CurrentFighter;
            var req = AimRequest.Create(me.BowPosition(m.Setup.Shot), me.Facing, m.PropShapeAt(prop, m.Clock).Center, m.Wind,
                m.Setup.Tips[tip].GravityScale);
            req.Clock = m.Clock;
            req.PreferHighArc = highArc;
            AimSolution sol;
            bool ok = AimSolver.SolveValidatedProp(req, m.Setup.Shot, m.BuildWorld(), m.Setup.Arena, me.Index, prop, out sol);
            NUnit.Framework.Assert.IsTrue(ok, "solver found no shot at prop " + prop);
            return sol.ToInput(tip);
        }

        /// <summary>Shot through a point, ignoring what is in the way.</summary>
        public static ShotInput AimAtPoint(MatchState m, Vec2 point, ArrowTip tip = ArrowTip.Normal, bool highArc = false)
        {
            Fighter me = m.CurrentFighter;
            var req = AimRequest.Create(me.BowPosition(m.Setup.Shot), me.Facing, point, m.Wind, m.Setup.Tips[tip].GravityScale);
            req.PreferHighArc = highArc;
            AimSolution sol;
            NUnit.Framework.Assert.IsTrue(AimSolver.Solve(req, m.Setup.Shot, out sol), "no shot through " + point);
            return sol.ToInput(tip);
        }

        /// <summary>Solo level: just the player at x = 0 on an island (targets, apples, trick shots).</summary>
        public static MatchSetup Solo(double width = 40.0, ArrowTip[] tips = null)
        {
            var s = new MatchSetup { Seed = 1UL };
            s.Arena.Grounds.Add(Shape.BoxFromTop(0, 0, 6, 2));
            s.Arena.MinX = -30;
            s.Arena.MaxX = width + 30;
            s.Fighters.Add(new FighterSpec { Def = ArcherTable.Ranger(), Side = 0, Feet = new Vec2(0, 0), Facing = 1, Tips = tips ?? new ArrowTip[0] });
            return s;
        }

        /// <summary>A shot that clearly flies over everything into the void.</summary>
        public static ShotInput Miss() => new ShotInput(80, 0.0);
    }
}
