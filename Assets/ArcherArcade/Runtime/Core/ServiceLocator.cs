using ArcherArcade.Feel;
using ArcherArcade.Logic.Meta;
using ArcherArcade.Save;
using ArcherArcade.Settings;
using ArcherArcade.Theme;

namespace ArcherArcade.Core
{
    /// <summary>
    /// The game's services, set up once by <see cref="GameManager"/> (Boot) and kept for the app's lifetime.
    /// </summary>
    public static class ServiceLocator
    {
        public static GameConfig Config { get; internal set; }
        public static SaveSystem Save { get; internal set; }
        public static Profile Profile { get; internal set; }
        public static SettingsManager Settings { get; internal set; }
        public static ThemeManager Theme { get; internal set; }
        public static AudioManager Audio { get; internal set; }
        public static HapticsManager Haptics { get; internal set; }
        public static DisplayRate Display { get; internal set; }

        /// <summary>Call after changing the profile (coins, unlocks, stars…): saves and refreshes live UI.</summary>
        public static void CommitProfile()
        {
            Save?.MarkDirty();
            GameEvents.RaiseCoinsChanged();
            GameEvents.RaiseProfileChanged();
        }

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics()
        {
            Config = null;
            Save = null;
            Profile = null;
            Settings = null;
            Theme = null;
            Audio = null;
            Haptics = null;
            Display = null;
        }
    }
}
