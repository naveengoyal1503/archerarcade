using ArcherArcade.Logic;
using ArcherArcade.Logic.Campaign;
using NUnit.Framework;

namespace ArcherArcade.Tests
{
    public class BossTests
    {
        static bool Has(MatchState m, MatchEventKind kind)
        {
            foreach (MatchEvent e in m.Events)
            {
                if (e.Kind == kind) return true;
            }
            return false;
        }

        static MatchState BossMatch(ulong seed = 1)
        {
            return new MatchState(LevelBuilder.Build(WorldOne.Level(20), LevelBuilder.DefaultPlayer(), seed));
        }

        static int ShieldSlot(MatchState m, int slot)
        {
            for (int i = 0; i < m.PropCount; i++)
            {
                if (m.GetProp(i).Spec.RotatingSlot == slot) return i;
            }
            return -1;
        }

        [Test]
        public void WardenIsBigWithTwoRotatingShieldsAndAVineWall()
        {
            MatchState m = BossMatch();
            Fighter w = m.GetFighter(1);
            Assert.AreEqual(250, w.MaxHp);
            Assert.AreEqual(1.35, w.Def.BodyScale, 1e-12);
            Assert.GreaterOrEqual(ShieldSlot(m, 0), 0);
            Assert.GreaterOrEqual(ShieldSlot(m, 1), 0);
            int vine = -1;
            for (int i = 0; i < m.PropCount; i++)
            {
                if (m.GetProp(i).Kind == PropKind.VineWall) vine = i;
            }
            Assert.GreaterOrEqual(vine, 0);
            Assert.IsFalse(m.PropPresent(vine), "the vine wall starts hidden");
        }

        [Test]
        public void ShieldsRotateThroughThreeOpenings()
        {
            MatchState m = BossMatch();
            PropConfig pc = m.Setup.PropRules;
            double s = 1.35;
            double feetY = m.GetFighter(1).Feet.Y;
            double[][] expected =
            {
                new[] { pc.BossShieldHeadY, pc.BossShieldBodyY },
                new[] { pc.BossShieldHeadY, pc.BossShieldLegsY },
                new[] { pc.BossShieldBodyY, pc.BossShieldLegsY }
            };
            // Turn 1 → pattern 0, turn 2 → 1, turn 3 → 2.
            int[] patternOfTurn = { 0, 1, 2 };
            for (int turn = 0; turn < 3; turn++)
            {
                int p = patternOfTurn[turn];
                double a = m.PropRestShape(ShieldSlot(m, 0)).A.Y - feetY;
                double b = m.PropRestShape(ShieldSlot(m, 1)).A.Y - feetY;
                double wantA = p == 1 ? pc.BossShieldBodyY : pc.BossShieldHeadY;
                double wantB = p == 0 ? pc.BossShieldBodyY : pc.BossShieldLegsY;
                Assert.AreEqual(wantA * s, a, 1e-9, "turn " + (turn + 1));
                Assert.AreEqual(wantB * s, b, 1e-9, "turn " + (turn + 1));
                m.ApplyShot(TestArena.Miss());
            }
            Assert.IsNotNull(expected);
        }

        [Test]
        public void WeakSpotDealsTwoAndAHalfTimes()
        {
            MatchState m = BossMatch();
            m.ApplyShot(TestArena.Miss()); // turn 2: warden
            m.ApplyShot(TestArena.Miss()); // turn 3: pattern 2 → head + legs covered, body and knot open
            Fighter w = m.GetFighter(1);
            ShotResult r = m.ApplyShot(TestArena.Aim(m, HitZone.WeakSpot));
            Assert.AreEqual(HitZone.WeakSpot, r.Arrows[0].Zone);
            Assert.AreEqual(250 - 63, w.Hp, "25 × 2.5 = 62.5 → 63");
        }

        [Test]
        public void VineWallGrowsInFrontOfThePlayerEveryThirdWardenTurn()
        {
            MatchState m = BossMatch();
            int vine = -1;
            for (int i = 0; i < m.PropCount; i++)
            {
                if (m.GetProp(i).Kind == PropKind.VineWall) vine = i;
            }
            for (int round = 0; round < 2; round++)
            {
                m.ApplyShot(TestArena.Miss());
                m.ApplyShot(TestArena.Miss());
            }
            Assert.IsFalse(m.PropPresent(vine));
            m.ApplyShot(TestArena.Miss()); // warden's 3rd turn starts
            Assert.IsTrue(Has(m, MatchEventKind.VineWallGrown));
            Assert.IsTrue(m.PropPresent(vine));
            Shape wall = m.PropRestShape(vine);
            Assert.AreEqual(3.5, wall.A.X, 1e-9, "3.5 m in front of the player");
            Assert.AreEqual(2.6, wall.A.Y + wall.HalfSize.Y, 1e-9);
        }

