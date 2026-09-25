using ArcherArcade.Logic;
using NUnit.Framework;

namespace ArcherArcade.Tests
{
    public class PropTests
    {
        static int Count(MatchState m, MatchEventKind kind)
        {
            int n = 0;
            foreach (MatchEvent e in m.Events)
            {
                if (e.Kind == kind) n++;
            }
            return n;
        }

        static bool Has(MatchState m, MatchEventKind kind) => Count(m, kind) > 0;

        static MatchSetup DuelWithMiddleIsland()
        {
            MatchSetup s = TestArena.Duel(20, WindRange.Calm);
            s.Arena.Grounds.Add(Shape.BoxFromTop(10, 0, 4, 2));
            return s;
        }

        static Shape Crate(double x, double y) => Shape.Box(new Vec2(x, y + 0.45), new Vec2(0.45, 0.45));

        [Test]
        public void CrateBreaksAfterTwoHits()
        {
            MatchSetup s = DuelWithMiddleIsland();
            s.Props.Add(PropSpec.Of(PropKind.Crate, Crate(10, 0)));
            var m = new MatchState(s);
            m.ApplyShot(TestArena.AimAtProp(m, 0));
            Assert.IsTrue(Has(m, MatchEventKind.PropHit));
            Assert.IsTrue(m.GetProp(0).Alive);
            m.ApplyShot(TestArena.Miss());
            m.ApplyShot(TestArena.AimAtProp(m, 0));
            Assert.IsTrue(Has(m, MatchEventKind.CrateBroken));
            Assert.IsFalse(m.GetProp(0).Alive);
        }

        [Test]
        public void BombBreaksACrateInOneHit()
        {
            MatchSetup s = DuelWithMiddleIsland();
            s.Fighters[0].Tips = new[] { ArrowTip.Bomb };
            s.Props.Add(PropSpec.Of(PropKind.Crate, Crate(10, 0)));
            var m = new MatchState(s);
            m.ApplyShot(TestArena.AimAtProp(m, 0, ArrowTip.Bomb));
            Assert.IsTrue(Has(m, MatchEventKind.CrateBroken));
        }

        [Test]
        public void TowerCrateIsKnockedOffAndCratesAboveSettle()
        {
            MatchSetup s = DuelWithMiddleIsland();
            TowerBuilder.Add(s.Props, 0, 10, 0, 4);
            var m = new MatchState(s);
            m.ApplyShot(TestArena.AimAtProp(m, 1));
            Assert.IsTrue(Has(m, MatchEventKind.CrateKnockedOff));
            Assert.IsFalse(Has(m, MatchEventKind.TowerToppled));
            Assert.IsFalse(m.GetProp(1).Alive);
            Assert.AreEqual(0.0, m.GetProp(0).StackOffset.Y, 1e-12);
            Assert.AreEqual(-0.9, m.GetProp(2).StackOffset.Y, 1e-12);
            Assert.AreEqual(-0.9, m.GetProp(3).StackOffset.Y, 1e-12);
            Assert.AreEqual(4 * 0.9 - 0.9, m.PropRestShape(3).A.Y + 0.45, 1e-9, "top crate now sits 3 crates high");
        }

        static MatchSetup SniperOnTower(int crates, int tnt = -1)
        {
            MatchSetup s = TestArena.Duel(20, WindRange.Calm);
            TowerBuilder.Add(s.Props, 0, 20, 0, crates, 0.9, tnt);
            s.Fighters[1].Feet = new Vec2(20, crates * 0.9);
            s.Fighters[1].StandOnTower = 0;
            return s;
        }

        [Test]
        public void TowerSniperDropsWhenACrateIsKnockedOff()
        {
            var m = new MatchState(SniperOnTower(3));
            Assert.AreEqual(2.7, m.GetFighter(1).Feet.Y, 1e-12);
            m.ApplyShot(TestArena.AimAtProp(m, 0));
            Assert.IsTrue(Has(m, MatchEventKind.CrateKnockedOff));
            Assert.IsTrue(Has(m, MatchEventKind.FighterDropped));
            Assert.AreEqual(1.8, m.GetFighter(1).Feet.Y, 1e-9);
        }

