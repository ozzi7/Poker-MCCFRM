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

        static float[][] histogramsFlop; 
        static float[][] histogramsTurn;

        static readonly string filenameEMDTurnTable = "EMDTurnTable.txt";
        static readonly string filenameEMDFlopTable = "EMDFlopTable.txt";
        static readonly string filenameEMDFlopHistogram = "EMDFlopHistogram.txt";
        static readonly string filenameEMDTurnHistogram = "EMDTurnHistogram.txt"; 
        public static void Init()
        {
            LoadFromFile();

            if (turnIndices == null)
            {
                if (histogramsTurn == null)
                {
                    GenerateTurnHistograms();
                    SaveToFile();
                }
                ClusterTurn();
                SaveToFile();
                GenerateFlopHistograms();
                SaveToFile();
                ClusterFlop();
                SaveToFile();
            }
            else if (flopIndices == null)
            {
                GenerateFlopHistograms();
                SaveToFile();
                ClusterFlop();
                SaveToFile();
            }
        }

        private static void GenerateTurnHistograms()
        {
            Console.WriteLine("Generating histograms for {0} turn hands of length {1} each...",
                Global.indexer_2_4.roundSize[1], Global.turnHistogramSize);
            DateTime start = DateTime.UtcNow;
            
            histogramsTurn = new float[Global.indexer_2_4.roundSize[1]][]; // c# doesnt allow more than int max indices (its 2019, bitch pls)
            for(int i = 0; i < Global.indexer_2_4.roundSize[1]; ++i)
            {
                histogramsTurn[i] = new float[Global.turnHistogramSize];
            }

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
                        int[] cardsTurn = new int[6];
                        int[,] countTable = new int[3, 3];

                        Global.indexer_2_4.unindex(Global.indexer_2_4.rounds - 1, i, cardsTurn);
                        long deadCardMask = (1L << cardsTurn[0]) + (1L << cardsTurn[1]) + (1L << cardsTurn[2]) + (1L << cardsTurn[3]) + (1L << cardsTurn[4])
                        + (1L << cardsTurn[5]);

                        ulong shared = (1uL << cardsTurn[2]) + (1uL << cardsTurn[3]) + (1uL << cardsTurn[4]) + (1uL << cardsTurn[5]);
                        ulong handTurn = (1uL << cardsTurn[0]) + (1uL << cardsTurn[1]) + shared;
                        int valueTurn = Global.handEvaluator.Evaluate(handTurn);

                        for (int cardRiver = 0; cardRiver < 52; cardRiver++)
                        {
                            countTable = new int[3, 3];
                            if (((1L << cardRiver) & deadCardMask) != 0)
                            {
                                continue;
                            }
                            deadCardMask |= (1L << cardRiver);

                            ulong handRiver = (1uL << cardsTurn[0]) + (1uL << cardsTurn[1]) + shared + (1uL << cardRiver);
                            int valueRiver = Global.handEvaluator.Evaluate(handRiver);

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

                                    ulong handOppRiver = (1uL << card1Opponent) + (1uL << card2Opponent) + shared + (1uL << cardRiver);
                                    ulong handOppTurn = (1uL << card1Opponent) + (1uL << card2Opponent) + shared;

                                    int valueOppTurn = Global.handEvaluator.Evaluate(handOppTurn);
                                    int valueOppRiver = Global.handEvaluator.Evaluate(handOppRiver);

                                    // index 0 = win, 1 = draw, 2 = loss
                                    int indexTurn = valueTurn > valueOppTurn ? 0 : valueTurn == valueOppTurn ? 1 : 2;
                                    int indexRiver = valueRiver > valueOppRiver ? 0 : valueRiver == valueOppRiver ? 1 : 2;

                                    countTable[indexTurn, indexRiver] += 1;
                                }
                            }

                            // save the equity in histogram
                            //float behindRiver = countTable[0, 2] + countTable[1, 2] + countTable[2, 2];
                            //float tiedRiver = countTable[0, 1] + countTable[1, 1] + countTable[2, 1];
                            //float aheadRiver = countTable[0, 0] + countTable[1, 0] + countTable[2, 0];

                            float behindTurn = countTable[2, 0] + countTable[2, 1] + countTable[2, 2];
                            float tiedTurn = countTable[1, 0] + countTable[1, 1] + countTable[1, 2];
                            float aheadTurn = countTable[0, 0] + countTable[0, 1] + countTable[0, 2];

                            float handstrengthTurn = (aheadTurn + tiedTurn / 2.0f) / (aheadTurn + tiedTurn + behindTurn);
                            float PpotTurn = (behindTurn + tiedTurn == 0) ? 0 : (countTable[2, 0] + countTable[2, 1] / 2.0f + countTable[1, 0] / 2.0f) / (behindTurn + tiedTurn);
                            float NPotTurn = (aheadTurn + tiedTurn == 0) ? 0 : (countTable[0, 2] + countTable[1, 2] / 2.0f + countTable[0, 1] / 2.0f) / (aheadTurn + tiedTurn);
                            
                            histogramsTurn[i][Math.Min(Global.turnHistogramSize -1,
                                (int)(Global.turnHistogramSize * (handstrengthTurn * (1 - NPotTurn) + (1 - handstrengthTurn) * PpotTurn)))] += 1;

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
                Global.indexer_2_3.roundSize[1], Global.flopHistogramSize);
            DateTime start = DateTime.UtcNow;

            histogramsFlop = new float[Global.indexer_2_3.roundSize[1]][]; // c# doesnt allow more than int max indices (its 2019, bitch pls)
            for (int i = 0; i < Global.indexer_2_3.roundSize[1]; ++i)
            {
                histogramsFlop[i] = new float[Global.turnHistogramSize];
            }

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
                        int[] cardsFlop = new int[5];
                        int[,] countTable = new int[3, 3];

                        Global.indexer_2_3.unindex(Global.indexer_2_3.rounds - 1, i, cardsFlop);
                        long deadCardMask = (1L << cardsFlop[0]) + (1L << cardsFlop[1]) + (1L << cardsFlop[2]) + (1L << cardsFlop[3]) + (1L << cardsFlop[4]);

                        for (int cardTurn = 0; cardTurn < 52; cardTurn++)
                        {
                            if (((1L << cardTurn) & deadCardMask) != 0)
                            {
                                continue;
                            }
                            deadCardMask |= (1L << cardTurn);
                            for (int cardRiver = 0; cardRiver < 52; cardRiver++)
                            {
                                if (((1L << cardRiver) & deadCardMask) != 0)
                                {
                                    continue;
                                }
                                deadCardMask |= (1L << cardRiver);

                                ulong handSevenCards = (1uL << cardsFlop[0]) + (1uL << cardsFlop[1]) + (1uL << cardsFlop[2]) + (1uL << cardsFlop[3]) +
                                (1uL << cardsFlop[4]) + (1uL << cardsFlop[5]) + (1uL << cardRiver);

                                for (int card1Opponent = 0; card1Opponent < 51; card1Opponent++)
                                {
                                    if (((1L << card1Opponent) & deadCardMask) != 0)
                                    {
                                        continue;
                                    }
                                    deadCardMask |= (1L << card1Opponent);
                                    for (int card2Opponent = card1Opponent + 1; card2Opponent < 52; card2Opponent++)
                                    {
                                        if (((1L << card2Opponent) & deadCardMask) != 0)
                                        {
                                            continue;
                                        }

                                        ulong handRiver = (1uL << cardsFlop[0]) + (1uL << cardsFlop[1]) + (1uL << cardsFlop[2]) + (1uL << cardsFlop[3]) +
                                            (1uL << cardsFlop[4]) + (1uL << cardTurn) + (1uL << cardRiver);
                                        ulong handOppRiver = (1uL << card1Opponent) + (1uL << card2Opponent) + (1uL << cardsFlop[2]) + (1uL << cardsFlop[3]) +
                                            (1uL << cardsFlop[4]) + (1uL << cardTurn) + (1uL << cardRiver);
                                        ulong handFlop = (1uL << cardsFlop[0]) + (1uL << cardsFlop[1]) + (1uL << cardsFlop[2]) +
                                            (1uL << cardsFlop[3]) + (1uL << cardsFlop[4]);
                                        ulong handOppFlop = (1uL << card1Opponent) + (1uL << card2Opponent) + (1uL << cardsFlop[2]) +
                                            (1uL << cardsFlop[3]) + (1uL << cardsFlop[4]);

                                        int valueFlop = Global.handEvaluator.Evaluate(handFlop);
                                        int valueOppFlop = Global.handEvaluator.Evaluate(handOppFlop);
                                        int valueRiver = Global.handEvaluator.Evaluate(handSevenCards);
                                        int valueOppRiver = Global.handEvaluator.Evaluate(handOppRiver);

                                        // index 0 = win, 1 = draw, 2 = loss
                                        int indexFlop = valueFlop > valueOppFlop ? 0 : valueFlop == valueOppFlop ? 1 : 2;
                                        int indexRiver = valueRiver > valueOppRiver ? 0 : valueRiver == valueOppRiver ? 1 : 2;

                                        countTable[indexFlop, indexRiver] += 1;
                                    }
                                    deadCardMask &= ~(1L << card1Opponent);
                                }
                                deadCardMask &= ~(1L << cardRiver);
                            }
                            // save the equity in histogram
                            float behindRiver = countTable[0, 2] + countTable[1, 2] + countTable[2, 2];
                            float tiedRiver = countTable[0, 1] + countTable[1, 1] + countTable[2, 1];
                            float aheadRiver = countTable[0, 0] + countTable[1, 0] + countTable[2, 0];

                            float behindFlop = countTable[2, 0] + countTable[2, 1] + countTable[2, 2];
                            float tiedFlop = countTable[1, 0] + countTable[1, 1] + countTable[1, 2];
                            float aheadFlop = countTable[0, 0] + countTable[0, 1] + countTable[0, 2];

                            float handstrengthFlop = (aheadFlop + tiedFlop / 2.0f) / (aheadFlop + tiedFlop + behindFlop);
                            float PpotFlop = (behindFlop + tiedFlop == 0) ? 0 : (countTable[2, 0] + countTable[2, 1] / 2.0f + countTable[1, 0] / 2.0f) / (behindFlop + tiedFlop);
                            float NPotFlop = (aheadFlop + tiedFlop == 0) ? 0 : (countTable[0, 2] + countTable[1, 2] / 2.0f + countTable[0, 1] / 2.0f) / (aheadFlop + tiedFlop);

                            histogramsFlop[i][Math.Min(Global.turnHistogramSize -1,
                                (int)(Global.turnHistogramSize * (handstrengthFlop * (1 - NPotFlop) + (1 - handstrengthFlop) * PpotFlop)))] += 1;

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
            if (histogramsFlop != null)
            {
                Console.WriteLine("Saving flop histograms to file {0}", filenameEMDFlopHistogram);
                using var fileStream = File.Create(filenameEMDFlopTable);
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(fileStream, filenameEMDFlopHistogram);
            }
            if (histogramsTurn != null)
            {
                Console.WriteLine("Saving turn histograms to file {0}", filenameEMDTurnHistogram);
                using var fileStream = File.Create(filenameEMDTurnHistogram);
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(fileStream, histogramsTurn);
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
            if (flopIndices == null && File.Exists(filenameEMDFlopHistogram))
            {
                Console.WriteLine("Loading turn histograms from file {0}", filenameEMDFlopHistogram);
                using var fileStream = File.OpenRead(filenameEMDFlopHistogram);
                var binForm = new BinaryFormatter();
                histogramsFlop = (float[][])binForm.Deserialize(fileStream);
            }
            if (turnIndices == null && File.Exists(filenameEMDTurnHistogram))
            {
                Console.WriteLine("Loading flop histograms from file {0}", filenameEMDTurnHistogram);
                using var fileStream = File.OpenRead(filenameEMDTurnHistogram);
                var binForm = new BinaryFormatter();
                histogramsTurn = (float[][])binForm.Deserialize(fileStream);
            }
        }
    }
}
