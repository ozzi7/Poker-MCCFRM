//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Poker_MCCFRM
//{
//    /// <summary>
//    /// this class controls the main activities of the poker game, such as dealer position,
//    /// paying blinds, dealing cards, which player is making a decision, starting new rounds,
//    /// showdowns
//    /// </summary>
//    public class Table
//    {
//        private PlayerList players = new PlayerList();
//        private Deck deck;
//        private Hand tableHand = new Hand();
//        private int roundCounter;
//        private Pot mainPot;
//        private List<Pot> sidePots;
//        private Random rand;
//        private int turnCount;
//        public string winnermessage;
//        //the blind class, containing the amount of blinds, the position of the player
//        //who must pay the blinds
//        private class Blind
//        {
//            private int amount;
//            public int position;
//            public int Amount
//            {
//                get
//                {
//                    return amount;
//                }
//                set
//                {
//                    amount = value;
//                }
//            }

//        }
//        Blind smallBlind, bigBlind;
//        //the index of the player who's the dealer
//        private int dealerPosition;
//        //the index of the current player who turn it is
//        private int currentIndex;
//        //various propeties
//        public int TurnCount
//        {
//            get { return turnCount; }
//            set { turnCount = value; }
//        }
//        public int SmallBlind
//        {
//            get { return smallBlind.Amount; }
//        }
//        public int BigBlind
//        {
//            get{return bigBlind.Amount;}
//        }
//        public int RoundCount
//        {
//            get { return roundCounter; }
//            set { roundCounter = value; }
//        }
//        /// <summary>
//        /// contructor to begin the game
//        /// blinds are set to &500/1000 initially 
//        /// </summary>
//        /// <param name="players"></param>

//        public Table(PlayerList players)
//        {
//            this.players = players;
//            deck = new Deck();
//            mainPot = new Pot();
//            sidePots = new List<Pot>();
//            smallBlind = new Blind();
//            bigBlind = new Blind();
//            roundCounter = 0;
//            turnCount = 0;
//            dealerPosition = 0;
//            //set blind amount and position
//            smallBlind.Amount = 500;
//            bigBlind.Amount = 1000;
//            mainPot.SmallBlind = 500;
//            mainPot.BigBlind = 1000;
//            smallBlind.position = dealerPosition + 1;
//            bigBlind.position = (dealerPosition + 2) % 2;
//            currentIndex = dealerPosition;


//        }
//        public Table()
//        {
//            players = new PlayerList();
//            deck = new Deck();
//            rand = new Random();
//            mainPot = new Pot();
//            sidePots = new List<Pot>();
//            smallBlind = new Blind();
//            bigBlind = new Blind();
//            roundCounter = 0;
//            turnCount = 0;
//            dealerPosition = 0;
//            //set blind amount and position
//            smallBlind.Amount = 500;
//            bigBlind.Amount = 1000;
//            mainPot.SmallBlind = 500;
//            mainPot.BigBlind = 1000;
//            smallBlind.position = dealerPosition + 1;
//            bigBlind.position = (dealerPosition + 2) %2;
//            currentIndex = dealerPosition;
//        }
//        //indexer of players
//        public Player this[int index]
//        {
//            get
//            {
//                return players.GetPlayer(ref index);
//            }
//            set
//            {
//                players[index] = value;
//            }
//        }