        [Test]
        public void BombTopplesTheTowerAndTheSniperLandsOnTheIsland()
        {
            MatchSetup s = SniperOnTower(3);
            s.Fighters[0].Tips = new[] { ArrowTip.Bomb };
            var m = new MatchState(s);
            m.ApplyShot(TestArena.AimAtProp(m, 0, ArrowTip.Bomb));
            Assert.IsTrue(Has(m, MatchEventKind.TowerToppled));
            for (int i = 0; i < 3; i++) Assert.IsFalse(m.GetProp(i).Alive);
            Assert.AreEqual(0.0, m.GetFighter(1).Feet.Y, 1e-9);
        }

        [Test]
        public void TntExplodesHurtsNearbyArchersAndBlowsTheTowerApart()
        {
            MatchSetup s = TestArena.Duel(20, WindRange.Calm);
            TowerBuilder.Add(s.Props, 0, 18, 0, 3, 0.9, 1);
            var m = new MatchState(s);
            m.ApplyShot(TestArena.AimAtProp(m, 1, ArrowTip.Normal, true));
            Assert.AreEqual(1, Count(m, MatchEventKind.Explosion));
            Assert.IsTrue(Has(m, MatchEventKind.TowerToppled));
            Assert.IsTrue(Has(m, MatchEventKind.ExplosionHit));
            Assert.AreEqual(100 - 35, m.GetFighter(1).Hp);
            for (int i = 0; i < 3; i++) Assert.IsFalse(m.GetProp(i).Alive);
        }

        [Test]
        public void BarrelHurtsOnlyInsideItsRadius()
        {
            MatchSetup s = DuelWithMiddleIsland();
            s.Props.Add(PropSpec.Of(PropKind.ExplosiveBarrel, Shape.Circle(new Vec2(18.4, 0.45), 0.45)));
            s.Props.Add(PropSpec.Of(PropKind.ExplosiveBarrel, Shape.Circle(new Vec2(10, 0.45), 0.45)));
            var m = new MatchState(s);
            m.ApplyShot(TestArena.AimAtProp(m, 1));
            Assert.AreEqual(1, Count(m, MatchEventKind.Explosion));
            Assert.AreEqual(100, m.GetFighter(1).Hp, "8 m away: safe");
            m.ApplyShot(TestArena.Miss());
            m.ApplyShot(TestArena.AimAtProp(m, 0, ArrowTip.Normal, true));
            Assert.AreEqual(100 - 30, m.GetFighter(1).Hp, "1.6 m away: hit");
        }

        [Test]
        public void BarrelsSetEachOtherOff()
        {
            MatchSetup s = DuelWithMiddleIsland();
            s.Props.Add(PropSpec.Of(PropKind.ExplosiveBarrel, Shape.Circle(new Vec2(9.2, 0.45), 0.45)));
            s.Props.Add(PropSpec.Of(PropKind.ExplosiveBarrel, Shape.Circle(new Vec2(10.8, 0.45), 0.45)));
            var m = new MatchState(s);
            m.ApplyShot(TestArena.AimAtProp(m, 0));
            Assert.AreEqual(2, Count(m, MatchEventKind.Explosion));
            Assert.IsFalse(m.GetProp(1).Alive);
        }

        [Test]
        public void BouncePadMirrorsTheArrowAndKeepsEightyPercentSpeed()
        {
            MatchSetup s = DuelWithMiddleIsland();
            s.Props.Add(PropSpec.Of(PropKind.BouncePad, Shape.Box(new Vec2(10, 0.1), new Vec2(1.5, 0.1))));
            var m = new MatchState(s);
            ShotResult r = m.ApplyShot(TestArena.AimAtPoint(m, new Vec2(10, 0.2), ArrowTip.Normal, true));
            Assert.AreEqual(ContactKind.Bounce, r.Arrows[0].Contact);
            Assert.AreEqual(0, r.Arrows[0].HitProp);
            Assert.GreaterOrEqual(r.Arrows.Count, 2);
            ArrowPath child = r.Arrows[1];
            Assert.AreEqual(0, child.Parent);
            Assert.AreEqual(1, child.BounceCount);
            Vec2 vin = r.Arrows[0].EndVelocity;
            Assert.AreEqual(vin.X * 0.8, child.StartVelocity.X, 1e-9);
            Assert.AreEqual(-vin.Y * 0.8, child.StartVelocity.Y, 1e-9);
            Assert.AreEqual(r.Arrows[0].EndTime, child.StartTime, 1e-12);
            Assert.IsTrue(Has(m, MatchEventKind.Bounce));
        }

