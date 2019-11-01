//using System;
//using System.Collections.Generic;
//using System.IO;
//using System.Linq;
//using System.Runtime.Serialization.Formatters.Binary;
//using System.Text;
//using System.Threading.Tasks;

//namespace PokerAI
//{
//    /// <summary>
//    /// Opponent Cluster Hand Strength (Last round) of IR-KE-KO
//    /// http://poker.cs.ualberta.ca/publications/AAMAS13-abstraction.pdf
//    /// 
//    /// cluster starting hands (169) into 8 buckets using earth mover's distance (monte carlo sampling)
//    /// then, for each river private state calculate winning chance against each of the 8 buckets (opponent hands)
//    /// so we have 2428287420 river combinations (7 cards), which need to be checked against the 169 opponent cards (monte carlo sampling)
//    /// then we use k means with L2 distance metric to cluster all these "histograms" into 9000 or 5000 buckets
//    /// 
//    /// </summary>
//    class OCHS
//    {
//        int nofOpponentClusters = 8;
//        int[,] histograms;

//        float stepSize = 0;
//        string filenameOppClusters = "OCHSOpponentClusters.txt";
//        string filenameRiverClusters = "OCHSRiverClusters.txt";
//        HandIndexer[] indexer;

//        public OCHS(SnapCall.Evaluator evaluator, HandIndexer handIndexerPrivate)
//        {
//            if (filenameOppClusters != null && File.Exists(filenameOppClusters))
//            {
//                Console.WriteLine("Loading {0} from memory", filenameOppClusters);
//                LoadFromFile(filenameOppClusters);
//            }
//            else
//            {
//                evaluator.Initialize();
//                CalculateOCHSOpponentClusters(evaluator, handIndexerPrivate);

//                SaveToFile();
//            }
//        }
//        private void CalculateOCHSOpponentClusters(SnapCall.Evaluator evaluator, HandIndexer indexer)
//        {
//            Console.WriteLine("Generating {0} Opponent Clusters for OCHS using Monte Carlo Sampling...", nofOpponentClusters);
//            long deadCardMask = 0;
//            histograms = new int[169, 169];
//            int[] cards = new int[2];
//            using (var progress = new ProgressBar())
//            {
//                for (int steps = 0; steps < 169; steps++)
//                {
//                    for (int i = 0; i < 169; i++)
//                    {
//                        indexer.unindex(indexer.rounds - 1, i, cards);
//                        deadCardMask |= (1L << cards[0]) + (1L << cards[1]);

//                        int cardTurn = RandomGen.Next(0, 52);
//                        while (((1L << cardTurn) & deadCardMask) != 0)
//                            cardTurn = RandomGen.Next(0, 52);
//                            for (int card1Opponent = 0; card1Opponent < 51; card1Opponent++)
//                    {
//                        if (((1L << card1Opponent) & deadCardMask) != 0)
//                        {
//                            continue;
//                        }
//                        deadCardMask |= (1L << card1Opponent);
//                        for (int card2Opponent = card1Opponent + 1; card2Opponent < 52; card2Opponent++)
//                        {
//                            if (((1L << card2Opponent) & deadCardMask) != 0)
//                            {
//                                continue;
//                            }
//                            deadCardMask |= (1L << card2Opponent);


//                            indexer.unindex(indexer.rounds - 1, i, cards);
//                    deadCardMask |= (1L << cards[0]) + (1L << cards[1]) + (1L << cards[2]) + (1L << cards[3]) + (1L << cards[4]);

//                    for (int card1Opponent = 0; card1Opponent < 51; card1Opponent++)
//                    {
//                        if (((1L << card1Opponent) & deadCardMask) != 0)
//                        {
//                            continue;
//                        }
//                        deadCardMask |= (1L << card1Opponent);
//                        for (int card2Opponent = card1Opponent + 1; card2Opponent < 52; card2Opponent++)
//                        {
//                            if (((1L << card2Opponent) & deadCardMask) != 0)
//                            {
//                                continue;
//                            }
//                            deadCardMask |= (1L << card2Opponent);

//                            for (int cardTurn = 0; cardTurn < 52; cardTurn++)
//                            {
//                                if (((1L << cardTurn) & deadCardMask) != 0)
//                                {
//                                    continue;
//                                }
//                                deadCardMask |= (1L << cardTurn);
//                                for (int cardRiver = cardTurn + 1; cardRiver < 52; cardRiver++)
//                                {
//                                    if (((1L << cardRiver) & deadCardMask) != 0)
//                                    {
//                                        continue;
//                                    }

//                                    deadCardMask |= (1L << cardRiver);

//                                    ulong handSevenCards = (1uL << cards[0]) + (1uL << cards[1]) + (1uL << cards[2]) + (1uL << cards[3]) + (1uL << cards[4]) + (1uL << cardTurn) + (1uL << cardRiver);
//                                    ulong handOpponentSevenCards = (1uL << card1Opponent) + (1uL << card2Opponent) + (1uL << cards[2]) + (1uL << cards[3]) + (1uL << cards[4]) + (1uL << cardTurn) + (1uL << cardRiver);
//                                    ulong handFiveCards = (1uL << cards[0]) + (1uL << cards[1]) + (1uL << cards[2]) + (1uL << cards[3]) + (1uL << cards[4]);
//                                    ulong handOpponentFiveCards = (1uL << card1Opponent) + (1uL << card2Opponent) + (1uL << cards[2]) + (1uL << cards[3]) + (1uL << cards[4]);

//                                    int valueFiveCards = evaluator.Evaluate(handFiveCards);
//                                    int valueOpponentFiveCards = evaluator.Evaluate(handOpponentFiveCards);
//                                    int valueSevenCards = evaluator.Evaluate(handSevenCards);
//                                    int valueOpponentSevenCards = evaluator.Evaluate(handOpponentSevenCards);

//                                    int index5Cards = (valueFiveCards > valueOpponentFiveCards ? 0 : valueFiveCards == valueOpponentFiveCards ? 1 : 2);
//                                    int index7Cards = (valueSevenCards > valueOpponentSevenCards ? 0 : valueSevenCards == valueOpponentSevenCards ? 1 : 2);

//                                    tableFlop[index5Cards, index7Cards] += 1;

//                                }
//                            }
//                        }
//                    }
//                }
//            }
//        }
//        public void SaveToFile()
//        {
//            Console.WriteLine("Saving table to file EHSTable5Cards.txt");

//            using (var fileStream = File.Create("EHSTable5Cards.txt"))
//            {
//                BinaryFormatter bf = new BinaryFormatter();
//                bf.Serialize(fileStream, EHSFlop);
//            }
//            Console.WriteLine("Saving table to file EHSTable6Cards.txt");

//            using (var fileStream = File.Create("EHSTable6Cards.txt"))
//            {
//                BinaryFormatter bf = new BinaryFormatter();
//                bf.Serialize(fileStream, EHSTurn);
//            }
//        }
//        private void LoadFromFile(string filename)
//        {
//            using (var fileStream = File.OpenRead(filename))
//            {
//                var binForm = new BinaryFormatter();
//                EHSFlop = (float[])binForm.Deserialize(fileStream);
//            }
//        }
//    }
//}