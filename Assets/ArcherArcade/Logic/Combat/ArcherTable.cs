namespace ArcherArcade.Logic
{
    /// <summary>Default v1.0 hero archers (GAME_DESIGN §4). Enemy stats come from level data.</summary>
    public static class ArcherTable
    {
        public static ArcherDef Ranger() => new ArcherDef
        {
            Id = "ranger", Element = Element.None, BaseHp = 100, BaseDamage = 25, PreviewBonus = 0.10
        };

        public static ArcherDef FireArcher() => new ArcherDef
        {
            Id = "fire", Element = Element.Fire, BaseHp = 95, BaseDamage = 25, PassiveBurnPerTurn = 6, PassiveBurnTurns = 2
        };

        public static ArcherDef ElectricArcher() => new ArcherDef
        {
            Id = "electric", Element = Element.Electric, BaseHp = 100, BaseDamage = 24,
            PassiveChainFraction = 0.5, PassiveChainRadius = 3.0
        };

        public static ArcherDef BombArcher() => new ArcherDef
        {
            Id = "bomb", Element = Element.Bomb, BaseHp = 105, BaseDamage = 22,
            PassiveSplashDamage = 10, PassiveSplashRadius = 1.2
        };

        /// <summary>A plain opponent (Bandit Archer style) with the given HP and damage.</summary>
        public static ArcherDef Basic(string id, int hp, double damage = 25) => new ArcherDef
        {
            Id = id, Element = Element.None, BaseHp = hp, BaseDamage = damage
        };
    }
}
