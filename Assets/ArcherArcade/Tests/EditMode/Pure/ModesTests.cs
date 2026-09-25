using ArcherArcade.Logic;
using ArcherArcade.Logic.Campaign;
using ArcherArcade.Logic.Modes;
using NUnit.Framework;

namespace ArcherArcade.Tests
{
    public class ModesTests
    {
        static ulong SetupHash(LevelDef l)
        {
            var m = new MatchState(LevelBuilder.Build(l, LevelBuilder.DefaultPlayer(), 1));
            return m.ComputeHash() ^ (ulong)m.PropCount * 31UL ^ (ulong)l.Goal * 7UL ^ (ulong)(l.Wind.Min * 13 + l.Wind.Max);
        }

        [Test]
        public void DailyIsTheSameEverywhereForADay()
        {
            for (int day = 20000; day < 20030; day++)
            {
                DailyChallenge.Daily a = DailyChallenge.ForDay(day);
                DailyChallenge.Daily b = DailyChallenge.ForDay(day);
                Assert.AreEqual(a.Template, b.Template);
                Assert.AreEqual(a.Twist, b.Twist);
                Assert.AreEqual(SetupHash(a.Level), SetupHash(b.Level), "day " + day);
            }
            Assert.AreEqual(20356, DailyChallenge.DayNumber(new System.DateTime(2025, 9, 25, 23, 59, 0, System.DateTimeKind.Utc)));
        }

        [Test]
        public void DailyUsesEveryTemplateAndTwist()
        {
            var templates = new System.Collections.Generic.HashSet<DailyTemplate>();
            var twists = new System.Collections.Generic.HashSet<DailyTwist>();
            for (int day = 20000; day < 20200; day++)
            {
                DailyChallenge.Daily d = DailyChallenge.ForDay(day);
                templates.Add(d.Template);
                twists.Add(d.Twist);
            }
            Assert.AreEqual(4, templates.Count);
            Assert.AreEqual(6, twists.Count);
        }

        [Test]
        public void EveryDailyForAYearIsSolvable()
        {
            for (int day = 20000; day < 20365; day++)
            {
                LevelDef l = DailyChallenge.ForDay(day).Level;
                var m = new MatchState(LevelBuilder.Build(l, LevelBuilder.DefaultPlayer(), 1));
                Fighter me = m.GetFighter(0);
                TipDef arrow = m.BuildShotTip(me, l.OnlyTip ?? ArrowTip.Normal, false, false, new TipDef());
                foreach (int sign in new[] { -1, 1 })
                {
                    int wind = sign * l.Wind.Max;
                    if (!m.IsSolo)
                    {
                        Fighter foe = m.GetFighter(1);
                        var req = AimRequest.Create(me.BowPosition(m.Setup.Shot), 1, m.Setup.Body.ZoneCenter(HitZone.Body, foe), wind, arrow.GravityScale);
                        AimSolution sol;
                        Assert.IsTrue(AimSolver.SolveValidated(req, m.Setup.Shot, m.BuildWorld(), m.Setup.Arena, 0, foe.Index, HitZone.None, out sol),
                            "day " + day + " duel unreachable in wind " + wind);
                        continue;
                    }
                    if (l.Goal == GoalKind.TrickShot) continue; // same geometry as level 12, validated there
                    for (int i = 0; i < m.PropCount; i++)
                    {
                        PropKind k = m.GetProp(i).Kind;
                        if (k != PropKind.Target && k != PropKind.Apple) continue;
                        var req = AimRequest.Create(me.BowPosition(m.Setup.Shot), 1, m.PropShapeAt(i, 0).Center, wind, arrow.GravityScale);
                        AimSolution sol;
                        Assert.IsTrue(AimSolver.SolveValidatedProp(req, m.Setup.Shot, m.BuildWorld(), m.Setup.Arena, 0, i, out sol),
                            "day " + day + " " + k + " " + i + " unreachable in wind " + wind);
                    }
                }
            }
        }

        [Test]
        public void SplitOnlyDailyGivesUnlimitedSplitsAndNoNormalArrows()
        {
            for (int day = 20000; day < 20400; day++)
            {
                DailyChallenge.Daily d = DailyChallenge.ForDay(day);
                if (d.Twist != DailyTwist.SplitOnly) continue;
                var m = new MatchState(LevelBuilder.Build(d.Level, LevelBuilder.DefaultPlayer(), 1));
                Assert.AreEqual(0, m.GetFighter(0).Ammo[(int)ArrowTip.Normal]);
                Assert.AreEqual(-1, m.GetFighter(0).Ammo[(int)ArrowTip.Split]);
                Assert.AreEqual(ShotRejectReason.NoAmmo, m.ApplyShot(new ShotInput(30, 0.5)).Reason);
                Assert.IsTrue(m.ApplyShot(new ShotInput(30, 0.5, ArrowTip.Split)).Accepted);
                return;
            }
            Assert.Fail("no Split-only day found");
        }

