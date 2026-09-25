namespace ArcherArcade.Logic.Meta
{
    public enum GameMode
    {
        Campaign = 0,
        QuickDuel = 1,
        TwoPlayer = 2,
        Daily = 3,
        Training = 4,
        /// <summary>Endless waves (added 2026-09-25; save arrays grew from 5 to 6 entries, old saves load fine).</summary>
        Survival = 5
    }
}
