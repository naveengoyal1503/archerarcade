using System.Collections.Generic;

namespace ArcherArcade.Logic.Meta
{
    /// <summary>Everything a finished campaign level gives (Victory screen, unlock modals, chest).</summary>
    public sealed class CampaignOutcome
    {
        public bool Won;
        public bool FirstClear;
        public int Stars;
        public int PreviousBestStars;
        public CoinBreakdown Coins;

        /// <summary>Tip unlocked by this clear (show the "New tip!" card), or null.</summary>
        public ArrowTip? NewTip;

        /// <summary>Archers unlocked by this result (by clearing a level or reaching a star total).</summary>
        public readonly List<string> NewArchers = new List<string>();

        /// <summary>Chest now waiting to be opened (1–4), 0 = none.</summary>
        public int ChestReady;

        public readonly List<BadgeUpdate> Badges = new List<BadgeUpdate>();

        /// <summary>Losses in a row on this level after this attempt (drives the tip / Assist offers).</summary>
        public int LossesInARow;
    }
}
