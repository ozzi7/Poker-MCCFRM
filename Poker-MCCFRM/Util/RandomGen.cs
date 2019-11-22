using Haus.Math;
using System;
using System.Threading;

namespace Poker_MCCFRM
{
    /// <summary>
    /// Static class representing a single instance of the Random class
    /// </summary>
    public static class RandomGen
    {
        private static readonly ThreadLocal<XorShiftRandom> Random =
            new ThreadLocal<XorShiftRandom>(() => new XorShiftRandom((ulong)Interlocked.Increment(ref seed)));

        private static int seed = Environment.TickCount;

        public static int Next(int minValueInclusive, int maxValueExclusive)
        {
            // can doubles be rounded up potentially? then this would need a math.min(_ , maxvalue -1 ) 
            return minValueInclusive + (int)(Random.Value.NextDouble()*(maxValueExclusive - minValueInclusive));
        }
        public static double NextDouble()
        {
            return Random.Value.NextDouble();
        }
        public static float NextFloat()
        {
            return (float)Random.Value.NextDouble();
        }
    }
}