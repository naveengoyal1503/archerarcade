namespace ArcherArcade.Logic.Meta
{
    /// <summary>
    /// Release switches for staged updates (ROADMAP). One source tree builds any version; content for a later
    /// version stays in the code but is hidden until its switch is on. Only flip a switch when that version ships.
    /// </summary>
    public static class Features
    {
        /// <summary>The version this build ships as.</summary>
        public const string Version = "1.0";

        // v1.0 "Whispering Forest"
        public const bool World1 = true;
        public const bool TwoPlayerSamePhone = true;
        public const bool DailyChallenge = true;
        public const bool TrainingRange = true;
        public const bool Boosters = true;

        // v1.1 "Sunscorch Desert"
        public const bool World2 = false;
        public const bool SurvivalMode = false;
        public const bool DailyMissions = false;
        public const bool IceAndPoisonArchers = false;
        public const bool LaserAndSawTips = false;

        // v1.2 "Frostpeak Mountains"
        public const bool World3 = false;
        public const bool WeeklyChallenge = false;
        public const bool Tournament = false;
        public const bool Replays = false;

        // v1.3 "Ember Volcano" + "Sky Castle"
        public const bool World4And5 = false;
        public const bool BossRush = false;

        // v2.0 Online. Needs the INTERNET permission; keep false until it is really built.
        public const bool Online = false;
    }
}
