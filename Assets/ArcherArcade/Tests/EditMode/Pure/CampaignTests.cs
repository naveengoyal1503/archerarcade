using System;
using ArcherArcade.Logic;
using ArcherArcade.Logic.Campaign;
using ArcherArcade.Logic.Meta;
using NUnit.Framework;

namespace ArcherArcade.Tests
{
    public class CampaignTests
    {
        // LEVELS.md, World 1: number, name, tier, goal, par, preview %, coins, chest, wind min–max.
        static readonly object[][] Table =
        {
            new object[] { 1, "First Arrow", LevelTier.Easy, GoalKind.Targets, 4, 60, 20, 0, 0, 0 },
            new object[] { 2, "Hello, Pip", LevelTier.Easy, GoalKind.Duel, 6, 45, 20, 0, 0, 0 },
            new object[] { 3, "Behind the Fence", LevelTier.Easy, GoalKind.Duel, 6, 45, 20, 0, 0, 1 },
            new object[] { 4, "Feel the Breeze", LevelTier.Easy, GoalKind.Targets, 6, 45, 20, 0, 1, 1 },
            new object[] { 5, "Headhunter", LevelTier.Medium, GoalKind.Duel, 5, 30, 30, 1, 0, 2 },
            new object[] { 6, "Crate Escape", LevelTier.Easy, GoalKind.Duel, 6, 45, 20, 0, 0, 1 },
            new object[] { 7, "Apple of My Eye", LevelTier.Medium, GoalKind.AppleShot, 4, 30, 30, 0, 0, 2 },
            new object[] { 8, "Swing Time", LevelTier.Medium, GoalKind.Targets, 6, 30, 30, 0, 0, 2 },
            new object[] { 9, "Moss Returns", LevelTier.Hard, GoalKind.Duel, 5, 18, 45, 0, 1, 3 },
            new object[] { 10, "Captain Thorn", LevelTier.MiniBoss, GoalKind.Duel, 7, 18, 80, 2, 1, 3 },
            new object[] { 11, "Shield Up", LevelTier.Easy, GoalKind.Duel, 6, 45, 20, 0, 0, 1 },
            new object[] { 12, "Boing!", LevelTier.Medium, GoalKind.TrickShot, 4, 30, 30, 0, 0, 2 },
            new object[] { 13, "Twig Twins", LevelTier.Medium, GoalKind.Gauntlet, 9, 30, 30, 0, 1, 3 },
            new object[] { 14, "Cut the Rope", LevelTier.Hard, GoalKind.Rescue, 5, 18, 45, 0, 1, 3 },
            new object[] { 15, "Kaboom Valley", LevelTier.Medium, GoalKind.Duel, 5, 30, 30, 3, 1, 3 },
            new object[] { 16, "Dusk Duel", LevelTier.Easy, GoalKind.Duel, 6, 45, 20, 0, 0, 2 },
            new object[] { 17, "Split Decision", LevelTier.Medium, GoalKind.Targets, 3, 30, 30, 0, 1, 3 },
            new object[] { 18, "Bramble's Revenge", LevelTier.Hard, GoalKind.Duel, 5, 18, 45, 0, 2, 4 },
            new object[] { 19, "The Long Night", LevelTier.Hard, GoalKind.Gauntlet, 12, 18, 45, 0, 2, 4 },
            new object[] { 20, "The Forest Warden", LevelTier.Boss, GoalKind.Boss, 9, 18, 150, 4, 2, 5 }
        };

        [Test]
        public void LevelTableMatchesLevelsMd()
        {
            LevelDef[] levels = WorldOne.Levels();
            Assert.AreEqual(20, levels.Length);
            foreach (object[] row in Table)
            {
                LevelDef l = levels[(int)row[0] - 1];
                string at = "level " + row[0];
                Assert.AreEqual((int)row[0], l.Number, at);
                Assert.AreEqual((string)row[1], l.Name, at);
                Assert.AreEqual((LevelTier)row[2], l.Tier, at);
                Assert.AreEqual((GoalKind)row[3], l.Goal, at);
                Assert.AreEqual((int)row[4], l.Par, at);
                Assert.AreEqual((int)row[5] / 100.0, l.PreviewShare, 1e-9, at);
                Assert.AreEqual((int)row[6], l.RewardCoins, at);
                Assert.AreEqual((int)row[7], l.Chest, at);
                Assert.AreEqual((int)row[8], l.Wind.Min, at);
                Assert.AreEqual((int)row[9], l.Wind.Max, at);
            }
        }

