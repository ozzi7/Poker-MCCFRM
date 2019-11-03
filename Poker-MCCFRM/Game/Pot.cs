//using System;
//using System.Collections.Generic;
//using System.Linq;
//using System.Text;

//namespace Poker_MCCFRM
//{
//    public class Pot
//    {
//        private PlayerList playersInPot = new PlayerList();
//        private int amountInPot;
//        private int minimumRaise;
//        private int maximumAmountPutIn;

//        private int agressorIndex;
//        private int smallBlind, bigBlind;
//        public int SmallBlind
//        {
//            get { return smallBlind; }
//            set { smallBlind = value; }
//        }
//        public int BigBlind
//        {
//            get { return bigBlind; }
//            set { bigBlind = value; }
//        }
//        public int MinimumRaise
//        {
//            get { return minimumRaise; }
//            set
//            {
//                minimumRaise = value;
//            }
//        }
//        public int Amount
//        {
//            get { return amountInPot; }
//            set
//            {
//                if (value < 0)
//                    value = 0;
//                amountInPot = value;
//            }
//        }
//        public int AgressorIndex
//        {
//            get { return agressorIndex; }
//            set { agressorIndex = value; }
//        }
//        //construct pot
//        public Pot()
//        {
//            amountInPot = 0;
//            minimumRaise = 0;
//            maximumAmountPutIn = 0;
//            agressorIndex = -1;
//        }
//        public Pot(int amount, PlayerList playersInPot)
//        {
//            this.Amount = amount;
//            this.playersInPot = playersInPot;
//            agressorIndex = -1;
//        }
//        //getter
//        public PlayerList getPlayersInPot()
//        {
//            return playersInPot;
//        }
//        //add player to pot
//        public void AddPlayer(Player player)
//        {
//            if(!playersInPot.Contains(player))
//                playersInPot.Add(player);
//        }
//        //add money to pot
//        public void Add(int amount)
//        {
//            if (amount < 0)
//                return;
//            amountInPot += amount;
//        }
//        //get maximum amount in pot
//        public int getMaximumAmountPutIn()
//        {
//            return maximumAmountPutIn;
//        }
//        //set maximum amount in pot
//        public void setMaximumAmount(int amount)
//        {
//            maximumAmountPutIn = amount;
//        }
        
//    }
//}
