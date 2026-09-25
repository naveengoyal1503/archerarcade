namespace ArcherArcade.Logic
{
    /// <summary>Default v1.0 hero archers (GAME_DESIGN §4). Enemy types are in <see cref="EnemyTable"/>.</summary>
    public static class ArcherTable
    {
        public static readonly string[] HeroIds = { "ranger", "fire", "electric", "bomb" };

        public static ArcherDef[] Heroes() => new[] { Ranger(), FireArcher(), ElectricArcher(), BombArcher() };

        public static ArcherDef Hero(string id)
        {
            switch (id)
            {
                case "fire": return FireArcher();
                case "electric": return ElectricArcher();
                case "bomb": return BombArcher();
                default: return Ranger();
            }
        }

        public static ArcherDef Ranger() => new ArcherDef
        {
            Id = "ranger", Name = "Ranger", Element = Element.None, BaseHp = 100, BaseDamage = 25, PreviewBonus = 0.10,
            Ability = AbilityKind.TripleShot, Unlock = ArcherUnlock.Owned
        };

        public static ArcherDef FireArcher() => new ArcherDef
        {
            Id = "fire", Name = "Fire Archer", Element = Element.Fire, BaseHp = 95, BaseDamage = 25, PassiveBurnPerTurn = 6,
            PassiveBurnTurns = 2, Ability = AbilityKind.MeteorArrow, Unlock = new ArcherUnlock(UnlockKind.ClearLevel, 5)
        };

        public static ArcherDef ElectricArcher() => new ArcherDef
        {
            Id = "electric", Name = "Electric Archer", Element = Element.Electric, BaseHp = 100, BaseDamage = 24,
            PassiveChainFraction = 0.5, PassiveChainRadius = 3.0,
            Ability = AbilityKind.StormBolt, Unlock = new ArcherUnlock(UnlockKind.Stars, 20)
        };

        public static ArcherDef BombArcher() => new ArcherDef
        {
            Id = "bomb", Name = "Bomb Archer", Element = Element.Bomb, BaseHp = 105, BaseDamage = 22,
            PassiveSplashDamage = 10, PassiveSplashRadius = 1.2,
            Ability = AbilityKind.ClusterBomb, Unlock = new ArcherUnlock(UnlockKind.ClearLevel, 20)
        };

        /// <summary>A plain opponent (Bandit Archer style) with the given HP and damage.</summary>
        public static ArcherDef Basic(string id, int hp, double damage = 25) => new ArcherDef
        {
            Id = id, Name = id, Element = Element.None, BaseHp = hp, BaseDamage = damage
        };
    }
}
