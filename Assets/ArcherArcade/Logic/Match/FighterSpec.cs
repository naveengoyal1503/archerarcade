namespace ArcherArcade.Logic
{
    /// <summary>How one archer enters a match.</summary>
    public sealed class FighterSpec
    {
        public ArcherDef Def;
        public int Side;
        public int Level = 1;
        public Vec2 Feet;

        /// <summary>+1 faces right (player side), −1 faces left.</summary>
        public int Facing = 1;

        /// <summary>Special tips picked in the Loadout (Normal is always available unless NormalArrows is false).</summary>
        public ArrowTip[] Tips = new ArrowTip[0];

        /// <summary>False for "Split only" / "Heavy only" daily twists.</summary>
        public bool NormalArrows = true;

        /// <summary>2-Player handicap (0.7–1.3) and Extra Heart booster scale max HP.</summary>
        public double HpScale = 1.0;
        public int BonusHp;

        /// <summary>Stands on this prop (moving platform), −1 = on the ground at Feet.</summary>
        public int StandOnProp = -1;

        /// <summary>Stands on top of this crate tower (Tower Sniper), −1 = none. Drops when crates are knocked off.</summary>
        public int StandOnTower = -1;

        /// <summary>Boosters picked in the Loadout (one of each at most; duplicates are ignored).</summary>
        public BoosterKind[] Boosters = new BoosterKind[0];
    }
}
