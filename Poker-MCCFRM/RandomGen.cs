using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PokerMCCFRM
{
    /// <summary>
    /// Static class representing a single instance of the Random class
    /// </summary>
    public static class RandomGen
    {
        private static readonly ThreadLocal<Random> Random =
            new ThreadLocal<Random>(() => new Random(Interlocked.Increment(ref seed)));

        private static int seed = Environment.TickCount;

        public static int Next(int minValueInclusive, int maxValueExclusive)
        {
            return Random.Value.Next(minValueInclusive, maxValueExclusive);
        }
        public static double NextDouble()
        {
            return Random.Value.NextDouble();
        }
    }
}