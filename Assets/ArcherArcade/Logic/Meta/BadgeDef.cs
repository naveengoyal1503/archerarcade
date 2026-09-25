namespace ArcherArcade.Logic.Meta
{
    /// <summary>One badge with bronze / silver / gold thresholds on a stat (GAME_DESIGN §9).</summary>
    public sealed class BadgeDef
    {
        public string Id;
        public string Name;

        /// <summary>Icon name in the icon font (see Runtime Icons).</summary>
        public string Icon;

        /// <summary>One line: what it counts.</summary>
        public string Description;

        public StatKey Stat;
        public long Bronze;
        public long Silver;
        public long Gold;

        /// <summary>Warden Slayer: gold is "3★ on the boss" instead of a count.</summary>
        public bool GoldIsBossThreeStars;

        public long Threshold(BadgeTier tier)
        {
            switch (tier)
            {
                case BadgeTier.Bronze: return Bronze;
                case BadgeTier.Silver: return Silver;
                case BadgeTier.Gold: return Gold;
                default: return 0;
            }
        }
    }
}
