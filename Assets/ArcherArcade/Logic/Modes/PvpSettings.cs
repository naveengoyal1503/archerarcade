namespace ArcherArcade.Logic.Modes
{
    /// <summary>2-Player setup screen values (GAME_DESIGN §7, SCREEN_INVENTORY #21).</summary>
    public sealed class PvpSettings
    {
        public string Name1 = "P1";
        public string Name2 = "P2";
        public string Archer1 = "ranger";
        public string Archer2 = "fire";
        public string Skin1;
        public string Skin2;

        /// <summary>Arena id or "random".</summary>
        public string Arena = "mirror";

        /// <summary>1, 3 or 5.</summary>
        public int BestOf = 3;

        public bool WindOn = true;

        /// <summary>Trajectory preview share (default 30 %).</summary>
        public double PreviewShare = 0.30;

        /// <summary>HP handicap per player, 0.7–1.3.</summary>
        public double Handicap1 = 1.0;
        public double Handicap2 = 1.0;

        public const double MinHandicap = 0.7;
        public const double MaxHandicap = 1.3;
        public const int MaxNameLength = 12;
    }
}
