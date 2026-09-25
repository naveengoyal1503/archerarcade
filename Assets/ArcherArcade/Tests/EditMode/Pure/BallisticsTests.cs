using System;
using ArcherArcade.Logic;
using NUnit.Framework;

namespace ArcherArcade.Tests
{
    public class BallisticsTests
    {
        readonly ShotConfig _cfg = new ShotConfig();

        [Test]
        public void PowerFromDragUsesEaseOutQuad()
        {
            Assert.AreEqual(0.0, Ballistics.PowerFromDrag(0, _cfg), 1e-12);
            Assert.AreEqual(0.75, Ballistics.PowerFromDrag(90, _cfg), 1e-12);
            Assert.AreEqual(1.0, Ballistics.PowerFromDrag(180, _cfg), 1e-12);
            Assert.AreEqual(1.0, Ballistics.PowerFromDrag(400, _cfg), 1e-12);
        }

        [Test]
        public void LaunchSpeedSpansConfigAndMirrorsWithFacing()
        {
            Assert.AreEqual(12.0, Ballistics.LaunchVelocity(30, 0, 1, _cfg).Length, 1e-9);
            Assert.AreEqual(34.0, Ballistics.LaunchVelocity(30, 1, 1, _cfg).Length, 1e-9);
            Vec2 r = Ballistics.LaunchVelocity(30, 0.5, 1, _cfg);
            Vec2 l = Ballistics.LaunchVelocity(30, 0.5, -1, _cfg);
            Assert.AreEqual(r.X, -l.X, 1e-12);
            Assert.AreEqual(r.Y, l.Y, 1e-12);
        }

        [Test]
        public void AngleIsClampedToLimits()
        {
            Vec2 v = Ballistics.LaunchVelocity(120, 1, 1, _cfg);
            Vec2 max = Ballistics.LaunchVelocity(80, 1, 1, _cfg);
            Assert.AreEqual(max.X, v.X, 1e-12);
            Assert.AreEqual(max.Y, v.Y, 1e-12);
        }

        [Test]
        public void FixedStepMatchesAnalyticArc()
        {
            Vec2 p = new Vec2(0, 1.25), v = Ballistics.LaunchVelocity(40, 0.8, 1, _cfg);
            Vec2 a = Ballistics.Acceleration(3, 1, _cfg);
            Vec2 start = p, v0 = v;
            for (int i = 0; i < 240; i++) Ballistics.Step(ref p, ref v, a, _cfg.StepSeconds);
            Vec2 exact = Ballistics.PositionAt(start, v0, a, 2.0);
            Assert.AreEqual(exact.X, p.X, 1e-9);
            Assert.AreEqual(exact.Y, p.Y, 1e-9);
        }

        static double LandingX(double angle, double power, double wind, double gravityScale, ShotConfig cfg)
        {
            var arena = new ArenaLayout { MinX = -200, MaxX = 400 };
            // Flat ground from x = 1 on, so the arrow does not start inside it.
            arena.Grounds.Add(Shape.BoxFromTop(300.5, 0, 599, 2));
            var world = new CollisionWorld();
            world.AddArena(arena);
            var paths = new System.Collections.Generic.List<ArrowPath>();
            FlightSimulator.Simulate(new Vec2(0, 0), Ballistics.LaunchVelocity(angle, power, 1, cfg),
                Ballistics.Acceleration(wind, gravityScale, cfg), 0, 0, cfg, world, arena, -1, paths);
            Assert.AreEqual(ContactKind.Ground, paths[0].Contact);
            return paths[0].EndPosition.X;
        }

        [TestCase(15, 0.5)]
        [TestCase(30, 0.5)]
        [TestCase(45, 0.5)]
        [TestCase(45, 1.0)]
        [TestCase(60, 0.75)]
        [TestCase(75, 1.0)]
        public void RangeTableMatchesFormula(double angle, double power)
        {
            double v = Ballistics.SpeedFromPower(power, _cfg);
            double expected = v * v * Math.Sin(2 * angle * Math.PI / 180) / _cfg.Gravity;
            // Landing is interpolated inside one 1/120 s step, so allow a few millimetres.
            Assert.AreEqual(expected, LandingX(angle, power, 0, 1, _cfg), 0.01);
        }

        static double MaxRange(double wind, double gravityScale, ShotConfig cfg)
        {
            double best = 0;
            for (double a = 20; a <= 60; a += 0.5) best = Math.Max(best, LandingX(a, 1, wind, gravityScale, cfg));
            return best;
        }

        [Test]
        public void LongestLevelDistancesAreReachableInFullHeadwind()
        {
            // LEVELS.md: duels up to 30 m, Daily "One Arrow at 35 m", Long Shot badge at 35 m, solver tests to 40 m.
            Assert.Greater(MaxRange(-5, 1.0, _cfg), 40.0, "Normal tip, headwind 5");
            Assert.Greater(MaxRange(-5, 1.35, _cfg), 35.0, "Heavy tip, headwind 5");
        }

        [Test]
        public void WindShiftsLandingByHalfATSquared()
        {
            double calm = LandingX(45, 0.6, 0, 1, _cfg);
            double tail = LandingX(45, 0.6, 3, 1, _cfg);
            double head = LandingX(45, 0.6, -3, 1, _cfg);
            Assert.Greater(tail, calm);
            Assert.Less(head, calm);
            double vy = Ballistics.LaunchVelocity(45, 0.6, 1, _cfg).Y;
            double t = 2 * vy / _cfg.Gravity;
            double shift = 0.5 * 3 * _cfg.WindAccelPerUnit * t * t;
            Assert.AreEqual(calm + shift, tail, 0.01);
            Assert.AreEqual(calm - shift, head, 0.01);
        }

        [Test]
        public void HeavyTipFallsShorter()
        {
            Assert.Less(LandingX(40, 0.7, 0, 1.35, _cfg), LandingX(40, 0.7, 0, 1, _cfg) * 0.8);
        }

        [Test]
        public void TimeToHeightMatchesFlight()
        {
            Vec2 v = Ballistics.LaunchVelocity(45, 1, 1, _cfg);
            Vec2 a = Ballistics.Acceleration(0, 1, _cfg);
            Assert.AreEqual(2 * v.Y / _cfg.Gravity, Ballistics.TimeToHeight(Vec2.Zero, v, a, 0), 1e-9);
        }
    }
}
