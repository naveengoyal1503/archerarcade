using ArcherArcade.Logic;
using NUnit.Framework;

namespace ArcherArcade.Tests
{
    public class EnemyTests
    {
        static bool Has(MatchState m, MatchEventKind kind)
        {
            foreach (MatchEvent e in m.Events)
            {
                if (e.Kind == kind) return true;
            }
            return false;
        }

        [Test]
        public void CrossbowScoutShootsFlatterAndFaster()
        {
            var scout = new MatchState(TestArena.Duel(20, WindRange.Calm, player: EnemyTable.CrossbowScout()));
            var bandit = new MatchState(TestArena.Duel(20, WindRange.Calm, player: EnemyTable.BanditArcher()));
            var shot = new ShotInput(40, 0.3);
            ShotResult rs = scout.ApplyShot(shot);
            ShotResult rb = bandit.ApplyShot(shot);
            Assert.Greater(rs.Arrows[0].StartVelocity.Length, rb.Arrows[0].StartVelocity.Length * 1.19);
            Assert.Less(rs.Arrows[0].Acceleration.Y, 0.0);
            Assert.Greater(rs.Arrows[0].Acceleration.Y, rb.Arrows[0].Acceleration.Y, "less gravity");
            Assert.AreEqual(70, scout.GetFighter(0).MaxHp, "low HP");
        }

        [Test]
        public void AiAimsTheScoutsFastShotExactly()
        {
            MatchSetup s = TestArena.Duel(24, WindRange.Fixed(3), 5, enemy: EnemyTable.CrossbowScout());
            var m = new MatchState(s);
            m.ApplyShot(TestArena.Miss());
            AiDecision d = new AiPlayer(AiProfile.Hard(), 2).Decide(m);
            ShotResult r = m.ApplyShot(d.PerfectInput);
            Assert.AreEqual(ContactKind.Fighter, r.Arrows[0].Contact);
            Assert.AreEqual(0, r.Arrows[0].HitFighter);
        }

        [Test]
        public void TwinShooterGetsTwoShotsPerTurn()
        {
            var m = new MatchState(TestArena.Duel(20, WindRange.Calm, enemy: EnemyTable.TwinShooter()));
            m.ApplyShot(TestArena.Miss());
            Assert.AreEqual(1, m.CurrentSide);
            Assert.AreEqual(2, m.ShotsLeftThisTurn);
            m.ApplyShot(TestArena.Aim(m, HitZone.Body));
            Assert.IsTrue(Has(m, MatchEventKind.ExtraShot));
            Assert.AreEqual(1, m.CurrentSide, "still the twins' turn");
            Assert.AreEqual(12.0, m.TurnTimeLeft, 1e-12);
            m.ApplyShot(TestArena.Aim(m, HitZone.Body));
            Assert.AreEqual(0, m.CurrentSide);
            Assert.AreEqual(100 - 15 - 15, m.GetFighter(0).Hp, "two hits at lower damage");
        }

        [Test]
        public void HealerDruidHealsTenEverySecondTurn()
        {
            var m = new MatchState(TestArena.Duel(20, WindRange.Calm, enemy: EnemyTable.HealerDruid()));
            m.ApplyShot(TestArena.Aim(m, HitZone.Head)); // 90 → 40, druid turn 1: no heal
            Assert.AreEqual(40, m.GetFighter(1).Hp);
            m.ApplyShot(TestArena.Miss());
            m.ApplyShot(TestArena.Miss()); // druid turn 2: heal
            Assert.IsTrue(Has(m, MatchEventKind.Healed));
            Assert.AreEqual(50, m.GetFighter(1).Hp);
            m.ApplyShot(TestArena.Miss());
            m.ApplyShot(TestArena.Miss()); // turn 3: none
            Assert.AreEqual(50, m.GetFighter(1).Hp);
        }

