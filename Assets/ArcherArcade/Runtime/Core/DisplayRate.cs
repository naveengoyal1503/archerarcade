using UnityEngine;

namespace ArcherArcade.Core
{
    /// <summary>
    /// Frame rate policy (CLAUDE.md): 60 fps by default, 90/120 on high-refresh screens (the display's best mode),
    /// 60 with "Battery saver", and 60 while Android reports thermal throttling (checked every few seconds).
    /// </summary>
    public sealed class DisplayRate
    {
        const float ThermalCheckSeconds = 8f;
        /// <summary>PowerManager.THERMAL_STATUS_MODERATE: the phone is getting warm and starts to throttle.</summary>
        const int ThermalModerate = 2;

        bool _batterySaver;
        bool _hot;
        float _nextCheck;

        public int MaxRefresh { get; private set; } = 60;
        public int Target { get; private set; } = 60;
        public bool Throttled => _hot;

        public DisplayRate()
        {
            double best = 60.0;
            foreach (Resolution r in Screen.resolutions)
            {
                double hz = r.refreshRateRatio.value;
                if (hz > best) best = hz;
            }
            double current = Screen.currentResolution.refreshRateRatio.value;
            if (current > best) best = current;
            MaxRefresh = Mathf.Clamp(Mathf.RoundToInt((float)best), 60, 120);
        }

        public void SetBatterySaver(bool on)
        {
            _batterySaver = on;
            Apply();
        }

        public void Tick(float unscaledTime)
        {
            if (unscaledTime < _nextCheck) return;
            _nextCheck = unscaledTime + ThermalCheckSeconds;
            bool hot = ThermalStatus() >= ThermalModerate;
            if (hot == _hot) return;
            _hot = hot;
            Apply();
        }

        void Apply()
        {
            int target = _batterySaver || _hot ? 60 : MaxRefresh;
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = target;
            Target = target;
        }

        static int ThermalStatus()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    if (version.GetStatic<int>("SDK_INT") < 29) return 0;
                }
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject power = activity.Call<AndroidJavaObject>("getSystemService", "power"))
                {
                    return power.Call<int>("getCurrentThermalStatus");
                }
            }
            catch (System.Exception)
            {
                return 0;
            }
#else
            return 0;
#endif
        }
    }
}
