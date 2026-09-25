namespace ArcherArcade.Logic
{
    /// <summary>Wind strength range in gauge bars (0–5), rolled at the start of every turn (LEVELS.md "Wind").</summary>
    public struct WindRange
    {
        public int Min;
        public int Max;

        public WindRange(int min, int max)
        {
            Min = min;
            Max = max;
        }

        public static WindRange Calm => new WindRange(0, 0);
        public static WindRange Fixed(int strength) => new WindRange(strength, strength);
    }
}
