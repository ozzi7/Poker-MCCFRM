using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading.Tasks;

namespace Poker_MCCFRM
{
    public static class EMDTable
    {
        // lossless bucketing on preflop
        public static int[] flopIndices; // mapping each canonical flop hand (2+3 cards) to a cluster
        public static int[] turnIndices; // mapping each canonical turn hand (2+4 cards) to a cluster

        const int nofTurnBuckets = 200;
        const int nofFlopBuckets = 200;

        static float[,] histogramsFlop;
        static float[,] histogramsTurn;

        public static void Init(HandIndexer handIndexerPreflop, HandIndexer handIndexerFlop, HandIndexer handIndexerTurn,
            HandIndexer handIndexerRiver)
        {
            LoadFromFile();

            if (turnIndices == null)
            {
                GenerateTurnHistograms(handIndexerTurn, handIndexerRiver);
                ClusterTurn(handIndexerTurn);
                SaveToFile();
                GenerateFlopHistograms(handIndexerFlop, handIndexerTurn);
                ClusterFlop(handIndexerFlop);
                SaveToFile();
            }
            else if (flopIndices == null)
            {
                GenerateFlopHistograms(handIndexerFlop, handIndexerTurn);
                ClusterFlop(handIndexerFlop);
                SaveToFile();
            }
        }

