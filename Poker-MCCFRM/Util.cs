using System;
using System.Collections.Generic;
using System.Text;

namespace Poker_MCCFRM
{
    public static class Util
    {
        /// <summary>
        /// Returns the indices in the data for the thread to work on (inclusive, exclusive)
        /// The first thread will get additional work (they are started first)
        /// </summary>
        /// <param name="dataCount"></param>
        /// <param name="threadCount"></param>
        /// <param name="threadIndex"></param>
        /// <returns></returns>
        public static Tuple<int,int> GetWorkItemsIndices(int dataCount, int threadCount, int threadIndex)
        {
            int minItems = dataCount / threadCount;
            int extraItems = dataCount % threadCount;

            if(threadIndex < extraItems)
            {
                return Tuple.Create((minItems + 1) * threadIndex, (minItems + 1) * (threadIndex + 1));
            }
            return Tuple.Create((minItems * threadIndex) + extraItems, (minItems * threadIndex) + extraItems + minItems);
        }
    }
}
