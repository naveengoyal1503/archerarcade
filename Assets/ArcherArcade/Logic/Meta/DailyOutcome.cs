using System.Collections.Generic;

namespace ArcherArcade.Logic.Meta
{
    public sealed class DailyOutcome
    {
        public int Coins;
        public int Streak;
        public bool StreakChest;
        public string CosmeticId;
        public readonly List<BadgeUpdate> Badges = new List<BadgeUpdate>();
    }
}