//        //various getters/setters
//        public PlayerList getPlayers()
//        {
//            return players;
//        }
//        public int getDealerPosition()
//        {
//            return dealerPosition;
//        }
//        public int getCurrentIndex()
//        {
//            return currentIndex;
//        }
//        public void setCurrentIndex(int index)
//        {
//            currentIndex = index;
//        }
//        public string getSmallBlind()
//        {
//            return smallBlind.Amount.ToString();
//        }
//        public string getBigBlind()
//        {
//            return bigBlind.Amount.ToString();
//        }
//        public Pot getPot()
//        {
//            return mainPot;
//        }
//        public List<Pot> getSidePots()
//        {
//            return sidePots;
//        }
//        public Hand getCommunityCards()
//        {
//            return tableHand;
//        }
//        public Deck getDeck()
//        {
//            return deck;
//        }
//        /// <summary>
//        /// Remove a player when the player busts out.
//        /// </summary>
//        /// <param name="player"></param>
//        public void RemovePlayer(Player player)
//        {
//            if (player.ChipStack != 0)
//                throw new InvalidOperationException();
//            players.Remove(player);
//        }
//        public void RemovePlayer(int index)
//        {
//            if (players[index].ChipStack != 0)
//                throw new InvalidOperationException();
//            players.RemoveAt(index);
//        }
        
        
//        /// <summary>
//        /// Start a new round, dealer/smallblind position are moved up one spot
//        /// players/counter variables are reset
//        /// blinds are reset if necessary.
//        /// </summary>
//        public void startNextMatch()
//        {
//            players.ResetPlayers();
//            deck = new Deck();
//            if (roundCounter == 10)
//            {
//                roundCounter = 0;
//                smallBlind.Amount *= 2;
//                bigBlind.Amount = smallBlind.Amount * 2;
//                mainPot.SmallBlind = SmallBlind;
//                mainPot.BigBlind = BigBlind;
//            }
//            if (roundCounter != 0)
//            {
//                dealerPosition = incrementIndex(dealerPosition);
//                smallBlind.position = incrementIndex(dealerPosition);
//                bigBlind.position = incrementIndex(smallBlind.position);
//            }
//            roundCounter++;
//            mainPot.Amount = 0;
//            mainPot.AgressorIndex = -1;
//            mainPot.MinimumRaise = bigBlind.Amount;
//            tableHand.Clear();
//            currentIndex = dealerPosition;
//            winnermessage = null;
//            mainPot.getPlayersInPot().Clear();
//            sidePots.Clear();
//        }


//        /// <summary>
//        /// Determine when the current betting round is over
//        /// </summary>
//        /// <returns></returns>
//        public bool beginNextTurn()
//        {
//            turnCount++;
//            while (players[mainPot.AgressorIndex].IsFolded()&&currentIndex!=mainPot.AgressorIndex)
//                mainPot.AgressorIndex = decrementIndex(mainPot.AgressorIndex);
//            if (currentIndex == mainPot.AgressorIndex && turnCount > 1)
//                return false;
//            else if (EveryoneAllIn())
//                return false;
//            else
//                return true;
//        }
//        //method to determine if every player has already went all in
//        public bool EveryoneAllIn()
//        {
//            int zeroCount = 0;
//            int totalCount = 0;
//            for (int i = 0; i < getPlayers().Count; i++)
//            {
//                if (this[i].isbusted || this[i].IsFolded())
//                    continue;
//                if (this[i].ChipStack == 0)
//                    zeroCount++;
//                totalCount++;
//            }
//            if (zeroCount != 0 && totalCount==zeroCount)
//                return true;
//            else if (totalCount - zeroCount == 1)
//            {
//                for (int i = 0; i < getPlayers().Count; i++)
//                {
//                    if (this[i].isbusted || this[i].IsFolded())
//                        continue;
//                    if (this[i].ChipStack != 0 && this[i].getAmountToCall(mainPot) == 0)
//                        return true;
//                }
//            }
//            return false;
//        }
        
//        /// <summary>
//        /// increment index, skipping folded players, busted players and supports 
//        ///wrapping around classes
//        /// </summary>
//        /// <param name="currentIndex"></param>
//        /// <returns></returns>
//        public int incrementIndex(int currentIndex)
//        {
//            currentIndex++;
//            while (players.GetPlayer(ref currentIndex).IsFolded()||players.GetPlayer(ref currentIndex).isbusted||players.GetPlayer(ref currentIndex).ChipStack==0)
//                currentIndex++;
//            players.GetPlayer(ref currentIndex);
//            return currentIndex;
//        }
//        //increment index, not skipping players with a chipstack of zero
//        public int incrementIndexShowdown(int currentIndex)
//        {
//            currentIndex++;
//            while (players.GetPlayer(ref currentIndex).IsFolded() || players.GetPlayer(ref currentIndex).isbusted)
//                currentIndex++;
//            players.GetPlayer(ref currentIndex);
//            return currentIndex;
//        }
//        //same as increment class except in the other direction
//        public int decrementIndex(int currentIndex)
//        {
//            currentIndex--;
//            while (players.GetPlayer(ref currentIndex).IsFolded() || players.GetPlayer(ref currentIndex).isbusted || players.GetPlayer(ref currentIndex).ChipStack == 0)
//                currentIndex--;
//            players.GetPlayer(ref currentIndex);
//            return currentIndex;
//        }

