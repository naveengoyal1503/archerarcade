namespace ArcherArcade.Logic
{
    /// <summary>
    /// What an ability does to the shot. Either it modifies the tip the player picked (Triple Shot fans it out)
    /// or it replaces the arrow with its own (<see cref="Arrow"/>; no ammo used).
    /// </summary>
    public sealed class AbilityDef
    {
        public AbilityKind Kind;

        /// <summary>Own arrow; null = modify the picked tip.</summary>
        public TipDef Arrow;

        /// <summary>For tip-modifying abilities: arrows in the fan, spread each side, damage share per arrow.</summary>
        public int LaunchCount = 1;
        public double LaunchSpreadDeg;
        public double DamageScale = 1.0;

        /// <summary>
        /// Rain from the sky (Rain of Leaves): this many arrows appear <see cref="RainHeight"/> above the opponent,
        /// <see cref="RainSpacing"/> apart, falling at <see cref="RainSpeed"/>. Angle and power are ignored.
        /// </summary>
        public int RainCount;
        public double RainSpacing = 1.2;
        public double RainHeight = 14.0;
        public double RainSpeed = 8.0;
    }
}
