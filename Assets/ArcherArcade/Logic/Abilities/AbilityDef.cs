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
    }
}
