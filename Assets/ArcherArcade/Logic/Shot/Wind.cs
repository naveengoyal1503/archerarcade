namespace ArcherArcade.Logic
{
    public static class Wind
    {
        public const int MaxBars = 5;

        /// <summary>
        /// Rolls a signed wind value: strength uniform in [min, max] bars, random direction.
        /// Always draws two numbers so the random stream does not depend on the result.
        /// </summary>
        public static int Roll(WindRange range, Rng rng)
        {
            int min = range.Min < 0 ? 0 : (range.Min > MaxBars ? MaxBars : range.Min);
            int max = range.Max < min ? min : (range.Max > MaxBars ? MaxBars : range.Max);
            int strength = rng.RangeInclusive(min, max);
            bool left = rng.NextInt(2) == 0;
            return left ? -strength : strength;
        }
    }
}