        [Test]
        public void OpponentsMatchTheTable()
        {
            Assert.AreEqual("scout_pip", WorldOne.Level(2).Opponents[0].EnemyId);
            Assert.AreEqual(60, WorldOne.Level(2).Opponents[0].Hp);
            Assert.AreEqual(80, WorldOne.Level(3).Opponents[0].Hp);
            Assert.AreEqual("medium", WorldOne.Level(5).Opponents[0].Ai);
            Assert.AreEqual("crossbow_scout", WorldOne.Level(6).Opponents[0].EnemyId);
            Assert.AreEqual("hard", WorldOne.Level(9).Opponents[0].Ai);
            Assert.AreEqual("captain_thorn", WorldOne.Level(10).Opponents[0].EnemyId);
            Assert.AreEqual(160, WorldOne.Level(10).Opponents[0].Hp);
            Assert.AreEqual("shield_bearer", WorldOne.Level(11).Opponents[0].EnemyId);
            Assert.AreEqual(2, WorldOne.Level(13).Opponents.Length);
            Assert.AreEqual("tower_sniper", WorldOne.Level(15).Opponents[0].EnemyId);
            Assert.AreEqual("healer_druid", WorldOne.Level(16).Opponents[0].EnemyId);
            Assert.AreEqual("ranger_bramble", WorldOne.Level(18).Opponents[0].EnemyId);
            LevelDef l19 = WorldOne.Level(19);
            Assert.AreEqual(new[] { "crossbow_scout", "healer_druid", "hunter_moss" },
                Array.ConvertAll(l19.Opponents, o => o.EnemyId));
            Assert.AreEqual(new[] { "medium", "hard", "hard" }, Array.ConvertAll(l19.Opponents, o => o.Ai));
            Assert.AreEqual("forest_warden", WorldOne.Level(20).Opponents[0].EnemyId);
            Assert.AreEqual(250, WorldOne.Level(20).Opponents[0].Hp);
        }

        [Test]
        public void TipUnlocksAndForcedTipsMatchTheTable()
        {
            foreach (LevelDef l in WorldOne.Levels())
            {
                Assert.AreEqual(TipUnlocks.UnlockedBy(l.Number), l.UnlockTip, "level " + l.Number);
            }
            Assert.AreEqual(new[] { ArrowTip.Bomb }, WorldOne.Level(11).ForcedTips);
            Assert.AreEqual(new[] { ArrowTip.Split }, WorldOne.Level(17).ForcedTips);
        }

        [Test]
        public void ArcherUnlocksAndChestsMatchTheTable()
        {
            Assert.AreEqual("fire", WorldOne.Level(5).UnlockArcherId);
            Assert.AreEqual("bomb", WorldOne.Level(20).UnlockArcherId);
            Assert.IsTrue(ArcherTable.FireArcher().Unlock.IsMet(5, 0));
            Assert.IsTrue(ArcherTable.BombArcher().Unlock.IsMet(20, 0));
            foreach (LevelDef l in WorldOne.Levels())
            {
                Assert.AreEqual(l.Number % 5 == 0 ? l.Number / 5 : 0, l.Chest, "chests after 5/10/15/20, level " + l.Number);
            }
        }

        [Test]
        public void FirstClearCoinsFollowTheTier()
        {
            foreach (LevelDef l in WorldOne.Levels())
            {
                int expected = l.Tier == LevelTier.Easy ? 20 : l.Tier == LevelTier.Medium ? 30 : l.Tier == LevelTier.Hard ? 45
                    : l.Tier == LevelTier.MiniBoss ? 80 : 150;
                Assert.AreEqual(expected, l.RewardCoins, "level " + l.Number);
            }
        }

