namespace ArcherArcade.Logic.Campaign
{
    /// <summary>
    /// Losing flow (LEVELS.md): "Try again" is free and unlimited; a tip after 2 losses; after 3 losses the level
    /// offers Assist (+15 % trajectory preview) for that attempt, which still allows ★★ but not ★★★.
    /// </summary>
    public static class LossHelp
    {
        public const int LossesForTip = 2;
        public const int LossesForAssist = 3;
        public const double AssistPreviewBonus = 0.15;

        public static bool ShowTip(int lossesInARow) => lossesInARow >= LossesForTip;

        public static bool OfferAssist(int lossesInARow) => lossesInARow >= LossesForAssist;
    }
}
