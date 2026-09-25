namespace ArcherArcade.Logic.Meta
{
    /// <summary>
    /// Campaign level that unlocks each arrow tip (LEVELS.md: Fire L3 · Electric L7 · Split L9 · Bomb L11 ·
    /// Heavy L13 · Ice L15 · Poison L18). Normal is always owned (0).
    /// </summary>
    public static class TipUnlocks
    {
        static readonly int[] Levels = { 0, 3, 7, 9, 11, 13, 15, 18 };

        public static int LevelFor(ArrowTip tip) => Levels[(int)tip];

        public static bool IsUnlocked(ArrowTip tip, int highestLevelCleared) => highestLevelCleared >= Levels[(int)tip];

        /// <summary>Tip unlocked by clearing exactly this level, or null.</summary>
        public static ArrowTip? UnlockedBy(int level)
        {
            for (int i = 1; i < Levels.Length; i++)
            {
                if (Levels[i] == level) return (ArrowTip)i;
            }
            return null;
        }
    }
}
