namespace ArcherArcade.Logic
{
    /// <summary>Turn rules (GAME_DESIGN §3.5).</summary>
    public sealed class MatchConfig
    {
        public double TurnSeconds = 12.0;

        /// <summary>A stunned turn never gets less than this.</summary>
        public double MinTurnSeconds = 4.0;

        /// <summary>Last seconds that tick + pulse (read by the HUD and haptics).</summary>
        public double TimerWarnSeconds = 3.0;
    }
}
