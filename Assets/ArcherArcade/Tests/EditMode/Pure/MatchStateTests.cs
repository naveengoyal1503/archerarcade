using System.Collections.Generic;
using ArcherArcade.Logic;
using NUnit.Framework;

namespace ArcherArcade.Tests
{
    public class MatchStateTests
    {
        static bool HasEvent(MatchState m, MatchEventKind kind, int fighter = -2)
        {
            foreach (MatchEvent e in m.Events)
            {
                if (e.Kind == kind && (fighter == -2 || e.Fighter == fighter)) return true;
            }
            return false;
        }

        static MatchEvent FindEvent(MatchState m, MatchEventKind kind)
        {
            foreach (MatchEvent e in m.Events)
            {
                if (e.Kind == kind) return e;
            }
            Assert.Fail("missing event " + kind);
            return default;
        }

        [Test]
        public void CampaignStartsWithPlayerTurn()
        {
            var m = new MatchState(TestArena.Duel(20, WindRange.Calm));
            Assert.AreEqual(MatchPhase.Aiming, m.Phase);
            Assert.AreEqual(0, m.CurrentSide);
            Assert.AreEqual(1, m.TurnNumber);
            Assert.AreEqual(12.0, m.TurnTimeLeft, 1e-12);
            Assert.IsTrue(HasEvent(m, MatchEventKind.TurnStarted, 0));
        }

        [Test]
        public void CoinFlipGivesBothSidesTheFirstTurn()
        {
            bool zero = false, one = false;
            for (ulong seed = 1; seed <= 40; seed++)
            {
                MatchSetup s = TestArena.Duel(20, WindRange.Calm, seed);
                s.FirstTurn = FirstTurnRule.CoinFlip;
                var m = new MatchState(s);
                zero |= m.CurrentSide == 0;
                one |= m.CurrentSide == 1;
            }
            Assert.IsTrue(zero && one);
        }

        [TestCase(HitZone.Head, 50)]
        [TestCase(HitZone.Body, 25)]
        [TestCase(HitZone.Legs, 15)]
        public void ZoneHitsDealZoneDamage(HitZone zone, int damage)
        {
            var m = new MatchState(TestArena.Duel(20, WindRange.Calm));
            ShotResult r = m.ApplyShot(TestArena.Aim(m, zone));
            Assert.IsTrue(r.Accepted);
            Assert.AreEqual(ContactKind.Fighter, r.Arrows[0].Contact);
            Assert.AreEqual(zone, r.Arrows[0].Zone);
            Assert.AreEqual(100 - damage, m.GetFighter(1).Hp);
            MatchEvent hit = FindEvent(m, MatchEventKind.Hit);
            Assert.AreEqual(damage, hit.Amount);
            Assert.AreEqual(zone, hit.Zone);
            Assert.AreEqual(r.Arrows[0].EndTime, hit.Time, 1e-12);
            Assert.AreEqual(1, m.CurrentSide, "turn passes after the shot");
        }

        [Test]
        public void TwoHeadshotsKnockOutAndEndTheMatch()
        {
            var m = new MatchState(TestArena.Duel(20, WindRange.Calm));
            m.ApplyShot(TestArena.Aim(m, HitZone.Head));
            m.ApplyShot(TestArena.Miss());
            m.ApplyShot(TestArena.Aim(m, HitZone.Head));
            Assert.AreEqual(0, m.GetFighter(1).Hp);
            Assert.AreEqual(MatchPhase.Over, m.Phase);
            Assert.AreEqual(0, m.Winner);
            Assert.IsTrue(HasEvent(m, MatchEventKind.Knockout, 1));
            Assert.IsTrue(HasEvent(m, MatchEventKind.MatchOver));
            Assert.AreEqual(ShotRejectReason.MatchOver, m.ApplyShot(TestArena.Miss()).Reason);
            Assert.IsFalse(m.Tick(20));
        }

