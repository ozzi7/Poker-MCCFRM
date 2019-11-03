using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokerMCCFRM
{
    class Trainer
    {
        State rootState;

        public Trainer()
        {
            Global.Deck.Value.Create();
            rootState = new ChanceState();
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
                List<float> sigma = infoset.CalculateStrategy();
                int randomIndex = SampleDistribution(sigma.ToList());
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
                    List<float> sigma = infoset.CalculateStrategy();
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
                    List<float> sigma = infoset.CalculateStrategy();
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
                List<float> sigma = infoset.CalculateStrategy();

                int randomIndex = SampleDistribution(sigma.ToList());
                gs.CreateChildren();

                return TraverseMCCFRPruned(gs.children[randomIndex], traverser);
            }
        }
        private float TraverseMCCFR(State gs, int traverser, int iteration)
        {
            if (gs is TerminalState)
            {
                //if (iteration % 10000 == 0)
                //{
                //    Console.WriteLine(gs.playerCards[0].Item1.ToStringShort() + "" + gs.playerCards[0].Item2.ToStringShort() + ", " +
                //        gs.playerCards[1].Item1.ToStringShort() + "" + gs.playerCards[1].Item2.ToStringShort());
                //    Console.WriteLine(string.Join(",", gs.history.ToArray()));
                //    Console.WriteLine(gs.GetReward(0)+ ", " + gs.GetReward(1));
                //    Console.WriteLine();
                //}
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
                List<float> sigma = infoset.CalculateStrategy();
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
                List<float> sigma = infoset.CalculateStrategy();

                int randomIndex = SampleDistribution(sigma.ToList());
                gs.CreateChildren();

                return TraverseMCCFR(gs.children[randomIndex], traverser, iteration);
            }
        }
        private int SampleDistribution(List<float> probabilities)
        {
            double rand = RandomGen.NextDouble();
            double sum = 0.0;
            for (int i = 0; i < probabilities.Count; ++i)
            {
                sum += probabilities[i];
                if (sum >= rand)
                {
                    return i;
                }
            }
            return probabilities.Count - 1;
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

        internal void SaveToDisk()
        {
            //throw new NotImplementedException();
            return;
        }
        public void PrintStartingHandsChart()
        {
            ResetGame();
            List<PlayState> gs = ((ChanceState)rootState).GetFirstActionStates();

            foreach (PlayState ps in gs)
            {
                Infoset infoset = ps.GetInfoset();
                //List<float> sigma = infoset.CalculateStrategy();
                List<float> phi = infoset.GetFinalStrategy();

                Console.Write(ps.playerCards[ps.playerToMove].Item1.ToStringShort() +
                    ps.playerCards[ps.playerToMove].Item2.ToStringShort() + " ");
                List<ACTION> actions = ps.GetValidActions();

                for (int j = 0; j < actions.Count(); ++j)
                {
                    if (actions[j] == ACTION.FOLD)
                    {
                        Console.Write("FOLD: " + phi[j].ToString("0.00") + " ");
                    }
                    if (actions[j] == ACTION.CALL)
                    {
                        Console.Write("CALL: " + phi[j].ToString("0.00") + " ");
                    }
                    if (actions[j] == ACTION.RAISE1)
                    {
                        Console.Write("RAISE "+ Global.raises[0] + "*POT " + phi[j].ToString("0.00") + " ");
                    }
                    if (actions[j] == ACTION.RAISE2)
                    {
                        Console.Write("RAISE " + Global.raises[1] + "*POT " + phi[j].ToString("0.00") + " ");
                    }
                    if (actions[j] == ACTION.RAISE3)
                    {
                        Console.Write("RAISE " + Global.raises[2] + "*POT " + phi[j].ToString("0.00") + " ");
                    }
                    if (actions[j] == ACTION.ALLIN)
                    {
                        Console.Write("ALLIN: " + phi[j].ToString("0.00") + " ");
                    }
                }
                Console.WriteLine();
            }
        }
    }
}
