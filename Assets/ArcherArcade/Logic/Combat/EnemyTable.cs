namespace ArcherArcade.Logic
{
    /// <summary>
    /// v1.0 enemy types (GAME_DESIGN §4.1). HP here is the type's default; campaign levels may override it
    /// (e.g. Scout Pip 60 HP on level 2). Difficulty comes from the AI profile, not from these stats.
    /// </summary>
    public static class EnemyTable
    {
        public static ArcherDef BanditArcher(int hp = 100) => new ArcherDef { Id = "bandit", BaseHp = hp, BaseDamage = 25 };

        /// <summary>Flatter, faster shots (less arc), low HP.</summary>
        public static ArcherDef CrossbowScout(int hp = 70) => new ArcherDef
        {
            Id = "crossbow_scout", BaseHp = hp, BaseDamage = 22, ShotSpeedScale = 1.2, ShotGravityScale = 0.8
        };

        /// <summary>Big wooden shield in front blocks body shots; Heavy / Bomb knock it down.</summary>
        public static ArcherDef ShieldBearer(int hp = 100) => new ArcherDef
        {
            Id = "shield_bearer", BaseHp = hp, BaseDamage = 25, CarriesShield = true
        };

        /// <summary>Two aimed shots per turn at lower damage.</summary>
        public static ArcherDef TwinShooter(int hp = 80) => new ArcherDef
        {
            Id = "twin_shooter", BaseHp = hp, BaseDamage = 15, ShotsPerTurn = 2
        };

        /// <summary>Heals itself 10 HP every 2 turns.</summary>
        public static ArcherDef HealerDruid(int hp = 90) => new ArcherDef
        {
            Id = "healer_druid", BaseHp = hp, BaseDamage = 22, HealAmount = 10, HealEveryTurns = 2
        };

        /// <summary>Stands high on a crate tower (placement comes from the level); knock the tower down to drop it.</summary>
        public static ArcherDef TowerSniper(int hp = 90) => new ArcherDef { Id = "tower_sniper", BaseHp = hp, BaseDamage = 25 };

        /// <summary>Ranger Bramble: Triple Shot and a shield bubble every 3 turns (Electric pops it).</summary>
        public static ArcherDef RangerBramble(int hp = 120) => new ArcherDef
        {
            Id = "ranger_bramble", BaseHp = hp, BaseDamage = 25, Ability = AbilityKind.TripleShot, BubbleEveryTurns = 3
        };
    }
}
