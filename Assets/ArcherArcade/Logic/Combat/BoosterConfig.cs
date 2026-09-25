namespace ArcherArcade.Logic
{
    /// <summary>Booster effects (GAME_DESIGN §5.1). Prices live in the economy config (Phase 11).</summary>
    [System.Serializable]
    public sealed class BoosterConfig
    {
        public int ExtraHeartHp = 25;

        /// <summary>Multi Arrow: the first shot fans out like Triple Shot.</summary>
        public int MultiArrowCount = 3;
        public double MultiArrowSpreadDeg = 4.0;
        public double MultiArrowDamageScale = 0.6;
    }
}
