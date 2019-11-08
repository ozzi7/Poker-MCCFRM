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
    public class Global
    {
        public static int NOF_THREADS = 24;

        public static HandIndexer flopIndexer;
        public static HandIndexer privIndexer;
        public static HandIndexer privFlopIndexer;
        public static HandIndexer privFlopTurnIndexer;
        public static HandIndexer privFlopTurnIndexer2;
        public static HandIndexer privFlopTurnRiver;
        public static HandIndexer privFlopTurnRiver2;
        public static HandIndexer fiveCardIndexer;
        public static HandIndexer sixCardIndexer;
        public static HandIndexer sevenCardIndexer;
        public static HandIndexer showdownIndexer;
        public static Evaluator handEvaluator;

        public static ConcurrentDictionary<string, Infoset> nodeMap = new ConcurrentDictionary<string, Infoset>();
        public static ThreadLocal<Deck> Deck = new ThreadLocal<Deck>(() => new Deck());
        public static List<float> raises = new List<float>() { 2f, 3f, 5.0f };
        public static int buyIn = 200;
        public static int nofPlayers = 2;
        public static int C = -300000000;
        public static int regretFloor = -310000000;

        public static int BB = 2;
        public static int SB = 1;
    }
}
