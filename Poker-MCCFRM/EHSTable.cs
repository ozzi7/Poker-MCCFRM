/// Uses Poker Effective Hand Strength (EHS) algorithm to create a lookup table
/// https://en.wikipedia.org/wiki/Poker_Effective_Hand_Strength_(EHS)_algorithm
/// http://www.cs.virginia.edu/~evans/poker/wp-content/uploads/2011/02/opponent_modeling_in_poker_billings.pdf
/// 
using SnapCall;
using System;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Threading;
using System.Threading.Tasks;

namespace Poker_MCCFRM
{
    class EHSTable
    {
        int[,] tableFlop = new int[3, 3];
        int[,] tableTurn = new int[3, 3];
        static float[] EHSFlop = new float[1286792];
        static float[] EHSTurn = new float[13960050];
        float[,] histogramsFlop = new float[1286792, Global.flopHistogramSize];
        private static string EHSTable5Cards = "EHSTable5Cards.txt";
        private static string EHSTable6Cards = "EHSTable6Cards.txt";

        public EHSTable()
        {
            // In essence, go through the 2+3 HandIndexer set and combine with opponent, turn, river
            // 1286792 * 1081*990 = 1 377 111 930 480 combinations

            LoadFromFile();

            if (EHSFlop == null)
            {
                Generate5CardsTable();
                SaveToFile();
                Generate6CardTable();
                SaveToFile();
            }
            else if(EHSTurn == null)
            {
                Generate6CardTable();
                SaveToFile();
            }
            CalculateFlopHistograms();
            ClusterFlop();
        }
        private void ClusterFlop()
        {
            //Console.WriteLine("Generating {0} clusters from {1} hands for the Flop, using histograms of length {2}...",
            //    9000, indexerFlop.roundSize[0], nofOpponentClusters);
            //// k-means clustering
            //Kmeans kmeans = new Kmeans();
            //flopIndices = kmeans.ClusterEMD(histogramsFlop, 9000);

            //for (int i = 0; i < indexerRiver.roundSize[0]; ++i)
            //{
            //    if (riverIndices[i] == 0)
            //    {
            //        int[] cards = new int[7];
            //        indexerRiver.unindex(indexerRiver.rounds - 1, i, cards);

            //        Poker_MCCFRM.Hand hand = new Poker_MCCFRM.Hand();
            //        hand.Cards.Add(new Poker_MCCFRM.Card(cards[0]));
            //        hand.Cards.Add(new Poker_MCCFRM.Card(cards[1]));
            //        hand.Cards.Add(new Poker_MCCFRM.Card(cards[2]));
            //        hand.Cards.Add(new Poker_MCCFRM.Card(cards[3]));
            //        hand.Cards.Add(new Poker_MCCFRM.Card(cards[4]));
            //        hand.Cards.Add(new Poker_MCCFRM.Card(cards[5]));
            //        hand.Cards.Add(new Poker_MCCFRM.Card(cards[6]));
            //        hand.PrintColoredCards();
            //        Console.WriteLine();
            //    }
            //}
            //TimeSpan elapsed = DateTime.UtcNow - start;
            //Console.WriteLine("River clustering completed completed in {0:0.00}s", elapsed.TotalSeconds);
        }
        private void Generate5CardsTable()
        {
            Console.WriteLine("Calculating Effective Hand Strength Table For 2 + 3 (1286792)");
            int[] cards = new int[5];
            long deadCardMask = 0;
            using var progress = new ProgressBar();
            for (int i = 0; i < 1286792; i++)
            {
                tableFlop = new int[3, 3];

                Global.indexer_2_3.unindex(Global.indexer_2_3.rounds - 1, i, cards);
                deadCardMask |= (1L << cards[0]) + (1L << cards[1]) + (1L << cards[2]) + (1L << cards[3]) + (1L << cards[4]);

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
                        deadCardMask |= (1L << card2Opponent);

                        for (int cardTurn = 0; cardTurn < 52; cardTurn++)
                        {
                            if (((1L << cardTurn) & deadCardMask) != 0)
                            {
                                continue;
                            }
                            deadCardMask |= (1L << cardTurn);
                            for (int cardRiver = cardTurn + 1; cardRiver < 52; cardRiver++)
                            {
                                if (((1L << cardRiver) & deadCardMask) != 0)
                                {
                                    continue;
                                }

                                deadCardMask |= (1L << cardRiver);

                                ulong handSevenCards = (1uL << cards[0]) + (1uL << cards[1]) + (1uL << cards[2]) + (1uL << cards[3]) + (1uL << cards[4]) + (1uL << cardTurn) + (1uL << cardRiver);
                                ulong handOpponentSevenCards = (1uL << card1Opponent) + (1uL << card2Opponent) + (1uL << cards[2]) + (1uL << cards[3]) + (1uL << cards[4]) + (1uL << cardTurn) + (1uL << cardRiver);
                                ulong handFiveCards = (1uL << cards[0]) + (1uL << cards[1]) + (1uL << cards[2]) + (1uL << cards[3]) + (1uL << cards[4]);
                                ulong handOpponentFiveCards = (1uL << card1Opponent) + (1uL << card2Opponent) + (1uL << cards[2]) + (1uL << cards[3]) + (1uL << cards[4]);

                                int valueFiveCards = Global.handEvaluator.Evaluate(handFiveCards);
                                int valueOpponentFiveCards = Global.handEvaluator.Evaluate(handOpponentFiveCards);
                                int valueSevenCards = Global.handEvaluator.Evaluate(handSevenCards);
                                int valueOpponentSevenCards = Global.handEvaluator.Evaluate(handOpponentSevenCards);

                                int index5Cards = (valueFiveCards > valueOpponentFiveCards ? 0 : valueFiveCards == valueOpponentFiveCards ? 1 : 2);
                                int index7Cards = (valueSevenCards > valueOpponentSevenCards ? 0 : valueSevenCards == valueOpponentSevenCards ? 1 : 2);

                                tableFlop[index5Cards, index7Cards] += 1;

                                deadCardMask &= ~(1L << cardRiver);
                            }
                            deadCardMask &= ~(1L << cardTurn);

                        }
                        deadCardMask &= ~(1L << card2Opponent);

                    }
                    deadCardMask &= ~(1L << card1Opponent);
                }

