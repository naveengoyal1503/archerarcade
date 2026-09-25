using ArcherArcade.Logic;
using NUnit.Framework;

namespace ArcherArcade.Tests
{
    public class AbilityTests
    {
        static bool Has(MatchState m, MatchEventKind kind)
        {
            foreach (MatchEvent e in m.Events)
            {
                if (e.Kind == kind) return true;
            }
            return false;
        }

        static int Count(MatchState m, MatchEventKind kind)
        {
            int n = 0;
            foreach (MatchEvent e in m.Events)
            {
                if (e.Kind == kind) n++;
            }
            return n;
        }

        /// <summary>Plays misses on both sides until the player's ability is ready.</summary>
        static void ChargePlayer(MatchState m)
        {
            while (!m.GetFighter(0).AbilityReady)
            {
                Assert.AreEqual(0, m.CurrentSide);
                m.ApplyShot(TestArena.Miss());
                m.ApplyShot(TestArena.Miss());
            }
            Assert.AreEqual(0, m.CurrentSide);
        }

        [Test]
        public void AbilityChargesAfterThreeOwnTurns()
        {
            var m = new MatchState(TestArena.Duel(20, WindRange.Calm));
            for (int turn = 1; turn <= 3; turn++)
            {
                Assert.IsFalse(m.GetFighter(0).AbilityReady, "turn " + turn);
                Assert.AreEqual(ShotRejectReason.AbilityUnavailable, m.ApplyShot(new ShotInput(30, 0.5, ArrowTip.Normal, true)).Reason);
                m.ApplyShot(TestArena.Miss());
                if (turn == 3) Assert.IsTrue(Has(m, MatchEventKind.AbilityReady));
                m.ApplyShot(TestArena.Miss());
            }
            Assert.IsTrue(m.GetFighter(0).AbilityReady);
            Assert.IsTrue(m.ApplyShot(new ShotInput(80, 0, ArrowTip.Normal, true)).Accepted);
            Assert.IsTrue(Has(m, MatchEventKind.AbilityUsed));
            Assert.AreEqual(0, m.GetFighter(0).AbilityCharge, "the turn it was used in does not count");
            Assert.IsFalse(m.GetFighter(0).AbilityReady, "used: charging again");
            m.ApplyShot(TestArena.Miss()); // enemy
            for (int turn = 1; turn <= 3; turn++)
            {
                m.ApplyShot(TestArena.Miss());
                Assert.AreEqual(turn == 3, m.GetFighter(0).AbilityReady, "second cycle, turn " + turn);
                m.ApplyShot(TestArena.Miss());
            }
        }

        [Test]
        public void HeadshotChargesItInTwoTurns()
        {
            var m = new MatchState(TestArena.Duel(20, WindRange.Calm));
            m.GetFighter(1).Hp = 1000;
            m.ApplyShot(TestArena.Aim(m, HitZone.Head));
            m.ApplyShot(TestArena.Miss());
            Assert.IsFalse(m.GetFighter(0).AbilityReady);
            m.ApplyShot(TestArena.Miss());
            Assert.IsTrue(m.GetFighter(0).AbilityReady, "ready after 2 turns because of the headshot");
        }

        [Test]
        public void TimeoutsCountAsTurnsForTheCharge()
        {
            var m = new MatchState(TestArena.Duel(20, WindRange.Calm));
            for (int i = 0; i < 3; i++)
            {
                m.Tick(13);
                m.Tick(13);
            }
            Assert.IsTrue(m.GetFighter(0).AbilityReady);
        }

        [Test]
        public void TripleShotFansThreeArrowsAtSixtyPercent()
        {
            var m = new MatchState(TestArena.Duel(20, WindRange.Calm, playerTips: new[] { ArrowTip.Fire }));
            ChargePlayer(m);
            ShotResult r = m.ApplyShot(TestArena.Aim(m, HitZone.Body, ArrowTip.Fire, true));
            Assert.AreEqual(3, r.Arrows.Count);
            for (int i = 0; i < 3; i++) Assert.AreEqual(-1, r.Arrows[i].Parent);
            Assert.AreEqual(4.0, System.Math.Abs(VelocityAngle(r.Arrows[1]) - VelocityAngle(r.Arrows[0])), 1e-9);
            Assert.AreEqual(2, m.GetFighter(0).Ammo[(int)ArrowTip.Fire], "one ammo for the whole fan");
            foreach (MatchEvent e in m.Events)
            {
                if (e.Kind == MatchEventKind.Hit) Assert.AreEqual(13, e.Amount, "22 × 0.6 = 13.2");
            }
        }

        static double VelocityAngle(ArrowPath p) => System.Math.Atan2(p.StartVelocity.Y, p.StartVelocity.X) * 180.0 / System.Math.PI;

        [Test]
        public void MeteorArrowHitsForFortyAndBurns()
        {
            var m = new MatchState(TestArena.Duel(20, WindRange.Calm, player: ArcherTable.FireArcher()));
            ChargePlayer(m);
            m.ApplyShot(TestArena.Aim(m, HitZone.Body, ArrowTip.Normal, true));
            Assert.IsTrue(Has(m, MatchEventKind.BurnApplied));
            Assert.AreEqual(100 - 40 - 6, m.GetFighter(1).Hp, "40 on hit, first burn tick when the enemy's turn starts");
        }

        [Test]
        public void MeteorSplashesOnANearMiss()
        {
            var m = new MatchState(TestArena.Duel(20, WindRange.Calm, player: ArcherTable.FireArcher()));
            ChargePlayer(m);
            ShotInput aim = TestArena.AimAtPoint(m, new Vec2(19.0, 0.01), ArrowTip.Normal, true);
            aim.UseAbility = true;
            m.ApplyShot(aim);
            Assert.IsTrue(Has(m, MatchEventKind.SplashHit));
            Assert.AreEqual(100 - 12, m.GetFighter(1).Hp);
        }

