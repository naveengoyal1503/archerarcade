namespace ArcherArcade.Logic
{
    /// <summary>
    /// One archer or enemy type: level-1 stats, element and passive (GAME_DESIGN §4). Abilities are added in
    /// Phase 7. Filled by the Runtime archer config; defaults in <see cref="ArcherTable"/>.
    /// </summary>
    public sealed class ArcherDef
    {
        public string Id = "ranger";
        public Element Element = Element.None;
        public int BaseHp = 100;
        public double BaseDamage = 25.0;

        /// <summary>Ranger: trajectory preview longer by this share (0.10 = +10 %).</summary>
        public double PreviewBonus;

        /// <summary>Fire Archer: every hit adds burn.</summary>
        public int PassiveBurnPerTurn;
        public int PassiveBurnTurns;

        /// <summary>Electric Archer: hits chain to one other target within the radius.</summary>
        public double PassiveChainFraction;
        public double PassiveChainRadius;

        /// <summary>Bomb Archer: arrows explode on impact.</summary>
        public double PassiveSplashDamage;
        public double PassiveSplashRadius;
    }
}
