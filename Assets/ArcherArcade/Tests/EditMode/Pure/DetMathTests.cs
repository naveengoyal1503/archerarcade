using System;
using ArcherArcade.Logic;
using NUnit.Framework;

namespace ArcherArcade.Tests
{
    public class DetMathTests
    {
        [Test]
        public void SinCosMatchSystemMath()
        {
            for (double deg = -720; deg <= 720; deg += 0.37)
            {
                double r = deg * Math.PI / 180.0;
                Assert.AreEqual(Math.Sin(r), DetMath.Sin(r), 1e-12, "sin " + deg);
                Assert.AreEqual(Math.Cos(r), DetMath.Cos(r), 1e-12, "cos " + deg);
            }
        }

        [Test]
        public void RoundsHalfAwayFromZero()
        {
            Assert.AreEqual(3, DetMath.RoundToInt(2.5));
            Assert.AreEqual(-3, DetMath.RoundToInt(-2.5));
            Assert.AreEqual(15, DetMath.RoundToInt(15.0));
        }

        [Test]
        public void RotateQuarterTurn()
        {
            var v = new Vec2(1, 0).Rotated(90);
            Assert.AreEqual(0.0, v.X, 1e-12);
            Assert.AreEqual(1.0, v.Y, 1e-12);
        }
    }
}
