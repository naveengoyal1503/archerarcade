namespace ArcherArcade.Logic
{
    /// <summary>What the computer archer will do this turn.</summary>
    public struct AiDecision
    {
        public ShotInput Input;

        /// <summary>Seconds to show the aim animation before releasing (already slowed by Ice).</summary>
        public double ThinkSeconds;

        /// <summary>Zone it aimed at (HitZone.None when it fell back to a blind lob).</summary>
        public HitZone AimZone;

        /// <summary>The exact shot before noise (for "it's adjusting its aim" hints and tests).</summary>
        public ShotInput PerfectInput;
    }
}
