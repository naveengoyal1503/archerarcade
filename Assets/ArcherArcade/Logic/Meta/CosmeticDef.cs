namespace ArcherArcade.Logic.Meta
{
    /// <summary>A skin or arrow trail (cosmetic only, never affects play).</summary>
    public sealed class CosmeticDef
    {
        public string Id;
        public CosmeticKind Kind;

        /// <summary>Archer the skin belongs to (skins only).</summary>
        public string ArcherId;

        public string Name;

        /// <summary>Price in earned coins (Shop items), 0 otherwise.</summary>
        public int Price;

        public CosmeticSource Source;

        /// <summary>Badge that grants it (Badge source) and the tier needed.</summary>
        public string BadgeId;
        public BadgeTier BadgeTier;

        /// <summary>Can also drop from a chest.</summary>
        public bool InChestPool;
    }
}
