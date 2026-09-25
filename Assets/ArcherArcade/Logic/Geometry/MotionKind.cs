namespace ArcherArcade.Logic
{
    public enum MotionKind
    {
        None,

        /// <summary>Pendulum around a pivot (swinging targets).</summary>
        Swing,

        /// <summary>Back and forth between two offsets (moving targets).</summary>
        PingPong
    }
}
