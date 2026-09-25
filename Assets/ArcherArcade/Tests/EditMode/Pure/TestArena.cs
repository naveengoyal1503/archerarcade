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

        /// <summary>A shot that clearly flies over everything into the void.</summary>
        public static ShotInput Miss() => new ShotInput(80, 0.0);
    }
}