        [Test]
        public void TimeoutPassesTheTurnWithoutAShot()
        {
            var m = new MatchState(TestArena.Duel(20, WindRange.Calm));
            Assert.IsFalse(m.Tick(11.9));
            Assert.AreEqual(0, m.CurrentSide);
            Assert.IsTrue(m.Tick(0.2));
            Assert.IsTrue(HasEvent(m, MatchEventKind.TurnTimedOut, 0));
            Assert.AreEqual(1, m.CurrentSide);
            Assert.AreEqual(12.0, m.TurnTimeLeft, 1e-12);
            Assert.AreEqual(100, m.GetFighter(1).Hp);
        }

        [Test]
        public void FireTipBurnsForTwoTurnsAtTargetTurnStart()
        {
            var m = new MatchState(TestArena.Duel(20, WindRange.Calm, playerTips: new[] { ArrowTip.Fire }));
            m.ApplyShot(TestArena.Aim(m, HitZone.Body, ArrowTip.Fire));
            Assert.IsTrue(HasEvent(m, MatchEventKind.BurnApplied, 1));
            Assert.IsTrue(HasEvent(m, MatchEventKind.BurnDamage, 1), "burn ticks when the enemy's turn starts");
            Assert.AreEqual(100 - 22 - 6, m.GetFighter(1).Hp);
            m.ApplyShot(TestArena.Miss()); // enemy
            m.ApplyShot(TestArena.Miss()); // player → enemy turn starts, second burn
            Assert.AreEqual(100 - 22 - 12, m.GetFighter(1).Hp);
            m.ApplyShot(TestArena.Miss());
            m.ApplyShot(TestArena.Miss());
            Assert.AreEqual(100 - 22 - 12, m.GetFighter(1).Hp, "burn is over after 2 turns");
            Assert.IsFalse(m.GetFighter(1).Status.IsBurning);
        }

        [Test]
        public void FireArcherPassiveBurnsWithNormalArrows()
        {
            var m = new MatchState(TestArena.Duel(20, WindRange.Calm, player: ArcherTable.FireArcher()));
            m.ApplyShot(TestArena.Aim(m, HitZone.Body));
            Assert.AreEqual(100 - 25 - 6, m.GetFighter(1).Hp);
        }

        [Test]
        public void PoisonTipDamagesOverTwoTurns()
        {
            var m = new MatchState(TestArena.Duel(20, WindRange.Calm, playerTips: new[] { ArrowTip.Poison }));
            m.ApplyShot(TestArena.Aim(m, HitZone.Body, ArrowTip.Poison));
            Assert.AreEqual(100 - 10 - 8, m.GetFighter(1).Hp);
            m.ApplyShot(TestArena.Miss());
            m.ApplyShot(TestArena.Miss());
            Assert.AreEqual(100 - 10 - 16, m.GetFighter(1).Hp);
        }

        [Test]
        public void ElectricTipStunsTheNextTimer()
        {
            var m = new MatchState(TestArena.Duel(20, WindRange.Calm, playerTips: new[] { ArrowTip.Electric }));
            m.ApplyShot(TestArena.Aim(m, HitZone.Body, ArrowTip.Electric));
            Assert.AreEqual(100 - 20, m.GetFighter(1).Hp);
            Assert.AreEqual(8.0, m.TurnTimeLeft, 1e-12);
            m.ApplyShot(TestArena.Miss());
            m.ApplyShot(TestArena.Miss());
            Assert.AreEqual(12.0, m.TurnTimeLeft, 1e-12, "stun lasts one turn");
        }

        [Test]
        public void IceTipSlowsOnlyTheNextDraw()
        {
            var m = new MatchState(TestArena.Duel(20, WindRange.Calm, playerTips: new[] { ArrowTip.Ice }));
            m.ApplyShot(TestArena.Aim(m, HitZone.Body, ArrowTip.Ice));
            Assert.AreEqual(0.3, m.GetFighter(1).Status.ActiveDrawSlow, 1e-12);
            m.ApplyShot(TestArena.Miss());
            Assert.AreEqual(0.0, m.GetFighter(1).Status.ActiveDrawSlow, 1e-12);
            m.ApplyShot(TestArena.Miss());
            Assert.AreEqual(0.0, m.GetFighter(1).Status.ActiveDrawSlow, 1e-12);
        }

