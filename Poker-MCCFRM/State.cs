/// Creates a tree: 
/// Rootstate
///     -> ChanceState 
///         -> Playstate 
///             -> ChanceState
///                 -> PlayState
///                 -> ChanceState
///                     -> ChanceState
///                         -> TerminalState
///             -> ChanceState
///                 -> PlayState
///             -> ChanceState 
///                 -> PlayState
///             -> TerminalState
/// 
/// NOTE:
/// Prior to the flop, the player to the left of the big blind will bet first.
/// After the flop the SB player will be the first to act.
/// 

using System;
using System.Collections.Generic;
using System.Linq;

namespace Poker_MCCFRM
{
    abstract class State
    {
        public List<Tuple<ulong, ulong>> playerCards = new List<Tuple<ulong, ulong>>();
        public List<ulong> tableCards = new List<ulong>();
        public List<int> stacks = new List<int>();
        public List<int> bets = new List<int>();
        public List<float> rewards = new List<float>();
        public List<bool> isPlayerIn = new List<bool>();

        public List<State> children = new List<State>();

        public int playerToMove = 2 % Global.nofPlayers;
        public int bettingRound = 0; // 0, 1, 2, 3 (0 deal player cards, 1 first betting round, 1 deal flop)
        public int playersInHand = 0;
        public int lastPlayer = 1;
        public int minRaise = Global.BB; // see https://poker.stackexchange.com/questions/2729/what-is-the-min-raise-and-min-reraise-in-holdem-no-limit#targetText=The%20minimum%20raise%20is%20going,blind%20and%20then%20raising%20%242.
        public bool isBettingOpen = false;

        public string infosetString;
        public List<ACTION> history = new List<ACTION>();
        public List<ACTION> lastActions = new List<ACTION>();

        public int GetNextPlayer()
        {
            return GetNextPlayer(lastPlayer);
        }
        public int GetNextPlayer(int lastToMoveTemp)
        {
            for (int i = (playerToMove + 1) % Global.nofPlayers; i != (lastToMoveTemp + 1) % Global.nofPlayers;
                i = (i + 1) % Global.nofPlayers)
            {
                if (isPlayerIn[i] && lastActions[i] != ACTION.ALLIN)
                {
                    return i;
                }
            }
            return -1;
        }
        public int GetLastPlayer(int playerThatRaised)
        {
            for (int i = (playerThatRaised + 1) % Global.nofPlayers; i != (playerThatRaised) % Global.nofPlayers;
                i = (i + 1) % Global.nofPlayers)
            {
                if (isPlayerIn[i] && lastActions[i] != ACTION.ALLIN)
                {
                    return i;
                }
            }
            return lastPlayer;
        }
        public int GetNumberOfPlayersThatNeedToAct()
        {
            // does not include all-in players
            int count = 0;
            for (int i = 0; i < Global.nofPlayers; i++) {
                if (isPlayerIn[i] == true && lastActions[i] != ACTION.ALLIN)
                    count++;
            }
            return count;
        }
        public int GetActivePlayers(List<bool> newIsPlayerIn)
        {
            return newIsPlayerIn.Where(c => c).Count();
        }
        public int GetNumberOfAllInPlayers()
        {
            return lastActions.Where(c => c == ACTION.ALLIN).Count();
        }
        public virtual void CreateChildren() { }

