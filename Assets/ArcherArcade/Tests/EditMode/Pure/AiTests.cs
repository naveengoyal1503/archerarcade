using ArcherArcade.Logic;
using NUnit.Framework;

namespace ArcherArcade.Tests
{
    public class AiTests
    {
        static MatchState AiDuel(ulong seed, double distance = 20, WindRange? wind = null)
        {
            MatchSetup s = TestArena.Duel(distance, wind ?? WindRange.Fixed(2), seed);
            return new MatchState(s);
        }

        static bool HitFoe(MatchState m, ShotResult r)
        {
            int side = m.GetFighter(r.Shooter).Side;
            foreach (ArrowPath a in r.Arrows)
            {
                if (a.Contact == ContactKind.Fighter && m.GetFighter(a.HitFighter).Side != side) return true;
            }
            return false;
        }

        // GAME_DESIGN §6.3: first-shot hit rate on a 20 m target in wind 2, 1,000 duels per profile.
        [TestCase("easy", 15.0, 30.0)]
        [TestCase("medium", 30.0, 50.0)]
        [TestCase("hard", 55.0, 75.0)]
        [TestCase("boss", 65.0, 85.0)]
        public void FirstShotHitRateIsFair(string id, double min, double max)
        {
            int hits = 0;
            const int duels = 1000;
            for (int i = 0; i < duels; i++)
            {
                MatchState m = AiDuel((ulong)(i + 1));
                var ai = new AiPlayer(Profile(id), m.DeriveSeed(0));
                if (HitFoe(m, m.ApplyShot(ai.Decide(m).Input))) hits++;
            }
            double rate = hits * 100.0 / duels;
            Assert.That(rate, Is.InRange(min, max), id + " first-shot hit rate " + rate + " %");
        }

        static AiProfile Profile(string id)
        {
            switch (id)
            {
                case "easy": return AiProfile.Easy();
                case "hard": return AiProfile.Hard();
                case "boss": return AiProfile.Boss();
                default: return AiProfile.Medium();
            }
        }

        [Test]
        public void PerfectAimHitsTheChosenZone()
        {
            for (ulong seed = 1; seed <= 20; seed++)
            {
                MatchState m = AiDuel(seed, 26, new WindRange(0, 5));
                AiDecision d = new AiPlayer(AiProfile.Hard(), m.DeriveSeed(0)).Decide(m);
                ShotResult r = m.ApplyShot(d.PerfectInput);
                Assert.AreEqual(ContactKind.Fighter, r.Arrows[0].Contact, "seed " + seed);
                Assert.AreEqual(d.AimZone, r.Arrows[0].Zone, "seed " + seed);
            }
        }

        [Test]
        public void EasyNeverAimsForTheHeadAndHardOftenDoes()
        {
            int hardHeads = 0;
            for (ulong seed = 1; seed <= 200; seed++)
            {
                MatchState m = AiDuel(seed);
                Assert.AreEqual(HitZone.Body, new AiPlayer(AiProfile.Easy(), seed).Decide(m).AimZone);
                if (new AiPlayer(AiProfile.Hard(), seed).Decide(m).AimZone == HitZone.Head) hardHeads++;
            }
            Assert.That(hardHeads, Is.InRange(95, 145), "about 60 % of 200");
        }

        [Test]
        public void EveryMissShrinksTheError()
        {
            MatchState m = AiDuel(3);
            var ai = new AiPlayer(AiProfile.Medium(), 3);
            ai.Observe(m, m.ApplyShot(TestArena.Miss()));
            Assert.AreEqual(0.65, ai.ErrorScale, 1e-12);
            m.ApplyShot(TestArena.Miss()); // other side
            ai.Observe(m, m.ApplyShot(TestArena.Miss()));
            Assert.AreEqual(0.65 * 0.65, ai.ErrorScale, 1e-12);
            m.ApplyShot(TestArena.Miss());
            ai.Observe(m, m.ApplyShot(TestArena.Aim(m, HitZone.Body)));
            Assert.AreEqual(0.65 * 0.65, ai.ErrorScale, 1e-12, "a hit keeps the current accuracy");
        }

