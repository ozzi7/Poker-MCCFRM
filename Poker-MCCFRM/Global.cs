using SnapCall;
using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Poker_MCCFRM
{
    public static class Global
    {
        // adjust threads to cpu
        public const int NOF_THREADS = 24;

        // currently each round has the same raise values available
        // the values are multiples of the current pot
        // if new elements are added then the code must be adjusted in other places currently TODO
        public static List<float> raises = new List<float>() { 2f, 3f, 5.0f }; 
        public const int buyIn = 200;
        public const int nofPlayers = 2; // !=2 not tested yet TODO
        public const int C = -300000000;
        public const int regretFloor = -310000000;

        public const int BB = 2;
        public const int SB = 1;

        // information abstraction parameters, currently this would be a
        // 169 - 200 - 200 - 200 abstraction, where the river is bucketed using OCHS and the turn and flop using EMD
        public const int nofRiverBuckets = 200;
        public const int nofTurnBuckets = 200;
        public const int nofFlopBuckets = 200;
        // 100k or even 1 million shouldn't take too much time compared to the rest of the information abstraction
        public const int nofMCSimsPerPreflopHand = 100000;
        // for the river, determines the river histogram size (in theory could be up to 169 but will be very slow)
        public const int nofOpponentClusters = 8;
        // this is used to create the nofOpponentClusters, it can be increased with little time penalty because the clustering for 169 hands is very fast
        public const int preflopHistogramSize = 50;
        
        // dont change
        public static HandIndexer indexer_2;
        public static HandIndexer indexer_2_3;
        public static HandIndexer indexer_2_4;
        public static HandIndexer indexer_2_5;
        public static HandIndexer indexer_2_5_2;
        public static Evaluator handEvaluator;

        public static ConcurrentDictionary<string, Infoset> nodeMap = new ConcurrentDictionary<string, Infoset>();
        public static ThreadLocal<Deck> Deck = new ThreadLocal<Deck>(() => new Deck());
    }
}