//        //deal two unique cards to all players
//        public void DealHoleCards()
//        {
//            Global.DeckShuffle();
//            for (int i = 0; i < players.Count; i++)
//            {
//                if (i == 0)
//                {
//                    players[i].AddToHand(Global.DeckDeal());
//                    players[i].AddToHand(Global.DeckDeal());
//                }
//                else
//                {
//                    players[i].AddToHand(Global.DeckDeal(false));
//                    players[i].AddToHand(Global.DeckDeal(false));
//                }

//            }
//        }
//        //pay small/big blind amount
//        public void PaySmallBlind()
//        {
//            players.GetPlayer(ref smallBlind.position).PaySmallBlind(smallBlind.Amount, mainPot,currentIndex);
//            currentIndex = smallBlind.position;
//        }
//        public void PayBigBlind()
//        {
//            players.GetPlayer(ref bigBlind.position).PayBigBlind(bigBlind.Amount, mainPot, currentIndex);
//            currentIndex = bigBlind.position;
//            turnCount = 0;
//        }
//        //deal the flop
//        public void DealFlop()
//        {
//            tableHand.Add(Global.DeckDeal());
//            tableHand.Add(Global.DeckDeal());
//            tableHand.Add(Global.DeckDeal());
//            for (int i = 0; i < players.Count; i++)
//            {
//                players[i].AddToHand(tableHand);
//            }
//        }
//        //deal the turn
//        public void DealTurn()
//        {
//            Card turn = Global.DeckDeal();
//            tableHand.Add(turn);
//            for (int i = 0; i < players.Count; i++)
//            {
//                players[i].AddToHand(turn);
//            }
//        }
//        //deal the river
//        public void DealRiver()
//        {
//            Card river = Global.DeckDeal();
//            tableHand.Add(river);
//            for (int i = 0; i < players.Count; i++)
//            {
//                players[i].AddToHand(river);
//            }
//        }
//        //showdown code!
//        public void ShowDown()
//        {
//            //creating sidepots
//            if (CreateSidePots())
//            {
//                mainPot.getPlayersInPot().Sort();
                