        [Test]
        public void AmmoRunsOutAndUnpickedTipsAreRejected()
        {
            var m = new MatchState(TestArena.Duel(20, WindRange.Calm, playerTips: new[] { ArrowTip.Fire }));
            Assert.AreEqual(ShotRejectReason.NoAmmo, m.ApplyShot(new ShotInput(30, 0.5, ArrowTip.Bomb)).Reason);
            Assert.AreEqual(0, m.CurrentSide, "a rejected shot does not use the turn");
            for (int i = 0; i < 3; i++)
            {
                Assert.IsTrue(m.ApplyShot(new ShotInput(80, 0, ArrowTip.Fire)).Accepted);
                m.ApplyShot(TestArena.Miss());
            }
            Assert.AreEqual(ShotRejectReason.NoAmmo, m.ApplyShot(new ShotInput(80, 0, ArrowTip.Fire)).Reason);
            Assert.IsTrue(m.ApplyShot(TestArena.Miss()).Accepted, "Normal arrows never run out");
        }

        [Test]
        public void AbilitiesAndBadInputAreRejected()
        {
            var m = new MatchState(TestArena.Duel(20, WindRange.Calm));
            Assert.AreEqual(ShotRejectReason.AbilityUnavailable, m.ApplyShot(new ShotInput(30, 0.5, ArrowTip.Normal, true)).Reason);
            Assert.AreEqual(ShotRejectReason.InvalidInput, m.ApplyShot(new ShotInput(double.NaN, 0.5)).Reason);
            Assert.AreEqual(0, m.CurrentSide);
        }

        [Test]
        public void SplitArrowBecomesThreeAtTheApex()
        {
            var m = new MatchState(TestArena.Duel(24, WindRange.Calm, playerTips: new[] { ArrowTip.Split }));
            ShotResult r = m.ApplyShot(TestArena.Aim(m, HitZone.Body, ArrowTip.Split));
            Assert.AreEqual(3, r.Arrows.Count);
            Assert.AreEqual(-1, r.Arrows[0].Parent);
            Assert.AreEqual(0, r.Arrows[1].Parent);
            Assert.AreEqual(0, r.Arrows[2].Parent);
            Assert.AreEqual(r.Arrows[1].StartTime, r.Arrows[2].StartTime, 1e-12);
            Assert.Greater(r.Arrows[1].StartTime, 0.0);
            Assert.AreEqual(ContactKind.Fighter, r.Arrows[0].Contact, "the middle arrow keeps the aimed line");
            Assert.LessOrEqual(m.GetFighter(1).Hp, 100 - 12);
        }

        [Test]
        public void BombNearMissSplashes()
        {
            var m = new MatchState(TestArena.Duel(20, WindRange.Calm, playerTips: new[] { ArrowTip.Bomb }));
            Fighter me = m.CurrentFighter;
            var req = AimRequest.Create(me.BowPosition(m.Setup.Shot), 1, new Vec2(19.2, 0.01), 0);
            req.PreferHighArc = true;
            AimSolution sol;
            Assert.IsTrue(AimSolver.Solve(req, m.Setup.Shot, out sol));
            ShotResult r = m.ApplyShot(sol.ToInput(ArrowTip.Bomb));
            Assert.AreEqual(ContactKind.Ground, r.Arrows[0].Contact);
            Assert.IsTrue(HasEvent(m, MatchEventKind.Miss));
            Assert.IsTrue(HasEvent(m, MatchEventKind.SplashHit, 1));
            Assert.AreEqual(100 - 12, m.GetFighter(1).Hp);
        }

        [Test]
        public void WallBlocksAFlatShot()
        {
            MatchSetup s = TestArena.Duel(20, WindRange.Calm);
            s.Arena.Walls.Add(Shape.BoxFromTop(10, 4, 0.6, 8));
            var m = new MatchState(s);
            ShotResult r = m.ApplyShot(new ShotInput(5, 1));
            Assert.AreEqual(ContactKind.Wall, r.Arrows[0].Contact);
            Assert.AreEqual(9.7, r.Arrows[0].EndPosition.X, 1e-9);
            Assert.AreEqual(100, m.GetFighter(1).Hp);
        }

