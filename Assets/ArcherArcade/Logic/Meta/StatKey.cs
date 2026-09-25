namespace ArcherArcade.Logic.Meta
{
    /// <summary>Counters that badges and the Stats screen read (GAME_DESIGN §9, Stats screen).</summary>
    public enum StatKey
    {
        Shots = 0,
        Hits = 1,
        Headshots = 2,
        TargetHits = 3,
        FlawlessWins = 4,
        StrongWindHits = 5,
        LongShotHits = 6,
        BurnDamage = 7,
        ElectricChains = 8,
        PropsDestroyed = 9,
        TripleShotHits = 10,
        WardenWins = 11,
        QuickDraws = 12,
        Comebacks = 13,
        PvpMatches = 14,
        PvpSeriesFinished = 15,
        ApplesHit = 16,
        LightningStrikes = 17,
        TowersToppled = 18,
        PlaySeconds = 19,

        // Derived from the save when badges are evaluated (not counted per shot).
        WorldOneStars = 100,
        DailyStreakBest = 101,
        ArchersOwned = 102,
        ArcherLevels = 103,
        CoinsEarned = 104
    }
}
