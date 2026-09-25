namespace ArcherArcade.Archers
{
    /// <summary>How the back (string) arm is posed.</summary>
    public enum RigArmMode
    {
        /// <summary>Fixed upper-arm / forearm angles.</summary>
        Angles,
        /// <summary>Wrist reaches for a point relative to the back shoulder (design "Release").</summary>
        ShoulderOffset,
        /// <summary>Wrist holds the nock (drawing the string).</summary>
        Nock,
        /// <summary>Wrist reaches for an absolute point (used when blending poses).</summary>
        Absolute
    }
}
