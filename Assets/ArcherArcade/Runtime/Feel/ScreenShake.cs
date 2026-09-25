using ArcherArcade.Core;
using UnityEngine;

namespace ArcherArcade.Feel
{
    /// <summary>
    /// Camera shake in dp (DESIGN_TOKENS §7: head 8, body 5; bombs and TNT more). Decays quickly with a smooth
    /// noise wobble; "Reduce motion" keeps a third of it. The camera rig samples it every frame.
    /// </summary>
    public static class ScreenShake
    {
        const float Decay = 38f; // dp per second
        const float MaxDp = 18f;

        static float _amount;
        static float _t;

        public static void Add(float dp)
        {
            if (ServiceLocator.Settings != null && ServiceLocator.Settings.ReduceMotion) dp *= 0.35f;
            _amount = Mathf.Min(MaxDp, Mathf.Max(_amount, dp) + dp * 0.25f);
        }

        /// <summary>Current offset in dp; advances the shake by <paramref name="dt"/> seconds.</summary>
        public static Vector2 Sample(float dt)
        {
            if (_amount <= 0f) return Vector2.zero;
            _t += dt * 38f;
            _amount = Mathf.Max(0f, _amount - Decay * dt);
            float x = (Mathf.PerlinNoise(_t, 0.37f) - 0.5f) * 2f;
            float y = (Mathf.PerlinNoise(0.71f, _t) - 0.5f) * 2f;
            return new Vector2(x, y) * _amount;
        }

        public static void Clear() => _amount = 0f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _amount = 0f;
            _t = 0f;
        }
    }
}
