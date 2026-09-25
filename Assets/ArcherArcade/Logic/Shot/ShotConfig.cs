namespace ArcherArcade.Logic
{
    /// <summary>
    /// Shot and flight tuning (GAME_DESIGN §3.1–3.2). Defaults are the design's starting values; the Runtime
    /// ScriptableObject config fills these fields so tuning never needs a code change.
    /// </summary>
    [System.Serializable]
    public sealed class ShotConfig
    {
        public double MinSpeed = 12.0;
        public double MaxSpeed = 34.0;
        public double Gravity = 18.0;
        public double WindAccelPerUnit = 0.9;
        public int SimHz = 120;
        public double MaxFlightSeconds = 8.0;
        public double MinAngleDeg = -10.0;
        public double MaxAngleDeg = 80.0;

        /// <summary>Drag distance (dp) for 100 % power, and the dead zone that cancels a shot.</summary>
        public double PowerDragDp = 180.0;
        public double CancelDragDp = 12.0;

        /// <summary>Bow position relative to the archer's feet (forward along facing, up).</summary>
        public double BowForward = 0.35;
        public double BowHeight = 1.25;

        public double StepSeconds => 1.0 / SimHz;
    }
}
