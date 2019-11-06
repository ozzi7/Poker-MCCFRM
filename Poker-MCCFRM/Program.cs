using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Poker_MCCFRM;

namespace Poker_MCCFRM
{
    class Program
    {
        static void Main(string[] args)
        {
            Global.handEvaluator = new Evaluator();
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

            Console.Write("Creating Public Flop Index... ");
            int[] cardsPerRound = new int[1] { 3 };
            Global.flopIndexer = new HandIndexer(cardsPerRound);
            Console.WriteLine(Global.flopIndexer.roundSize[0] + " non-isomorphic hands found");

            Console.Write("Creating Private Hand Index (2 cards)... ");
            cardsPerRound = new int[1] { 2 };
            Global.privIndexer = new HandIndexer(cardsPerRound);
            Console.WriteLine(Global.privIndexer.roundSize[0] + " non-isomorphic hands found");

            Console.Write("Creating Private + Flop Index (2 & 3 cards)... ");
            cardsPerRound = new int[2] { 2, 3 };
            Global.privFlopIndexer = new HandIndexer(cardsPerRound);
            Console.WriteLine(Global.privFlopIndexer.roundSize[1] + " non-isomorphic hands found");

            Console.Write("Creating Private + Flop + Turn Index (2 & 4 cards)... ");
            cardsPerRound = new int[2] { 2, 4 };
            Global.privFlopTurnIndexer = new HandIndexer(cardsPerRound);
            Console.WriteLine(Global.privFlopTurnIndexer.roundSize[1] + " non-isomorphic hands found");

            Console.Write("Creating Private + Flop + Turn Index (2 & 3 & 1 cards)... ");
            cardsPerRound = new int[3] { 2, 3, 1 };
            Global.privFlopTurnIndexer2 = new HandIndexer(cardsPerRound);
            Console.WriteLine(Global.privFlopTurnIndexer2.roundSize[2] + " non-isomorphic hands found");

            Console.Write("Creating Private + Flop + Turn + River Index (2 & 5 cards)... ");
            cardsPerRound = new int[2] { 2, 5 };
            Global.privFlopTurnRiver = new HandIndexer(cardsPerRound);
            Console.WriteLine(Global.privFlopTurnRiver.roundSize[1] + " non-isomorphic hands found");

            Console.Write("Creating Private + Flop + Turn + River Index (2 & 3 & 1 & 1 cards)... ");
            cardsPerRound = new int[4] { 2, 3, 1, 1 };
            Global.privFlopTurnRiver2 = new HandIndexer(cardsPerRound);
            Console.WriteLine(Global.privFlopTurnRiver2.roundSize[3] + " non-isomorphic hands found");

            Console.Write("Creating 5 card index... ");
            cardsPerRound = new int[1] { 5 };
            Global.fiveCardIndexer = new HandIndexer(cardsPerRound);
            Console.WriteLine(Global.fiveCardIndexer.roundSize[0] + " non-isomorphic hands found");

            Console.Write("Creating 6 card index... ");
            cardsPerRound = new int[1] { 6 };
            Global.sixCardIndexer = new HandIndexer(cardsPerRound);
            Console.WriteLine(Global.sixCardIndexer.roundSize[0] + " non-isomorphic hands found");

            Console.Write("Creating 7 card index... ");
            cardsPerRound = new int[1] { 7 };
            Global.sevenCardIndexer = new HandIndexer(cardsPerRound);
            Console.WriteLine(Global.sevenCardIndexer.roundSize[0] + " non-isomorphic hands found");

            Console.Write("Creating 2 + 5 + 2 card index... ");
            cardsPerRound = new int[3] { 2, 5, 2 };
            Global.showdownIndexer = new HandIndexer(cardsPerRound);
            Console.WriteLine(Global.showdownIndexer.roundSize[2] + " non-isomorphic hands found");

            Evaluator evaluator = new Evaluator();
            OCHS.Init(Global.privIndexer, Global.privFlopTurnRiver);
            EMDTable.Init(Global.privIndexer, Global.privFlopIndexer,
                Global.privFlopTurnIndexer, Global.privFlopTurnRiver);
        }
        private static void Train()
        {
            Console.WriteLine("Starting Monte Carlo Counterfactual Regret Minimization (MCCFRM)...");
            Global.handEvaluator.Initialize();

            int T = 100000000; // the total number of training rounds
            int StrategyInterval = 100; // bb rounds before updating player strategy (recursive through tree)
            int PruneThreshold = 1000000; // bb rounds after this time we stop checking all actions 
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