        [Test]
        public void SameSeedSameDecision()
        {
            MatchState a = AiDuel(9);
            MatchState b = AiDuel(9);
            AiDecision da = new AiPlayer(AiProfile.Medium(), a.DeriveSeed(1)).Decide(a);
            AiDecision db = new AiPlayer(AiProfile.Medium(), b.DeriveSeed(1)).Decide(b);
            Assert.AreEqual(da.Input.AngleDeg, db.Input.AngleDeg);
            Assert.AreEqual(da.Input.Power, db.Input.Power);
            Assert.AreEqual(da.ThinkSeconds, db.ThinkSeconds);
        }

        [Test]
        public void ThinkTimeFollowsTheProfileAndIceSlowsIt()
        {
            for (ulong seed = 1; seed <= 50; seed++)
            {
                double t = new AiPlayer(AiProfile.Easy(), seed).Decide(AiDuel(seed)).ThinkSeconds;
                Assert.That(t, Is.InRange(1.0, 1.4));
            }
            MatchSetup s = TestArena.Duel(20, WindRange.Calm, playerTips: new[] { ArrowTip.Ice });
            var m = new MatchState(s);
            m.ApplyShot(TestArena.Aim(m, HitZone.Body, ArrowTip.Ice));
            double slowed = new AiPlayer(AiProfile.Easy(), 5).Decide(m).ThinkSeconds;
            Assert.That(slowed, Is.InRange(1.3, 1.82), "30 % slower draw after an Ice hit");
        }

        [Test]
        public void FindsAWayPastAShield()
        {
            MatchSetup s = TestArena.Duel(20, WindRange.Calm);
            s.Props.Add(new PropSpec
            {
                Kind = PropKind.Shield, Shape = Shape.Box(new Vec2(0.55, 0.72), new Vec2(0.12, 0.72)), ShieldOwner = 0
            });
            var m = new MatchState(s);
            m.ApplyShot(TestArena.Miss());
            AiDecision d = new AiPlayer(AiProfile.Easy(), 1).Decide(m);
            ShotResult r = m.ApplyShot(d.PerfectInput);
            Assert.AreEqual(ContactKind.Fighter, r.Arrows[0].Contact, "over the shield (lob) or at the head, never into it");
            Assert.AreEqual(d.AimZone, r.Arrows[0].Zone);
            Assert.AreEqual(0, r.Arrows[0].HitFighter);
        }

        [Test]
        public void LobsOverAWallThatBlocksFlatShots()
        {
            MatchSetup s = TestArena.Duel(24, WindRange.Fixed(3), 4);
            s.Arena.Walls.Add(Shape.BoxFromTop(12, 7, 0.8, 9));
            var m = new MatchState(s);
            AiDecision d = new AiPlayer(AiProfile.Hard(), 4).Decide(m);
            Assert.Greater(d.PerfectInput.AngleDeg, 30.0);
            Assert.IsTrue(HitFoe(m, m.ApplyShot(d.PerfectInput)));
        }

        [Test]
        public void AiVersusAiMatchesFinishAndBothSidesWin()
        {
            int[] wins = new int[2];
            for (ulong seed = 1; seed <= 20; seed++)
            {
                MatchSetup s = TestArena.Duel(22, new WindRange(0, 3), seed);
                s.FirstTurn = FirstTurnRule.CoinFlip;
                var m = new MatchState(s);
                var ais = new[] { new AiPlayer(AiProfile.Medium(), m.DeriveSeed(0)), new AiPlayer(AiProfile.Medium(), m.DeriveSeed(1)) };
                int turns = 0;
                while (m.Phase == MatchPhase.Aiming)
                {
                    Assert.Less(++turns, 200, "match " + seed + " never ended");
                    AiPlayer ai = ais[m.CurrentSide];
                    ai.Observe(m, m.ApplyShot(ai.Decide(m).Input));
                }
                wins[m.Winner]++;
            }
            Assert.Greater(wins[0], 0);
            Assert.Greater(wins[1], 0);
        }
    }
}