        private static void GenerateTurnHistograms(HandIndexer indexerTurn, HandIndexer indexerRiver)
        {
            Console.WriteLine("Generating histograms for {0} turn hands of length {1} each...",
                indexerTurn.roundSize[1], OCHS.nofRiverBuckets);
            DateTime start = DateTime.UtcNow;
            histogramsTurn = new float[indexerTurn.roundSize[1], OCHS.nofRiverBuckets];

            long sharedLoopCounter = 0;
            using (var progress = new ProgressBar())
            {
                progress.Report((double)(sharedLoopCounter) / indexerTurn.roundSize[1]);

                Parallel.For(0, Global.NOF_THREADS,
                t =>
                {
                    for (int i = Util.GetWorkItemsIndices((int)indexerTurn.roundSize[1], Global.NOF_THREADS, t).Item1;
                        i < Util.GetWorkItemsIndices((int)indexerTurn.roundSize[1], Global.NOF_THREADS, t).Item2; ++i)
                    {
                        int[] cards = new int[6];
                        indexerTurn.unindex(indexerTurn.rounds - 1, i, cards);
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

                            long indexRiver = indexerRiver.indexLast(cardsTarget);
                            histogramsTurn[i, OCHS.riverIndices[indexRiver]] += 1.0f;

                            deadCardMask &= ~(1L << cardRiver);
                        }

                        sharedLoopCounter++;
                        progress.Report((double)(sharedLoopCounter) / indexerTurn.roundSize[1]);
                    }
                });
            }
            TimeSpan elapsed = DateTime.UtcNow - start;
            Console.WriteLine("Generating Turn histograms completed in {0:0.00}s", elapsed.TotalSeconds);
        }
        private static void GenerateFlopHistograms(HandIndexer indexerFlop, HandIndexer indexerTurn)
        {
            Console.WriteLine("Generating histograms for {0} flop hands of length {1} each...",
                indexerFlop.roundSize[1], nofFlopBuckets);
            DateTime start = DateTime.UtcNow;
            histogramsFlop = new float[indexerFlop.roundSize[1], nofTurnBuckets];

            long sharedLoopCounter = 0;
            using (var progress = new ProgressBar())
            {
                progress.Report((double)(sharedLoopCounter) / indexerFlop.roundSize[1]);

                Parallel.For(0, Global.NOF_THREADS,
                t =>
                {
                    for (int i = Util.GetWorkItemsIndices((int)indexerFlop.roundSize[1], Global.NOF_THREADS, t).Item1;
                        i < Util.GetWorkItemsIndices((int)indexerFlop.roundSize[1], Global.NOF_THREADS, t).Item2; ++i)
                    {
                        int[] cards = new int[6];
                        indexerFlop.unindex(indexerFlop.rounds - 1, i, cards);

                        long deadCardMask = (1L << cards[0]) + (1L << cards[1]) + (1L << cards[2]) + (1L << cards[3]) + (1L << cards[4]);

                        for (int cardTurn = 0; cardTurn < 52; cardTurn++)
                        {
                            if (((1L << cardTurn) & deadCardMask) != 0)
                            {
                                continue;
                            }
                            deadCardMask |= (1L << cardTurn);
                            cards[5] = cardTurn;

                            long indexTurn = indexerTurn.indexLast(cards);
                            histogramsFlop[i, turnIndices[indexTurn]] += 1.0f;

                            deadCardMask &= ~(1L << cardTurn);
                        }

                        sharedLoopCounter++;
                        progress.Report((double)(sharedLoopCounter) / indexerFlop.roundSize[1]);
                    }
                });
            }
            TimeSpan elapsed = DateTime.UtcNow - start;
            Console.WriteLine("Generating Turn histograms completed in {0:0.00}s", elapsed.TotalSeconds);
        }
        private static void ClusterTurn(HandIndexer indexer)
        {
            // k-means clustering
            DateTime start = DateTime.UtcNow;
            Kmeans kmeans = new Kmeans();
            turnIndices = kmeans.ClusterEMD(histogramsTurn, nofTurnBuckets, 4);

            Console.WriteLine("Created the following clusters for the Turn: ");

            for (int i = 0; i < indexer.roundSize[1]; ++i)
            {
                if (turnIndices[i] == 0)
                {
                    int[] cards = new int[6];
                    indexer.unindex(indexer.rounds - 1, i, cards);

                    Hand hand = new Hand();
                    hand.Cards.Add(new Card(cards[0]));
                    hand.Cards.Add(new Card(cards[1]));
                    hand.Cards.Add(new Card(cards[2]));
                    hand.Cards.Add(new Card(cards[3]));
                    hand.Cards.Add(new Card(cards[4]));
                    hand.Cards.Add(new Card(cards[5]));
                    hand.PrintColoredCards();
                    Console.WriteLine();
                }
            }
            TimeSpan elapsed = DateTime.UtcNow - start;
            Console.WriteLine("Turn clustering completed in {0:0.00}s", elapsed.TotalSeconds);
        }
        private static void ClusterFlop(HandIndexer indexer)
        {
            // k-means clustering
            DateTime start = DateTime.UtcNow;
            Kmeans kmeans = new Kmeans();
            flopIndices = kmeans.ClusterEMD(histogramsFlop, nofFlopBuckets, 1);

            Console.WriteLine("Created the following clusters for the Flop: ");

            for (int i = 0; i < indexer.roundSize[1]; ++i)
            {
                if (turnIndices[i] == 0)
                {
                    int[] cards = new int[5];
                    indexer.unindex(indexer.rounds - 1, i, cards);

                    Hand hand = new Hand();
                    hand.Cards.Add(new Card(cards[0]));
                    hand.Cards.Add(new Card(cards[1]));
                    hand.Cards.Add(new Card(cards[2]));
                    hand.Cards.Add(new Card(cards[3]));
                    hand.Cards.Add(new Card(cards[4]));
                    hand.PrintColoredCards();
                    Console.WriteLine();
                }
            }
            TimeSpan elapsed = DateTime.UtcNow - start;
            Console.WriteLine("Flop clustering completed in {0:0.00}s", elapsed.TotalSeconds);
        }
        public static void SaveToFile()
        {
            string filenameEMDTurnTable = "EMDTurnTable.txt";
            string filenameEMDFlopTable = "EMDFlopTable.txt";

            if (flopIndices != null)
            {
                Console.WriteLine("Saving flop cluster index to file {0}", filenameEMDFlopTable);

                using (var fileStream = File.Create(filenameEMDFlopTable))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(fileStream, flopIndices);
                }
            }
            if (turnIndices != null)
            {
                Console.WriteLine("Saving turn cluster index to file {0}", filenameEMDTurnTable);

                using (var fileStream = File.Create(filenameEMDTurnTable))
                {
                    BinaryFormatter bf = new BinaryFormatter();
                    bf.Serialize(fileStream, turnIndices);
                }
            }
        }
        private static void LoadFromFile()
        {
            string filenameEMDTurnTable = "EMDTurnTable.txt";
            string filenameEMDFlopTable = "EMDFlopTable.txt";

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
