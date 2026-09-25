using System;

namespace ArcherArcade.Tweening
{
    /// <summary>Easing curves (t in 0..1). Pure math, no UnityEngine, so it is unit-tested outside Unity too.</summary>
    public static class Ease
    {
        const float BackOvershoot = 1.70158f;
        const float TwoPi = 6.2831853f;

        public static float Evaluate(EaseType type, float t)
        {
            if (t <= 0f) return 0f;
            if (t >= 1f) return 1f;
            switch (type)
            {
                case EaseType.InQuad: return t * t;
                case EaseType.OutQuad: return t * (2f - t);
                case EaseType.InOutQuad: return t < 0.5f ? 2f * t * t : 1f - 2f * (1f - t) * (1f - t);
                case EaseType.OutCubic:
                {
                    float u = 1f - t;
                    return 1f - u * u * u;
                }
                case EaseType.InOutCubic:
                {
                    if (t < 0.5f) return 4f * t * t * t;
                    float u = -2f * t + 2f;
                    return 1f - u * u * u / 2f;
                }
                case EaseType.OutBack:
                {
                    float c3 = BackOvershoot + 1f;
                    float u = t - 1f;
                    return 1f + c3 * u * u * u + BackOvershoot * u * u;
                }
                case EaseType.InBack:
                {
                    float c3 = BackOvershoot + 1f;
                    return c3 * t * t * t - BackOvershoot * t * t;
                }
                case EaseType.OutElastic:
                    return (float)(Math.Pow(2.0, -10.0 * t) * Math.Sin((t * 10.0 - 0.75) * (TwoPi / 3.0)) + 1.0);
                case EaseType.OutBounce:
                    return OutBounce(t);
                default:
                    return t;
            }
        }

        static float OutBounce(float t)
        {
            const float n1 = 7.5625f;
            const float d1 = 2.75f;
            if (t < 1f / d1) return n1 * t * t;
            if (t < 2f / d1)
            {
                t -= 1.5f / d1;
                return n1 * t * t + 0.75f;
            }
            if (t < 2.5f / d1)
            {
                t -= 2.25f / d1;
                return n1 * t * t + 0.9375f;
            }
            t -= 2.625f / d1;
            return n1 * t * t + 0.984375f;
        }
    }
}
