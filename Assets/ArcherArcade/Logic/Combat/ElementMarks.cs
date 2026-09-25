namespace ArcherArcade.Logic
{
    /// <summary>Element marks that stay on an archer until the match ends (GAME_DESIGN §3.3.2).</summary>
    [System.Flags]
    public enum ElementMarks
    {
        None = 0,
        Soot = 1,
        Sparks = 2,
        Frost = 4,
        Poisoned = 8,
        Bruised = 16
    }
}
