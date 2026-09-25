namespace ArcherArcade.Logic.Meta
{
    /// <summary>What the player takes into a match (Loadout screen): archer + cosmetics, special tips, boosters.</summary>
    public sealed class Loadout
    {
        public string ArcherId = "ranger";
        public string SkinId = "default";
        public string TrailId = "classic";

        /// <summary>Special tips (Normal is always carried and never listed here).</summary>
        public ArrowTip[] Tips = new ArrowTip[0];

        public BoosterKind[] Boosters = new BoosterKind[0];
    }
}
