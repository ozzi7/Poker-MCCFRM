using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Poker_MCCFRM;
using SnapCall;

namespace Poker_MCCFRM
{
    class Program
    {
        static void Main(string[] args)
        {
            Global.handEvaluator = new Evaluator();
            Global.handEvaluator.Initialize();
            CalculateInformationAbstraction();
            Train();
        }
        private static void testRandom(HandIndexer indexer)
        {
            int totalCards = 0;
            for (int i = 0; i < indexer.rounds; ++i)
                totalCards += indexer.cardsPerRound[i];

            int[] cards = new int[totalCards];
            int[] cards2 = new int[totalCards];

            for (long n = 0; n < 10000000L; ++n)
            {
                long deadCardMask = 0;
                for (int i = 0; i < totalCards; ++i)
                {
                    do
                    {
                        cards[i] = (int)(RandomGen.NextDouble() * HandIndexer.CARDS);
                    } while (((1L << (int)cards[i]) & deadCardMask) != 0);
                    deadCardMask |= (1L << (int)cards[i]);
                }
                long index = indexer.indexLast(cards);

                indexer.unindex(indexer.rounds - 1, index, cards2);
                long index2 = indexer.indexLast(cards2);
                Debug.Assert(index2 == index);
            }
        }

        private static void CalculateInformationAbstraction()
        {
            Console.WriteLine("Calculating information abstractions... ");

            Console.Write("Creating Private Hand Index (2 cards)... ");
            Global.indexer_2 = new HandIndexer(new int[1] { 2 });
            Console.WriteLine(Global.indexer_2.roundSize[0] + " non-isomorphic hands found");

            Console.Write("Creating Private + Flop Index (2 & 3 cards)... ");
            Global.indexer_2_3 = new HandIndexer(new int[2] { 2, 3 });
            Console.WriteLine(Global.indexer_2_3.roundSize[1] + " non-isomorphic hands found");

            Console.Write("Creating Private + Flop + Turn Index (2 & 4 cards)... ");
            Global.indexer_2_4 = new HandIndexer(new int[2] { 2, 4 });
            Console.WriteLine(Global.indexer_2_4.roundSize[1] + " non-isomorphic hands found");

            Console.Write("Creating Private + Flop + Turn + River Index (2 & 5 cards)... ");
            Global.indexer_2_5 = new HandIndexer(new int[2] { 2, 5 });
            Console.WriteLine(Global.indexer_2_5.roundSize[1] + " non-isomorphic hands found");

            Console.Write("Creating 2 + 5 + 2 card index... ");
            Global.indexer_2_5_2 = new HandIndexer(new int[3] { 2, 5, 2 });
            Console.WriteLine(Global.indexer_2_5_2.roundSize[2] + " non-isomorphic hands found");

            OCHSTable.Init();
            EMDTable.Init();
        }
        private static void Train()
        {
            Console.WriteLine("Starting Monte Carlo Counterfactual Regret Minimization (MCCFRM)...");

            /* ok
            int T = 100000000; // the total number of training rounds
            int StrategyInterval = 10; // bb rounds before updating player strategy (recursive through tree)
            int PruneThreshold = 10000000; // bb rounds after this time we stop checking all actions 
            int LCFRThreshold = 500000; // bb rounds to discount old regrets
            int DiscountInterval = 100000; // bb rounds, discount values periodically but not every round
            int SaveToDiskInterval = 1000;*/

            long T = 2000000000000000000; // the total number of training rounds
            int StrategyInterval = 1; // bb rounds before updating player strategy (recursive through tree)
            int PruneThreshold = 10000000; // bb rounds after this time we stop checking all actions 
            int LCFRThreshold = 500000; // bb rounds to discount old regrets
            int DiscountInterval = 10000; // bb rounds, discount values periodically but not every round
            int SaveToDiskInterval = 1000;

            Parallel.For(0, Global.NOF_THREADS,
                  index => {
                      Trainer trainer = new Trainer();

                      for (int t = 1; t < T; t++) // bb rounds
                      {
                          if (t % 10000 == 1 && index == 0) // implement progress bar later
                          {
                              Console.WriteLine("Training steps " + ((t - 1)*Global.NOF_THREADS).ToString() + "/"+T);
                              trainer.PrintStartingHandsChart();
                              Console.WriteLine();
                          }
                          if (t % StrategyInterval == 0 && index == 0)
                          {
                              for (int traverser = 0; traverser < Global.nofPlayers; traverser++)
                              {
                                  trainer.UpdateStrategy(traverser);
                              }
                          }
                          for (int traverser = 0; traverser < Global.nofPlayers; traverser++)
                          {
                              if (t > PruneThreshold)
                              {
                                  float q = RandomGen.Next(0, 1);
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
                          if (t < LCFRThreshold && t % DiscountInterval == 0 && index == 0)
                          {
                              float d = ((float)t / DiscountInterval) / ((float)t / DiscountInterval + 1);
                              for (int i = 0; i < Global.nofPlayers; i++) // update regrets, etc for all players
                              {
                                  trainer.DiscountInfosets(d);
                              }
                          }
                          if (t % SaveToDiskInterval == 0)
                          {
                              trainer.SaveToDisk();
                          }
                      }
                  });
        }
    }
}
