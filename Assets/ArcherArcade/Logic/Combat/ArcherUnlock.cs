namespace ArcherArcade.Logic
{
    /// <summary>How an archer is unlocked — always by playing, never by paying (GAME_DESIGN §4).</summary>
    public struct ArcherUnlock
    {
        public UnlockKind Kind;
        public int Value;

        public ArcherUnlock(UnlockKind kind, int value)
        {
            Kind = kind;
            Value = value;
        }

        public static ArcherUnlock Owned => new ArcherUnlock(UnlockKind.Owned, 0);

        public bool IsMet(int highestLevelCleared, int totalStars)
        {
            switch (Kind)
            {
                case UnlockKind.ClearLevel: return highestLevelCleared >= Value;
                case UnlockKind.Stars: return totalStars >= Value;
                default: return true;
            }
        }
    }
}
