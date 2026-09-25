namespace ArcherArcade.Logic.Campaign
{
    /// <summary>
    /// Stars (GAME_DESIGN §7.2). Duel / Gauntlet / Boss: a win is ★; each of "≥ 50 % HP left" and "won within par
    /// turns" adds a star. Targets / Apple / Trick / Rescue: ★ complete · ★★ within par + 2 arrows · ★★★ within par.
    /// Assist never gives ★★★.
    /// </summary>
    public static class StarRules
    {
        public const double HpForSecondStar = 0.5;
        public const int ArrowsOverParForTwoStars = 2;

        public static int ForDuel(bool won, double hpFraction, int playerTurns, int par, bool assist)
        {
            if (!won) return 0;
            int stars = 1;
            if (hpFraction >= HpForSecondStar) stars++;
            if (playerTurns <= par) stars++;
            return Cap(stars, assist);
        }

        public static int ForShots(bool complete, int arrowsUsed, int par, bool assist)
        {
            if (!complete) return 0;
            int stars = arrowsUsed <= par ? 3 : (arrowsUsed <= par + ArrowsOverParForTwoStars ? 2 : 1);
            return Cap(stars, assist);
        }

        public static int For(LevelDef level, bool won, double hpFraction, int playerTurns, int arrowsUsed, bool assist)
        {
            return level.IsDuelGoal
                ? ForDuel(won, hpFraction, playerTurns, level.Par, assist)
                : ForShots(won, arrowsUsed, level.Par, assist);
        }

        static int Cap(int stars, bool assist) => assist && stars > 2 ? 2 : stars;
    }
}