        [Test]
        public void EveryLevelHasItsIdeaCardsOnce()
        {
            var seen = new System.Collections.Generic.HashSet<IdeaCard>();
            foreach (LevelDef l in WorldOne.Levels())
            {
                foreach (IdeaCard c in l.Ideas) Assert.IsTrue(seen.Add(c), c + " used twice");
            }
            Assert.AreEqual(0, Array.IndexOf(WorldOne.Level(1).Ideas, IdeaCard.Tutorial));
            Assert.AreEqual(0, WorldOne.Level(19).Ideas.Length, "level 19 has no new idea");
        }

        [Test]
        public void EveryLevelBuildsWithSpawnsOnSolidGround()
        {
            foreach (LevelDef l in WorldOne.Levels())
            {
                var m = new MatchState(LevelBuilder.Build(l, LevelBuilder.DefaultPlayer(), 1));
                Assert.AreEqual(1 + l.Opponents.Length, m.FighterCount, "level " + l.Number);
                Assert.AreEqual(l.Opponents.Length == 0, m.IsSolo);
                for (int i = 0; i < m.FighterCount; i++)
                {
                    Fighter f = m.GetFighter(i);
                    Assert.IsTrue(StandsOnSomething(m, f), "level " + l.Number + " fighter " + i + " floats at " + f.Feet);
                    Assert.That(f.Feet.X, Is.InRange(m.Setup.Arena.MinX, m.Setup.Arena.MaxX));
                }
                Assert.AreEqual(l.Goal == GoalKind.Targets || l.Goal == GoalKind.TrickShot ? l.TargetCount : CountKind(m, PropKind.Target),
                    CountKind(m, PropKind.Target), "level " + l.Number);
                if (l.Goal == GoalKind.AppleShot) Assert.AreEqual(l.TargetCount, CountKind(m, PropKind.Apple));
                if (l.Goal == GoalKind.Rescue) Assert.AreEqual(1, CountKind(m, PropKind.Rope));
            }
        }

        static int CountKind(MatchState m, PropKind kind)
        {
            int n = 0;
            for (int i = 0; i < m.PropCount; i++)
            {
                if (m.GetProp(i).Kind == kind) n++;
            }
            return n;
        }

        static bool StandsOnSomething(MatchState m, Fighter f)
        {
            Vec2 below = f.Feet + new Vec2(0, -0.05);
            foreach (Shape g in m.Setup.Arena.Grounds)
            {
                if (g.DistanceTo(below) <= 1e-6) return true;
            }
            for (int i = 0; i < m.PropCount; i++)
            {
                if (m.GetProp(i).Alive && m.PropRestShape(i).DistanceTo(below) <= 1e-6) return true;
            }
            return false;
        }

        [Test]
        public void ParIsNotBelowWhatAPerfectShotCanDo()
        {
            foreach (LevelDef l in WorldOne.Levels())
            {
                if (!l.IsDuelGoal)
                {
                    Assert.GreaterOrEqual(l.Par, l.TargetCount, "level " + l.Number);
                    continue;
                }
                int hp = 0;
                bool weak = false;
                foreach (OpponentSpec o in l.Opponents)
                {
                    ArcherDef d = EnemyTable.ById(o.EnemyId, o.Hp);
                    hp += d.BaseHp;
                    weak |= d.HasWeakSpot;
                }
                double best = 25 * (weak ? 2.5 : 2.0);
                Assert.GreaterOrEqual(l.Par, (int)Math.Ceiling(hp / best), "level " + l.Number);
            }
        }

