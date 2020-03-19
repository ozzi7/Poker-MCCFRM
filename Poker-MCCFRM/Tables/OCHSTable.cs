using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;

namespace Poker_MCCFRM
{
    /// <summary>
    /// Opponent Cluster Hand Strength (Last round) of IR-KE-KO
    /// http://poker.cs.ualberta.ca/publications/AAMAS13-abstraction.pdf
    /// 
    /// cluster starting hands (169) into 8 buckets using earth mover's distance (monte carlo sampling)
    /// then, for each river private state calculate winning chance against each of the 8 buckets (opponent hands)
    /// so we have 123156254 river combinations (2 + 5 cards), which need to be checked against the opponent cards
    /// then we use k means with L2 distance metric to cluster all these "histograms" into k buckets
    /// 
    /// </summary>
    public static class OCHSTable
    {       
        public static int[] preflopIndices = null; // has 169 elements to map each starting hand to a cluster
        public static int[] riverIndices = null; // mapping each canonical river hand (7 cards) to a cluster

        static float[][] histogramsPreflop;
        static float[][] histogramsRiver;

        static readonly string filenameOppClusters = "OCHSOpponentClusters.txt";
        static readonly string filenameRiverClusters = "OCHSRiverClusters.txt";
        static readonly string filenameRiverHistograms = "OCHSRiverHistograms.txt";

