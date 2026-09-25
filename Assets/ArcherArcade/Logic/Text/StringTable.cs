using System.Collections.Generic;

namespace ArcherArcade.Logic.Text
{
    /// <summary>All UI strings: key → { English, Hinglish }. Placeholders {0}, {1}… must match in both.</summary>
    public static class StringTable
    {
        public static readonly Dictionary<string, string[]> Entries = new Dictionary<string, string[]>
        {
            { "app_name", new[] { "Archer Arcade", "Archer Arcade" } },
        };
    }
}
