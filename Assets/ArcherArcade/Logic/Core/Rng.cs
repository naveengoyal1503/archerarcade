namespace ArcherArcade.Logic
{
    /// <summary>
    /// PCG32 (O'Neill, pcg-random.org, XSH RR 64/32). The only random source for gameplay and level data.
    /// Uses integer math plus IEEE +,-,*,/ only, so the same seed gives the same numbers on every device.
    /// </summary>
    public sealed class Rng
    {
        const ulong Multiplier = 6364136223846793005UL;
        const double InvTwo32 = 1.0 / 4294967296.0;

        ulong _state;
        ulong _inc;

        public Rng(ulong seed, ulong sequence = 54UL)
        {
            Seed(seed, sequence);
        }

        /// <summary>Raw state, for save files and determinism hashes.</summary>
        public ulong State => _state;
        public ulong Increment => _inc;

        public static Rng FromState(ulong state, ulong increment)
        {
            var r = new Rng(0UL);
            r._state = state;
            r._inc = increment | 1UL;
            return r;
        }

        public Rng Clone() => FromState(_state, _inc);

        public void Seed(ulong seed, ulong sequence)
        {
            _state = 0UL;
            _inc = (sequence << 1) | 1UL;
            NextUInt();
            _state += seed;
            NextUInt();
        }

        public uint NextUInt()
        {
            ulong old = _state;
            _state = unchecked(old * Multiplier + _inc);
            uint xorShifted = (uint)(((old >> 18) ^ old) >> 27);
            int rot = (int)(old >> 59);
            return (xorShifted >> rot) | (xorShifted << ((-rot) & 31));
        }

        /// <summary>Uniform in [0, 1).</summary>
        public double NextDouble() => NextUInt() * InvTwo32;

        /// <summary>Unbiased integer in [0, bound).</summary>
        public int NextInt(int bound)
        {
            if (bound <= 1) return 0;
            uint b = (uint)bound;
            uint threshold = unchecked((uint)(-(int)b)) % b;
            while (true)
            {
                uint r = NextUInt();
                if (r >= threshold) return (int)(r % b);
            }
        }

        /// <summary>Integer in [minInclusive, maxInclusive].</summary>
        public int RangeInclusive(int minInclusive, int maxInclusive)
        {
            if (maxInclusive <= minInclusive) return minInclusive;
            return minInclusive + NextInt(maxInclusive - minInclusive + 1);
        }

        /// <summary>Uniform double in [min, max).</summary>
        public double Range(double min, double max) => min + (max - min) * NextDouble();

        public bool Chance(double probability) => NextDouble() < probability;

        /// <summary>
        /// Approximately standard-normal value (Irwin–Hall: sum of 12 uniforms − 6). Avoids log/sin so it stays
        /// bit-identical across platforms. Range is limited to ±6, which suits aim noise.
        /// </summary>
        public double NextGaussian()
        {
            double sum = 0.0;
            for (int i = 0; i < 12; i++) sum += NextDouble();
            return sum - 6.0;
        }
    }
}
