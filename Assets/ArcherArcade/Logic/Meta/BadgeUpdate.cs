namespace ArcherArcade.Logic.Meta
{
    /// <summary>A badge that reached a new tier (toast + coins + maybe a cosmetic).</summary>
    public struct BadgeUpdate
    {
        public BadgeDef Badge;
        public BadgeTier Tier;
        public int Coins;

        /// <summary>Cosmetic granted by this tier, or null.</summary>
        public string CosmeticId;
    }
}
