using ArcherArcade.Logic;
using NUnit.Framework;

namespace ArcherArcade.Tests
{
    public class GeometryTests
    {
        [Test]
        public void SegmentHitsCircleAtFrontEdge()
        {
            double t;
            Assert.IsTrue(Sweep.SegmentCircle(new Vec2(0, 0), new Vec2(10, 0), new Vec2(5, 0), 1, out t));
            Assert.AreEqual(0.4, t, 1e-12);
            Assert.IsFalse(Sweep.SegmentCircle(new Vec2(0, 2), new Vec2(10, 2), new Vec2(5, 0), 1, out t));
        }

        [Test]
        public void SegmentHitsBoxAndStartInsideIsZero()
        {
            double t;
            Assert.IsTrue(Sweep.SegmentBox(new Vec2(0, 5), new Vec2(0, -5), new Vec2(0, -1), new Vec2(2, 1), out t));
            Assert.AreEqual(0.5, t, 1e-12);
            Assert.IsTrue(Sweep.SegmentBox(new Vec2(0, -1), new Vec2(3, -1), new Vec2(0, -1), new Vec2(2, 1), out t));
            Assert.AreEqual(0.0, t, 1e-12);
            Assert.IsFalse(Sweep.SegmentBox(new Vec2(-5, 3), new Vec2(5, 3), new Vec2(0, -1), new Vec2(2, 1), out t));
        }

        [Test]
        public void SegmentHitsCapsuleSideAndCaps()
        {
            double t;
            // Vertical capsule x = 5, y 1..3, r 0.5; horizontal ray at y = 2 hits the side at x = 4.5.
            Assert.IsTrue(Sweep.SegmentCapsule(new Vec2(0, 2), new Vec2(10, 2), new Vec2(5, 1), new Vec2(5, 3), 0.5, out t));
            Assert.AreEqual(0.45, t, 1e-12);
            // Ray at y = 3.3 only touches the top cap.
            Assert.IsTrue(Sweep.SegmentCapsule(new Vec2(0, 3.3), new Vec2(10, 3.3), new Vec2(5, 1), new Vec2(5, 3), 0.5, out t));
            Assert.AreEqual((5 - 0.4) / 10, t, 1e-12);
            Assert.IsFalse(Sweep.SegmentCapsule(new Vec2(0, 3.6), new Vec2(10, 3.6), new Vec2(5, 1), new Vec2(5, 3), 0.5, out t));
        }

        [Test]
        public void ShapeDistance()
        {
            Assert.AreEqual(1.0, Shape.Circle(new Vec2(0, 0), 1).DistanceTo(new Vec2(2, 0)), 1e-12);
            Assert.AreEqual(0.0, Shape.Box(new Vec2(0, 0), new Vec2(1, 1)).DistanceTo(new Vec2(0.5, 0.5)), 1e-12);
            Assert.AreEqual(1.0, Shape.Box(new Vec2(0, 0), new Vec2(1, 1)).DistanceTo(new Vec2(2, 0)), 1e-12);
        }
    }
}
