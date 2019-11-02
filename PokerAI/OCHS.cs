using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PokerAI
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
        int nofOpponentClusters = 8;
        int nofMonteCarloSimulations = 1000;

        float[,] histograms;
        string filenameOppClusters = "OCHSOpponentClusters.txt";

        public OCHS(SnapCall.Evaluator evaluator, HandIndexer handIndexerPrivate)
        {
            if (filenameOppClusters != null && File.Exists(filenameOppClusters))
            {
                Console.WriteLine("Loading {0} from memory", filenameOppClusters);
                LoadFromFile(filenameOppClusters);
            }
            else
            {
                evaluator.Initialize();
                CalculateOCHSOpponentClusters(evaluator, handIndexerPrivate);

                SaveToFile();
            }
        }
        private void CalculateOCHSOpponentClusters(SnapCall.Evaluator evaluator, HandIndexer indexer)
        {
            Console.WriteLine("Generating {0} Opponent Clusters for OCHS using Monte Carlo Sampling...", nofOpponentClusters);
            DateTime start = DateTime.UtcNow;

            histograms = new float[169, 8];
            long sharedLoopCounter = 0;

            using (var progress = new ProgressBar())
            {
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
                         histograms[i, (Math.Min(7, (int)(equity * (float)nofOpponentClusters)))] += 1;
                         deadCardMask = 0L;

                         Interlocked.Add(ref sharedLoopCounter, 1);
                         progress.Report((double)Interlocked.Read(ref sharedLoopCounter) / (nofMonteCarloSimulations * 169));
                     }
                     float sum = 0.0f;
                     for (int k = 0; k < nofOpponentClusters; ++k)
                     {
                         sum += histograms[i, k];
                     }
                     for (int k = 0; k < nofOpponentClusters; ++k)
                     {
                         histograms[i, k] /= sum;
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
                for (int j = 0; j < nofOpponentClusters; ++j)
                {
                    Console.Write(histograms[i,j] + " ");
                }
                Console.WriteLine();
            }
            Kmeans kmeans = new Kmeans();
            int[] indices = kmeans.Cluster(histograms, 8);


            Console.WriteLine("Created the following cluster for starting hands: ");
            List<SnapCall.Hand> startingHands = SnapCall.Utilities.GetStartingHandChart();
            ConsoleColor[] consoleColors = { ConsoleColor.Gray, ConsoleColor.Blue, ConsoleColor.Magenta,
                ConsoleColor.Yellow, ConsoleColor.Green, ConsoleColor.Red, ConsoleColor.Cyan, ConsoleColor.White };

            for (int i = 0; i < 169; ++i)
            {
                cardsOutput = new int[2];
                long index = indexer.indexLast(new int[] { startingHands[i].Cards[0].GetIndex(),
                    startingHands[i].Cards[1].GetIndex()});
                Console.ForegroundColor = consoleColors[indices[index]];
                Console.Write("X  ");
                if(i % 13 == 12)
                    Console.WriteLine();
            }
            Console.ResetColor();
            Console.WriteLine();
            Console.Read();
        }
        public void SaveToFile()
        {
            Console.WriteLine("Saving table to file EHSTable5Cards.txt");

            using (var fileStream = File.Create("EHSTable5Cards.txt"))
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(fileStream, histograms);
            }
            Console.WriteLine("Saving table to file EHSTable6Cards.txt");

            using (var fileStream = File.Create("EHSTable6Cards.txt"))
            {
                BinaryFormatter bf = new BinaryFormatter();
                bf.Serialize(fileStream, histograms);
            }
        }
        private void LoadFromFile(string filename)
        {
            using (var fileStream = File.OpenRead(filename))
            {
                var binForm = new BinaryFormatter();
                histograms = (float[,])binForm.Deserialize(fileStream);
            }
        }
    }
}