        [Test]
        public void SwingingTargetMovesWithTheMatchClock()
        {
            var motion = PropMotion.Swing(new Vec2(12, 6), 30, 2.0);
            Shape rest = Shape.Circle(new Vec2(12, 3), 0.4);
            Assert.AreEqual(12.0, motion.Apply(rest, 0).A.X, 1e-12);
            Vec2 quarter = motion.Apply(rest, 0.5).A;
            Assert.AreEqual(12 + 3 * System.Math.Sin(System.Math.PI / 6), quarter.X, 1e-9);
            Assert.AreEqual(12.0, motion.Apply(rest, 1.0).A.X, 1e-9);

            // The solver flies the shot against the swinging target, so its solution hits.
            MatchSetup s = TestArena.Solo();
            s.Props.Add(new PropSpec { Kind = PropKind.Target, Shape = rest, Motion = motion });
            var m = new MatchState(s);
            m.Tick(0.3);
            m.ApplyShot(TestArena.AimAtProp(m, 0));
            Assert.IsTrue(Has(m, MatchEventKind.TargetHit));
            Assert.AreEqual(0, m.CurrentSide, "solo levels stay on the player");
        }

        [Test]
        public void MovingTargetShotDependsOnReleaseTime()
        {
            MatchSetup s = TestArena.Solo();
            s.Props.Add(new PropSpec
            {
                Kind = PropKind.Target, Shape = Shape.Circle(new Vec2(12, 3), 0.4),
                Motion = PropMotion.PingPong(new Vec2(0, 0), new Vec2(0, 8), 20.0)
            });
            var m = new MatchState(s);
            ShotInput shot = TestArena.AimAtProp(m, 0);
            var late = new MatchState(s);
            late.Tick(5.0);
            late.ApplyShot(shot);
            Assert.AreEqual(5.0, late.LastShot.Clock, 1e-12);
            Assert.IsFalse(Has(late, MatchEventKind.TargetHit), "released 5 s later: the target has risen 4 m");
            m.ApplyShot(shot);
            Assert.IsTrue(Has(m, MatchEventKind.TargetHit));
            Assert.AreEqual(m.LastShot.Duration, m.Clock, 1e-12, "the clock advances by the flight time");
        }

        [Test]
        public void PingPongMovesBetweenOffsets()
        {
            var motion = PropMotion.PingPong(new Vec2(0, 0), new Vec2(0, 4), 4.0);
            Shape rest = Shape.Circle(new Vec2(10, 2), 0.4);
            Assert.AreEqual(2.0, motion.Apply(rest, 0).A.Y, 1e-12);
            Assert.AreEqual(4.0, motion.Apply(rest, 1).A.Y, 1e-12);
            Assert.AreEqual(6.0, motion.Apply(rest, 2).A.Y, 1e-12);
            Assert.AreEqual(4.0, motion.Apply(rest, 3).A.Y, 1e-12);
            Assert.AreEqual(2.0, motion.Apply(rest, 4).A.Y, 1e-12);
        }

        [Test]
        public void MovingPlatformStepsEachTurnAndCarriesItsArcher()
        {
            MatchSetup s = TestArena.Duel(20, WindRange.Calm);
            s.Props.Add(new PropSpec
            {
                Kind = PropKind.Platform, Shape = Shape.Box(new Vec2(20, 0.0), new Vec2(1.5, 0.2)),
                TurnOffsetA = new Vec2(0, 0), TurnOffsetB = new Vec2(0, 3), TurnCycle = 4
            });
            s.Fighters[1].Feet = new Vec2(20, 0.2);
            s.Fighters[1].StandOnProp = 0;
            var m = new MatchState(s);
            Assert.AreEqual(0.2, m.GetFighter(1).Feet.Y, 1e-12);
            m.ApplyShot(TestArena.Miss());
            Assert.IsTrue(Has(m, MatchEventKind.PlatformMoved));
            Assert.AreEqual(1.7, m.GetFighter(1).Feet.Y, 1e-12);
            m.ApplyShot(TestArena.Miss());
            Assert.AreEqual(3.2, m.GetFighter(1).Feet.Y, 1e-12);
            m.ApplyShot(TestArena.Miss());
            m.ApplyShot(TestArena.Miss());
            Assert.AreEqual(0.2, m.GetFighter(1).Feet.Y, 1e-12, "back at the start after a full cycle");
            ShotResult r = m.ApplyShot(TestArena.Aim(m, HitZone.Body));
            Assert.AreEqual(ContactKind.Fighter, r.Arrows[0].Contact);
        }