//                for (int i = 0; i < mainPot.getPlayersInPot().Count - 1; i++)
//                {
//                    if (mainPot.getPlayersInPot()[i].AmountInPot != mainPot.getPlayersInPot()[i + 1].AmountInPot)
//                    {
//                        PlayerList tempPlayers = new PlayerList();
//                        for (int j = mainPot.getPlayersInPot().Count - 1; j > i; j--)
//                        {
//                            tempPlayers.Add(mainPot.getPlayersInPot()[j]);
//                        }
//                        int potSize = (mainPot.getPlayersInPot()[i + 1].AmountInPot - mainPot.getPlayersInPot()[i].AmountInPot) * tempPlayers.Count;
//                        mainPot.Amount -= potSize;
//                        sidePots.Add(new Pot(potSize, tempPlayers));
//                    }
//                }
//            }
//            //awarding mainpot
//            PlayerList bestHandList = new PlayerList();
//            List<int> Winners = new List<int>();
//            bestHandList = QuickSortBestHand(new PlayerList(mainPot.getPlayersInPot()));
//            for (int i = 0; i < bestHandList.Count; i++)
//            {
//                for (int j = 0; j < this.getPlayers().Count; j++)
//                    if (players[j] == bestHandList[i])
//                    {
//                        Winners.Add(j);
//                    }
//                if (HandEvaluator.getBestHand(new Hand(bestHandList[i].getHand())) != HandEvaluator.getBestHand(new Hand(bestHandList[i + 1].getHand())))
//                    break;
//            }
//            mainPot.Amount /= Winners.Count;
//            if (Winners.Count > 1)
//            {
//                for (int i = 0; i < this.getPlayers().Count; i++)
//                {
//                    if (Winners.Contains(i))
//                    {
//                        currentIndex = i;
//                        players[i].CollectMoney(mainPot);
//                        winnermessage += players[i].Name + ", ";
//                    }
//                }
//                winnermessage +=Environment.NewLine+ " split the pot.";
//            }
//            else
//            {
//                currentIndex = Winners[0];
//                players[currentIndex].CollectMoney(mainPot);
//                winnermessage = players[currentIndex].Message;
//            }
//            //awarding sidepots
//            for (int i = 0; i < sidePots.Count; i++)
//            {
//                List<int> sidePotWinners = new List<int>();
//                for (int x = 0; x < bestHandList.Count; x++)
//                {
//                    for (int j = 0; j < this.getPlayers().Count; j++)
//                        if (players[j] == bestHandList[x]&&sidePots[i].getPlayersInPot().Contains(bestHandList[x]))
//                        {
//                            sidePotWinners.Add(j);
//                        }
//                    if (HandEvaluator.getBestHand(new Hand(bestHandList[x].getHand())) != HandEvaluator.getBestHand(new Hand(bestHandList[x + 1].getHand()))&&sidePotWinners.Count!=0)
//                        break;
//                }
//                sidePots[i].Amount /= sidePotWinners.Count;
//                for (int j = 0; j < this.getPlayers().Count; j++)
//                {
//                    if (sidePotWinners.Contains(j))
//                    {
//                        currentIndex = j;
//                        players[j].CollectMoney(sidePots[i]);
//                    }
//                }
//            }
//        }
//        //check if it is necessary to create sidepots
//        private bool CreateSidePots()
//        {
//            for(int i=0;i<mainPot.getPlayersInPot().Count()-1;i++)
//            {
//                if (mainPot.getPlayersInPot()[i].AmountInPot != mainPot.getPlayersInPot()[i + 1].AmountInPot)
//                    return true;
//            }
//            return false;
//        }
//        PlayerList QuickSortBestHand(PlayerList myPlayers)
//        {
//            Player pivot;
//            Random ran = new Random();

//            if (myPlayers.Count() <= 1)
//                return myPlayers;
//            pivot = myPlayers[ran.Next(myPlayers.Count())];
//            myPlayers.Remove(pivot);

//            var less = new PlayerList();
//            var greater = new PlayerList();
//            // Assign values to less or greater list
//            foreach (Player player in myPlayers)
//            {
//                if (HandEvaluator.getBestHand(new Hand(player.getHand())) > HandEvaluator.getBestHand(new Hand(pivot.getHand())))
//                {
//                    greater.Add(player);
//                }
//                else if (HandEvaluator.getBestHand(new Hand(player.getHand())) <= HandEvaluator.getBestHand(new Hand(pivot.getHand())))
//                {
//                    less.Add(player);
//                }
//            }
//            // Recurse for less and greaterlists
//            var list = new PlayerList();
//            list.AddRange(QuickSortBestHand(greater));
//            list.Add(pivot);
//            list.AddRange(QuickSortBestHand(less));
//            return list;
//        }
//        //check if everyone has folded except the player
//        public bool PlayerWon()
//        {
//            if (mainPot.getPlayersInPot().Count == 1)
//            {
//                foreach (Player player in this)
//                {
//                    if (player.isbusted)
//                        continue;
//                    if (player.IsFolded())
//                        return true;
//                }
//            }
//            return false;
//        }
//        //support for "foreach" loops
//        public IEnumerator<Player> GetEnumerator()
//        {
//            return players.GetEnumerator();
//        }
//    }
//}
