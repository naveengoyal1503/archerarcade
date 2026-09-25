namespace ArcherArcade.Logic.Save
{
    /// <summary>Daily Challenge streak (days are UTC day numbers).</summary>
    public sealed class DailySave
    {
        public int LastSolvedDay = -1;
        public int Streak;
        public int BestStreak;

        /// <summary>7-day streak chests already given.</summary>
        public int StreakChests;
    }
}
