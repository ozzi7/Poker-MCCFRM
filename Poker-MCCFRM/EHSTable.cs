/// Uses Poker Effective Hand Strength (EHS) algorithm to create a lookup table
/// https://en.wikipedia.org/wiki/Poker_Effective_Hand_Strength_(EHS)_algorithm
/// http://www.cs.virginia.edu/~evans/poker/wp-content/uploads/2011/02/opponent_modeling_in_poker_billings.pdf
/// TODO: EHSTable not used currently
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
        float[] EHSFlop = new float[1286792];
        float[] EHSTurn = new float[13960050];
        float[,] histogramsFlop = new float[1286792, 50];

        public EHSTable(Evaluator evaluator, HandIndexer privFlopIndexer, HandIndexer privFlopTurnIndexer)
        {
            // In essence, go through the 2+3 HandIndexer set and combine with opponent, turn, river
            // 1286792 * 1081*990 = 1 377 111 930 480 combinations

            string fileName = "EHSTable5Cards.txt";
            if (fileName != null && File.Exists(fileName))
            {
                if (File.Exists(fileName))
                {
                    Console.WriteLine("Loading EHS Table 5 Cards from {0}", "EHSTable5Cards.txt");
                    LoadFromFile("EHSTable5Cards.txt");
                    Console.WriteLine("Loading EHS Table 6 Cards from {0}", "EHSTable6Cards.txt");
                    LoadFromFile("EHSTable6Cards.txt");
                }
            }
            else
            {
                Calculate5Cards(evaluator, privFlopIndexer);
                Calculate6Cards(evaluator, privFlopTurnIndexer);

                SaveToFile();
            }
            CalculateFlopHistograms(evaluator, privFlopIndexer);
            ClusterFlop(privFlopIndexer);
        }
        private void ClusterFlop(HandIndexer indexerFlop)
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
        private void Calculate5Cards(Evaluator evaluator, HandIndexer indexer)
        {
            Console.WriteLine("Calculating Effective Hand Strength Table For 2 + 3 (1286792)");
            int[] cards = new int[5];
            long deadCardMask = 0;
            using var progress = new ProgressBar();
            for (int i = 0; i < 1286792; i++)
            {
                tableFlop = new int[3, 3];

                indexer.unindex(indexer.rounds - 1, i, cards);
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

                                int valueFiveCards = evaluator.Evaluate(handFiveCards);
                                int valueOpponentFiveCards = evaluator.Evaluate(handOpponentFiveCards);
                                int valueSevenCards = evaluator.Evaluate(handSevenCards);
                                int valueOpponentSevenCards = evaluator.Evaluate(handOpponentSevenCards);

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

                float behind7 = tableFlop[0, 2] + tableFlop[1, 2] + tableFlop[2, 2];
                float tied7 = tableFlop[0, 1] + tableFlop[1, 1] + tableFlop[2, 1];
                float ahead7 = tableFlop[0, 0] + tableFlop[1, 0] + tableFlop[2, 0];

                float behind5 = tableFlop[2, 0] + tableFlop[2, 1] + tableFlop[2, 2];
                float tied5 = tableFlop[1, 0] + tableFlop[1, 1] + tableFlop[1, 2];
                float ahead5 = tableFlop[0, 0] + tableFlop[0, 1] + tableFlop[0, 2];

                float handstrength5 = (ahead5 + tied5 / 2.0f) / (ahead5 + tied5 + behind5);
                float Ppot5 = (behind5 + tied5 == 0) ? 0 : (tableFlop[2, 0] + tableFlop[2, 1] / 2.0f + tableFlop[1, 0] / 2.0f) / (behind5 + tied5);
                float NPot5 = (ahead5 + tied5 == 0) ? 0 : (tableFlop[0, 2] + tableFlop[1, 2] / 2.0f + tableFlop[0, 1] / 2.0f) / (ahead5 + tied5);
                EHSFlop[i] = handstrength5 * (1 - NPot5) + (1 - handstrength5) * Ppot5;

                progress.Report((double)(i + 1) / 1286792, i);
            }
        }
        private void Calculate6Cards(Evaluator evaluator, HandIndexer indexer)
        {
            Console.WriteLine("Calculating Effective Hand Strength Table For 2 + 4 (13960050)");
            int[] cards = new int[6];
            long deadCardMask = 0;
            using var progress = new ProgressBar();
            for (int i = 0; i < 13960050; i++)
            {
                tableTurn = new int[3, 3];

                indexer.unindex(indexer.rounds - 1, i, cards);
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

                                int valueSevenCards = evaluator.Evaluate(handSevenCards);
                                int valueOpponentSevenCards = evaluator.Evaluate(handOpponentSevenCards);
                                int valueSixCards = evaluator.Evaluate(handSixCards);
                                int valueOpponentSixCards = evaluator.Evaluate(handOpponentSixCards);

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
        private void CalculateFlopHistograms(Evaluator evaluator, HandIndexer indexer)
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
                     indexer.unindex(indexer.rounds - 1, i, cards);
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

                                     int valueSevenCards = evaluator.Evaluate(handSevenCards);
                                     int valueOpponentSevenCards = evaluator.Evaluate(handOpponentSevenCards);

                                     int index7Cards = (valueSevenCards > valueOpponentSevenCards ? 0 : valueSevenCards == valueOpponentSevenCards ? 1 : 2);

                                     table[index7Cards] += 1;

                                     deadCardMask &= ~(1L << card2Opponent);
                                 }
                                 deadCardMask &= ~(1L << card1Opponent);
                             }                             
                             float equity = (table[0] + table[1] / 2.0f) / (table[0] + table[1] + table[2]);

                             histogramsFlop[i, (Math.Min(49, (int)(equity * 50.0f)))] += 1;

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
        public void SaveToFile()
        {
            Console.WriteLine("Saving table to file EHSTable5Cards.txt");

            using (var fileStream = File.Create("EHSTable5Cards.txt"))
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(fileStream, EHSFlop);
            }
            Console.WriteLine("Saving table to file EHSTable6Cards.txt");

            using (var fileStream = File.Create("EHSTable6Cards.txt"))
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(fileStream, EHSTurn);
            }
        }
        private void LoadFromFile(string filename)
        {
            using var fileStream = File.OpenRead(filename);
            var binForm = new BinaryFormatter();
            EHSFlop = (float[])binForm.Deserialize(fileStream);
        }
    }
}
