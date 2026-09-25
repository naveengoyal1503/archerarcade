namespace ArcherArcade.Logic.Save
{
    /// <summary>Progress on one campaign level. Replays keep the best stars and turns.</summary>
    public sealed class LevelSave
    {
        public bool Cleared;
        public int Stars;

        /// <summary>Best (fewest) turns for duel goals / arrows for target goals; 0 = none yet.</summary>
        public int BestTurns;
        public int BestArrows;

        public int Attempts;

        /// <summary>Losses since the last win (tips after 2, Assist after 3).</summary>
        public int LossesInARow;
    }
}
