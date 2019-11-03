using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PokerMCCFRM
{
    /// <summary>
    /// Opponent Cluster Hand Strength (Last round) of IR-KE-KO
    /// http://poker.cs.ualberta.ca/publications/AAMAS13-abstraction.pdf
    /// 
    /// cluster starting hands (169) into 8 buckets using earth mover's distance (monte carlo sampling)
    /// then, for each river private state calculate winning chance against each of the 8 buckets (opponent hands)
    /// so we have 2428287420 river combinations (7 cards), which need to be checked against the 169 opponent cards (monte carlo sampling)
    /// then we use k means with L2 distance metric to cluster all these "histograms" into 9000 or 5000 buckets
    /// 
    /// only 169 player cards because even if it was an equivalent suit we divide the hand by its occurrence so it doesnt matter if we over or undersample
    /// and the opponent hand: i dont think we can use the 169 cards here? it would make AhKh vs QhJh the same as vs QcJc but it is not the same  
    /// </summary>
    class OCHS
    {
        int nofOpponentClusters = 8; // histogram size for river bucketing
        int preflopHistogramSize = 50;
        int nofMonteCarloSimulations = 100000;

        int[] clusterIndices; // has 169 elements to map each starting hand to a cluster
        int[] riverIndices; // mapping each canonical river hand (5 cards) to a cluster
        float[,] histogramsPreflop;
        float[,] histogramsRiver;
        string filenameOppClusters = "OCHSOpponentClusters.txt";
        string filenameRiverClusters = "OCHSRiverClusters.txt";

        public OCHS(SnapCall.Evaluator evaluator, HandIndexer handIndexerPreflop, HandIndexer handIndexerRiver)
        {
            if (filenameOppClusters != null && File.Exists(filenameOppClusters))
            {
                Console.WriteLine("Loading {0} from memory", filenameOppClusters);
                LoadFromFile();
            }
            else
            {
                evaluator.Initialize();
                CalculateOCHSOpponentClusters(evaluator, handIndexerPreflop);
                ClusterRiver(evaluator, handIndexerPreflop, handIndexerRiver, 9000);
                SaveToFile();
            }
            evaluator.Initialize();
            ClusterRiver(evaluator, handIndexerPreflop, handIndexerRiver, 9000);
            SaveToFile();
        }
        private void CalculateOCHSOpponentClusters(SnapCall.Evaluator evaluator, HandIndexer indexer)
        {
            Console.WriteLine("Calculating {0} opponent clusters for OCHS using Monte Carlo Sampling...", nofOpponentClusters);
            DateTime start = DateTime.UtcNow;

            histogramsPreflop = new float[169, preflopHistogramSize];
            long sharedLoopCounter = 0;

            using (var progress = new ProgressBar())
            {
                progress.Report((double)Interlocked.Read(ref sharedLoopCounter) / (nofMonteCarloSimulations * 169));

                Parallel.For(0, 169,
                 i =>
                 {
                     long deadCardMask = 0;
                     for (int steps = 0; steps < nofMonteCarloSimulations; steps++)
                     {
                         int[] cards = new int[2];
                         indexer.unindex(indexer.rounds - 1, i, cards);
                         deadCardMask |= (1L << cards[0]) + (1L << cards[1]);

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
                             deadCardMask |= (1L << card1Opponent);
                             for (int card2Opponent = card1Opponent + 1; card2Opponent < 52; card2Opponent++)
                             {
                                 if (((1L << card2Opponent) & deadCardMask) != 0)
                                 {
                                     continue;
                                 }
                                 ulong handSevenCards = (1uL << cards[0]) + (1uL << cards[1]) + (1uL << cardFlop1) + (1uL << cardFlop2) + (1uL << cardFlop3) + (1uL << cardTurn) + (1uL << cardRiver);
                                 ulong handOpponentSevenCards = (1uL << cardFlop1) + (1uL << cardFlop2) + (1uL << cardFlop3) + (1uL << cardTurn) + (1uL << cardRiver) + (1uL << card1Opponent) + (1uL << card2Opponent);

                                 int valueSevenCards = evaluator.Evaluate(handSevenCards);
                                 int valueOpponentSevenCards = evaluator.Evaluate(handOpponentSevenCards);

                                 int index = (valueSevenCards > valueOpponentSevenCards ? 0 : valueSevenCards == valueOpponentSevenCards ? 1 : 2);

                                 strength[index] += 1;
                             }
                         }
                         float equity = (strength[0] + strength[1] / 2.0f) / (strength[0] + strength[1] + strength[2]);
                         histogramsPreflop[i, (Math.Min(preflopHistogramSize-1, (int)(equity * (float)preflopHistogramSize)))] += 1;
                         deadCardMask = 0L;

                         Interlocked.Add(ref sharedLoopCounter, 1);
                         progress.Report((double)Interlocked.Read(ref sharedLoopCounter) / (nofMonteCarloSimulations * 169));
                     }
                     float sum = 0.0f;
                     for (int k = 0; k < preflopHistogramSize; ++k)
                     {
                         sum += histogramsPreflop[i, k];
                     }
                     for (int k = 0; k < preflopHistogramSize; ++k)
                     {
                         histogramsPreflop[i, k] /= sum;
                     }
                 });
            }

            Console.WriteLine("Calculated histograms: ");
            int[] cardsOutput = new int[2];
            for (int i = 0; i < 169; ++i)
            {
                cardsOutput = new int[2];
                indexer.unindex(indexer.rounds - 1, i, cardsOutput);
                SnapCall.Hand hand = new SnapCall.Hand();
                hand.Cards.Add(new SnapCall.Card(cardsOutput[0]));
                hand.Cards.Add(new SnapCall.Card(cardsOutput[1]));
                hand.PrintColoredCards();
                Console.Write(": ");
                for (int j = 0; j < preflopHistogramSize; ++j)
                {
                    Console.Write(histogramsPreflop[i,j] + " ");
                }
                TimeSpan elapsed = DateTime.UtcNow - start;
                Console.WriteLine("Calculating opponent clusters completed in {0:0.00}s", elapsed.TotalSeconds);
            }

            // k-means clustering
            Kmeans kmeans = new Kmeans();
            clusterIndices = kmeans.Cluster(histogramsPreflop, 8);

            Console.WriteLine("Created the following cluster for starting hands: ");
            List<SnapCall.Hand> startingHands = SnapCall.Utilities.GetStartingHandChart();
            ConsoleColor[] consoleColors = { ConsoleColor.Gray, ConsoleColor.Blue, ConsoleColor.Magenta,
                ConsoleColor.Yellow, ConsoleColor.Green, ConsoleColor.Red, ConsoleColor.Cyan, ConsoleColor.White };

            for (int i = 0; i < 169; ++i)
            {
                long index = indexer.indexLast(new int[] { startingHands[i].Cards[0].GetIndex(),
                    startingHands[i].Cards[1].GetIndex()});

                // test
                //int[] cards = new int[2];
                //indexer.unindex(indexer.rounds - 1, index, cards);
                //Console.WriteLine(cards[0] + " " + cards[1]);
                Console.ForegroundColor = consoleColors[clusterIndices[index]];
                Console.Write("X  ");
                if(i % 13 == 12)
                    Console.WriteLine();
            }
            Console.ResetColor();
            Console.WriteLine();
        }
        private void ClusterRiver(SnapCall.Evaluator evaluator, HandIndexer indexerPreflop, HandIndexer indexerRiver, int nofClusters)
        {
            Console.WriteLine("Generating {0} clusters from {1} hands for the River, using histograms of length {2}...", 
                nofClusters, indexerRiver.roundSize[0], nofOpponentClusters);
            DateTime start = DateTime.UtcNow;
            riverIndices = new int[indexerRiver.roundSize[0]];
            histogramsRiver = new float[indexerRiver.roundSize[0], nofOpponentClusters];

            long sharedLoopCounter = 0;
            using (var progress = new ProgressBar())
            {
                progress.Report((double)(sharedLoopCounter) / indexerRiver.roundSize[0]);

                Parallel.For(0, Global.NOF_THREADS,
                 t =>
                 {
                     for (int i = Util.GetWorkItemsIndices((int)indexerRiver.roundSize[0], Global.NOF_THREADS, t).Item1;
                            i < Util.GetWorkItemsIndices((int)indexerRiver.roundSize[0], Global.NOF_THREADS, t).Item2; ++i)
                     {
                         int[] cards = new int[7];
                         int[] table = new int[3];
                         indexerRiver.unindex(indexerRiver.rounds - 1, i, cards);
                         long deadCardMask = (1L << cards[0]) + (1L << cards[1]) + (1L << cards[2]) + (1L << cards[3]) + (1L << cards[4])
                            + (1L << cards[5]) + (1L << cards[6]);

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

                                 ulong handSevenCards = (1uL << cards[0]) + (1uL << cards[1]) + (1uL << cards[2]) + (1uL << cards[3]) + (1uL << cards[4]) +
                                 +(1uL << cards[5]) + (1uL << cards[6]);
                                 ulong handOpponentSevenCards = (1uL << card1Opponent) + (1uL << card2Opponent) + (1uL << cards[2]) + (1uL << cards[3]) + (1uL << cards[4])
                                 + (1uL << cards[5]) + (1uL << cards[6]);

                                 int valueSevenCards = evaluator.Evaluate(handSevenCards);
                                 int valueOpponentSevenCards = evaluator.Evaluate(handOpponentSevenCards);

                                 int winDrawLoss = (valueSevenCards > valueOpponentSevenCards ? 0 : valueSevenCards == valueOpponentSevenCards ? 1 : 2);

                                 long indexPreflop = indexerPreflop.indexLast(new int[] { card1Opponent, card2Opponent });
                                 histogramsRiver[i, clusterIndices[indexPreflop]] += winDrawLoss == 0 ? 1.0f : winDrawLoss == 0 ? 0.5f : 0.0f;

                                 deadCardMask &= ~(1L << card2Opponent);
                             }
                             deadCardMask &= ~(1L << card1Opponent);
                         }
                         // normalize the histogram ? (this loses information on handstrength but EMD doesnt work well anymore)
                         // also: not normalizing between the histogram bars because hands that occur more often are more important
                         float sum = 0.0f;
                         for (int k = 0; k < nofOpponentClusters; ++k)
                         {
                             sum += histogramsRiver[i, k];
                         }
                         for (int k = 0; k < nofOpponentClusters; ++k)
                         {
                             histogramsRiver[i, k] /= sum;
                         }
                         sharedLoopCounter++;
                         progress.Report((double)(sharedLoopCounter) / indexerRiver.roundSize[0]);
                     }
                 });
            }

            // k-means clustering
            Kmeans kmeans = new Kmeans();
            riverIndices = kmeans.ClusterL2(histogramsRiver, 9000);


            Console.WriteLine("Created the following clusters for the River: ");

            for (int i = 0; i < indexerRiver.roundSize[0]; ++i)
            {
                if (riverIndices[i] == 0)
                {
                    int[] cards = new int[7];
                    indexerRiver.unindex(indexerRiver.rounds - 1, i, cards);

                    SnapCall.Hand hand = new SnapCall.Hand();
                    hand.Cards.Add(new SnapCall.Card(cards[0]));
                    hand.Cards.Add(new SnapCall.Card(cards[1]));
                    hand.Cards.Add(new SnapCall.Card(cards[2]));
                    hand.Cards.Add(new SnapCall.Card(cards[3]));
                    hand.Cards.Add(new SnapCall.Card(cards[4]));
                    hand.Cards.Add(new SnapCall.Card(cards[5]));
                    hand.Cards.Add(new SnapCall.Card(cards[6]));
                    hand.PrintColoredCards();
                    Console.WriteLine();
                }
            }
            TimeSpan elapsed = DateTime.UtcNow - start;
            Console.WriteLine("River clustering completed completed in {0:0.00}s", elapsed.TotalSeconds);
        }
        public void SaveToFile()
        {
            Console.WriteLine("Saving table to file {0}", filenameOppClusters);

            using (var fileStream = File.Create(filenameOppClusters))
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(fileStream, clusterIndices);
            }

            Console.WriteLine("Saving river cluster index to file {0}", filenameRiverClusters);

            using (var fileStream = File.Create(filenameRiverClusters))
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(fileStream, riverIndices);
            }
        }
        private void LoadFromFile()
        {
            Console.WriteLine("Loading flop opponent clusters from file {0}", filenameOppClusters);

            using var fileStream = File.OpenRead(filenameOppClusters);
            var binForm = new BinaryFormatter();
            clusterIndices = (int[])binForm.Deserialize(fileStream);

            Console.WriteLine("Loading river cluster from file {0}", filenameRiverClusters);

            using var fileStream2 = File.OpenRead(filenameRiverClusters);
            binForm = new BinaryFormatter();
            riverIndices = (int[])binForm.Deserialize(fileStream2);
        }
    }
}