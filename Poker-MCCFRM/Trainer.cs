using System;
using System.Collections.Generic;
using System.Linq;


namespace Poker_MCCFRM
{
    class Trainer
    {
        private int threadIndex = 0;
        State rootState;

        public Trainer(int threadIndex)
        {
            Global.Deck.Value = new Deck();
            rootState = new ChanceState();
            this.threadIndex = threadIndex;
        }
        /// <summary>
        /// Reset game state to save resources
        /// </summary>
        public void ResetGame()
        {
            rootState = new ChanceState();
        }
        /// <summary>
        /// Recursively update the strategy for the tree of player
        /// </summary>
        public void UpdateStrategy(State gs, int traverser)
        {
            if (gs.BettingRound() > 1 || gs is TerminalState || !gs.IsPlayerInHand(traverser))
            {
                return;
            }
            else if (gs is ChanceState)
            {
                gs = gs.DoRandomAction();
                UpdateStrategy(gs, traverser);
            }
            else if (gs.IsPlayerTurn(traverser))
            {
                Infoset infoset = gs.GetInfoset();
                float[] sigma = infoset.CalculateStrategy();
                int randomIndex = Util.SampleDistribution(sigma);
                gs.CreateChildren();
                gs = gs.children[randomIndex];
                infoset.actionCounter[randomIndex]++;

                UpdateStrategy(gs, traverser);
            }
            else
            {
                gs.CreateChildren();
                foreach (State state in gs.children)
                {
                    UpdateStrategy(state, traverser);
                }
            }
        }
        public void UpdateStrategy(int traverser)
        {
            ResetGame();
            if (rootState.bettingRound > 0)
            {
                throw new Exception("DAFUQ");
            }
            UpdateStrategy(rootState, traverser);
        }
        public float TraverseMCCFRPruned(int traverser)
        {
            ResetGame();
            return TraverseMCCFRPruned(rootState, traverser);
        }
        public float TraverseMCCFR(int traverser, int iteration)
        {
            ResetGame();
            return TraverseMCCFR(rootState, traverser, iteration);
        }
        private float TraverseMCCFRPruned(State gs, int traverser)
        {
            if (gs is TerminalState)
            {
                return gs.GetReward(traverser);
            }
            else if (!gs.IsPlayerInHand(traverser)) // we cant get the reward because this function is not implemented
            {
                return -gs.bets[traverser]; // correct?
            }
            else if (gs is ChanceState)
            {
                // sample a from chance
                return TraverseMCCFRPruned(gs.DoRandomAction(), traverser);
            }
            else if (gs.IsPlayerTurn(traverser))
            {
                // according to supp. mat. page 3, we do full MCCFR on the last betting round, otherwise skip low regret
                if (gs.bettingRound != 4)
                {
                    //Infoset of player i that corresponds to h
                    Infoset infoset = gs.GetInfoset();
                    float[] sigma = infoset.CalculateStrategy();
                    float expectedVal = 0.0f;

                    gs.CreateChildren();
                    List<float> expectedValsChildren = new List<float>();
                    List<bool> explored = new List<bool>();
                    for (int i = 0; i < gs.children.Count; ++i)
                    {
                        if (infoset.regret[i] > Global.C)
                        {
                            expectedValsChildren.Add(TraverseMCCFRPruned(gs.children[i], traverser));
                            explored.Add(true);
                            expectedVal += sigma[i] * expectedValsChildren.Last();
                        }
                        else
                        {
                            explored.Add(false);
                        }
                    }
                    for (int i = 0; i < gs.children.Count; ++i)
                    {
                        if (explored[i])
                        {
                            infoset.regret[i] += expectedValsChildren[i] - expectedVal;
                            infoset.regret[i] = Math.Max(Global.regretFloor, infoset.regret[i]);
                        }
                    }
                    return expectedVal;
                }
                else
                {
                    // do the same as in normal MCCFR
                    //Infoset of player i that corresponds to h
                    Infoset infoset = gs.GetInfoset();
                    float[] sigma = infoset.CalculateStrategy();
                    float expectedVal = 0.0f;

                    gs.CreateChildren();
                    List<float> expectedValsChildren = new List<float>();
                    for (int i = 0; i < gs.children.Count; ++i)
                    {
                        expectedValsChildren.Add(TraverseMCCFRPruned(gs.children[i], traverser));
                        expectedVal += sigma[i] * expectedValsChildren.Last();
                    }
                    for (int i = 0; i < gs.children.Count; ++i)
                    {
                        infoset.regret[i] += expectedValsChildren[i] - expectedVal;
                        infoset.regret[i] = Math.Max(Global.regretFloor, infoset.regret[i]);
                    }
                    return expectedVal;
                }
            }
            else
            {
                Infoset infoset = gs.GetInfoset();
                float[] sigma = infoset.CalculateStrategy();

                int randomIndex = Util.SampleDistribution(sigma);
                gs.CreateChildren();

                return TraverseMCCFRPruned(gs.children[randomIndex], traverser);
            }
        }
        public void PlayOneGame()
        {
            ResetGame();
            State gs = rootState;
            bool first = true;
            while (!(gs is TerminalState))
            {
                if (gs is ChanceState)
                {
                    // sample a from chance
                    gs = gs.DoRandomAction();

                    Console.WriteLine();

                    if (first)
                    {
                        Console.Write("Player Cards: ");
                        for (int i = 0; i < Global.nofPlayers; ++i)
                        {
                            List<Card> playerCards = new List<Card>();
                            playerCards.Add(new Card(gs.playerCards[i].Item1));
                            playerCards.Add(new Card(gs.playerCards[i].Item2));
                            playerCards[0].PrintBeautifulString();
                            playerCards[1].PrintBeautifulString(" ");
                        }
                        first = false;
                    }
                    else
                    {
                        if (gs.tableCards.Count != 0)
                            Console.Write("Table Cards: ");
                        for (int i = 0; i < gs.tableCards.Count; ++i)
                        {
                            new Card(gs.tableCards[i]).PrintBeautifulString();
                        }
                    }
                }
                else if (gs is PlayState)
                {
                    Console.WriteLine();
                    Console.Write("Player {0}'s turn : ", gs.playerToMove);
                    Infoset infoset = gs.GetInfoset();
                    float[] sigma = infoset.CalculateStrategy();

                    int randomIndex = Util.SampleDistribution(sigma);
                    gs.CreateChildren();
                    gs = gs.children[randomIndex];
                    Console.Write(gs.history[gs.history.Count - 1]);
                }
            }
            Console.WriteLine();
            Console.Write("Rewards: ");
            for (int i = 0; i < Global.nofPlayers; ++i)
            {
                Console.Write(gs.GetReward(i) + " ");
            }
            Console.WriteLine();
        }
        public float PlayOneGame_d(int mainPlayer, bool display)
        {
            ResetGame();
            State gs = rootState;
            bool first = true;
            while (!(gs is TerminalState))
            {
                if (gs is ChanceState)
                {
                    // sample a from chance
                    gs = gs.DoRandomAction();

                    if (display)
                        Console.WriteLine();

                    if (first)
                    {
                        if (display)
                            Console.Write("Player Cards: ");
                        for (int i = 0; i < Global.nofPlayers; ++i)
                        {
                            List<Card> playerCards = new List<Card>();
                            playerCards.Add(new Card(gs.playerCards[i].Item1));
                            playerCards.Add(new Card(gs.playerCards[i].Item2));
                            if (display)

                                playerCards[0].PrintBeautifulString();
                            if (display)

                                playerCards[1].PrintBeautifulString(" ");
                        }
                        first = false;
                    }
                    else
                    {
                        if (gs.tableCards.Count != 0)
                            if (display)
                                Console.Write("Table Cards: ");
                        for (int i = 0; i < gs.tableCards.Count; ++i)
                        {
                            if (display)
                                new Card(gs.tableCards[i]).PrintBeautifulString();
                        }
                    }
                }
                else if (gs is PlayState)
                {
                    if (display)
                        Console.WriteLine();
                    if (display)
                        Console.Write("Player {0}'s turn : ", gs.playerToMove);

                    Infoset infoset;
                    if (mainPlayer == gs.playerToMove)
                    {
                        infoset = gs.GetInfoset();
                    }
                    else
                    {
                        infoset = gs.GetInfosetSecondary();
                    }

                    float[] sigma = infoset.CalculateStrategy();

                    int randomIndex = Util.SampleDistribution(sigma);
                    gs.CreateChildren();
                    gs = gs.children[randomIndex];
                    if (display)
                        Console.Write(gs.history[gs.history.Count - 1]);
                }
            }
            if (display)
                Console.WriteLine();
            if (display)
                Console.Write("Rewards: ");
            for (int i = 0; i < Global.nofPlayers; ++i)
            {
                if (display)
                    Console.Write(gs.GetReward(i) + " ");
            }
            if (display)
                Console.WriteLine();
            return gs.GetReward(mainPlayer);
        }
        private float TraverseMCCFR(State gs, int traverser, int iteration)
        {
            if (gs is TerminalState)
            {
                return gs.GetReward(traverser);
            }
            else if (!gs.IsPlayerInHand(traverser)) // we cant get the reward because this function is not implemented
            {
                return -gs.bets[traverser]; // correct?
            }
            else if (gs is ChanceState)
            {
                // sample a from chance
                return TraverseMCCFR(gs.DoRandomAction(), traverser, iteration);
            }
            else if (gs.IsPlayerTurn(traverser))
            {
                //Infoset of player i that corresponds to h
                Infoset infoset = gs.GetInfoset();
                float[] sigma = infoset.CalculateStrategy();
                float expectedVal = 0.0f;

                gs.CreateChildren();
                List<float> expectedValsChildren = new List<float>();
                for (int i = 0; i < gs.children.Count; ++i)
                {
                    expectedValsChildren.Add(TraverseMCCFR(gs.children[i], traverser, iteration));
                    expectedVal += sigma[i] * expectedValsChildren.Last();
                }
                for (int i = 0; i < gs.children.Count; ++i)
                {
                    infoset.regret[i] += expectedValsChildren[i] - expectedVal;
                    infoset.regret[i] = Math.Max(Global.regretFloor, infoset.regret[i]);
                }
                return expectedVal;
            }
            else
            {
                Infoset infoset = gs.GetInfoset();
                float[] sigma = infoset.CalculateStrategy();

                int randomIndex = Util.SampleDistribution(sigma);
                gs.CreateChildren();

                return TraverseMCCFR(gs.children[randomIndex], traverser, iteration);
            }
        }
        internal void TraverseMCCFRPruned()
        {
            throw new NotImplementedException();
        }

