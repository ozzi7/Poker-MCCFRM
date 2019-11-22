using System;
using System.Collections.Concurrent;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using SnapCall;

namespace Poker_MCCFRM
{
    static class Program
    {
        static void Main(string[] args)
        {
            CreateIndexers();
            Global.handEvaluator = new Evaluator();

            //temp
            //LoadFromFile();
            //CalculateInformationAbstraction();
            //Trainer t = new Trainer(0);
            //while(true)
            //    t.PlayOneGame();

            CalculateInformationAbstraction();
            Train();
        }
        private static void handEvaluatorTest(HandIndexer indexer)
        {
            int totalCards = 0;
            for (int i = 0; i < indexer.rounds; ++i)
                totalCards += indexer.cardsPerRound[i];

            int[] cards = new int[totalCards];
            int[] cards2 = new int[totalCards];

            for (long n = 0; n < 10000000L; ++n)
            {
                long deadCardMask = 0;
                for (int i = 0; i < totalCards; ++i)
                {
                    do
                    {
                        cards[i] = (int)(RandomGen.NextDouble() * HandIndexer.CARDS);
                    } while (((1L << (int)cards[i]) & deadCardMask) != 0);
                    deadCardMask |= (1L << (int)cards[i]);
                }
                long index = indexer.indexLast(cards);

                indexer.unindex(indexer.rounds - 1, index, cards2);
                long index2 = indexer.indexLast(cards2);
                Debug.Assert(index2 == index);
            }
        }