        [Test]
        public void TrainingHasExactWindNoTimerAndStats()
        {
            var t = new TrainingRange(LevelBuilder.DefaultPlayer(), 20, -3);
            Assert.AreEqual(-3, t.Match.Wind);
            Assert.IsFalse(t.Match.Tick(600), "no timer");
            var req = AimRequest.Create(t.Match.GetFighter(0).BowPosition(t.Match.Setup.Shot), 1, new Vec2(20, TrainingRange.BoardHeight), -3);
            AimSolution sol;
            Assert.IsTrue(AimSolver.Solve(req, t.Match.Setup.Shot, out sol));
            t.Shoot(sol.ToInput());
            t.Shoot(TestArena.Miss());
            t.Shoot(sol.ToInput());
            Assert.AreEqual(3, t.Shots);
            Assert.AreEqual(2, t.Hits, "the board stays up");
            Assert.AreEqual(2.0 / 3.0, t.Accuracy, 1e-12);
            Assert.AreEqual(-3, t.Match.Wind, "wind stays where the slider is");
            Assert.AreEqual(0, t.Match.CurrentSide);
        }

        [Test]
        public void QuickDuelBuildsAFairDuel()
        {
            foreach (string diff in QuickDuel.Difficulties)
            {
                for (ulong seed = 1; seed <= 5; seed++)
                {
                    QuickDuel.Setup q = QuickDuel.Build(LevelBuilder.DefaultPlayer(), diff, "random", seed);
                    CollectionAssert.Contains(ArenaCatalog.QuickDuelArenas, q.ArenaId);
                    Assert.AreEqual(FirstTurnRule.CoinFlip, q.Match.FirstTurn);
                    Assert.That(q.Distance, Is.InRange(18, 26));
                    var m = new MatchState(q.Match);
                    Assert.AreEqual(2, m.FighterCount);
                    Assert.AreEqual(diff, q.Ai.Id);
                    QuickDuel.Setup again = QuickDuel.Build(LevelBuilder.DefaultPlayer(), diff, "random", seed);
                    Assert.AreEqual(q.ArenaId, again.ArenaId);
                    Assert.AreEqual(q.Opponent.Id, again.Opponent.Id);
                }
            }
        }

        [Test]
        public void EveryArenaIsPlayable()
        {
            foreach (string id in ArenaCatalog.TwoPlayerArenas)
            {
                var level = new LevelDef
                {
                    Goal = GoalKind.Duel, Opponents = new[] { OpponentSpec.Of("bandit", "easy") }, Wind = new WindRange(0, 3),
                    BuildArena = a => ArenaCatalog.Build(a, id, 22)
                };
                var m = new MatchState(LevelBuilder.Build(level, LevelBuilder.DefaultPlayer(), 1));
                Fighter me = m.GetFighter(0);
                Fighter foe = m.GetFighter(1);
                var req = AimRequest.Create(me.BowPosition(m.Setup.Shot), 1, m.Setup.Body.ZoneCenter(HitZone.Body, foe), 3);
                AimSolution sol;
                Assert.IsTrue(AimSolver.SolveValidated(req, m.Setup.Shot, m.BuildWorld(), m.Setup.Arena, 0, 1, HitZone.None, out sol), id);
            }
        }

        [Test]
        public void TwoPlayerBestOfThreeWithSwapAndRematch()
        {
            var series = new PvpSeries(new PvpSettings { Name1 = "  Aarav  ", Name2 = "", BestOf = 3, Handicap1 = 2.0, Handicap2 = 0.5 });
            Assert.AreEqual("Aarav", series.Settings.Name1);
            Assert.AreEqual("P2", series.Settings.Name2);
            Assert.AreEqual(1.3, series.Settings.Handicap1);
            Assert.AreEqual(0.7, series.Settings.Handicap2);
            Assert.AreEqual(2, series.WinsNeeded);

            MatchSetup round1 = series.NextRound(1);
            Assert.AreEqual(1.3, round1.Fighters[0].HpScale);
            series.RecordRound(0);
            series.SwapSides();
            Assert.AreEqual(1, series.PlayerOnSide(0));
            Assert.AreEqual("P2", series.NameOnSide(0));
            MatchSetup round2 = series.NextRound(2);
            Assert.AreEqual(0.7, round2.Fighters[0].HpScale, "P2 now on the left");
            series.RecordRound(1); // right side = P1 wins
            Assert.IsTrue(series.IsOver);
            Assert.AreEqual(0, series.Winner);
            series.RecordRound(0);
            Assert.AreEqual(2, series.Wins(0), "no rounds after the end");
            series.Rematch();
            Assert.IsFalse(series.IsOver);
            Assert.AreEqual(0, series.Round);
        }

        [Test]
        public void WindOffMeansCalm()
        {
            var series = new PvpSeries(new PvpSettings { WindOn = false });
            var m = new MatchState(series.NextRound(3));
            for (int i = 0; i < 6; i++)
            {
                Assert.AreEqual(0, m.Wind);
                m.ApplyShot(TestArena.Miss());
            }
        }
    }
}
