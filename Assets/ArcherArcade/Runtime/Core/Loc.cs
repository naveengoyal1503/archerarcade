using System.Globalization;
using ArcherArcade.Logic.Text;

namespace ArcherArcade.Core
{
    /// <summary>
    /// UI text in the chosen language (English / Hinglish) from the Logic strings table. Screens call
    /// <c>Loc.T("key")</c> / <c>Loc.F("key", args)</c> and refresh on <see cref="GameEvents.LanguageChanged"/>.
    /// Proper nouns (archer and enemy names, "Archer Arcade") stay the same in every language.
    /// </summary>
    public static class Loc
    {
        public static Lang Current { get; private set; } = Lang.English;

        public static void Set(Lang lang)
        {
            if (lang == Current) return;
            Current = lang;
            GameEvents.RaiseLanguageChanged();
        }

        public static string T(string key) => Strings.Get(key, Current);

        public static string F(string key, params object[] args) => string.Format(CultureInfo.InvariantCulture, T(key), args);

        /// <summary>Thousands separators like the design ("1,860").</summary>
        public static string N(long n) => n.ToString("N0", CultureInfo.InvariantCulture);

        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        static void ResetStatics() => Current = Lang.English;
    }
}
