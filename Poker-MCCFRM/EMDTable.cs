using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading.Tasks;

namespace Poker_MCCFRM
{
    public static class EMDTable
    {
        public static int[] flopIndices; // mapping each canonical flop hand (2+3 cards) to a cluster
        public static int[] turnIndices; // mapping each canonical turn hand (2+4 cards) to a cluster

        static float[,] histogramsFlop; 
        static float[,] histogramsTurn;

        static readonly string filenameEMDTurnTable = "EMDTurnTable.txt";
        static readonly string filenameEMDFlopTable = "EMDFlopTable.txt";
        public static void Init()
        {
            LoadFromFile();

            if (turnIndices == null)
            {
                GenerateTurnHistograms();
                ClusterTurn();
                SaveToFile();
                GenerateFlopHistograms();
                ClusterFlop();
                SaveToFile();
            }
            else if (flopIndices == null)
            {
                GenerateFlopHistograms();
                ClusterFlop();
                SaveToFile();
            }
        }

        private static void GenerateTurnHistograms()
        {
            Console.WriteLine("Generating histograms for {0} turn hands of length {1} each...",
                Global.indexer_2_4.roundSize[1], Global.nofRiverBuckets);
            DateTime start = DateTime.UtcNow;
            histogramsTurn = new float[Global.indexer_2_4.roundSize[1], Global.nofRiverBuckets];

            long sharedLoopCounter = 0;
            using (var progress = new ProgressBar())
            {
                progress.Report((double)(sharedLoopCounter) / Global.indexer_2_4.roundSize[1], sharedLoopCounter);

                Parallel.For(0, Global.NOF_THREADS,
                t =>
                {
                    for (int i = Util.GetWorkItemsIndices((int)Global.indexer_2_4.roundSize[1], Global.NOF_THREADS, t).Item1;
                        i < Util.GetWorkItemsIndices((int)Global.indexer_2_4.roundSize[1], Global.NOF_THREADS, t).Item2; ++i)
                    {
                        int[] cards = new int[6];
                        Global.indexer_2_4.unindex(Global.indexer_2_4.rounds - 1, i, cards);
                        int[] cardsTarget = new int[7];
                        cards.CopyTo(cardsTarget, 0);
                        long deadCardMask = (1L << cards[0]) + (1L << cards[1]) + (1L << cards[2]) + (1L << cards[3]) + (1L << cards[4])
                        + (1L << cards[5]);

                        for (int cardRiver = 0; cardRiver < 52; cardRiver++)
                        {
                            if (((1L << cardRiver) & deadCardMask) != 0)
                            {
                                continue;
                            }
                            deadCardMask |= (1L << cardRiver);
                            cardsTarget[6] = cardRiver;

                            long indexRiver = Global.indexer_2_5.indexLast(cardsTarget);
                            histogramsTurn[i, OCHSTable.riverIndices[indexRiver]] += 1.0f;

                            deadCardMask &= ~(1L << cardRiver);
                        }

                        sharedLoopCounter++;
                        progress.Report((double)(sharedLoopCounter) / Global.indexer_2_4.roundSize[1], sharedLoopCounter);
                    }
                });
            }
            TimeSpan elapsed = DateTime.UtcNow - start;
            Console.WriteLine("Generating Turn histograms completed in {0}d {1}h {2}m {3}s", elapsed.Days, elapsed.Hours, elapsed.Minutes, elapsed.Seconds);
        }
        private static void GenerateFlopHistograms()
        {
            Console.WriteLine("Generating histograms for {0} flop hands of length {1} each...",
                Global.indexer_2_3.roundSize[1], Global.nofFlopBuckets);
            DateTime start = DateTime.UtcNow;
            histogramsFlop = new float[Global.indexer_2_3.roundSize[1], Global.nofTurnBuckets];

            long sharedLoopCounter = 0;
            using (var progress = new ProgressBar())
            {
                progress.Report((double)(sharedLoopCounter) / Global.indexer_2_3.roundSize[1], sharedLoopCounter);

                Parallel.For(0, Global.NOF_THREADS,
                t =>
                {
                    for (int i = Util.GetWorkItemsIndices((int)Global.indexer_2_3.roundSize[1], Global.NOF_THREADS, t).Item1;
                        i < Util.GetWorkItemsIndices((int)Global.indexer_2_3.roundSize[1], Global.NOF_THREADS, t).Item2; ++i)
                    {
                        int[] cards = new int[6];
                        Global.indexer_2_3.unindex(Global.indexer_2_3.rounds - 1, i, cards);

                        long deadCardMask = (1L << cards[0]) + (1L << cards[1]) + (1L << cards[2]) + (1L << cards[3]) + (1L << cards[4]);

                        for (int cardTurn = 0; cardTurn < 52; cardTurn++)
                        {
                            if (((1L << cardTurn) & deadCardMask) != 0)
                            {
                                continue;
                            }
                            deadCardMask |= (1L << cardTurn);
                            cards[5] = cardTurn;

                            long indexTurn = Global.indexer_2_4.indexLast(cards);
                            histogramsFlop[i, turnIndices[indexTurn]] += 1.0f;

                            deadCardMask &= ~(1L << cardTurn);
                        }

                        sharedLoopCounter++;
                        progress.Report((double)(sharedLoopCounter) / Global.indexer_2_3.roundSize[1], sharedLoopCounter);
                    }
                });
            }
            TimeSpan elapsed = DateTime.UtcNow - start;
            Console.WriteLine("Generating Turn histograms completed in {0}d {1}h {2}m {3}s", elapsed.Days, elapsed.Hours, elapsed.Minutes, elapsed.Seconds);
        }
        private static void ClusterTurn()
        {
            // k-means clustering
            DateTime start = DateTime.UtcNow;
            Kmeans kmeans = new Kmeans();
            turnIndices = kmeans.ClusterEMD(histogramsTurn, Global.nofTurnBuckets, 1);

            TimeSpan elapsed = DateTime.UtcNow - start;
            Console.WriteLine("Turn clustering completed in {0}d {1}h {2}m {3}s", elapsed.Days, elapsed.Hours, elapsed.Minutes, elapsed.Seconds);

            Console.WriteLine("Created the following clusters for the Turn (extract of one cluster): ");
            int nofEntriesToDisplay = 20;
            for (int i = 0; i < Global.indexer_2_4.roundSize[1] && nofEntriesToDisplay > 0; ++i, nofEntriesToDisplay--)
            {
                if (turnIndices[i] == 0)
                {
                    int[] cards = new int[6];
                    Global.indexer_2_4.unindex(Global.indexer_2_4.rounds - 1, i, cards);

                    Hand hand = new Hand();
                    hand.Cards = new List<Card>(){new Card(cards[0]),new Card(cards[1]), new Card(cards[2]), new Card(cards[3]),
                            new Card(cards[4]), new Card(cards[5])};
                    hand.PrintColoredCards("\n");
                }
            }
            if(nofEntriesToDisplay == 0)
                Console.WriteLine("...");
        }
        private static void ClusterFlop()
        {
            // k-means clustering
            DateTime start = DateTime.UtcNow;
            Kmeans kmeans = new Kmeans();
            flopIndices = kmeans.ClusterEMD(histogramsFlop, Global.nofFlopBuckets, 1);

            TimeSpan elapsed = DateTime.UtcNow - start;
            Console.WriteLine("Flop clustering completed in {0}d {1}h {2}m {3}s", elapsed.Days, elapsed.Hours, elapsed.Minutes, elapsed.Seconds);

            Console.WriteLine("Created the following clusters for the Flop (extract of one cluster): ");
            int nofEntriesToDisplay = 20;
            for (int i = 0; i < Global.indexer_2_3.roundSize[1] && nofEntriesToDisplay > 0; ++i, nofEntriesToDisplay--)
            {
                if (turnIndices[i] == 0)
                {
                    int[] cards = new int[5];
                    Global.indexer_2_3.unindex(Global.indexer_2_3.rounds - 1, i, cards);

                    Hand hand = new Hand();
                    hand.Cards = new List<Card>(){new Card(cards[0]),new Card(cards[1]), new Card(cards[2]), new Card(cards[3]),
                            new Card(cards[4])};
                    hand.PrintColoredCards("\n");
                }
            }
            if (nofEntriesToDisplay == 0)
                Console.WriteLine("...");
        }
        public static void SaveToFile()
        {
            if (flopIndices != null)
            {
                Console.WriteLine("Saving flop cluster index to file {0}", filenameEMDFlopTable);
                using var fileStream = File.Create(filenameEMDFlopTable);
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(fileStream, flopIndices);
            }
            if (turnIndices != null)
            {
                Console.WriteLine("Saving turn cluster index to file {0}", filenameEMDTurnTable);
                using var fileStream = File.Create(filenameEMDTurnTable);
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(fileStream, turnIndices);
            }
        }
        private static void LoadFromFile()
        {
            if (File.Exists(filenameEMDTurnTable))
            {
                Console.WriteLine("Loading turn cluster index from file {0}", filenameEMDTurnTable);
                using var fileStream = File.OpenRead(filenameEMDTurnTable);
                var binForm = new BinaryFormatter();
                turnIndices = (int[])binForm.Deserialize(fileStream);
            }
            if (File.Exists(filenameEMDFlopTable))
            {
                Console.WriteLine("Loading flop cluster index from file {0}", filenameEMDFlopTable);
                using var fileStream = File.OpenRead(filenameEMDFlopTable);
                var binForm = new BinaryFormatter();
                flopIndices = (int[])binForm.Deserialize(fileStream);
            }
        }
    }
}
