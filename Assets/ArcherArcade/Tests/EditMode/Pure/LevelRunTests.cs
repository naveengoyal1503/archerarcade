using ArcherArcade.Logic;
using ArcherArcade.Logic.Campaign;
using NUnit.Framework;

namespace ArcherArcade.Tests
{
    public class LevelRunTests
    {
        static ShotInput AimProp(MatchState m, int prop)
        {
            Fighter me = m.GetFighter(0);
            var req = AimRequest.Create(me.BowPosition(m.Setup.Shot), 1, m.PropShapeAt(prop, m.Clock).Center, m.Wind);
            req.Clock = m.Clock;
            AimSolution sol;
            Assert.IsTrue(AimSolver.SolveValidatedProp(req, m.Setup.Shot, m.BuildWorld(), m.Setup.Arena, 0, prop, out sol));
            return sol.ToInput();
        }

        static int FirstOf(MatchState m, PropKind kind)
        {
            for (int i = 0; i < m.PropCount; i++)
            {
                if (m.GetProp(i).Kind == kind && m.PropPresent(i)) return i;
            }
            return -1;
        }

        [Test]
        public void HittingEveryTargetWithinParGivesThreeStars()
        {
            var run = new LevelRun(WorldOne.Level(1), LevelBuilder.DefaultPlayer(), 1, false);
            for (int i = 0; i < 3; i++) run.Shoot(AimProp(run.Match, FirstOf(run.Match, PropKind.Target)));
            Assert.AreEqual(LevelOutcome.Won, run.Outcome);
            LevelResult r = run.Result();
            Assert.IsTrue(r.Won);
            Assert.AreEqual(3, r.ArrowsUsed);
            Assert.AreEqual(3, r.Stars);
        }

        [Test]
        public void MissesCostStarsAndTheArrowLimitEndsTheLevel()
        {
            var run = new LevelRun(WorldOne.Level(1), LevelBuilder.DefaultPlayer(), 1, false);
            for (int i = 0; i < 3; i++) run.Shoot(TestArena.Miss());
            for (int i = 0; i < 3; i++) run.Shoot(AimProp(run.Match, FirstOf(run.Match, PropKind.Target)));
            Assert.AreEqual(LevelOutcome.Won, run.Outcome);
            Assert.AreEqual(2, run.Result().Stars, "6 arrows = par 4 + 2 → ★★");
        }

        [Test]
        public void ArrowLimitLosesTheLevel()
        {
            var run = new LevelRun(WorldOne.Level(1), LevelBuilder.DefaultPlayer(), 1, false);
            Assert.AreEqual(10, run.Level.EffectiveArrowLimit);
            for (int i = 0; i < 10; i++) run.Shoot(TestArena.Miss());
            Assert.AreEqual(LevelOutcome.Lost, run.Outcome);
            Assert.AreEqual(0, run.Result().Stars);
        }

        [Test]
        public void HittingTheDummyFailsTheAppleShot()
        {
            var run = new LevelRun(WorldOne.Level(7), LevelBuilder.DefaultPlayer(), 1, false);
            run.Shoot(AimProp(run.Match, FirstOf(run.Match, PropKind.Dummy)));
            Assert.IsTrue(run.DummyHit);
            Assert.AreEqual(LevelOutcome.Lost, run.Outcome);
        }

        [Test]
        public void ThreeApplesWinTheAppleShot()
        {
            var run = new LevelRun(WorldOne.Level(7), LevelBuilder.DefaultPlayer(), 1, false);
            for (int i = 0; i < 3; i++) run.Shoot(AimProp(run.Match, FirstOf(run.Match, PropKind.Apple)));
            Assert.AreEqual(LevelOutcome.Won, run.Outcome);
            Assert.AreEqual(3, run.Result().Stars);
        }

        [Test]
        public void CuttingTheRopeWinsTheRescue()
        {
            var run = new LevelRun(WorldOne.Level(14), LevelBuilder.DefaultPlayer(), 1, false);
            run.Shoot(AimProp(run.Match, FirstOf(run.Match, PropKind.Rope)));
            Assert.IsTrue(run.RopeCut);
            Assert.AreEqual(LevelOutcome.Won, run.Outcome);
            Assert.AreEqual(MatchPhase.Over, run.Match.Phase);
            bool cutEvent = false;
            foreach (MatchEvent e in run.Match.Events) cutEvent |= e.Kind == MatchEventKind.RopeCut;
            Assert.IsTrue(cutEvent, "the winning shot keeps its events for the screen");
        }

