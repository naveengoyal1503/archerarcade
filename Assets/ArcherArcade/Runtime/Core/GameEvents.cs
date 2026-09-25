using System;

namespace ArcherArcade.Core
{
    /// <summary>Game-wide notifications for UI that shows live values (coin pills, theme, language, settings).</summary>
    public static class GameEvents
    {
        public static event Action CoinsChanged;
        public static event Action ThemeChanged;
        public static event Action LanguageChanged;
        public static event Action SettingsChanged;
        public static event Action ProfileChanged;

        public static void RaiseCoinsChanged() => CoinsChanged?.Invoke();
        public static void RaiseThemeChanged() => ThemeChanged?.Invoke();
        public static void RaiseLanguageChanged() => LanguageChanged?.Invoke();
        public static void RaiseSettingsChanged() => SettingsChanged?.Invoke();
        public static void RaiseProfileChanged() => ProfileChanged?.Invoke();

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            CoinsChanged = null;
            ThemeChanged = null;
            LanguageChanged = null;
            SettingsChanged = null;
            ProfileChanged = null;
        }
    }
}
