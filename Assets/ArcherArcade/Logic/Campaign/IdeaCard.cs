namespace ArcherArcade.Logic.Campaign
{
    /// <summary>
    /// "New!" idea cards (LEVELS.md "New idea card" column) and "New tip!" cards. Each is shown once (flag saved)
    /// and can be re-read in Settings → How to play.
    /// </summary>
    public enum IdeaCard
    {
        Tutorial = 0,
        DuelsAndHp = 1,
        Walls = 2,
        Wind = 3,
        Headshots = 4,
        CrateTowers = 5,
        AppleShot = 6,
        SwingingTargets = 7,
        MovingPlatforms = 8,
        MiniBoss = 9,
        TntCrates = 10,
        ShieldsAndBombs = 11,
        BouncePads = 12,
        Gauntlet = 13,
        Rescue = 14,
        ExplosiveBarrels = 15,
        Healers = 16,
        SplitArrows = 17,
        ShieldBubbles = 18,
        Boss = 19,

        TipFire = 20,
        TipElectric = 21,
        TipSplit = 22,
        TipBomb = 23,
        TipHeavy = 24,
        TipIce = 25,
        TipPoison = 26
    }
}
