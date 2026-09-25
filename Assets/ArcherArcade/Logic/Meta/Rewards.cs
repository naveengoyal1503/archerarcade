using ArcherArcade.Logic.Campaign;

namespace ArcherArcade.Logic.Meta
{
    /// <summary>Coin rewards (GAME_DESIGN §8). Pure functions; the caller adds the total to the wallet.</summary>
    public static class Rewards
    {
        /// <summary>
        /// Campaign win: first clear pays the level's coins, a replay 50 %; +10 per star earned for the first time;
        /// +3 per headshot (max 15 per match). A loss pays nothing in the campaign (Quick Duel pays for losses).
        /// </summary>
        public static CoinBreakdown Campaign(LevelDef level, bool won, bool firstClear, int previousBestStars, int stars,
            int headshots, EconomyConfig cfg)
        {
            var c = new CoinBreakdown();
            if (!won) return c;
            c.Clear = firstClear ? level.RewardCoins : DetMath.RoundToInt(level.RewardCoins * cfg.ReplayShare);
            int newStars = stars - previousBestStars;
            c.Stars = newStars > 0 ? newStars * cfg.CoinsPerNewStar : 0;
            c.Headshots = HeadshotCoins(headshots, cfg);
            return c;
        }

        public static CoinBreakdown QuickDuel(string difficulty, bool won, int headshots, EconomyConfig cfg)
        {
            return new CoinBreakdown
            {
                Clear = won ? cfg.QuickDuelWin(difficulty) : cfg.QuickDuelLoss,
                Headshots = HeadshotCoins(headshots, cfg)
            };
        }

        /// <summary>Survival run: coins per wave cleared (capped) plus headshot coins.</summary>
        public static CoinBreakdown Survival(int wavesCleared, int headshots, EconomyConfig cfg)
        {
            int waves = wavesCleared * cfg.SurvivalCoinsPerWave;
            return new CoinBreakdown
            {
                Clear = waves > cfg.SurvivalCoinsCap ? cfg.SurvivalCoinsCap : (waves < 0 ? 0 : waves),
                Headshots = HeadshotCoins(headshots, cfg)
            };
        }

        /// <summary>2-Player gives badge progress only, never coins (so it can't be farmed).</summary>
        public static CoinBreakdown TwoPlayer() => new CoinBreakdown();

        public static int HeadshotCoins(int headshots, EconomyConfig cfg)
        {
            int c = headshots * cfg.CoinsPerHeadshot;
            return c > cfg.HeadshotCoinsCap ? cfg.HeadshotCoinsCap : (c < 0 ? 0 : c);
        }
    }
}