        public static void Init()
        {
            LoadFromFile();

            if (riverIndices != null)
                return;

            if (histogramsRiver == null)
            {
                if (preflopIndices == null)
                {
                    CalculateOCHSOpponentClusters();
                    ClusterPreflopHands();
                    SaveToFile();
                }
                GenerateRiverHistograms();
                SaveToFile();
            }
            ClusterRiver();
            SaveToFile();
        }
        private static void CalculateOCHSOpponentClusters()
        {
            Console.WriteLine("Calculating {0} opponent clusters for OCHS using Monte Carlo Sampling...", Global.nofOpponentClusters);
            DateTime start = DateTime.UtcNow;

            histogramsPreflop = new float[169][]; // c# doesnt allow more than int max indices (its 2019, bitch pls)
            for (int i = 0; i < 169; ++i)
            {
                histogramsPreflop[i] = new float[Global.preflopHistogramSize];
            }
            long sharedLoopCounter = 0;

            using (var progress = new ProgressBar())
            {
                progress.Report((double)Interlocked.Read(ref sharedLoopCounter) / (169 * Global.nofMCSimsPerPreflopHand), sharedLoopCounter);

                Parallel.For(0, 169,
                 i =>
                 {
                     int[] cards = new int[2];
                     Global.indexer_2.unindex(Global.indexer_2.rounds - 1, i, cards);
                     long deadCardMask = (1L << cards[0]) + (1L << cards[1]);
                     for (int steps = 0; steps < Global.nofMCSimsPerPreflopHand; steps++)
                     {
                         int cardFlop1 = RandomGen.Next(0, 52);
                         while (((1L << cardFlop1) & deadCardMask) != 0)
                             cardFlop1 = RandomGen.Next(0, 52);
                         deadCardMask |= (1L << cardFlop1);

                         int cardFlop2 = RandomGen.Next(0, 52);
                         while (((1L << cardFlop2) & deadCardMask) != 0)
                             cardFlop2 = RandomGen.Next(0, 52);
                         deadCardMask |= (1L << cardFlop2);

                         int cardFlop3 = RandomGen.Next(0, 52);
                         while (((1L << cardFlop3) & deadCardMask) != 0)
                             cardFlop3 = RandomGen.Next(0, 52);
                         deadCardMask |= (1L << cardFlop3);

                         int cardTurn = RandomGen.Next(0, 52);
                         while (((1L << cardTurn) & deadCardMask) != 0)
                             cardTurn = RandomGen.Next(0, 52);
                         deadCardMask |= (1L << cardTurn);

                         int cardRiver = RandomGen.Next(0, 52);
                         while (((1L << cardRiver) & deadCardMask) != 0)
                             cardRiver = RandomGen.Next(0, 52);
                         deadCardMask |= (1L << cardRiver);

                         int[] strength = new int[3];
                         for (int card1Opponent = 0; card1Opponent < 51; card1Opponent++)
                         {
                             if (((1L << card1Opponent) & deadCardMask) != 0)
                             {
                                 continue;
                             }
                             //deadCardMask |= (1L << card1Opponent); // card2Opponent is anyway > card1Opponent
                             for (int card2Opponent = card1Opponent + 1; card2Opponent < 52; card2Opponent++)
                             {
                                 if (((1L << card2Opponent) & deadCardMask) != 0)
                                 {
                                     continue;
                                 }
                                 ulong handSevenCards = (1uL << cards[0]) + (1uL << cards[1]) + (1uL << cardFlop1) + (1uL << cardFlop2) + (1uL << cardFlop3) + (1uL << cardTurn) + (1uL << cardRiver);
                                 ulong handOpponentSevenCards = (1uL << cardFlop1) + (1uL << cardFlop2) + (1uL << cardFlop3) + (1uL << cardTurn) + (1uL << cardRiver) + (1uL << card1Opponent) + (1uL << card2Opponent);

                                 int valueSevenCards = Global.handEvaluator.Evaluate(handSevenCards);
                                 int valueOpponentSevenCards = Global.handEvaluator.Evaluate(handOpponentSevenCards);

                                 int index = (valueSevenCards > valueOpponentSevenCards ? 0 : valueSevenCards == valueOpponentSevenCards ? 1 : 2);

                                 strength[index] += 1;
                             }
                         }
                         float equity = (strength[0] + strength[1] / 2.0f) / (strength[0] + strength[1] + strength[2]);
                         histogramsPreflop[i][(Math.Min(Global.preflopHistogramSize-1, (int)(equity * (float)Global.preflopHistogramSize)))] += 1;
                         deadCardMask = (1L << cards[0]) + (1L << cards[1]);

                         Interlocked.Add(ref sharedLoopCounter, 1);
                         progress.Report((double)Interlocked.Read(ref sharedLoopCounter) / (169*Global.nofMCSimsPerPreflopHand), sharedLoopCounter);
                     }
                 });
            }
            TimeSpan elapsed = DateTime.UtcNow - start;
            Console.WriteLine("Calculating opponent clusters completed in {0}d {1}h {2}m {3}s", elapsed.Days,
                elapsed.Hours, elapsed.Minutes, elapsed.Seconds);

            Console.WriteLine("Calculated histograms: ");
            int[] cardsOutput = new int[2];
            for (int i = 0; i < 169; ++i)
            {
                cardsOutput = new int[2];
                Global.indexer_2.unindex(Global.indexer_2.rounds - 1, i, cardsOutput);
                Hand hand = new Hand();
                hand.Cards.Add(new Card(cardsOutput[0]));
                hand.Cards.Add(new Card(cardsOutput[1]));
                hand.PrintColoredCards();
                Console.Write(": ");
                for (int j = 0; j < Global.preflopHistogramSize; ++j)
                {
                    Console.Write(histogramsPreflop[i][j] + " ");
                }
                Console.WriteLine();
            }
        }
        private static void ClusterPreflopHands()
        {
            // k-means clustering
            Kmeans kmeans = new Kmeans();
            preflopIndices = kmeans.ClusterEMD(histogramsPreflop, Global.nofOpponentClusters, 100, null);

            Console.WriteLine("Created the following cluster for starting hands: ");
            List<Hand> startingHands = Utilities.GetStartingHandChart();

            for (int i = 0; i < 169; ++i)
            {
                long index = Global.indexer_2.indexLast(new int[] { startingHands[i].Cards[0].GetIndex(),
                    startingHands[i].Cards[1].GetIndex()});
                
                var nofConsoleColors = Enum.GetNames(typeof(ConsoleColor)).Length;

                if (nofConsoleColors <= Global.nofOpponentClusters)
                {
                    Console.ForegroundColor = (ConsoleColor)preflopIndices[index];
                    Console.Write("X  ");
                    Console.ResetColor();
                }
                else
                {
                    Console.Write(preflopIndices[index] + "  ");
                }
                if (i % 13 == 12)
                    Console.WriteLine();
            }

            Console.WriteLine();
        }
        private static void ClusterRiver()
        {
            // k-means clustering
            DateTime start = DateTime.UtcNow;
            Kmeans kmeans = new Kmeans();
            int[] indices = FileHandler.LoadFromFileIndex("OCHSRiverClusters_temp.txt");
            riverIndices = kmeans.ClusterL2(histogramsRiver, Global.nofRiverBuckets, 1, indices);

            Console.WriteLine("Created the following clusters for the River: ");

            int nofExamplesToPrint = 10;
            for (int i = 0; i < Global.indexer_2_5.roundSize[1]; ++i)
            {
                if (riverIndices[i] == 0 && nofExamplesToPrint > 0)
                {
                    int[] cards = new int[7];
                    Global.indexer_2_5.unindex(Global.indexer_2_5.rounds - 1, i, cards);

                    Hand hand = new Hand();
                    hand.Cards.Add(new Card(cards[0]));
                    hand.Cards.Add(new Card(cards[1]));
                    hand.Cards.Add(new Card(cards[2]));
                    hand.Cards.Add(new Card(cards[3]));
                    hand.Cards.Add(new Card(cards[4]));
                    hand.Cards.Add(new Card(cards[5]));
                    hand.Cards.Add(new Card(cards[6]));
                    hand.PrintColoredCards();
                    Console.WriteLine();
                    nofExamplesToPrint--;
                }
            }
            TimeSpan elapsed = DateTime.UtcNow - start;
            Console.WriteLine("River clustering completed in {0}d {1}h {2}m {3}s", elapsed.Days,
                elapsed.Hours, elapsed.Minutes, elapsed.Seconds);
        }
        private static void GenerateRiverHistograms()
        {
            Console.WriteLine("Generating histograms for {0} river hands of length {1} each...", 
                Global.indexer_2_5.roundSize[1], Global.nofOpponentClusters);
            DateTime start = DateTime.UtcNow;

            histogramsRiver = new float[Global.indexer_2_5.roundSize[1]][];
            for (int i = 0; i < Global.indexer_2_5.roundSize[1]; ++i)
            {
                histogramsRiver[i] = new float[Global.nofOpponentClusters];
            }

            long sharedLoopCounter = 0;
            using (var progress = new ProgressBar())
            {
                progress.Report((double)(sharedLoopCounter) / Global.indexer_2_5.roundSize[1], sharedLoopCounter);

                Parallel.For(0, Global.NOF_THREADS,
                 t =>
                 {
                     long iter = 0;
                     for (int i = Util.GetWorkItemsIndices((int)Global.indexer_2_5.roundSize[1], Global.NOF_THREADS, t).Item1;
                            i < Util.GetWorkItemsIndices((int)Global.indexer_2_5.roundSize[1], Global.NOF_THREADS, t).Item2; ++i)
                     {
                         int[] cards = new int[7];
                         Global.indexer_2_5.unindex(Global.indexer_2_5.rounds - 1, i, cards);
                         long deadCardMask = (1L << cards[0]) + (1L << cards[1]) + (1L << cards[2]) + (1L << cards[3]) + (1L << cards[4])
                            + (1L << cards[5]) + (1L << cards[6]);

                         for (int card1Opponent = 0; card1Opponent < 51; card1Opponent++)
                         {
                             if (((1L << card1Opponent) & deadCardMask) != 0)
                             {
                                 continue;
                             }
                             for (int card2Opponent = card1Opponent + 1; card2Opponent < 52; card2Opponent++)
                             {
                                 if (((1L << card2Opponent) & deadCardMask) != 0)
                                 {
                                     continue;
                                 }

                                 ulong handSevenCards = (1uL << cards[0]) + (1uL << cards[1]) + (1uL << cards[2]) + (1uL << cards[3]) + (1uL << cards[4]) +
                                 +(1uL << cards[5]) + (1uL << cards[6]);
                                 ulong handOpponentSevenCards = (1uL << card1Opponent) + (1uL << card2Opponent) + (1uL << cards[2]) + (1uL << cards[3]) + (1uL << cards[4])
                                 + (1uL << cards[5]) + (1uL << cards[6]);

                                 int valueSevenCards = Global.handEvaluator.Evaluate(handSevenCards);
                                 int valueOpponentSevenCards = Global.handEvaluator.Evaluate(handOpponentSevenCards);

                                 long indexPreflop = Global.indexer_2.indexLast(new int[] { card1Opponent, card2Opponent });
                                 histogramsRiver[i][preflopIndices[indexPreflop]] += valueSevenCards > valueOpponentSevenCards ? 1 : (valueSevenCards == valueOpponentSevenCards) ? 0.5f : 0.0f;
                             }
                         }

                         iter++;
                         if(iter % 10000 == 0)
                         {
                             Interlocked.Add(ref sharedLoopCounter, 10000);
                             progress.Report((double)(sharedLoopCounter) / Global.indexer_2_5.roundSize[1], sharedLoopCounter);
                         }
                     }
                     Interlocked.Add(ref sharedLoopCounter, iter % 10000);
                     progress.Report((double)(sharedLoopCounter) / Global.indexer_2_5.roundSize[1], sharedLoopCounter);
                 });
            }
            TimeSpan elapsed = DateTime.UtcNow - start;
            Console.WriteLine("Generating River histograms completed in {0}d {1}h {2}m {3}s", elapsed.Days,
                elapsed.Hours, elapsed.Minutes, elapsed.Seconds);
        }
        public static void SaveToFile()
        {
            if (preflopIndices != null)
            {
                Console.WriteLine("Saving table to file {0}", filenameOppClusters);
                FileHandler.SaveToFile(preflopIndices, filenameOppClusters);
            }
            if (histogramsRiver != null)
            {
                Console.WriteLine("Saving river histograms from file {0}", filenameRiverHistograms);
                BinaryWriter bin = new BinaryWriter(File.Open(filenameRiverHistograms, FileMode.Create));
                int dim1 = histogramsRiver.Length;
                int dim2 = histogramsRiver[0].Length;

                bin.Write(dim1);
                bin.Write(dim2);
                for (int i = 0; i < dim1; ++i)
                {
                    for (int j = 0; j < dim2; ++j)
                    {
                        bin.Write(histogramsRiver[i][j]);
                    }
                }
                bin.Close();
            }
            if (riverIndices != null)
            {
                Console.WriteLine("Saving river cluster index to file {0}", filenameRiverClusters);
                using var fileStream = File.Create(filenameRiverClusters);
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(fileStream, riverIndices);
            }
        }
        private static void LoadFromFile()
        {
            if (File.Exists(filenameRiverClusters))
            {
                riverIndices = FileHandler.LoadFromFileIndex(filenameRiverClusters);
            }
            else
            {
                if (File.Exists(filenameRiverHistograms))
                {
                    Console.WriteLine("Loading river histograms from file {0}", filenameRiverHistograms);
                    BinaryReader binR = new BinaryReader(File.OpenRead(filenameRiverHistograms));
                    int dim1 = binR.ReadInt32();
                    int dim2 = binR.ReadInt32();
                    histogramsRiver = new float[dim1][];
                    for (int i = 0; i < dim1; ++i)
                    {
                        histogramsRiver[i] = new float[dim2];
                    }

                    for (int i = 0; i < dim1; ++i)
                    {
                        for (int j = 0; j < dim2; ++j)
                        {
                            histogramsRiver[i][j] = binR.ReadSingle();
                        }
                    }
                }
                if (File.Exists(filenameOppClusters))
                {
                    Console.WriteLine("Loading flop opponent clusters from file {0}", filenameOppClusters);
                    using var fileStream = File.OpenRead(filenameOppClusters);
                    var binForm = new BinaryFormatter();
                    preflopIndices = (int[])binForm.Deserialize(fileStream);
                }
            }
        }
    }
}