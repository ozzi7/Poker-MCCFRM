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
            CalculateInformationAbstraction();
            Train();
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

            long StrategyInterval = Math.Max(1, 1000 / Global.NOF_THREADS); ; // bb rounds before updating player strategy (recursive through tree) 10k
            long PruneThreshold = 20000000 / Global.NOF_THREADS; // bb rounds after this time we stop checking all actions, 200 minutes
            long LCFRThreshold = 20000000 / Global.NOF_THREADS; // bb rounds when to stop discounting old regrets, no clue what it should be
            long DiscountInterval = 1000000 / Global.NOF_THREADS; // bb rounds, discount values periodically but not every round, 10 minutes
            long SaveToDiskInterval = 1000000 / Global.NOF_THREADS;
            long testGamesInterval = 100000 / Global.NOF_THREADS;

            long sharedLoopCounter = 0;

            LoadFromFile();
            LoadFromFile_d();

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
                            Console.WriteLine("Training steps " + sharedLoopCounter);
                        }

                        if (t % testGamesInterval == 0 && index == 0) // implement progress bar later
                        {
                            trainer.PrintStartingHandsChart();
                            trainer.PrintStatistics(sharedLoopCounter);

                            Console.WriteLine("Sample games (against self)");
                            for (int z = 0; z < 20; z++)
                            {
                                trainer.PlayOneGame();
                            }

                            //Console.WriteLine("Sample games (against baseline)");
                            //float mainScore = 0.0f;
                            //for (int x = 0; x < 100; x++) // 100 games not statistically significant
                            //{
                            //    if (x < 20)
                            //    {
                            //        mainScore += trainer.PlayOneGame_d(x % 2, true);
                            //    }
                            //    mainScore += trainer.PlayOneGame_d(x % 2, false);
                            //}
                            //WritePlotStatistics((mainScore / 10000) / Global.BB);
                            //Console.WriteLine("BBs per hand: {0}", (mainScore / 10000) / Global.BB);

                            Console.WriteLine("Iterations per second: {0}", 1000 * sharedLoopCounter / (stopwatch.ElapsedMilliseconds + 1));
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
                        }
                        if (t % SaveToDiskInterval == 0 && index == 0) // allow only one thread to do saving
                        {
                            Console.WriteLine("Saving nodeMap to disk disabled!");
                            //SaveToFile();
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
        private static void WritePlotStatistics(float bbWins)
        {
            using (StreamWriter file = new StreamWriter("progress.txt", true))
            {
                file.WriteLine(Math.Round(bbWins, 2));
            }
        }
        private static void SaveToFile_d()
        {
            Console.WriteLine("Saving dictionary to file {0}", "nodeMap_d.txt");

            using FileStream fs = File.OpenWrite("nodeMap.txt");
            using BinaryWriter writer = new BinaryWriter(fs);
            foreach (var pair in Global.nodeMapBaseline)
            {
                byte[] bytes = Encoding.ASCII.GetBytes(pair.Key);

                writer.Write(bytes.Length);
                writer.Write(bytes);

                bytes = SerializeToBytes(pair.Value);
                writer.Write(bytes.Length);
                writer.Write(bytes);
            }
        }
        private static void LoadFromFile_d()
        {
            if (!File.Exists("nodeMap_d.txt"))
                return;
            Console.WriteLine("Loading nodes from file nodeMap_d.txt...");
            using FileStream fs = File.OpenRead("nodeMap_d.txt");
            using BinaryReader reader = new BinaryReader(fs);
            Global.nodeMapBaseline = new ConcurrentDictionary<string, Infoset>();

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
                    Global.nodeMapBaseline.TryAdd(keyString, infoset);
                }
            }
            catch (EndOfStreamException e)
            {
                return;
            }
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

                //bytes = SerializeToBytes(pair.Value);

                writer.Write(pair.Value.actionCounter.Length);
                for (int i = 0; i < pair.Value.actionCounter.Length; i++)
                    writer.Write(pair.Value.actionCounter[i]);

                for (int i = 0; i < pair.Value.regret.Length; i++)
                    writer.Write(pair.Value.regret[i]);
            }
        }
        private static void LoadFromFile()
        {
            if (!File.Exists("nodeMap.txt"))
                return;
            Console.WriteLine("Loading nodes from file nodeMap.txt...");
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

                    Infoset infoset = new Infoset(valueLength);
                    for (int i = 0; i < valueLength; i++)
                    {
                        infoset.actionCounter[i] = reader.ReadInt32();
                    }
                    for (int i = 0; i < valueLength; i++)
                    {
                        infoset.regret[i] = reader.ReadInt32();
                    }
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
