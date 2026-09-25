namespace ArcherArcade.Archers
{
    /// <summary>Where the bow string's middle is.</summary>
    public enum RigNockMode
    {
        /// <summary>String at rest (straight).</summary>
        Rest,
        /// <summary>Halfway to the cheek (design "Drawing").</summary>
        Half,
        /// <summary>At the cheek (design "Full draw").</summary>
        Full,
        /// <summary>Gameplay aim: along the exact aim angle, pulled back by <see cref="RigPose.Draw"/>.</summary>
        Aim,
        /// <summary>An absolute point (used when blending poses).</summary>
        Absolute
    }
}
