namespace ArcherArcade.Logic.Campaign
{
    /// <summary>How a level attempt ended (Victory / Defeat screens, rewards, badges, stats).</summary>
    public struct LevelResult
    {
        public int LevelNumber;
        public bool Won;
        public int Stars;

        /// <summary>Player's turns (duel goals) and arrows fired (target goals).</summary>
        public int PlayerTurns;
        public int ArrowsUsed;

        public double HpFraction;
        public int Headshots;
        public int Hits;
        public bool Assist;
        public bool TookNoDamage;
    }
}
