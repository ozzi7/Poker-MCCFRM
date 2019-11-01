using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PokerAI
{
    public static class Global
    {
        public static int NOF_THREADS = 4;

        public static ConcurrentDictionary<string, Infoset> nodeMap = new ConcurrentDictionary<string, Infoset>();
        public static ThreadLocal<Deck> Deck = new ThreadLocal<Deck>(() => new Deck());
        public static List<float> raises = new List<float>() { 2f, 5f, 20.0f };
        public static int buyIn = 200;
        public static int nofPlayers = 2;
        public static int C = -300000000;
        public static int regretFloor = -310000000;

        public static int BB = 2;
        public static int SB = 1;
    }
}