        [Test]
        public void DuelStarsCountHpAndPar()
        {
            Assert.AreEqual(0, StarRules.ForDuel(false, 1.0, 1, 5, false));
            Assert.AreEqual(1, StarRules.ForDuel(true, 0.3, 9, 5, false));
            Assert.AreEqual(2, StarRules.ForDuel(true, 0.5, 9, 5, false));
            Assert.AreEqual(2, StarRules.ForDuel(true, 0.3, 5, 5, false));
            Assert.AreEqual(3, StarRules.ForDuel(true, 0.9, 4, 5, false));
            Assert.AreEqual(2, StarRules.ForDuel(true, 0.9, 4, 5, true), "no ★★★ with Assist");
        }

        [Test]
        public void ShotStarsCountArrowsAgainstPar()
        {
            Assert.AreEqual(3, StarRules.ForShots(true, 4, 4, false));
            Assert.AreEqual(2, StarRules.ForShots(true, 6, 4, false));
            Assert.AreEqual(1, StarRules.ForShots(true, 7, 4, false));
            Assert.AreEqual(0, StarRules.ForShots(false, 3, 4, false));
            Assert.AreEqual(2, StarRules.ForShots(true, 3, 4, true));
        }

        [Test]
        public void LossHelpComesAfterTwoAndThreeLosses()
        {
            Assert.IsFalse(LossHelp.ShowTip(1));
            Assert.IsTrue(LossHelp.ShowTip(2));
            Assert.IsFalse(LossHelp.OfferAssist(2));
            Assert.IsTrue(LossHelp.OfferAssist(3));
            Assert.AreEqual(0.15, LossHelp.AssistPreviewBonus, 1e-12);
        }

        [Test]
        public void DuelLevelEndsWithTheMatch()
        {
            var run = new LevelRun(WorldOne.Level(2), LevelBuilder.DefaultPlayer(), 1, false);
            run.Match.GetFighter(1).Hp = 10;
            run.Shoot(TestArena.Aim(run.Match, HitZone.Body));
            Assert.AreEqual(LevelOutcome.Won, run.Outcome);
            LevelResult r = run.Result();
            Assert.AreEqual(3, r.Stars);
            Assert.AreEqual(1, r.PlayerTurns);
            Assert.IsTrue(r.TookNoDamage);
        }

        [Test]
        public void GauntletCarriesHpToTheNextArcher()
        {
            var run = new LevelRun(WorldOne.Level(13), LevelBuilder.DefaultPlayer(), 1, false);
            run.Match.GetFighter(0).Hp = 70;
            run.Match.GetFighter(1).Hp = 10;
            run.Shoot(TestArena.Aim(run.Match, HitZone.Body));
            Assert.AreEqual(LevelOutcome.Playing, run.Outcome);
            Assert.AreEqual(2, run.Match.ActiveFighter(1));
            Assert.AreEqual(70, run.Match.GetFighter(0).Hp);
        }

        [Test]
        public void ForcedTipsReplaceTheLoadout()
        {
            FighterSpec p = LevelBuilder.DefaultPlayer();
            p.Tips = new[] { ArrowTip.Fire, ArrowTip.Ice };
            var run = new LevelRun(WorldOne.Level(11), p, 1, false);
            Assert.AreEqual(2, run.Match.GetFighter(0).Ammo[(int)ArrowTip.Bomb]);
            Assert.AreEqual(0, run.Match.GetFighter(0).Ammo[(int)ArrowTip.Fire]);
        }

        [Test]
        public void ComputerOpponentsPlayTheirTurns()
        {
            var run = new LevelRun(WorldOne.Level(5), LevelBuilder.DefaultPlayer(), 3, false);
            Assert.IsNull(run.CurrentAi);
            run.Shoot(TestArena.Miss());
            Assert.IsNotNull(run.CurrentAi);
            AiDecision d = run.DecideAi();
            Assert.That(d.ThinkSeconds, Is.InRange(0.8, 1.2));
            Assert.IsTrue(run.Shoot(d.Input).Accepted);
            Assert.IsTrue(run.IsPlayerTurn);
        }
    }
}
