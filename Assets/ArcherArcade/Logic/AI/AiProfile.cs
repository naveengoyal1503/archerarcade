namespace ArcherArcade.Logic
{
    /// <summary>
    /// Difficulty profile of the computer opponent (GAME_DESIGN §6.2). The Runtime AI config fills these fields.
    /// Noise values are tuned so the fairness tests land in the middle of the §6.3 hit-rate ranges.
    /// </summary>
    public sealed class AiProfile
    {
        public string Id = "medium";

        /// <summary>Aim noise (one standard deviation) before any learning.</summary>
        public double AngleSigmaDeg = 5.5;
        public double PowerSigma = 0.095;

        /// <summary>Each miss multiplies the remaining noise by (1 − this).</summary>
        public double LearnPerMiss = 0.35;

        /// <summary>Chance to aim at the head instead of the body.</summary>
        public double HeadAimChance = 0.15;

        public AbilityUse AbilityUse = AbilityUse.Sometimes;
        public double AbilityChance = 0.5;

        /// <summary>Human-like pause with a visible aim animation before the shot.</summary>
        public double ThinkMinSeconds = 0.8;
        public double ThinkMaxSeconds = 1.2;

        public static AiProfile Easy() => new AiProfile
        {
            Id = "easy", AngleSigmaDeg = 12.0, PowerSigma = 0.20, LearnPerMiss = 0.15, HeadAimChance = 0.0,
            AbilityUse = AbilityUse.Never, AbilityChance = 0.0, ThinkMinSeconds = 1.0, ThinkMaxSeconds = 1.4
        };

        public static AiProfile Medium() => new AiProfile();

        public static AiProfile Hard() => new AiProfile
        {
            Id = "hard", AngleSigmaDeg = 2.8, PowerSigma = 0.05, LearnPerMiss = 0.50, HeadAimChance = 0.60,
            AbilityUse = AbilityUse.WhenCharged, AbilityChance = 1.0, ThinkMinSeconds = 0.6, ThinkMaxSeconds = 1.0
        };

        public static AiProfile Boss() => new AiProfile
        {
            Id = "boss", AngleSigmaDeg = 1.8, PowerSigma = 0.035, LearnPerMiss = 0.50, HeadAimChance = 0.70,
            AbilityUse = AbilityUse.Always, AbilityChance = 1.0, ThinkMinSeconds = 0.6, ThinkMaxSeconds = 0.9
        };
    }
}
