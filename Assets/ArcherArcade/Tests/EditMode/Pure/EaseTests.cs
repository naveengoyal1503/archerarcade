using System;
using ArcherArcade.Tweening;
using NUnit.Framework;

namespace ArcherArcade.Tests
{
    public class EaseTests
    {
        [Test]
        public void EveryCurveStartsAtZeroAndEndsAtOne()
        {
            foreach (EaseType e in Enum.GetValues(typeof(EaseType)))
            {
                Assert.AreEqual(0f, Ease.Evaluate(e, 0f), 1e-5f, e.ToString());
                Assert.AreEqual(1f, Ease.Evaluate(e, 1f), 1e-5f, e.ToString());
                Assert.AreEqual(1f, Ease.Evaluate(e, 0.99999f), 0.01f, e + " continuous at 1");
            }
        }

        [Test]
        public void OutBackOvershoots()
        {
            float max = 0f;
            for (float t = 0f; t <= 1f; t += 0.01f) max = Math.Max(max, Ease.Evaluate(EaseType.OutBack, t));
            Assert.Greater(max, 1.05f);
        }

        [Test]
        public void OutQuadMatchesFormula()
        {
            Assert.AreEqual(0.75f, Ease.Evaluate(EaseType.OutQuad, 0.5f), 1e-6f);
        }
    }
}