        static MatchSetup ShieldBearer(ArrowTip[] tips = null)
        {
            MatchSetup s = TestArena.Duel(20, WindRange.Calm, playerTips: tips);
            s.Props.Add(new PropSpec
            {
                Kind = PropKind.Shield, Shape = Shape.Box(new Vec2(-0.55, 0.72), new Vec2(0.12, 0.72)), ShieldOwner = 1
            });
            return s;
        }

        [Test]
        public void ShieldBlocksBodyShotsButNotHeadshots()
        {
            var m = new MatchState(ShieldBearer());
            m.ApplyShot(TestArena.AimAtPoint(m, m.Setup.Body.ZoneCenter(HitZone.Body, m.GetFighter(1).Feet)));
            Assert.IsTrue(Has(m, MatchEventKind.ShieldBlocked));
            Assert.AreEqual(100, m.GetFighter(1).Hp);
            m.ApplyShot(TestArena.Miss());
            m.ApplyShot(TestArena.Aim(m, HitZone.Head));
            Assert.AreEqual(50, m.GetFighter(1).Hp);
        }

        [Test]
        public void HeavyTipKnocksTheShieldDownForOneFreeTurn()
        {
            var m = new MatchState(ShieldBearer(new[] { ArrowTip.Heavy }));
            m.ApplyShot(TestArena.AimAtProp(m, 0, ArrowTip.Heavy));
            Assert.IsTrue(Has(m, MatchEventKind.ShieldKnockedDown));
            Assert.IsFalse(m.PropPresent(0));
            m.ApplyShot(TestArena.Miss()); // bearer
            Assert.IsFalse(m.PropPresent(0), "still down on the player's next turn");
            m.ApplyShot(TestArena.Aim(m, HitZone.Body));
            Assert.AreEqual(75, m.GetFighter(1).Hp);
            Assert.IsTrue(Has(m, MatchEventKind.ShieldRaised), "back up when the bearer's turn starts");
            Assert.IsTrue(m.PropPresent(0));
        }

        [Test]
        public void RopeIsCutByAnyHit()
        {
            MatchSetup s = TestArena.Solo();
            s.Props.Add(PropSpec.Of(PropKind.Rope, Shape.Box(new Vec2(14, 5), new Vec2(0.05, 1.2))));
            var m = new MatchState(s);
            m.ApplyShot(TestArena.AimAtProp(m, 0));
            Assert.IsTrue(Has(m, MatchEventKind.RopeCut));
            Assert.IsFalse(m.GetProp(0).Alive);
        }

        [Test]
        public void AppleShotAndDummy()
        {
            MatchSetup s = TestArena.Solo();
            s.Arena.Grounds.Add(Shape.BoxFromTop(16, 0, 3, 2));
            s.Props.Add(PropSpec.Of(PropKind.Dummy, Shape.Box(new Vec2(16, 0.9), new Vec2(0.25, 0.9))));
            s.Props.Add(PropSpec.Of(PropKind.Apple, Shape.Circle(new Vec2(16, 1.96), 0.15)));
            var m = new MatchState(s);
            m.ApplyShot(TestArena.AimAtProp(m, 1));
            Assert.IsTrue(Has(m, MatchEventKind.AppleHit));
            Assert.IsFalse(Has(m, MatchEventKind.DummyHit));
            m.ApplyShot(TestArena.AimAtProp(m, 0));
            Assert.IsTrue(Has(m, MatchEventKind.DummyHit));
        }

        [Test]
        public void BombKnockbackPushesAndStumblesAtTheEdge()
        {
            MatchSetup s = TestArena.Duel(20, WindRange.Calm, playerTips: new[] { ArrowTip.Bomb });
            var m = new MatchState(s);
            m.ApplyShot(TestArena.Aim(m, HitZone.Body, ArrowTip.Bomb));
            Assert.AreEqual(100 - 20, m.GetFighter(1).Hp);
            Assert.AreEqual(20.8, m.GetFighter(1).Feet.X, 1e-9);
            Assert.IsTrue(Has(m, MatchEventKind.Knockback));
            Assert.IsFalse(Has(m, MatchEventKind.Stumble));

            MatchSetup edge = TestArena.Duel(20, WindRange.Calm, playerTips: new[] { ArrowTip.Bomb });
            edge.Arena.Grounds[1] = Shape.BoxFromTop(20, 0, 2, 2);
            var e = new MatchState(edge);
            e.ApplyShot(TestArena.Aim(e, HitZone.Body, ArrowTip.Bomb));
            Assert.AreEqual(20.6, e.GetFighter(1).Feet.X, 1e-9, "stops 0.4 m from the edge, no fall");
            Assert.IsTrue(Has(e, MatchEventKind.Stumble));
            Assert.IsTrue(e.GetFighter(1).IsAlive);
        }

