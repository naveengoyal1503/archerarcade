using ArcherArcade.Core;
using UnityEngine;

namespace ArcherArcade.Feel
{
    /// <summary>
    /// The one owner of <see cref="Time.timeScale"/> in a match: pause (0), hit-stop (a frozen instant on impact) and
    /// slow-mo (0.25× knockout, 0.1 s bomb). All timers run on unscaled time. "Reduce motion" turns slow-mo off.
    /// </summary>
    public sealed class TimeScaleDriver : MonoBehaviour
    {
        static TimeScaleDriver _instance;
        static bool _paused;
        static float _stopLeft, _slowLeft, _slowScale = 1f;

        public static bool Paused
        {
            get => _paused;
            set
            {
                _paused = value;
                Apply();
            }
        }

        public static bool SlowMoActive => _slowLeft > 0f;

        public static void Ensure(Transform parent)
        {
            if (_instance) return;
            var go = new GameObject("TimeScale");
            go.transform.SetParent(parent, false);
            _instance = go.AddComponent<TimeScaleDriver>();
        }

        /// <summary>Freeze the action for <paramref name="ms"/> milliseconds (longest request wins).</summary>
        public static void HitStop(float ms)
        {
            float s = ms / 1000f;
            if (s > _stopLeft) _stopLeft = s;
            Apply();
        }

        /// <summary>Run at <paramref name="scale"/> for <paramref name="realSeconds"/> of real time.</summary>
        public static void SlowMo(float scale, float realSeconds)
        {
            if (ServiceLocator.Settings != null && ServiceLocator.Settings.ReduceMotion) return;
            if (realSeconds > _slowLeft || scale < _slowScale)
            {
                _slowLeft = Mathf.Max(_slowLeft, realSeconds);
                _slowScale = Mathf.Min(_slowScale, scale);
            }
            Apply();
        }

        public static void ResetAll()
        {
            _paused = false;
            _stopLeft = 0f;
            _slowLeft = 0f;
            _slowScale = 1f;
            Time.timeScale = 1f;
        }

        static void Apply()
        {
            Time.timeScale = _paused ? 0f : _stopLeft > 0f ? 0f : _slowLeft > 0f ? _slowScale : 1f;
        }

        void Update()
        {
            if (_paused) return;
            float dt = Time.unscaledDeltaTime;
            if (_stopLeft > 0f) _stopLeft -= dt;
            else if (_slowLeft > 0f)
            {
                _slowLeft -= dt;
                if (_slowLeft <= 0f) _slowScale = 1f;
            }
            Apply();
        }

        void OnDestroy()
        {
            if (_instance != this) return;
            _instance = null;
            ResetAll();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            _instance = null;
            _paused = false;
            _stopLeft = _slowLeft = 0f;
            _slowScale = 1f;
        }
    }
}
