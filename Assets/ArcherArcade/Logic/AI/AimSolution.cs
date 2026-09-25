namespace ArcherArcade.Logic
{
    public struct AimSolution
    {
        public double AngleDeg;
        public double Power;

        /// <summary>Seconds until the arrow reaches the target's x.</summary>
        public double FlightTime;

        public ShotInput ToInput(ArrowTip tip = ArrowTip.Normal) => new ShotInput(AngleDeg, Power, tip);
    }
}
