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

        /// <summary>Special tips picked in the Loadout (Normal is always available).</summary>
        public ArrowTip[] Tips = new ArrowTip[0];

        /// <summary>2-Player handicap (0.7–1.3) and Extra Heart booster scale max HP.</summary>
        public double HpScale = 1.0;
        public int BonusHp;
    }
}