                //float behind7 = tableFlop[0, 2] + tableFlop[1, 2] + tableFlop[2, 2];
                //float tied7 = tableFlop[0, 1] + tableFlop[1, 1] + tableFlop[2, 1];
                //float ahead7 = tableFlop[0, 0] + tableFlop[1, 0] + tableFlop[2, 0];

                float behind5 = tableFlop[2, 0] + tableFlop[2, 1] + tableFlop[2, 2];
                float tied5 = tableFlop[1, 0] + tableFlop[1, 1] + tableFlop[1, 2];
                float ahead5 = tableFlop[0, 0] + tableFlop[0, 1] + tableFlop[0, 2];

                float handstrength5 = (ahead5 + tied5 / 2.0f) / (ahead5 + tied5 + behind5);
                float Ppot5 = (behind5 + tied5 == 0) ? 0 : (tableFlop[2, 0] + tableFlop[2, 1] / 2.0f + tableFlop[1, 0] / 2.0f) / (behind5 + tied5);
                float NPot5 = (ahead5 + tied5 == 0) ? 0 : (tableFlop[0, 2] + tableFlop[1, 2] / 2.0f + tableFlop[0, 1] / 2.0f) / (ahead5 + tied5);
                EHSFlop[i] = handstrength5 * (1 - NPot5) + (1 - handstrength5) * Ppot5; // for each 2+3 card combo measure ehs 
                // (combo of current strength + river strength but single number)

