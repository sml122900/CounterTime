using System;

namespace CounterTime.Core
{
    /// All randomness is injected so every test is deterministic.
    public interface IRandomSource
    {
        float NextFloat();                              // [0,1)
        int NextInt(int minInclusive, int maxExclusive);
    }

    public sealed class SystemRandomSource : IRandomSource
    {
        readonly Random _r;
        public SystemRandomSource(int seed) => _r = new Random(seed);
        public float NextFloat() => (float)_r.NextDouble();
        public int NextInt(int minInclusive, int maxExclusive) => _r.Next(minInclusive, maxExclusive);
    }

    /// Test double: always returns the configured values, clamped into range.
    public sealed class FixedRandomSource : IRandomSource
    {
        readonly float _f;
        readonly int _i;
        public FixedRandomSource(float f = 0f, int i = 0) { _f = f; _i = i; }
        public float NextFloat() => _f;
        public int NextInt(int minInclusive, int maxExclusive) =>
            Math.Clamp(_i, minInclusive, maxExclusive - 1);
    }
}
