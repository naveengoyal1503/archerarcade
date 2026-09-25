namespace ArcherArcade.Logic
{
    /// <summary>
    /// v1.0 enemy types (GAME_DESIGN §4.1) and the named World 1 characters (LEVELS.md). HP here is the default;
    /// campaign levels may override it (e.g. Scout Pip 60 HP on level 2). Difficulty comes from the AI profile.
    /// </summary>
    public static class EnemyTable
    {
        /// <summary>Enemy by id (level data refers to enemies by id). Unknown ids give a Bandit Archer.</summary>
        public static ArcherDef ById(string id, int hp = 0)
        {
            ArcherDef d;
            switch (id)
            {
                case "scout_pip": d = BanditArcher(); d.Id = id; d.Name = "Scout Pip"; break;
                case "hunter_moss": d = BanditArcher(); d.Id = id; d.Name = "Hunter Moss"; break;
                case "crossbow_scout": d = CrossbowScout(); break;
                case "shield_bearer": d = ShieldBearer(); break;
                case "twig_twin": d = TwinShooter(); d.Id = id; d.Name = "Twig Twin"; break;
                case "healer_druid": d = HealerDruid(); break;
                case "tower_sniper": d = TowerSniper(); break;
                case "ranger_bramble": d = RangerBramble(); break;
                case "captain_thorn": d = CaptainThorn(); break;
                case "forest_warden": d = ForestWarden(); break;
                default: d = BanditArcher(); break;
            }
            if (hp > 0) d.BaseHp = hp;
            return d;
        }

        public static ArcherDef BanditArcher(int hp = 100) => new ArcherDef
        {
            Id = "bandit", Name = "Bandit Archer", BaseHp = hp, BaseDamage = 25
        };

        /// <summary>Flatter, faster shots (less arc), low HP.</summary>
        public static ArcherDef CrossbowScout(int hp = 70) => new ArcherDef
        {
            Id = "crossbow_scout", Name = "Crossbow Scout", BaseHp = hp, BaseDamage = 22, ShotSpeedScale = 1.2, ShotGravityScale = 0.8
        };

        /// <summary>Big wooden shield in front blocks body shots; Heavy / Bomb knock it down.</summary>
        public static ArcherDef ShieldBearer(int hp = 100) => new ArcherDef
        {
            Id = "shield_bearer", Name = "Shield Bearer", BaseHp = hp, BaseDamage = 25, CarriesShield = true
        };

        /// <summary>Two aimed shots per turn at lower damage.</summary>
        public static ArcherDef TwinShooter(int hp = 80) => new ArcherDef
        {
            Id = "twin_shooter", Name = "Twin Shooter", BaseHp = hp, BaseDamage = 15, ShotsPerTurn = 2
        };

        /// <summary>Heals itself 10 HP every 2 turns.</summary>
        public static ArcherDef HealerDruid(int hp = 90) => new ArcherDef
        {
            Id = "healer_druid", Name = "Healer Druid", BaseHp = hp, BaseDamage = 22, HealAmount = 10, HealEveryTurns = 2
        };

        /// <summary>Stands high on a crate tower (placement comes from the level); knock the tower down to drop it.</summary>
        public static ArcherDef TowerSniper(int hp = 90) => new ArcherDef
        {
            Id = "tower_sniper", Name = "Tower Sniper", BaseHp = hp, BaseDamage = 25
        };

        /// <summary>Ranger Bramble: Triple Shot and a shield bubble every 3 turns (Electric pops it).</summary>
        public static ArcherDef RangerBramble(int hp = 120) => new ArcherDef
        {
            Id = "ranger_bramble", Name = "Ranger Bramble", BaseHp = hp, BaseDamage = 25, Ability = AbilityKind.TripleShot,
            BubbleEveryTurns = 3
        };

        /// <summary>Mini-boss (level 10): Thorn Volley.</summary>
        public static ArcherDef CaptainThorn(int hp = 160) => new ArcherDef
        {
            Id = "captain_thorn", Name = "Captain Thorn", BaseHp = hp, BaseDamage = 25, Ability = AbilityKind.ThornVolley,
            BodyScale = 1.1
        };

        /// <summary>
        /// Boss (level 20, GAME_DESIGN §6.4): 250 HP, big, two rotating shields, weak-spot knot ×2.5, Vine Wall every
        /// 3 turns, Rain of Leaves, Enrage at 30 % HP (shoots twice per turn).
        /// </summary>
        public static ArcherDef ForestWarden(int hp = 250) => new ArcherDef
        {
            Id = "forest_warden", Name = "Forest Warden", BaseHp = hp, BaseDamage = 25, Ability = AbilityKind.RainOfLeaves,
            BodyScale = 1.35, HasWeakSpot = true, RotatingShields = true, VineWallEveryTurns = 3, EnrageBelow = 0.3
        };
    }
}