        [Test]
        public void SolverReachesEveryOpponentInTheWorstWind()
        {
            foreach (LevelDef l in WorldOne.Levels())
            {
                for (int k = 0; k < l.Opponents.Length; k++)
                {
                    foreach (int sign in new[] { -1, 1 })
                    {
                        var m = new MatchState(LevelBuilder.Build(l, LevelBuilder.DefaultPlayer(), 1));
                        for (int e = 0; e < k; e++) m.GetFighter(1 + e).Hp = 0; // earlier gauntlet archers are down
                        Fighter me = m.GetFighter(0);
                        Fighter foe = m.GetFighter(1 + k);
                        CollisionWorld world = m.BuildWorld();
                        bool shielded = foe.Def.CarriesShield || foe.Def.RotatingShields;
                        int reachable = 0;
                        foreach (HitZone zone in new[] { HitZone.Head, HitZone.Body })
                        {
                            var req = AimRequest.Create(me.BowPosition(m.Setup.Shot), 1, m.Setup.Body.ZoneCenter(zone, foe), sign * l.Wind.Max);
                            AimSolution sol;
                            bool ok = AimSolver.SolveValidated(req, m.Setup.Shot, world, m.Setup.Arena, 0, foe.Index, zone, out sol);
                            if (ok) reachable++;
                            else if (!shielded) Assert.Fail("level " + l.Number + ": " + foe.Def.Name + " " + zone + " unreachable, wind " + sign * l.Wind.Max);
                        }
                        if (shielded)
                        {
                            var legs = AimRequest.Create(me.BowPosition(m.Setup.Shot), 1, m.Setup.Body.ZoneCenter(HitZone.Legs, foe), sign * l.Wind.Max);
                            AimSolution sol;
                            if (AimSolver.SolveValidated(legs, m.Setup.Shot, world, m.Setup.Arena, 0, foe.Index, HitZone.None, out sol)) reachable++;
                            Assert.Greater(reachable, 0, "level " + l.Number + ": no open zone on " + foe.Def.Name);
                        }
                    }
                }
            }
        }

        [Test]
        public void SolverReachesEveryTargetAppleAndRope()
        {
            foreach (LevelDef l in WorldOne.Levels())
            {
                if (l.IsDuelGoal || l.Goal == GoalKind.TrickShot) continue;
                foreach (int sign in new[] { -1, 1 })
                {
                    var m = new MatchState(LevelBuilder.Build(l, LevelBuilder.DefaultPlayer(), 1));
                    for (int i = 0; i < m.PropCount; i++)
                    {
                        PropKind k = m.GetProp(i).Kind;
                        if (k != PropKind.Target && k != PropKind.Apple && k != PropKind.Rope) continue;
                        Assert.IsTrue(ReachableAtSomeTime(m, i, sign * l.Wind.Max),
                            "level " + l.Number + ": " + k + " " + i + " unreachable, wind " + sign * l.Wind.Max);
                    }
                }
            }
        }

        static bool ReachableAtSomeTime(MatchState m, int prop, int wind)
        {
            Fighter me = m.GetFighter(0);
            double period = m.GetProp(prop).Spec.Motion.PeriodSeconds;
            double span = period > 0 ? period : 0.0;
            for (double clock = 0.0; clock <= span + 1e-9; clock += 0.1)
            {
                var req = AimRequest.Create(me.BowPosition(m.Setup.Shot), 1, m.PropShapeAt(prop, clock).Center, wind);
                req.Clock = clock;
                AimSolution sol;
                if (AimSolver.SolveValidatedProp(req, m.Setup.Shot, m.BuildWorld(), m.Setup.Arena, 0, prop, out sol)) return true;
                if (span == 0.0) break;
            }
            return false;
        }

        [Test]
        public void TrickShotTargetsNeedTheBouncePadAndCanBeHit()
        {
            LevelDef l = WorldOne.Level(12);
            var m = new MatchState(LevelBuilder.Build(l, LevelBuilder.DefaultPlayer(), 1));
            for (int i = 0; i < m.PropCount; i++)
            {
                if (m.GetProp(i).Kind != PropKind.Target) continue;
                var req = AimRequest.Create(m.GetFighter(0).BowPosition(m.Setup.Shot), 1, m.PropShapeAt(i, 0).Center, 0);
                AimSolution sol;
                Assert.IsFalse(AimSolver.SolveValidatedProp(req, m.Setup.Shot, m.BuildWorld(), m.Setup.Arena, 0, i, out sol),
                    "target " + i + " should be hidden from direct shots");
                Assert.IsNotNull(PerfectPlayer.SearchProp(m, m.GetFighter(0), i), "target " + i + " has no bounce shot");
            }
        }

        [Test]
        public void AppleShotsNeverRequireHittingTheDummy()
        {
            LevelDef l = WorldOne.Level(7);
            var m = new MatchState(LevelBuilder.Build(l, LevelBuilder.DefaultPlayer(), 1));
            for (int i = 0; i < m.PropCount; i++)
            {
                if (m.GetProp(i).Kind != PropKind.Apple) continue;
                Assert.IsTrue(ReachableAtSomeTime(m, i, 0), "apple " + i);
            }
        }
    }
}
