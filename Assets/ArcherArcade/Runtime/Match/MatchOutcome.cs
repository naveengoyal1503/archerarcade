using System.Collections.Generic;
using ArcherArcade.Logic;
using ArcherArcade.Logic.Meta;

namespace ArcherArcade.Match
{
    /// <summary>Everything the result screen shows after a match (design `result`, states 07 / 08, 2-Player final).</summary>
    public sealed class MatchOutcome
    {
        public GameMode Mode;
        public bool Won;
        public string Title;
        public string Subtitle;
        public bool ShowStars;
        public int Stars;
        public int PreviousStars;
        /// <summary>Stat tiles: value and label (design resStats).</summary>
        public readonly List<string[]> Stats = new List<string[]>();
        public int Coins;
        public CoinBreakdown Breakdown;
        public bool FirstClear;
        public string Tip;
        public int ChestReady;
        public ArrowTip? NewTip;
        public readonly List<string> NewArchers = new List<string>();
        public readonly List<BadgeUpdate> Badges = new List<BadgeUpdate>();
        public bool OfferAssist;
        public bool HasNextLevel;
        public int NextLevel;
        /// <summary>2-Player: a round (not the series) just ended.</summary>
        public bool PvpRoundOnly;
        public string WinnerName;
        public string Score;
        /// <summary>Survival: waves cleared this run and whether it beat the best.</summary>
        public int Waves;
        public bool NewBest;
        public int StreakDays;
        public string CosmeticId;
    }
}