        private static void CreateIndexers()
        {
            Console.Write("Creating 2 card index... ");
            Global.indexer_2 = new HandIndexer(new int[1] { 2 });
            Console.WriteLine(Global.indexer_2.roundSize[0] + " non-isomorphic hands found");

            Console.Write("Creating 2 & 3 card index... ");
            Global.indexer_2_3 = new HandIndexer(new int[2] { 2, 3 });
            Console.WriteLine(Global.indexer_2_3.roundSize[1] + " non-isomorphic hands found");

            Console.Write("Creating 2 & 4 card index... ");
            Global.indexer_2_4 = new HandIndexer(new int[2] { 2, 4 });
            Console.WriteLine(Global.indexer_2_4.roundSize[1] + " non-isomorphic hands found");

            Console.Write("Creating 2 & 5 card index... ");
            Global.indexer_2_5 = new HandIndexer(new int[2] { 2, 5 });
            Console.WriteLine(Global.indexer_2_5.roundSize[1] + " non-isomorphic hands found");

            Console.Write("Creating 2 & 5 & 2 card index... ");
            Global.indexer_2_5_2 = new HandIndexer(new int[3] { 2, 5, 2 });
            Console.WriteLine(Global.indexer_2_5_2.roundSize[2] + " non-isomorphic hands found");

            Console.Write("Creating 2 & 3 & 1 card index... ");
            Global.indexer_2_3_1 = new HandIndexer(new int[3] { 2, 3, 1 });
            Console.WriteLine(Global.indexer_2_3_1.roundSize[2] + " non-isomorphic hands found");

            Console.Write("Creating 2 & 3 & 1 & 1 card index... ");
            Global.indexer_2_3_1_1 = new HandIndexer(new int[4] { 2, 3, 1, 1 });
            Console.WriteLine(Global.indexer_2_3_1_1.roundSize[3] + " non-isomorphic hands found");

            Console.Write("Creating 2 & 5 & 2 card index... ");
            Global.indexer_2_5_2 = new HandIndexer(new int[3] { 2, 5, 2 });
            Console.WriteLine(Global.indexer_2_5_2.roundSize[2] + " non-isomorphic hands found");
        }
        private static void CalculateInformationAbstraction()
        {
            Console.WriteLine("Calculating information abstractions... ");

            OCHSTable.Init();
            EMDTable.Init();
        }
        private static void Train()
        {
            Console.WriteLine("Starting Monte Carlo Counterfactual Regret Minimization (MCCFRM)...");

            long StrategyInterval = Math.Max(1,100/Global.NOF_THREADS); ; // bb rounds before updating player strategy (recursive through tree) 10k
            long PruneThreshold = 50000000/Global.NOF_THREADS; // bb rounds after this time we stop checking all actions, 200 minutes
            long LCFRThreshold = 10000000/Global.NOF_THREADS; // bb rounds when to stop discounting old regrets, no clue what it should be
            long DiscountInterval = 1000000/Global.NOF_THREADS; // bb rounds, discount values periodically but not every round, 10 minutes
            long SaveToDiskInterval = 5000000/Global.NOF_THREADS; // not used currently during trial runs

            long sharedLoopCounter = 0;

            LoadFromFile();

            Stopwatch stopwatch = new Stopwatch();
            stopwatch.Start();
            Parallel.For(0, Global.NOF_THREADS,
                index =>
                {
                    Trainer trainer = new Trainer(index);

                    for (int t = 1; ; t++) // bb rounds
                    {
                        if (t % 1000 == 0)
                        {
                            Interlocked.Add(ref sharedLoopCounter, 1000);
                        }
                        if (t % 10000 == 1 && index == 0) // implement progress bar later
                        {
                            Console.WriteLine("Training steps " + sharedLoopCounter);
                            trainer.PrintStartingHandsChart();
                            trainer.PrintStatistics(sharedLoopCounter);
                            for (int z = 0; z < 20; z++)
                            {
                                trainer.PlayOneGame();
                            }
                            Console.WriteLine("Iterations per second: {0}",1000 * sharedLoopCounter / (stopwatch.ElapsedMilliseconds+1));
                            Console.WriteLine();
                        }
                        for (int traverser = 0; traverser < Global.nofPlayers; traverser++) // traverser 
                        {
                            if (t % StrategyInterval == 0 && index == 0)
                            {
                                trainer.UpdateStrategy(traverser);
                            }
                            if (t > PruneThreshold)
                            {
                                float q = RandomGen.NextFloat();
                                if (q < 0.05)
                                {
                                    trainer.TraverseMCCFR(traverser, t);
                                }
                                else
                                {
                                    trainer.TraverseMCCFRPruned(traverser);
                                }
                            }
                            else
                            {
                                trainer.TraverseMCCFR(traverser, t);
                            }
                            if (t % SaveToDiskInterval == 0 && index == 0) // allow only one thread to do saving
                            {
                                SaveToFile();
                            }
                        }
                        // discount all infosets (for all players)
                        if (t < LCFRThreshold && t % DiscountInterval == 0 && index == 0) // allow only one thread to do discounting
                        {
                            float d = ((float)t / DiscountInterval) / ((float)t / DiscountInterval + 1);
                            trainer.DiscountInfosets(d);
                        }
                    }
                });
            
        }
        private static void SaveToFile()
        {
            Console.WriteLine("Saving dictionary to file {0}", "nodeMap.txt");

            using FileStream fs = File.OpenWrite("nodeMap.txt");
            using BinaryWriter writer = new BinaryWriter(fs);
            foreach (var pair in Global.nodeMap)
            {
                byte[] bytes = Encoding.ASCII.GetBytes(pair.Key);

                writer.Write(bytes.Length);
                writer.Write(bytes);

                bytes = SerializeToBytes(pair.Value);
                writer.Write(bytes.Length);
                writer.Write(bytes);
            }
        }
        private static void LoadFromFile()
        {
            if (!File.Exists("nodeMap.txt"))
                return;
            using FileStream fs = File.OpenRead("nodeMap.txt");
            using BinaryReader reader = new BinaryReader(fs);
            Global.nodeMap = new ConcurrentDictionary<string, Infoset>();

            try
            {
                while (true)
                {
                    int keyLength = reader.ReadInt32();
                    byte[] key = reader.ReadBytes(keyLength);
                    string keyString = Encoding.ASCII.GetString(key);
                    int valueLength = reader.ReadInt32();
                    byte[] value = reader.ReadBytes(valueLength);
                    Infoset infoset = Deserialize(value);
                    Global.nodeMap.TryAdd(keyString, infoset);
                }
            }
            catch (EndOfStreamException e)
            {
                return;
            }
        }
        private static byte[] SerializeToBytes<T>(T item)
        {
            var formatter = new BinaryFormatter();
            using var stream = new MemoryStream();
            formatter.Serialize(stream, item);
            stream.Seek(0, SeekOrigin.Begin);
            return stream.ToArray();
        }
        private static Infoset Deserialize(this byte[] byteArray)
        {
            if (byteArray == null)
            {
                return null;
            }
            using var memStream = new MemoryStream();
            var binForm = new BinaryFormatter();
            memStream.Write(byteArray, 0, byteArray.Length);
            memStream.Seek(0, SeekOrigin.Begin);
            Infoset obj = (Infoset)binForm.Deserialize(memStream);
            return obj;
        }
    }
}