        [Test]
        public void VineWallTakesTwoHitsOrOneFireArrow()
        {
            MatchSetup s = LevelBuilder.Build(WorldOne.Level(20), LevelBuilder.DefaultPlayer(), 1);
            s.Fighters[0].Tips = new[] { ArrowTip.Fire };
            var m = new MatchState(s);
            int vine = -1;
            for (int i = 0; i < m.PropCount; i++)
            {
                if (m.GetProp(i).Kind == PropKind.VineWall) vine = i;
            }
            for (int i = 0; i < 6; i++) m.ApplyShot(TestArena.Miss()); // the vine grows as the warden's 3rd turn starts
            Assert.IsTrue(m.PropPresent(vine));
            Assert.AreEqual(0, m.CurrentSide);
            m.ApplyShot(new ShotInput(5, 0.4));
            Assert.IsTrue(m.PropPresent(vine), "one normal hit");
            m.ApplyShot(TestArena.Miss());
            m.ApplyShot(new ShotInput(5, 0.4));
            Assert.IsFalse(m.PropPresent(vine), "second hit breaks it");
            Assert.IsTrue(Has(m, MatchEventKind.VineWallBroken));

            var fire = new MatchState(s);
            for (int i = 0; i < 6; i++) fire.ApplyShot(TestArena.Miss());
            fire.ApplyShot(new ShotInput(5, 0.4, ArrowTip.Fire));
            Assert.IsFalse(fire.PropPresent(vine), "fire burns it at once");
        }

        [Test]
        public void EnragedWardenShootsTwicePerTurn()
        {
            MatchState m = BossMatch();
            m.GetFighter(1).Hp = 75; // 30 %
            m.ApplyShot(TestArena.Miss());
            Assert.IsTrue(Has(m, MatchEventKind.Enraged));
            Assert.AreEqual(2, m.ShotsLeftThisTurn);
            m.ApplyShot(TestArena.Miss());
            Assert.AreEqual(1, m.CurrentSide, "second shot");
            m.ApplyShot(TestArena.Miss());
            Assert.AreEqual(0, m.CurrentSide);
        }

        [Test]
        public void RainOfLeavesFallsOnThePlayer()
        {
            MatchState m = BossMatch();
            m.GetFighter(1).AbilityCharge = 3;
            m.ApplyShot(TestArena.Miss());
            ShotResult r = m.ApplyShot(new ShotInput(0, 0, ArrowTip.Normal, true));
            Assert.IsTrue(r.Accepted);
            Assert.AreEqual(3, r.Arrows.Count);
            for (int i = 0; i < 3; i++)
            {
                Assert.AreEqual(14.0, r.Arrows[i].StartPosition.Y, 1e-9);
                Assert.AreEqual(0.0, r.Arrows[i].StartVelocity.X, 1e-12);
            }
            Assert.IsTrue(Has(m, MatchEventKind.RainOfLeaves));
            Assert.Less(m.GetFighter(0).Hp, 100, "the middle leaf lands on the player");
        }

        [Test]
        public void ThornVolleyFiresFiveArrowsAtFortyPercent()
        {
            MatchState m = new MatchState(LevelBuilder.Build(WorldOne.Level(10), LevelBuilder.DefaultPlayer(), 1));
            Assert.AreEqual(160, m.GetFighter(1).MaxHp);
            m.GetFighter(1).AbilityCharge = 3;
            m.ApplyShot(TestArena.Miss());
            ShotResult r = m.ApplyShot(new ShotInput(40, 0.9, ArrowTip.Normal, true));
            Assert.AreEqual(5, r.Arrows.Count);
            foreach (MatchEvent e in m.Events)
            {
                if (e.Kind == MatchEventKind.Hit) Assert.AreEqual(e.Zone == HitZone.Head ? 20 : e.Zone == HitZone.Legs ? 6 : 10, e.Amount);
            }
        }

        [Test]
        public void TntNearThornHurtsHimAndBlowsTheTower()
        {
            MatchState m = new MatchState(LevelBuilder.Build(WorldOne.Level(10), LevelBuilder.DefaultPlayer(), 1));
            int tnt = -1;
            for (int i = 0; i < m.PropCount; i++)
            {
                if (m.GetProp(i).Kind == PropKind.TntCrate) tnt = i;
            }
            m.ApplyShot(TestArena.AimAtProp(m, tnt, ArrowTip.Normal, true));
            Assert.IsTrue(Has(m, MatchEventKind.Explosion));
            Assert.IsTrue(Has(m, MatchEventKind.TowerToppled));
            Assert.AreEqual(160 - 35, m.GetFighter(1).Hp);
        }
    }
}
