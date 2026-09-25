using ArcherArcade.Core;
using UnityEngine;

namespace ArcherArcade.Theme
{
    /// <summary>
    /// Light (candy) / dark (purple + gold) theme. "Follow system" reads Android's night mode. Screens read
    /// <see cref="Palette"/> and rebuild their colors on <see cref="GameEvents.ThemeChanged"/>.
    /// </summary>
    public sealed class ThemeManager
    {
        public ThemeMode Mode { get; private set; } = ThemeMode.FollowSystem;
        public bool IsDark { get; private set; }
        public Palette Palette => IsDark ? Palette.Dark : Palette.Light;

        public static ThemeMode Parse(string code)
        {
            switch (code)
            {
                case "light": return ThemeMode.Light;
                case "dark": return ThemeMode.Dark;
                default: return ThemeMode.FollowSystem;
            }
        }

        public static string Code(ThemeMode mode) => mode == ThemeMode.Light ? "light" : mode == ThemeMode.Dark ? "dark" : "system";

        public void SetMode(ThemeMode mode)
        {
            Mode = mode;
            Refresh();
        }

        /// <summary>Re-reads the system theme (called when the app comes back to the front).</summary>
        public void Refresh()
        {
            bool dark = Mode == ThemeMode.Dark || (Mode == ThemeMode.FollowSystem && SystemPrefersDark());
            if (dark == IsDark) return;
            IsDark = dark;
            GameEvents.RaiseThemeChanged();
        }

        public static bool SystemPrefersDark()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            try
            {
                using (var player = new AndroidJavaClass("com.unity3d.player.UnityPlayer"))
                using (AndroidJavaObject activity = player.GetStatic<AndroidJavaObject>("currentActivity"))
                using (AndroidJavaObject resources = activity.Call<AndroidJavaObject>("getResources"))
                using (AndroidJavaObject config = resources.Call<AndroidJavaObject>("getConfiguration"))
                {
                    int uiMode = config.Get<int>("uiMode");
                    const int nightMask = 0x30, nightYes = 0x20;
                    return (uiMode & nightMask) == nightYes;
                }
            }
            catch (System.Exception)
            {
                return false;
            }
#else
            return false;
#endif
        }
    }
}
