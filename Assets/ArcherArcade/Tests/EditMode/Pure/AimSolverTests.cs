using ArcherArcade.Logic;
using NUnit.Framework;

namespace ArcherArcade.Tests
{
    public class AimSolverTests
    {
        [Test]
        public void HitsBodyAndHeadFromTenToFortyMetresInAnyWind()
        {
            int shots = 0;
            for (int distance = 10; distance <= 40; distance += 5)
            {
                for (int strength = 0; strength <= 5; strength++)
                {
                    for (ulong seed = 1; seed <= 4; seed++)
                    {
                        foreach (HitZone zone in new[] { HitZone.Body, HitZone.Head })
                        {
                            var m = new MatchState(TestArena.Duel(distance, WindRange.Fixed(strength), seed));
                            ShotResult r = m.ApplyShot(TestArena.Aim(m, zone));
                            string at = distance + " m, wind " + r.Wind + ", " + zone;
                            Assert.AreEqual(ContactKind.Fighter, r.Arrows[0].Contact, at);
                            Assert.AreEqual(1, r.Arrows[0].HitFighter, at);
                            Assert.AreEqual(zone, r.Arrows[0].Zone, at);
                            shots++;
                        }
                    }
                }
            }
            Assert.AreEqual(7 * 6 * 4 * 2, shots);
        }

        [Test]
        public void HeavyTipSolutionAccountsForExtraGravity()
        {
            var m = new MatchState(TestArena.Duel(30, WindRange.Fixed(3), 2, playerTips: new[] { ArrowTip.Heavy }));
            ShotResult r = m.ApplyShot(TestArena.Aim(m, HitZone.Body, ArrowTip.Heavy));
            Assert.AreEqual(HitZone.Body, r.Arrows[0].Zone);
            Assert.AreEqual(100 - 35, m.GetFighter(1).Hp);
        }

        [Test]
        public void LobsOverATallWall()
        {
            MatchSetup s = TestArena.Duel(24, WindRange.Fixed(2), 3);
            s.Arena.Walls.Add(Shape.BoxFromTop(12, 6, 0.8, 8));
            var m = new MatchState(s);
            ShotInput shot = TestArena.Aim(m, HitZone.Body);
            Assert.Greater(shot.AngleDeg, 20.0);
            ShotResult r = m.ApplyShot(shot);
            Assert.AreEqual(ContactKind.Fighter, r.Arrows[0].Contact);
        }

        [Test]
        public void ReportsUnreachableTargets()
        {
            var cfg = new ShotConfig();
            AimSolution sol;
            Assert.IsFalse(AimSolver.Solve(AimRequest.Create(new Vec2(0, 1.25), 1, new Vec2(120, 1), -5), cfg, out sol));
            Assert.IsFalse(AimSolver.Solve(AimRequest.Create(new Vec2(0, 1.25), 1, new Vec2(-10, 1), 0), cfg, out sol),
                "target behind the archer");
        }

        [Test]
        public void SolutionIsDeterministic()
        {
            var cfg = new ShotConfig();
            AimSolution a, b;
            var req = AimRequest.Create(new Vec2(0, 1.25), 1, new Vec2(27.3, 1.1), -3.0);
            Assert.IsTrue(AimSolver.Solve(req, cfg, out a));
            Assert.IsTrue(AimSolver.Solve(req, cfg, out b));
            Assert.AreEqual(a.AngleDeg, b.AngleDeg);
            Assert.AreEqual(a.Power, b.Power);
        }
    }
}