        public virtual bool IsPlayerInHand(int traverser)
        {
            throw new NotImplementedException();
        }
        public virtual Infoset GetInfoset()
        {
            throw new NotImplementedException();
        }
        public virtual bool IsPlayerTurn(int traverser)
        {
            throw new NotImplementedException();
        }
        public int BettingRound()
        {
            return bettingRound;
        }
        public virtual State DoRandomAction()
        {
            throw new NotImplementedException();
        }
        public virtual float GetReward(int traverser)
        {
            throw new NotImplementedException();
        }
    }
    class TerminalState : State
    {
        public TerminalState(List<int> stacks, List<int> bets, List<ACTION> history,
             List<Tuple<ulong, ulong>> playerCards, List<ulong> tableCards, List<ACTION> lastActions,
             List<bool> isPlayerIn)
        {
            this.stacks = stacks;
            this.playerCards = playerCards;
            this.tableCards = tableCards;
            this.lastActions = lastActions;
            this.isPlayerIn = isPlayerIn;
            this.bets = bets;
            this.stacks = stacks;
            this.history = history;
        }
        public override float GetReward(int player)
        {
            if (rewards.Count == 0)
                CreateRewards();
            if (rewards.Sum() != 0)
                throw new Exception("Wrong reward calculation");
            return rewards[player];
        }
        public void CreateRewards()
        {
            rewards = new List<float>(new float[Global.nofPlayers]);

            for (int i = 0; i < Global.nofPlayers; ++i)
            {
                rewards[i] -= bets[i]; // the bet amounts are considered lost
            }
            playersInHand = isPlayerIn.Where(c => c).Count();

            if (playersInHand == 1)
            {
                for (int i = 0; i < Global.nofPlayers; ++i)
                {
                    if (isPlayerIn[i])
                    {
                        rewards[i] += bets.Sum();
                    }
                }
            }
            else
            {
                // at least 2 players are in
                List<int> handValues = new List<int>(Global.nofPlayers);
                for (int i = 0; i < Global.nofPlayers; ++i)
                {
                    if (isPlayerIn[i])
                    {
                        Hand hand = new Hand();
                        hand.Cards.Add(new Card(playerCards[i].Item1));
                        hand.Cards.Add(new Card(playerCards[i].Item2));
                        for (int j = 0; j < tableCards.Count(); ++j)
                        {
                            hand.Cards.Add(new Card(tableCards[j]));
                        }
                        ulong cardsBitmask = 0uL;
                        for (int k = 0; k < hand.Cards.Count; ++k)
                        {
                            cardsBitmask |= hand.Cards[k].GetBit();
                        }
                        handValues.Add(Global.handEvaluator.Evaluate(cardsBitmask));
                    }
                }
                // temphandval contains values of each players hand who is in
                List<int> indicesWithBestHands = new List<int>();
                int maxVal = handValues.Max();

                var maxIndex = handValues.IndexOf(maxVal);
                while (maxIndex != -1)
                {
                    indicesWithBestHands.Add(maxIndex);
                    handValues[maxIndex] = 0;
                    maxIndex = handValues.IndexOf(maxVal);
                }
                for (int i = 0; i < indicesWithBestHands.Count(); ++i)
                {
                    rewards[indicesWithBestHands[i]] += bets.Sum() / indicesWithBestHands.Count();
                }
            }
        }
    }
    class ChanceState : State
    {
        // this is the root state
        public ChanceState()
        {
            bets = new List<int>(new int[Global.nofPlayers]);
            rewards = new List<float>(new float[Global.nofPlayers]);
            for (int i = 0; i < Global.nofPlayers; ++i)
            {
                isPlayerIn.Add(true);
                stacks.Add(Global.buyIn);
                lastActions.Add(ACTION.NONE);
            }
            bets[0] = Global.SB;
            bets[1] = Global.BB;
            stacks[0] = Global.buyIn - Global.SB;
            stacks[1] = Global.buyIn - Global.BB;
            bettingRound = 0;
            lastPlayer = 1; // initially the BB player is last to act
            minRaise = Global.BB;
            playersInHand = Global.nofPlayers;
        }
        public ChanceState(int bettingRound, int playersInHand, List<int> stacks, List<int> bets, List<ACTION> history,
             List<Tuple<ulong, ulong>> playerCards, List<ulong> tableCards, List<ACTION> lastActions,
             List<bool> isPlayerIn)
        {
            this.stacks = stacks;
            this.playerCards = playerCards;
            this.tableCards = tableCards;
            this.lastActions = lastActions;
            this.isPlayerIn = isPlayerIn;
            this.bets = bets;
            this.stacks = stacks;
            this.history = history;
            this.playersInHand = playersInHand;
            this.bettingRound = bettingRound;
        }
        public override void CreateChildren()
        {
            if (children.Count != 0) return;

            // create one playstate child after chance
            int lastToMoveTemp = -1;
            int minRaiseTemp = Global.BB;
            int newBettingRound = bettingRound + 1;
            if (bettingRound == 0)
            {
                for (int i = 2 % Global.nofPlayers; ; i = (i + 1) % Global.nofPlayers)
                {
                    if (isPlayerIn[i] && lastActions[i] != ACTION.ALLIN)
                    {
                        lastToMoveTemp = i;
                    }
                    if (i == 1) break;
                }
            }
            else if (bettingRound > 0)
            {
                for (int i = 0; i < Global.nofPlayers; ++i)
                {
                    if (isPlayerIn[i] && stacks[i] != 0)
                    {
                        lastToMoveTemp = i;
                    }
                }
            }

            // todo: wouldnt need to always copy
            List<Tuple<ulong, ulong>> playerCardsNew = new List<Tuple<ulong, ulong>>(playerCards);
            List<ulong> tableCardsNew = new List<ulong>(tableCards);

            switch (bettingRound)
            {
                case 0: // preflop, deal player hands
                    Global.Deck.Value.Shuffle();
                    for (int i = 0; i < Global.nofPlayers; ++i)
                    {
                        playerCardsNew.Add(Tuple.Create(Global.Deck.Value.Draw(i * 2), Global.Deck.Value.Draw(i * 2 + 1)));
                    }
                    break;
                case 1: // deal flop
                    Global.Deck.Value.Shuffle(Global.nofPlayers * 2); // not necessarily needed, check
                    tableCardsNew.Add(Global.Deck.Value.Draw(Global.nofPlayers * 2 + 0));
                    tableCardsNew.Add(Global.Deck.Value.Draw(Global.nofPlayers * 2 + 1));
                    tableCardsNew.Add(Global.Deck.Value.Draw(Global.nofPlayers * 2 + 2));
                    break;
                case 2: // deal turn
                    Global.Deck.Value.Shuffle(Global.nofPlayers * 2 + 3);
                    tableCardsNew.Add(Global.Deck.Value.Draw(Global.nofPlayers * 2 + 3));
                    break;
                case 3: // deal river
                    Global.Deck.Value.Shuffle(Global.nofPlayers * 2 + 4);
                    tableCardsNew.Add(Global.Deck.Value.Draw(Global.nofPlayers * 2 + 4));
                    break;
            }
            if (GetNumberOfPlayersThatNeedToAct() >= 2 && bettingRound < 3)
            {
                // there is someone left that plays
                children.Add(new PlayState(newBettingRound, playerToMove, lastToMoveTemp, minRaiseTemp, playersInHand,
                        stacks, bets, history, playerCardsNew, tableCardsNew, lastActions, isPlayerIn, true));
            }
            else
            {
                if (GetNumberOfPlayersThatNeedToAct() == 1)
                {
                    throw new Exception("We just dealt new cards but only 1 player has any actions left");
                }
                if (bettingRound < 3 && GetNumberOfAllInPlayers() >= 2 )
                {
                    // directly go to next chance node
                    children.Add(new ChanceState(newBettingRound, GetNumberOfAllInPlayers(), stacks,
                        bets, history, playerCardsNew, tableCardsNew, lastActions, isPlayerIn));
                }
                else
                {
                    children.Add(new TerminalState(stacks, bets, history, 
                        playerCardsNew, tableCardsNew, lastActions, isPlayerIn));
                }
            }
        }
        /// <summary>
        /// Note: The single child was already randomly created
        /// </summary>
        /// <returns></returns>
        public override State DoRandomAction()
        {
            CreateChildren();
            return children[0];
        }
        public List<PlayState> GetFirstActionStates()
        {
            List<PlayState> gs = new List<PlayState>();

            // create one playstate child after chance
            int lastToMoveTemp = -1;
            int minRaiseTemp = Global.BB;
            int newBettingRound = bettingRound + 1;

            if (bettingRound == 0)
            {
                for (int i = 2 % Global.nofPlayers; ; i = (i + 1) % Global.nofPlayers)
                {
                    if (isPlayerIn[i] && lastActions[i] != ACTION.ALLIN)
                    {
                        lastToMoveTemp = i;
                    }
                    if (i == 1) break;
                }
            }
            else if (bettingRound > 0)
            {
                for (int i = 0; i < Global.nofPlayers; ++i)
                {
                    if (isPlayerIn[i] && stacks[i] != 0)
                    {
                        lastToMoveTemp = i;
                    }
                }
            }

            for (int i = 0; i < 169; ++i)
            {
                List<Tuple<ulong, ulong>> playerCardsNew = new List<Tuple<ulong, ulong>>();
                List<ulong> tableCardsNew = new List<ulong>();

                int[] cardsOutput = new int[2];
                Global.indexer_2.unindex(Global.indexer_2.rounds - 1, i, cardsOutput);
                Hand hand = new Hand();
                hand.Cards.Add(new Card(cardsOutput[0]));
                hand.Cards.Add(new Card(cardsOutput[1]));
                playerCardsNew.Add(Tuple.Create(new Card(cardsOutput[0]).GetBit(),new Card(cardsOutput[1]).GetBit()));

                gs.Add(new PlayState(newBettingRound, playerToMove, lastToMoveTemp, minRaiseTemp, playersInHand,
                    stacks, bets, history, playerCardsNew, tableCardsNew, lastActions, isPlayerIn, true));
            }

            return gs;
        }
        public override bool IsPlayerInHand(int player)
        {
            return isPlayerIn[player];
        }
    }
    class PlayState : State
    {
        public PlayState(int bettingRound, int playerToMove, int lastToMove, int minRaise,
            int playersInHand, List<int> stacks, List<int> bets, List<ACTION> history,
             List<Tuple<ulong, ulong>> playerCards, List<ulong> tableCards, List<ACTION> lastActions,
             List<bool> isPlayerIn, bool isBettingOpen)
        {
            this.lastPlayer = lastToMove;
            this.minRaise = minRaise;
            this.stacks = stacks;
            this.playerCards = playerCards;
            this.tableCards = tableCards;
            this.lastActions = lastActions;
            this.isPlayerIn = isPlayerIn;
            this.bets = bets;
            this.stacks = stacks;
            this.history = history;
            this.playerToMove = playerToMove;
            this.bettingRound = bettingRound;
            this.playersInHand = playersInHand;
            this.isBettingOpen = isBettingOpen;
        }
        public override void CreateChildren()
        {
            if (children.Count != 0) return;

            int pot = bets.Sum();
            int currentCall = bets.Max();

            if (isBettingOpen)
            {
                // raises
                for (int i = 0; i < Global.raises.Count; ++i)
                {
                    List<ACTION> newHistory = new List<ACTION>(history);
                    List<int> newStacks = new List<int>(stacks);
                    List<int> newBets = new List<int>(bets);
                    List<ACTION> newLastActions = new List<ACTION>(lastActions);
                    List<bool> newIsPlayerIn = new List<bool>(isPlayerIn);

                    // we add <raise> chips to our current bet
                    int raise = (int)(Global.raises[i] * pot);
                    int actualRaise = (raise + bets[playerToMove]) - currentCall;
                    if (actualRaise < minRaise || raise >= stacks[playerToMove]) continue;

                    // valid raise, if stack is equal it would be an all in
                    if (i == 0)
                        newHistory.Add(ACTION.RAISE1);
                    if (i == 1)
                        newHistory.Add(ACTION.RAISE2);
                    if (i == 2)
                        newHistory.Add(ACTION.RAISE3);

                    newStacks[playerToMove] -= raise;
                    newBets[playerToMove] += raise;
                    newLastActions[playerToMove] = ACTION.RAISE;

                    int newLastPlayer = GetLastPlayer(playerToMove);
                    int nextPlayer = GetNextPlayer(newLastPlayer);

                    if (nextPlayer != -1)
                    {
                        children.Add(new PlayState(bettingRound, nextPlayer, newLastPlayer,
                            actualRaise, playersInHand, newStacks, newBets, newHistory,
                            playerCards, tableCards, newLastActions, newIsPlayerIn, true));
                    }
                    else
                    {
                        throw new Exception("Someone raised but there is noone left to play next");
                    }
                }

                // all-in 
                if (stacks[playerToMove] > 0)
                {
                    //(currently, multiple all-ins in a row dont accumulate the raises and re-open betting round but probably they should)
                    int raise = stacks[playerToMove];
                    int actualRaise = (raise + bets[playerToMove]) - currentCall;

                    List<ACTION> newHistory = new List<ACTION>(history);
                    List<ACTION> newLastActions = new List<ACTION>(lastActions);
                    List<int> newStacks = new List<int>(stacks);
                    List<int> newBets = new List<int>(bets);
                    List<bool> newIsPlayerIn = new List<bool>(isPlayerIn);

                    if (actualRaise >= minRaise)
                    {
                        // re-open betting if raise high enough
                        newHistory.Add(ACTION.ALLIN);
                        newLastActions[playerToMove] = ACTION.ALLIN;

                        newBets[playerToMove] += raise;
                        newStacks[playerToMove] = 0;

                        int newLastPlayer = GetLastPlayer(playerToMove);
                        int nextPlayer = GetNextPlayer(newLastPlayer);

                        // check if there is any player that has to play..
                        if (nextPlayer != -1)
                        {
                            children.Add(new PlayState(bettingRound, nextPlayer, newLastPlayer,
                            actualRaise, playersInHand, newStacks, newBets, newHistory,
                            playerCards, tableCards, newLastActions, newIsPlayerIn, true));
                        }
                        else
                        {
                            // ...otherwise go to chance
                            if (bettingRound != 4)
                            {
                                children.Add(new ChanceState(bettingRound, GetActivePlayers(newIsPlayerIn), newStacks,
                                    newBets, newHistory, playerCards, tableCards, newLastActions, newIsPlayerIn));
                            }
                            else
                            {
                                children.Add(new TerminalState(newStacks, newBets, newHistory,
                                    playerCards, tableCards, newLastActions, newIsPlayerIn));
                            }
                        }
                    }
                    else
                    {
                        // all in possible but not re-open betting
                        newHistory.Add(ACTION.ALLIN);
                        newLastActions[playerToMove] = ACTION.ALLIN;

                        newBets[playerToMove] += raise;
                        newStacks[playerToMove] = 0;

                        int newLastPlayer = GetLastPlayer(playerToMove);
                        int nextPlayer = GetNextPlayer(newLastPlayer);

                        // check if there is any player that has to play..
                        if (nextPlayer != -1)
                        {
                            children.Add(new PlayState(bettingRound, nextPlayer, newLastPlayer,
                            minRaise, playersInHand, newStacks, newBets, newHistory,
                            playerCards, tableCards, newLastActions, newIsPlayerIn, isBettingOpen));
                        }
                        else
                        {
                            // ...otherwise go to chance
                            if (bettingRound != 4)
                            {
                                children.Add(new ChanceState(bettingRound, GetActivePlayers(newIsPlayerIn), newStacks,
                                    newBets, newHistory, playerCards, tableCards, newLastActions, newIsPlayerIn));
                            }
                            else
                            {
                                children.Add(new TerminalState(newStacks, newBets, newHistory,
                                    playerCards, tableCards, newLastActions, newIsPlayerIn));
                            }
                        }
                    }
                }
            }
            if (currentCall > bets[playerToMove])
            {
                // fold
                List<ACTION> newHistory = new List<ACTION>(history);
                List<int> newStacks = new List<int>(stacks);
                List<int> newBets = new List<int>(bets);
                List<ACTION> newLastActions = new List<ACTION>(lastActions);
                List<bool> newIsPlayerIn = new List<bool>(isPlayerIn);

                newHistory.Add(ACTION.FOLD);
                newLastActions[playerToMove] = ACTION.FOLD;
                newIsPlayerIn[playerToMove] = false;

                int nextPlayer = GetNextPlayer();

                if (GetActivePlayers(newIsPlayerIn) == 1)
                {
                    // terminal state
                    children.Add(new TerminalState(newStacks, newBets, newHistory,
                            playerCards, tableCards, newLastActions, newIsPlayerIn));
                }
                else if (nextPlayer != -1)
                {
                    children.Add(new PlayState(bettingRound, nextPlayer, lastPlayer,
                        minRaise, playersInHand, newStacks, newBets, newHistory,
                        playerCards, tableCards, newLastActions, newIsPlayerIn, isBettingOpen));
                }
                else
                {
                    // here the betting round is over, there is more than 1 player left
                    if (bettingRound != 4)
                    {
                        // chance
                        children.Add(new ChanceState(bettingRound, GetActivePlayers(newIsPlayerIn), newStacks,
                            newBets, newHistory, playerCards, tableCards, newLastActions, newIsPlayerIn));
                    }
                    else
                    {
                        children.Add(new TerminalState(newStacks, newBets, newHistory,
                            playerCards, tableCards, newLastActions, newIsPlayerIn));
                    }
                }
            }
            if (currentCall - bets[playerToMove] < stacks[playerToMove])
            {
                // call possible if needed chips is LESS (otherwise its all in), if same its a check
                List<ACTION> newHistory = new List<ACTION>(history);
                List<int> newStacks = new List<int>(stacks);
                List<int> newBets = new List<int>(bets);
                List<ACTION> newLastActions = new List<ACTION>(lastActions);
                List<bool> newIsPlayerIn = new List<bool>(isPlayerIn);

                newHistory.Add(ACTION.CALL);
                newLastActions[playerToMove] = ACTION.CALL;

                newBets[playerToMove] += currentCall - bets[playerToMove];
                newStacks[playerToMove] -= currentCall - bets[playerToMove];

                int nextPlayer = GetNextPlayer();

                if (nextPlayer != -1) // the round isnt over
                {
                    children.Add(new PlayState(bettingRound, nextPlayer, lastPlayer,
                        minRaise, playersInHand, newStacks, newBets, newHistory,
                        playerCards, tableCards, newLastActions, newIsPlayerIn, isBettingOpen));
                }
                else
                {
                    // all players have moved
                    if (bettingRound < 4)
                    {
                        // some cards are missing still, chance
                        children.Add(new ChanceState(bettingRound, GetActivePlayers(newIsPlayerIn), newStacks,
                            newBets, newHistory, playerCards, tableCards, newLastActions, newIsPlayerIn));
                    }
                    else if (bettingRound == 4)
                    {
                        // terminal, all cards are already dealt
                        children.Add(new TerminalState(newStacks, newBets, newHistory,
                            playerCards, tableCards, lastActions, isPlayerIn));
                    }
                }
            }

            if (stacks.Sum() + bets.Sum() != Global.buyIn*Global.nofPlayers)
            {
                throw new Exception("Impossible chip counts");
            } 
        }
        public int GetValidActionsCount()
        {
            if (children.Count != 0)
            {
                return children.Count;
            }
            else
                return GetValidActions().Count();
        }
        public List<ACTION> GetValidActions()
        {
            List<ACTION> validActions = new List<ACTION>();
            int pot = bets.Sum();
            int currentCall = bets.Max();

            if (playersInHand == 0)
            {
                throw new Exception("There must always be >= one player in hand");
            }
            if (playersInHand == 1)
            {
                return validActions; // no valid actions 
            }

            // raises
            if (isBettingOpen)
            {
                int raise = -1;
                for (int i = 0; i < Global.raises.Count; ++i)
                {
                    raise = (int)(Global.raises[i] * pot);
                    int actualRaise = raise - (currentCall - bets[playerToMove]);

                    if (actualRaise < minRaise || raise >= stacks[playerToMove]) continue;

                    // valid raise, if stack is equal it would be an all in
                    if (i == 0)
                        validActions.Add(ACTION.RAISE1);
                    if (i == 1)
                        validActions.Add(ACTION.RAISE2);
                    if (i == 2)
                        validActions.Add(ACTION.RAISE3);
                }

                raise = stacks[playerToMove];
                // all-in 
                if (raise > 0)
                {
                    //(currently, multiple all-ins in a row dont accumulate the raises and reopen betting round but probably they should)
                    int actualRaise = (raise + bets[playerToMove]) - currentCall;
                    validActions.Add(ACTION.ALLIN);
                }
            }
            if (currentCall > bets[playerToMove])
            {
                // fold
                validActions.Add(ACTION.FOLD);
            }
            if (currentCall - bets[playerToMove] < stacks[playerToMove])
            {
                // call
                validActions.Add(ACTION.CALL);
            }

            return validActions;
        }
        public override bool IsPlayerTurn(int player)
        {
            if (playerToMove == player)
            {
                return true;
            }
            return false;
        }
        public override bool IsPlayerInHand(int player)
        {
            return isPlayerIn[player];
        }
        public override Infoset GetInfoset()
        {
            // Betting history R, A, CH, C, F
            // Player whose turn it is // not needed?
            // Cards of player whose turn it is
            // community cards

            if (infosetString == null)
            {
                string historyString = string.Join(",", history.ToArray());

                List<int> cards = new List<int>();
                cards.Add(new Card(playerCards[playerToMove].Item1).GetIndex());
                cards.Add(new Card(playerCards[playerToMove].Item2).GetIndex());
                for (int i = 0; i < tableCards.Count; ++i)
                {
                    cards.Add(new Card(tableCards[i]).GetIndex());
                }
                int[] cardArray = cards.ToArray();

                string cardString = "";
                if(tableCards.Count == 0)
                {
                    long index = Global.indexer_2.indexLast(cardArray);
                    cardString += "Preflop" + index.ToString();
                }
                else if (tableCards.Count == 3)
                {
                    long index = EMDTable.flopIndices[Global.indexer_2_3.indexLast(cardArray)];
                    cardString += "Flop" + index.ToString();
                }
                else if (tableCards.Count == 4)
                {
                    long index = EMDTable.turnIndices[Global.indexer_2_4.indexLast(cardArray)];
                    cardString += "Turn" + index.ToString();
                }
                else
                {
                    long index = OCHSTable.riverIndices[Global.indexer_2_5.indexLast(cardArray)];
                    cardString += "River" + index.ToString();
                }
                infosetString = historyString + cardString;
            }
 
            Infoset infoset;
            Global.nodeMap.TryGetValue(infosetString, out infoset);
            if (infoset != null)
            {
                return infoset;
            }
            else
            {
                infoset = new Infoset(GetValidActionsCount());
                Infoset infosetRet = Global.nodeMap.GetOrAdd(infosetString, infoset);
                return infosetRet;
            }
        }
    }
}
