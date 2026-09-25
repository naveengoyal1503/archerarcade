namespace ArcherArcade.Logic
{
    /// <summary>Prop tuning (GAME_DESIGN §10).</summary>
    public sealed class PropConfig
    {
        public int CrateHits = 2;

        public int BarrelDamage = 30;
        public double BarrelRadius = 2.0;

        public int TntDamage = 35;
        public double TntRadius = 2.5;

        /// <summary>Bounce pads keep this share of the arrow's speed; after MaxBounces a pad just stops the arrow.</summary>
        public double BounceSpeedKeep = 0.8;
        public int MaxBounces = 3;

        /// <summary>Bomb-tip knockback distance, and how close to an island edge a pushed archer can stand.</summary>
        public double KnockbackMeters = 0.8;
        public double EdgeMargin = 0.4;

        /// <summary>A knocked-down shield comes back up this many turns after the knock (the shooter gets a free turn).</summary>
        public int ShieldDownTurns = 2;
    }
}
