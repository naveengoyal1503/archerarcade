using System.Collections.Generic;

namespace ArcherArcade.Logic.Text
{
    /// <summary>
    /// UI text lookup. Every key has English and Hinglish (EditMode test: both present, same {n} placeholders,
    /// every key the Runtime uses exists). Unknown keys show the key itself so a missing string is visible, not blank.
    /// </summary>
    public static class Strings
    {
        public static string Get(string key, Lang lang)
        {
            if (key == null) return "";
            if (!StringTable.Entries.TryGetValue(key, out string[] pair)) return key;
            string s = pair[(int)lang];
            return string.IsNullOrEmpty(s) ? pair[0] : s;
        }

        public static bool Has(string key) => key != null && StringTable.Entries.ContainsKey(key);

        public static IEnumerable<string> Keys => StringTable.Entries.Keys;

        public static string English(string key) => Get(key, Lang.English);
    }
}
