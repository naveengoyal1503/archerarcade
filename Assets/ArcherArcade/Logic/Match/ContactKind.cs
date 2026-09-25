namespace ArcherArcade.Logic
{
    public enum ContactKind
    {
        None,
        Ground,
        Wall,
        Fighter,
        Prop,

        /// <summary>Bounced off a pad; the flight continues in a child ArrowPath.</summary>
        Bounce,
        OutOfBounds,
        Timeout
    }
}