                progress.Report((double)(i + 1) / 1286792, i);
            }
        }
        private void Generate6CardTable()
        {
            Console.WriteLine("Calculating Effective Hand Strength Table For 2 + 4 (13960050)");
            int[] cards = new int[6];
            long deadCardMask = 0;
            using var progress = new ProgressBar();
            for (int i = 0; i < 13960050; i++)
            {
                tableTurn = new int[3, 3];

                Global.indexer_2_4.unindex(Global.indexer_2_4.rounds - 1, i, cards);
                deadCardMask |= (1L << cards[0]) + (1L << cards[1]) + (1L << cards[2]) + (1L << cards[3]) + (1L << cards[4]);

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
                        deadCardMask |= (1L << card2Opponent);

                        for (int cardTurn = 0; cardTurn < 52; cardTurn++)
                        {
                            if (((1L << cardTurn) & deadCardMask) != 0)
                            {
                                continue;
                            }
                            deadCardMask |= (1L << cardTurn);
                            for (int cardRiver = cardTurn + 1; cardRiver < 52; cardRiver++)
                            {
                                if (((1L << cardRiver) & deadCardMask) != 0)
                                {
                                    continue;
                                }

                                deadCardMask |= (1L << cardRiver);

                                ulong handSixCards = (1uL << cards[0]) + (1uL << cards[1]) + (1uL << cards[2]) + (1uL << cards[3]) + (1uL << cards[4]) + (1uL << cardTurn);
                                ulong handOpponentSixCards = (1uL << card1Opponent) + (1uL << card2Opponent) + (1uL << cards[2]) + (1uL << cards[3]) + (1uL << cards[4]) + (1uL << cardTurn);
                                ulong handSevenCards = (1uL << cards[0]) + (1uL << cards[1]) + (1uL << cards[2]) + (1uL << cards[3]) + (1uL << cards[4]) + (1uL << cardTurn) + (1uL << cardRiver);
                                ulong handOpponentSevenCards = (1uL << card1Opponent) + (1uL << card2Opponent) + (1uL << cards[2]) + (1uL << cards[3]) + (1uL << cards[4]) + (1uL << cardTurn) + (1uL << cardRiver);

                                int valueSevenCards = Global.handEvaluator.Evaluate(handSevenCards);
                                int valueOpponentSevenCards = Global.handEvaluator.Evaluate(handOpponentSevenCards);
                                int valueSixCards = Global.handEvaluator.Evaluate(handSixCards);
                                int valueOpponentSixCards = Global.handEvaluator.Evaluate(handOpponentSixCards);

                                int index6Cards = (valueSixCards > valueOpponentSixCards ? 0 : valueSixCards == valueOpponentSixCards ? 1 : 2);
                                int index7Cards = (valueSevenCards > valueOpponentSevenCards ? 0 : valueSevenCards == valueOpponentSevenCards ? 1 : 2);

                                tableTurn[index6Cards, index7Cards] += 1;

                                deadCardMask &= ~(1L << cardRiver);
                            }
                            deadCardMask &= ~(1L << cardTurn);

                        }
                        deadCardMask &= ~(1L << card2Opponent);
                    }
                    deadCardMask &= ~(1L << card1Opponent);
                }

                float behind7 = tableTurn[0, 2] + tableTurn[1, 2] + tableTurn[2, 2];
                float tied7 = tableTurn[0, 1] + tableTurn[1, 1] + tableTurn[2, 1];
                float ahead7 = tableTurn[0, 0] + tableTurn[1, 0] + tableTurn[2, 0];

                float behind6 = tableTurn[2, 0] + tableTurn[2, 1] + tableTurn[2, 2];
                float tied6 = tableTurn[1, 0] + tableTurn[1, 1] + tableTurn[1, 2];
                float ahead6 = tableTurn[0, 0] + tableTurn[0, 1] + tableTurn[0, 2];

                float handstrength6 = (ahead6 + tied6 / 2.0f) / (ahead6 + tied6 + behind6);
                float Ppot6 = (behind6 + tied6 == 0) ? 0 : (tableTurn[2, 0] + tableTurn[2, 1] / 2.0f + tableTurn[1, 0] / 2.0f) / (behind6 + tied6);
                float NPot6 = (ahead6 + tied6 == 0) ? 0 : (tableTurn[0, 2] + tableTurn[1, 2] / 2.0f + tableTurn[0, 1] / 2.0f) / (ahead6 + tied6);
                EHSTurn[i] = handstrength6 * (1 - NPot6) + (1 - handstrength6) * Ppot6;

                progress.Report((double)(i + 1) / 13960050,i);
            }
        }
        private void CalculateFlopHistograms()
        {
            Console.WriteLine("Calculating Histograms for flop");

            DateTime start = DateTime.UtcNow;

            long sharedLoopCounter = 0;
            using (var progress = new ProgressBar())
            {
                Parallel.For(0, 1286792,
                 i => {
                     int[] cards = new int[5];
                     int[] table = new int[3];
                     Global.indexer_2_3.unindex(Global.indexer_2_3.rounds - 1, i, cards);
                     long deadCardMask = (1L << cards[0]) + (1L << cards[1]) + (1L << cards[2]) + (1L << cards[3]) + (1L << cards[4]);

                     for (int cardTurn = 0; cardTurn < 52; cardTurn++)
                     {
                         if (((1L << cardTurn) & deadCardMask) != 0)
                         {
                             continue;
                         }
                         deadCardMask |= (1L << cardTurn);
                         for (int cardRiver = cardTurn + 1; cardRiver < 52; cardRiver++)
                         {
                             if (((1L << cardRiver) & deadCardMask) != 0)
                             {
                                 continue;
                             }

                             deadCardMask |= (1L << cardRiver);
                             table = new int[3];

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
                                     deadCardMask |= (1L << card2Opponent);

                                     ulong handSevenCards = (1uL << cards[0]) + (1uL << cards[1]) + (1uL << cards[2]) + (1uL << cards[3]) + (1uL << cards[4]) + (1uL << cardTurn) + (1uL << cardRiver);
                                     ulong handOpponentSevenCards = (1uL << card1Opponent) + (1uL << card2Opponent) + (1uL << cards[2]) + (1uL << cards[3]) + (1uL << cards[4]) + (1uL << cardTurn) + (1uL << cardRiver);

                                     int valueSevenCards = Global.handEvaluator.Evaluate(handSevenCards);
                                     int valueOpponentSevenCards = Global.handEvaluator.Evaluate(handOpponentSevenCards);

                                     int index7Cards = (valueSevenCards > valueOpponentSevenCards ? 0 : valueSevenCards == valueOpponentSevenCards ? 1 : 2);

                                     table[index7Cards] += 1;

                                     deadCardMask &= ~(1L << card2Opponent);
                                 }
                                 deadCardMask &= ~(1L << card1Opponent);
                             }                             
                             float equity = (table[0] + table[1] / 2.0f) / (table[0] + table[1] + table[2]);

                             histogramsFlop[i, (Math.Min((Global.flopHistogramSize-1), (int)(equity * Global.flopHistogramSize)))] += 1;

                             deadCardMask &= ~(1L << cardRiver);
                         }
                         deadCardMask &= ~(1L << cardTurn);
                     }

                     float sum = 0.0f;
                     for (int k = 0; k < 50; ++k)
                     {
                         sum += histogramsFlop[i, k];
                     }
                     for (int k = 0; k < 50; ++k)
                     {
                         histogramsFlop[i, k] /= sum;
                     }

                     Interlocked.Add(ref sharedLoopCounter, 1);
                     progress.Report((double)Interlocked.Read(ref sharedLoopCounter) / 1286792, sharedLoopCounter);
                 });
            }

            TimeSpan elapsed = DateTime.UtcNow - start;
            Console.WriteLine("Histogram generation completed in {0}", elapsed.TotalSeconds);
        }
        public static void SaveToFile()
        {
            if (EHSFlop != null)
            {
                Console.WriteLine("Saving 5 Card EHS to file {0}", EHSTable5Cards);
                using var fileStream = File.Create(EHSTable5Cards);
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(fileStream, EHSFlop);
            }
            if (EHSTurn != null)
            {
                Console.WriteLine("Saving 6 Card EHS to file {0}", EHSTable6Cards);
                using var fileStream = File.Create(EHSTable6Cards);
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(fileStream, EHSTurn);
            }
        }
        private static void LoadFromFile()
        {
            if (File.Exists(EHSTable5Cards))
            {
                Console.WriteLine("Loading 5 Card EHS Table {0}", EHSTable5Cards);
                using var fileStream = File.OpenRead(EHSTable5Cards);
                var binForm = new BinaryFormatter();
                EHSFlop = (float[])binForm.Deserialize(fileStream);
            }
            if (File.Exists(EHSTable6Cards))
            {
                Console.WriteLine("Loading 6 Card EHS Table {0}", EHSTable6Cards);
                using var fileStream = File.OpenRead(EHSTable6Cards);
                var binForm = new BinaryFormatter();
                EHSTurn = (float[])binForm.Deserialize(fileStream);
            }
        }
    }
}
