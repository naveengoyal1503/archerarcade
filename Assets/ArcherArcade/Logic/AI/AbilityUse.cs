namespace ArcherArcade.Logic
{
    /// <summary>When a computer archer uses its ability (GAME_DESIGN §6.2). Abilities arrive in Phase 7.</summary>
    public enum AbilityUse
    {
        Never,

        /// <summary>When charged, with AiProfile.AbilityChance.</summary>
        Sometimes,

        WhenCharged,

        /// <summary>Whenever charged, plus boss moves.</summary>
        Always
    }
}