        internal void DiscountInfosets(float d)
        {
            foreach (Infoset infoset in Global.nodeMap.Values)
            {
                for (int i = 0; i < infoset.regret.Count(); ++i)
                {
                    infoset.regret[i] *= d;
                    infoset.actionCounter[i] *= d;
                }
            }
        }

        public void PrintStartingHandsChart()
        {
            ResetGame();
            List<PlayState> gs = ((ChanceState)rootState).GetFirstActionStates();

            for (int i = 0; i < gs[0].GetValidActions().Count; ++i)
            {
                if (gs[0].GetValidActions()[i] == ACTION.FOLD)
                {
                    Console.WriteLine("FOLD Table");
                }
                if (gs[0].GetValidActions()[i] == ACTION.CALL)
                {
                    Console.WriteLine("CALL Table");
                }
                if (gs[0].GetValidActions()[i] == ACTION.RAISE1)
                {
                    Console.WriteLine(Global.raises[0] + "*POT RAISE " + "Table");
                }
                if (gs[0].GetValidActions()[i] == ACTION.RAISE2)
                {
                    Console.WriteLine(Global.raises[1] + "*POT RAISE " + "Table");
                }
                if (gs[0].GetValidActions()[i] == ACTION.RAISE3)
                {
                    Console.WriteLine(Global.raises[2] + "*POT RAISE " + "Table");
                }
                if (gs[0].GetValidActions()[i] == ACTION.ALLIN)
                {
                    Console.WriteLine("ALLIN Table");
                }

                Console.WriteLine("    2    3    4    5    6    7    8    9    T    J    Q    K    A (suited)");
                for (int j = 0; j < gs.Count; ++j)
                {
                    PlayState ps = gs[j];
                    Infoset infoset = ps.GetInfoset();
                    //List<float> sigma = infoset.CalculateStrategy();
                    float[] phi = infoset.GetFinalStrategy();

                    if (j % 13 == 0 && j + 1 < gs.Count)
                    {
                        if (j / 13 < 8)
                            Console.Write((j / 13 + 2) + " ");
                        else if (j / 13 == 8)
                            Console.Write("T ");
                        else if (j / 13 == 9)
                            Console.Write("J ");
                        else if (j / 13 == 10)
                            Console.Write("Q ");
                        else if (j / 13 == 11)
                            Console.Write("K ");
                        else
                            Console.Write("A ");
                    }

                    if (phi[i] <= 0.25)
                        Console.ForegroundColor = ConsoleColor.Red;
                    else if (phi[i] <= 0.5)
                        Console.ForegroundColor = ConsoleColor.DarkRed;
                    else if (phi[i] <= 0.75)
                        Console.ForegroundColor = ConsoleColor.DarkGreen;
                    else if (phi[i] <= 1.0)
                        Console.ForegroundColor = ConsoleColor.Green;

                    Console.Write(phi[i].ToString("0.00") + " ");
                    Console.ResetColor();

                    if ((j + 1) % 13 == 0)
                        Console.WriteLine();
                }

                Console.WriteLine();
                Console.WriteLine();
            }
        }
        public void PrintStatistics(long iterations)
        {
            ResetGame();
            List<PlayState> gs = ((ChanceState)rootState).GetFirstActionStates();

            int maxOutput = -1; // todo
            foreach (PlayState ps in gs)
            {
                if (maxOutput < 0)
                    break;
                maxOutput--;

                Infoset infoset = ps.GetInfoset();

                Hand hand = new Hand();
                hand.Cards.Add(new Card(ps.playerCards[ps.playerToMove].Item1));
                hand.Cards.Add(new Card(ps.playerCards[ps.playerToMove].Item2));
                hand.PrintColoredCards("\n");
                List<ACTION> actions = ps.GetValidActions();

                for (int j = 0; j < actions.Count(); ++j)
                {
                    if (actions[j] == ACTION.FOLD)
                    {
                        Console.Write("FOLD: ");
                    }
                    if (actions[j] == ACTION.CALL)
                    {
                        Console.Write("CALL: ");
                    }
                    if (actions[j] == ACTION.RAISE1)
                    {
                        Console.Write(Global.raises[0] + "*POT ");
                    }
                    if (actions[j] == ACTION.RAISE2)
                    {
                        Console.Write(Global.raises[1] + "*POT ");
                    }
                    if (actions[j] == ACTION.RAISE3)
                    {
                        Console.Write(Global.raises[2] + "*POT ");
                    }
                    if (actions[j] == ACTION.ALLIN)
                    {
                        Console.Write("ALLIN: ");
                    }
                    Console.Write("Regret: " + infoset.regret[j].ToString("0.00") + " ");
                    Console.Write("ActionCounter: " + infoset.actionCounter[j].ToString("0.00") + " ");
                    Console.WriteLine();
                }
                Console.WriteLine();
            }
            Console.WriteLine("Number of infosets: " + Global.nodeMap.Count);
            Console.WriteLine("Number of training iterations: " + iterations);
        }
    }
}
