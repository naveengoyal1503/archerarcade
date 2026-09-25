using UnityEngine;

namespace ArcherArcade.Feel
{
    /// <summary>
    /// Vibration patterns (GAME_DESIGN §11): light tick per 10 % power, medium on release, heavy on hit, double on
    /// headshot, success on win, warning on the timer's last seconds. Off when the Haptics setting is off.
    /// Android: VibrationEffect (API 26+) with amplitude, older phones a plain vibrate.
    /// </summary>
    public sealed class HapticsManager
    {
        public bool Enabled = true;
        float _lastTick;

#if UNITY_ANDROID && !UNITY_EDITOR
        AndroidJavaObject _vibrator;
        AndroidJavaClass _effect;
        bool _hasAmplitude;
        bool _ready;

        void Init()
        {
            if (_ready) return;
            _ready = true;
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                {
                    _vibrator = activity.Call<AndroidJavaObject>("getSystemService", "vibrator");
                }
                using (var version = new AndroidJavaClass("android.os.Build$VERSION"))
                {
                    if (version.GetStatic<int>("SDK_INT") >= 26)
                    {
                        _effect = new AndroidJavaClass("android.os.VibrationEffect");
                        _hasAmplitude = _vibrator.Call<bool>("hasAmplitudeControl");
                    }
                }
            }
            catch (System.Exception)
            {
                _vibrator = null;
            }
        }

        void Vibrate(long ms, int amplitude)
        {
            Init();
            if (_vibrator == null) return;
            try
            {
                if (_effect != null)
                {
                    int amp = _hasAmplitude ? Mathf.Clamp(amplitude, 1, 255) : -1;
                    using (AndroidJavaObject e = _effect.CallStatic<AndroidJavaObject>("createOneShot", ms, amp))
                        _vibrator.Call("vibrate", e);
                }
                else
                {
                    _vibrator.Call("vibrate", ms);
                }
            }
            catch (System.Exception)
            {
                _vibrator = null;
            }
        }

        void Pattern(long[] timings, int[] amplitudes)
        {
            Init();
            if (_vibrator == null) return;
            try
            {
                if (_effect != null && _hasAmplitude)
                {
                    using (AndroidJavaObject e = _effect.CallStatic<AndroidJavaObject>("createWaveform", timings, amplitudes, -1))
                        _vibrator.Call("vibrate", e);
                }
                else
                {
                    _vibrator.Call("vibrate", timings, -1);
                }
            }
            catch (System.Exception)
            {
                _vibrator = null;
            }
        }
#else
        void Vibrate(long ms, int amplitude) { }

        void Pattern(long[] timings, int[] amplitudes) { }
#endif

        static readonly long[] DoubleTimings = { 0, 35, 60, 45 };
        static readonly int[] DoubleAmps = { 0, 255, 0, 255 };
        static readonly long[] WinTimings = { 0, 30, 70, 30, 70, 90 };
        static readonly int[] WinAmps = { 0, 140, 0, 180, 0, 255 };
        static readonly long[] WarnTimings = { 0, 25, 90, 25 };
        static readonly int[] WarnAmps = { 0, 120, 0, 120 };

        public void Play(HapticId id)
        {
            if (!Enabled) return;
            switch (id)
            {
                case HapticId.PowerTick:
                    if (Time.unscaledTime - _lastTick < 0.03f) return;
                    _lastTick = Time.unscaledTime;
                    Vibrate(8, 50);
                    break;
                case HapticId.Release: Vibrate(22, 140); break;
                case HapticId.Hit: Vibrate(45, 255); break;
                case HapticId.Headshot: Pattern(DoubleTimings, DoubleAmps); break;
                case HapticId.Win: Pattern(WinTimings, WinAmps); break;
                case HapticId.TimerWarn: Pattern(WarnTimings, WarnAmps); break;
                default: Vibrate(10, 60); break;
            }
        }
    }
}