        [Test]
        public void StormBoltIgnoresWindAndFliesFaster()
        {
            ShotConfig cfg = new ShotConfig();
            MatchSetup calm = TestArena.Duel(20, WindRange.Calm, player: ArcherTable.ElectricArcher());
            MatchSetup windy = TestArena.Duel(20, WindRange.Fixed(5), player: ArcherTable.ElectricArcher());
            var a = new MatchState(calm);
            var b = new MatchState(windy);
            ChargePlayer(a);
            ChargePlayer(b);
            var shot = new ShotInput(20, 0.5, ArrowTip.Normal, true);
            ShotResult ra = a.ApplyShot(shot);
            Vec2 endA = ra.Arrows[0].EndPosition;
            Vec2 v0 = ra.Arrows[0].StartVelocity;
            ShotResult rb = b.ApplyShot(shot);
            Assert.AreNotEqual(0, rb.Wind);
            Assert.AreEqual(endA.X, rb.Arrows[0].EndPosition.X, 1e-12);
            Assert.AreEqual(endA.Y, rb.Arrows[0].EndPosition.Y, 1e-12);
            Assert.AreEqual(Ballistics.SpeedFromPower(0.5, cfg) * 1.25, v0.Length, 1e-9);
        }

        [Test]
        public void StormBoltHitsForThirtyFiveAndChainsTwice()
        {
            MatchSetup s = TestArena.Duel(20, WindRange.Calm, player: ArcherTable.ElectricArcher());
            s.Props.Add(PropSpec.Of(PropKind.Crate, Shape.Box(new Vec2(21.8, 0.45), new Vec2(0.45, 0.45))));
            s.Props.Add(PropSpec.Of(PropKind.Crate, Shape.Box(new Vec2(22.5, 2.6), new Vec2(0.45, 0.45))));
            s.Arena.Grounds[1] = Shape.BoxFromTop(20, 0, 8, 2);
            var m = new MatchState(s);
            ChargePlayer(m);
            m.ApplyShot(TestArena.Aim(m, HitZone.Body, ArrowTip.Normal, true));
            Assert.AreEqual(100 - 35, m.GetFighter(1).Hp);
            Assert.AreEqual(2, Count(m, MatchEventKind.ChainHit), "archer → crate → crate");
            Assert.AreEqual(1, m.GetProp(0).HitsLeft);
            Assert.AreEqual(1, m.GetProp(1).HitsLeft);
        }

        [Test]
        public void ClusterBombSplitsIntoThreeBomblets()
        {
            var m = new MatchState(TestArena.Duel(24, WindRange.Calm, player: ArcherTable.BombArcher()));
            ChargePlayer(m);
            ShotResult r = m.ApplyShot(TestArena.Aim(m, HitZone.Body, ArrowTip.Normal, true));
            Assert.AreEqual(3, r.Arrows.Count);
            Assert.AreEqual(0, r.Arrows[1].Parent);
            Assert.AreEqual(0, r.Arrows[2].Parent);
            MatchEvent hit = default;
            foreach (MatchEvent e in m.Events)
            {
                if (e.Kind == MatchEventKind.Hit) hit = e;
            }
            Assert.AreEqual(18, hit.Amount, "bomblets carry their own 18, no archer scaling");
        }

        [Test]
        public void AbilityShotsDoNotUseAmmo()
        {
            var m = new MatchState(TestArena.Duel(20, WindRange.Calm, player: ArcherTable.FireArcher()));
            ChargePlayer(m);
            Assert.IsTrue(m.ApplyShot(new ShotInput(80, 0, ArrowTip.Bomb, true)).Accepted, "Meteor replaces the tip");
        }

        [Test]
        public void UpgradeLevelScalesAbilityDamage()
        {
            MatchSetup s = TestArena.Duel(20, WindRange.Calm, player: ArcherTable.FireArcher());
            s.Fighters[0].Level = 6; // +20 %
            var m = new MatchState(s);
            ChargePlayer(m);
            m.ApplyShot(TestArena.Aim(m, HitZone.Body, ArrowTip.Normal, true));
            Assert.AreEqual(100 - 48 - 6, m.GetFighter(1).Hp);
        }

        [Test]
        public void HardAiUsesItsAbilityWhenChargedAndEasyNever()
        {
            MatchSetup s = TestArena.Duel(20, WindRange.Calm, 3, ArcherTable.Ranger(), ArcherTable.FireArcher());
            var m = new MatchState(s);
            var hard = new AiPlayer(AiProfile.Hard(), 1);
            var easy = new AiPlayer(AiProfile.Easy(), 1);
            m.GetFighter(0).Hp = 5000;
            for (int i = 0; i < 3; i++)
            {
                m.ApplyShot(TestArena.Miss());
                Assert.IsFalse(hard.Decide(m).Input.UseAbility);
                m.ApplyShot(TestArena.Miss());
            }
            m.ApplyShot(TestArena.Miss());
            Assert.IsTrue(m.GetFighter(1).AbilityReady);
            AiDecision d = hard.Decide(m);
            Assert.IsTrue(d.Input.UseAbility);
            Assert.IsFalse(easy.Decide(m).Input.UseAbility);
            ShotResult r = m.ApplyShot(d.PerfectInput);
            Assert.IsTrue(r.Accepted);
            Assert.AreEqual(ContactKind.Fighter, r.Arrows[0].Contact, "solver aims the Meteor exactly");
        }
    }
}
