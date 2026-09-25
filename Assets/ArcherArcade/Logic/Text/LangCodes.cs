namespace ArcherArcade.Logic.Text
{
    /// <summary>Save codes for <see cref="Lang"/> (SettingsData.Language: "en" / "hinglish").</summary>
    public static class LangCodes
    {
        public const string English = "en";
        public const string Hinglish = "hinglish";

        public static Lang Parse(string code) => code == Hinglish ? Lang.Hinglish : Lang.English;

        public static string Code(Lang lang) => lang == Lang.Hinglish ? Hinglish : English;
    }
}