        [Test]
        public void ElectricChainSetsOffANearbyBarrel()
        {
            MatchSetup s = TestArena.Duel(20, WindRange.Calm, playerTips: new[] { ArrowTip.Electric });
            s.Props.Add(PropSpec.Of(PropKind.ExplosiveBarrel, Shape.Circle(new Vec2(21.5, 0.45), 0.45)));
            var m = new MatchState(s);
            m.ApplyShot(TestArena.Aim(m, HitZone.Body, ArrowTip.Electric));
            Assert.IsTrue(Has(m, MatchEventKind.ChainHit));
            Assert.IsTrue(Has(m, MatchEventKind.Explosion));
            Assert.AreEqual(100 - 20 - 30, m.GetFighter(1).Hp);
        }

        [Test]
        public void WallsStopArrowsAndReportTheHit()
        {
            MatchSetup s = DuelWithMiddleIsland();
            s.Props.Add(PropSpec.Of(PropKind.Wall, Shape.BoxFromTop(10, 3, 0.5, 3)));
            var m = new MatchState(s);
            ShotResult r = m.ApplyShot(new ShotInput(5, 1));
            Assert.AreEqual(ContactKind.Prop, r.Arrows[0].Contact);
            Assert.IsTrue(Has(m, MatchEventKind.PropHit));
            Assert.IsTrue(m.GetProp(0).Alive);
        }

        static System.Collections.Generic.List<ulong> PlayBusyArena(ulong seed)
        {
            MatchSetup s = TestArena.Duel(24, new WindRange(0, 4), seed, ArcherTable.BombArcher(), ArcherTable.ElectricArcher(),
                new[] { ArrowTip.Bomb, ArrowTip.Split, ArrowTip.Heavy });
            s.Fighters[1].Tips = new[] { ArrowTip.Electric, ArrowTip.Fire };
            s.FirstTurn = FirstTurnRule.CoinFlip;
            s.Arena.Grounds.Add(Shape.BoxFromTop(12, 0, 5, 2));
            TowerBuilder.Add(s.Props, 0, 12, 0, 5, 0.9, 2);
            s.Props.Add(PropSpec.Of(PropKind.ExplosiveBarrel, Shape.Circle(new Vec2(13.6, 0.45), 0.45)));
            s.Props.Add(PropSpec.Of(PropKind.BouncePad, Shape.Box(new Vec2(10.5, 0.1), new Vec2(0.8, 0.1))));
            s.Props.Add(new PropSpec
            {
                Kind = PropKind.Target, Shape = Shape.Circle(new Vec2(12, 7), 0.4),
                Motion = PropMotion.PingPong(new Vec2(-2, 0), new Vec2(2, 0), 3.0)
            });
            var m = new MatchState(s);
            var inputs = new Rng(seed * 31UL + 7UL);
            ArrowTip[] tips = { ArrowTip.Normal, ArrowTip.Bomb, ArrowTip.Split, ArrowTip.Heavy, ArrowTip.Electric, ArrowTip.Fire };
            var hashes = new System.Collections.Generic.List<ulong> { m.ComputeHash() };
            for (int i = 0; i < 60 && m.Phase == MatchPhase.Aiming; i++)
            {
                m.Tick(inputs.Range(0.2, 4.0));
                var input = new ShotInput(inputs.Range(5, 70), inputs.Range(0.2, 1.0), tips[inputs.NextInt(tips.Length)]);
                if (!m.ApplyShot(input).Accepted) m.ApplyShot(new ShotInput(input.AngleDeg, input.Power));
                hashes.Add(m.ComputeHash());
            }
            return hashes;
        }

        [Test]
        public void BusyArenaIsDeterministic()
        {
            for (ulong seed = 1; seed <= 4; seed++) CollectionAssert.AreEqual(PlayBusyArena(seed), PlayBusyArena(seed));
        }
    }
}
