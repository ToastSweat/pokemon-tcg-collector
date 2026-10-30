using System;
namespace VintageCollector.Domain
{
    public interface IRandomSource { int NextInt(int exclusiveMax); }
    public sealed class SeededRandom : IRandomSource
    {
        private uint state;
        public SeededRandom(uint seed)
        {
            if (seed == 0) throw new ArgumentOutOfRangeException("seed", "xorshift32-v1 requires nonzero seed");
            state = seed;
        }
        private uint NextUInt()
        {
            unchecked { state ^= state << 13; state ^= state >> 17; state ^= state << 5; }
            return state;
        }
        public int NextInt(int exclusiveMax)
        {
            if (exclusiveMax <= 0) throw new ArgumentOutOfRangeException("exclusiveMax");
            // Xorshift32 cycles over 1..2^32-1, not a domain containing zero.
            // Translate to 0..2^32-2 before rejecting the incomplete upper bucket.
            uint bound = (uint)exclusiveMax;
            uint limit = uint.MaxValue - uint.MaxValue % bound;
            uint value;
            do { value = NextUInt() - 1u; } while (value >= limit);
            return (int)(value % bound);
        }
    }
}