        [Test]
        public void HealingNeverGoesAboveMaxHp()
        {
            var m = new MatchState(TestArena.Duel(20, WindRange.Calm, enemy: EnemyTable.HealerDruid()));
            m.ApplyShot(TestArena.Aim(m, HitZone.Legs)); // 90 → 75
            m.ApplyShot(TestArena.Miss());
            m.ApplyShot(TestArena.Miss());
            Assert.AreEqual(85, m.GetFighter(1).Hp);
            m.GetFighter(1).Hp = 88;
            for (int i = 0; i < 4; i++) m.ApplyShot(TestArena.Miss());
            Assert.AreEqual(90, m.GetFighter(1).Hp);
        }

        [Test]
        public void BrambleCastsABubbleThatEatsOneArrow()
        {
            var m = new MatchState(TestArena.Duel(20, WindRange.Calm, enemy: EnemyTable.RangerBramble()));
            for (int i = 0; i < 2; i++)
            {
                m.ApplyShot(TestArena.Miss());
                m.ApplyShot(TestArena.Miss());
            }
            m.ApplyShot(TestArena.Miss()); // Bramble's 3rd turn starts: bubble
            Assert.IsTrue(Has(m, MatchEventKind.BubbleCast));
            Assert.IsTrue(m.GetFighter(1).HasBubble);
            m.ApplyShot(TestArena.Miss());
            m.ApplyShot(TestArena.Aim(m, HitZone.Head));
            Assert.IsTrue(Has(m, MatchEventKind.BubbleAbsorbed));
            Assert.AreEqual(120, m.GetFighter(1).Hp);
            Assert.IsFalse(m.GetFighter(1).HasBubble);
            m.ApplyShot(TestArena.Miss());
            m.ApplyShot(TestArena.Aim(m, HitZone.Head));
            Assert.AreEqual(70, m.GetFighter(1).Hp, "bubble gone: the next arrow hits");
        }

        [Test]
        public void ElectricPopsTheBubbleAndStillHits()
        {
            MatchSetup s = TestArena.Duel(20, WindRange.Calm, playerTips: new[] { ArrowTip.Electric }, enemy: EnemyTable.RangerBramble());
            var m = new MatchState(s);
            m.GetFighter(1).HasBubble = true;
            m.ApplyShot(TestArena.Aim(m, HitZone.Body, ArrowTip.Electric));
            Assert.IsTrue(Has(m, MatchEventKind.BubblePopped));
            Assert.AreEqual(120 - 20, m.GetFighter(1).Hp);
        }

        [Test]
        public void ShieldBearerCarriesAShieldProp()
        {
            var m = new MatchState(TestArena.Duel(20, WindRange.Calm, enemy: EnemyTable.ShieldBearer()));
            Assert.AreEqual(1, m.PropCount);
            Assert.AreEqual(PropKind.Shield, m.GetProp(0).Kind);
            Assert.AreEqual(1, m.GetProp(0).Spec.ShieldOwner);
            Assert.Less(m.PropRestShape(0).A.X, 20.0, "held in front, toward the player");
            m.ApplyShot(TestArena.AimAtPoint(m, m.Setup.Body.ZoneCenter(HitZone.Body, m.GetFighter(1).Feet)));
            Assert.IsTrue(Has(m, MatchEventKind.ShieldBlocked));
            Assert.AreEqual(100, m.GetFighter(1).Hp);
        }

        [Test]
        public void EnemyTypesMatchTheDesign()
        {
            Assert.AreEqual(2, EnemyTable.TwinShooter().ShotsPerTurn);
            Assert.AreEqual(10, EnemyTable.HealerDruid().HealAmount);
            Assert.AreEqual(2, EnemyTable.HealerDruid().HealEveryTurns);
            Assert.AreEqual(3, EnemyTable.RangerBramble().BubbleEveryTurns);
            Assert.AreEqual(AbilityKind.TripleShot, EnemyTable.RangerBramble().Ability);
            Assert.IsTrue(EnemyTable.ShieldBearer().CarriesShield);
            Assert.Greater(EnemyTable.CrossbowScout().ShotSpeedScale, 1.0);
        }
    }
}