        [Test]
        public void BurnCanKnockOutAtTurnStart()
        {
            var m = new MatchState(TestArena.Duel(20, WindRange.Calm, playerTips: new[] { ArrowTip.Fire }));
            m.GetFighter(1).Hp = 25;
            m.ApplyShot(TestArena.Aim(m, HitZone.Body, ArrowTip.Fire));
            Assert.AreEqual(MatchPhase.Over, m.Phase);
            Assert.AreEqual(0, m.Winner);
            Assert.IsTrue(HasEvent(m, MatchEventKind.BurnDamage, 1));
            Assert.IsTrue(HasEvent(m, MatchEventKind.Knockout, 1));
        }

        [Test]
        public void GauntletBringsInTheNextArcher()
        {
            MatchSetup s = TestArena.Duel(20, WindRange.Calm);
            s.Fighters[1].Def = ArcherTable.Basic("twig1", 50);
            s.Fighters.Add(new FighterSpec { Def = ArcherTable.Basic("twig2", 50), Side = 1, Feet = new Vec2(24, 0), Facing = -1 });
            s.Arena.Grounds.Add(Shape.BoxFromTop(24, 0, 6, 2));
            var m = new MatchState(s);
            m.ApplyShot(TestArena.Aim(m, HitZone.Head));
            Assert.AreEqual(0, m.GetFighter(1).Hp);
            Assert.AreEqual(MatchPhase.Aiming, m.Phase);
            Assert.IsTrue(HasEvent(m, MatchEventKind.FighterEntered, 2));
            Assert.AreEqual(2, m.ActiveFighter(1));
            Assert.AreEqual(2, m.CurrentFighter.Index);
            m.ApplyShot(TestArena.Miss());
            m.ApplyShot(TestArena.Aim(m, HitZone.Head));
            Assert.AreEqual(MatchPhase.Over, m.Phase);
            Assert.AreEqual(0, m.Winner);
        }

        static List<ulong> PlayScripted(ulong seed, int turns)
        {
            var setup = TestArena.Duel(26, new WindRange(0, 5), seed, ArcherTable.FireArcher(), ArcherTable.ElectricArcher(),
                new[] { ArrowTip.Fire, ArrowTip.Split, ArrowTip.Bomb });
            setup.Fighters[1].Tips = new[] { ArrowTip.Electric, ArrowTip.Ice, ArrowTip.Poison };
            setup.FirstTurn = FirstTurnRule.CoinFlip;
            var m = new MatchState(setup);
            var inputs = new Rng(seed ^ 0xABCDEFUL);
            var hashes = new List<ulong> { m.ComputeHash() };
            ArrowTip[] tips = { ArrowTip.Normal, ArrowTip.Fire, ArrowTip.Split, ArrowTip.Bomb, ArrowTip.Electric, ArrowTip.Ice, ArrowTip.Poison };
            for (int i = 0; i < turns && m.Phase == MatchPhase.Aiming; i++)
            {
                if (inputs.Chance(0.1))
                {
                    m.Tick(13);
                }
                else
                {
                    var input = new ShotInput(inputs.Range(10, 60), inputs.Range(0.3, 1.0), tips[inputs.NextInt(tips.Length)]);
                    if (!m.ApplyShot(input).Accepted) m.ApplyShot(new ShotInput(input.AngleDeg, input.Power));
                }
                hashes.Add(m.ComputeHash());
            }
            hashes.Add((ulong)(m.Winner + 1));
            return hashes;
        }

        [Test]
        public void SameSeedAndInputsGiveTheSameMatch()
        {
            for (ulong seed = 1; seed <= 5; seed++)
            {
                CollectionAssert.AreEqual(PlayScripted(seed, 60), PlayScripted(seed, 60), "seed " + seed);
            }
            CollectionAssert.AreNotEqual(PlayScripted(1, 60), PlayScripted(2, 60));
        }
    }
}
