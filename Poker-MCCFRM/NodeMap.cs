//using System;
//using System.Collections.Concurrent;
//using System.Collections.Generic;
//using System.Text;

//namespace Poker_MCCFRM
//{
//    /// <summary>
//    /// Creates a constant lookup table for the more often used states of the game
//    /// The other part of the states are in the dictionary
//    /// </summary>
//    public static class NodeMap
//    {
//        public static Infoset[] lookupTable;
//        public static ConcurrentDictionary<string, Infoset> nodeMap = 
//            new ConcurrentDictionary<string, Infoset>(Global.NOF_THREADS, 1000000);

//        /// <summary>
//        /// Pre-allocate the tables
//        /// </summary>
//        public void Initialize()
//        {
//            lookupTable = new Infoset[10]();
//        }
//    }
//}
