using ArcherArcade.Logic;
using NUnit.Framework;

namespace ArcherArcade.Tests
{
    public class WindTests
    {
        [Test]
        public void RollStaysInRangeAndUsesBothDirections()
        {
            var rng = new Rng(5UL);
            bool left = false, right = false, min = false, max = false;
            for (int i = 0; i < 2000; i++)
            {
                int w = Wind.Roll(new WindRange(2, 4), rng);
                int s = w < 0 ? -w : w;
                Assert.That(s, Is.InRange(2, 4));
                left |= w < 0;
                right |= w > 0;
                min |= s == 2;
                max |= s == 4;
            }
            Assert.IsTrue(left && right && min && max);
        }

        [Test]
        public void CalmIsAlwaysZero()
        {
            var rng = new Rng(5UL);
            for (int i = 0; i < 100; i++) Assert.AreEqual(0, Wind.Roll(WindRange.Calm, rng));
        }

        [Test]
        public void RangeIsClampedToFiveBars()
        {
            var rng = new Rng(8UL);
            for (int i = 0; i < 200; i++)
            {
                int w = Wind.Roll(new WindRange(4, 9), rng);
                Assert.That(w < 0 ? -w : w, Is.InRange(4, 5));
            }
        }

        [Test]
        public void SameSeedSameWinds()
        {
            var a = new Rng(77UL);
            var b = new Rng(77UL);
            for (int i = 0; i < 100; i++) Assert.AreEqual(Wind.Roll(new WindRange(0, 5), a), Wind.Roll(new WindRange(0, 5), b));
        }
    }
}
