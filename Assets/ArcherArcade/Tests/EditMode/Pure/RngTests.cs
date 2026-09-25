using ArcherArcade.Logic;
using NUnit.Framework;

namespace ArcherArcade.Tests
{
    public class RngTests
    {
        [Test]
        public void MatchesPcg32ReferenceOutput()
        {
            // pcg32-demo from pcg-c-basic: pcg32_srandom(42, 54).
            var rng = new Rng(42UL, 54UL);
            uint[] expected = { 0xa15c02b7u, 0x7b47f409u, 0xba1d3330u, 0x83d2f293u, 0xbfa4784bu, 0xcbed606eu };
            foreach (uint e in expected) Assert.AreEqual(e, rng.NextUInt());
        }

        [Test]
        public void SameSeedSameSequence()
        {
            var a = new Rng(20260925UL);
            var b = new Rng(20260925UL);
            for (int i = 0; i < 1000; i++) Assert.AreEqual(a.NextUInt(), b.NextUInt());
        }

        [Test]
        public void CloneAndFromStateContinueIdentically()
        {
            var a = new Rng(7UL);
            for (int i = 0; i < 10; i++) a.NextUInt();
            var b = a.Clone();
            var c = Rng.FromState(a.State, a.Increment);
            for (int i = 0; i < 100; i++)
            {
                uint v = a.NextUInt();
                Assert.AreEqual(v, b.NextUInt());
                Assert.AreEqual(v, c.NextUInt());
            }
        }

        [Test]
        public void NextIntStaysInBoundsAndCoversAllValues()
        {
            var rng = new Rng(1UL);
            var seen = new bool[7];
            for (int i = 0; i < 10000; i++)
            {
                int v = rng.NextInt(7);
                Assert.That(v, Is.InRange(0, 6));
                seen[v] = true;
            }
            foreach (bool s in seen) Assert.IsTrue(s);
        }

        [Test]
        public void RangeInclusiveHitsBothEnds()
        {
            var rng = new Rng(3UL);
            bool lo = false, hi = false;
            for (int i = 0; i < 1000; i++)
            {
                int v = rng.RangeInclusive(-2, 2);
                Assert.That(v, Is.InRange(-2, 2));
                lo |= v == -2;
                hi |= v == 2;
            }
            Assert.IsTrue(lo && hi);
        }

        [Test]
        public void NextDoubleInUnitInterval()
        {
            var rng = new Rng(9UL);
            double sum = 0;
            for (int i = 0; i < 20000; i++)
            {
                double d = rng.NextDouble();
                Assert.That(d, Is.GreaterThanOrEqualTo(0.0).And.LessThan(1.0));
                sum += d;
            }
            Assert.AreEqual(0.5, sum / 20000, 0.01);
        }

        [Test]
        public void GaussianHasUnitSpread()
        {
            var rng = new Rng(11UL);
            double sum = 0, sq = 0;
            const int n = 20000;
            for (int i = 0; i < n; i++)
            {
                double g = rng.NextGaussian();
                sum += g;
                sq += g * g;
            }
            double mean = sum / n;
            Assert.AreEqual(0.0, mean, 0.03);
            Assert.AreEqual(1.0, sq / n - mean * mean, 0.05);
        }
    }
}
