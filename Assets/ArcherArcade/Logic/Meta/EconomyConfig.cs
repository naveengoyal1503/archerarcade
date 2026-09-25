using ArcherArcade.Logic.Campaign;

namespace ArcherArcade.Logic.Meta
{
    /// <summary>
    /// Every coin number in the game (GAME_DESIGN §8, §5.1). Coins are only ever earned, never bought. The UI shows
    /// these values; nothing else may hard-code a price or a reward.
    /// </summary>
    public sealed class EconomyConfig
    {
        // Sources
        public int FirstClearEasy = 20;
        public int FirstClearMedium = 30;
        public int FirstClearHard = 45;
        public int FirstClearMiniBoss = 80;
        public int FirstClearBoss = 150;
        public double ReplayShare = 0.5;
        public int CoinsPerNewStar = 10;
        public int CoinsPerHeadshot = 3;
        public int HeadshotCoinsCap = 15;
        public int QuickDuelWinEasy = 10;
        public int QuickDuelWinMedium = 15;
        public int QuickDuelWinHard = 25;
        public int QuickDuelLoss = 3;
        public int DailyChallenge = 50;
        public int DailyStreakDays = 7;
        public int DailyStreakChestCoins = 300;
        public string DailyStreakChestTrail = "trail_rainbow";
        public int ChestCoinsMin = 80;
        public int ChestCoinsMax = 140;
        public double ChestCosmeticChance = 0.35;
        public int BadgeBronze = 30;
        public int BadgeSilver = 60;
        public int BadgeGold = 120;

        // Spending
        /// <summary>Upgrade cost to reach level 2, 3, … 10.</summary>
        public int[] UpgradeCosts = { 100, 150, 220, 300, 400, 520, 660, 820, 1000 };

        public int BoosterShieldBubble = 120;
        public int BoosterMultiArrow = 100;
        public int BoosterIronHelmet = 80;
        public int BoosterExtraHeart = 150;

        public int FirstClear(LevelTier tier)
        {
            switch (tier)
            {
                case LevelTier.Medium: return FirstClearMedium;
                case LevelTier.Hard: return FirstClearHard;
                case LevelTier.MiniBoss: return FirstClearMiniBoss;
                case LevelTier.Boss: return FirstClearBoss;
                default: return FirstClearEasy;
            }
        }

        /// <summary>Coins to go from <paramref name="level"/> to level + 1; −1 when already at max.</summary>
        public int UpgradeCost(int level)
        {
            int i = level - 1;
            return i >= 0 && i < UpgradeCosts.Length ? UpgradeCosts[i] : -1;
        }

        public int MaxArcherLevel => UpgradeCosts.Length + 1;

        public int BoosterCost(BoosterKind kind)
        {
            switch (kind)
            {
                case BoosterKind.ShieldBubble: return BoosterShieldBubble;
                case BoosterKind.MultiArrow: return BoosterMultiArrow;
                case BoosterKind.IronHelmet: return BoosterIronHelmet;
                default: return BoosterExtraHeart;
            }
        }

        public int QuickDuelWin(string difficulty)
        {
            switch (difficulty)
            {
                case "medium": return QuickDuelWinMedium;
                case "hard": return QuickDuelWinHard;
                default: return QuickDuelWinEasy;
            }
        }

        public int BadgeReward(BadgeTier tier)
        {
            switch (tier)
            {
                case BadgeTier.Bronze: return BadgeBronze;
                case BadgeTier.Silver: return BadgeSilver;
                case BadgeTier.Gold: return BadgeGold;
                default: return 0;
            }
        }
    }
}
