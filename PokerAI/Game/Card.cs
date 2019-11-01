using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokerAI
{
    public enum RANK
    {
        TWO = 2, THREE, FOUR, FIVE, SIX, SEVEN, EIGHT, NINE, TEN, JACK, QUEEN, KING, ACE
    }
    public enum SUIT
    {
        DIAMONDS = 1,
        CLUBS,
        HEARTS,
        SPADES
    }
    public class Card
    {
        private int rank, suit;
        private String shortString = "";

        //default two of diamonds
        public Card()
        {
            rank = (int)RANK.TWO;
            suit = (int)SUIT.DIAMONDS;
            shortString = rankToStringShort(rank) + suitToStringShort(suit);
        }
        public Card(RANK rank, SUIT suit)
        {
            this.rank = (int)rank;
            this.suit = (int)suit;
            shortString = rankToStringShort(this.rank) + suitToStringShort(this.suit);
        }
        public Card(int rank, int suit)
        {
            if (rank < 2 || rank > 14 || suit < 1 || suit > 4)
                throw new ArgumentOutOfRangeException();
            this.rank = rank;
            this.suit = suit;
            shortString = rankToStringShort(rank) + suitToStringShort(suit);
        }
        public Card(Card card)
        {
            this.rank = card.rank;
            this.suit = card.suit;
        }
        public static string rankToString(int rank)
        {
            switch (rank)
            {
                case 11:
                    return "Jack";
                case 12:
                    return "Queen";
                case 13:
                    return "King";
                case 14:
                    return "Ace";
                default:
                    return rank.ToString();
            }
        }
        public static string rankToStringShort(int rank)
        {
            switch (rank)
            {
                case 10:
                    return "T";
                case 11:
                    return "J";
                case 12:
                    return "Q";
                case 13:
                    return "K";
                case 14:
                    return "A";
                default:
                    return rank.ToString();
            }
        }
        public static string suitToString(int suit)
        {
            switch (suit)
            {
                case 1:
                    return "Diamonds";
                case 2:
                    return "Clubs";
                case 3:
                    return "Hearts";
                default:
                    return "Spades";
            }
        }
        public static string suitToStringShort(int suit)
        {
            switch (suit)
            {
                case 1:
                    return "d";
                case 2:
                    return "c";
                case 3:
                    return "h";
                default:
                    return "s";
            }
        }
        public int getRank()
        {
            return rank;
        }
        public int getSuit()
        {
            return suit;
        }

        public void setRank(RANK rank)
        {
            this.rank = (int)rank;
        }
        public void setCard(RANK rank, SUIT suit)
        {
            this.rank = (int)rank;
            this.suit = (int)suit;
        }
        public void setCard(int rank, int suit)
        {
            if (rank < 1 || rank > 14 || suit < 1 || suit > 4)
                throw new ArgumentOutOfRangeException();
            this.rank = rank;
            this.suit = suit;
        }
        public override string ToString()
        {
            return rankToString(rank) + " of " + suitToString(suit);
        }
        public string ToStringShort()
        {
            return shortString;
        }
        //compare rank of cards
        //public static bool operator ==(Card a, Card b)
        //{
        //    if (a.rank == b.rank)
        //        return true;
        //    else
        //        return false;
        //}
        //public static bool operator !=(Card a, Card b)
        //{
        //    if (a.rank != b.rank)
        //        return true;
        //    else
        //        return false;
        //}
        public static bool operator <(Card a, Card b)
        {
            if (a.rank < b.rank)
                return true;
            else
                return false;
        }
        public static bool operator >(Card a, Card b)
        {
            if (a.rank > b.rank)
                return true;
            else
                return false;
        }
        public static bool operator <=(Card a, Card b)
        {
            if (a.rank <= b.rank)
                return true;
            else
                return false;
        }
        public static bool operator >=(Card a, Card b)
        {
            if (a.rank >= b.rank)
                return true;
            else
                return false;
        }
        public override bool Equals(object obj)
        {
            var anotherCard = obj as Card;
            return anotherCard != null && this.Equals(anotherCard);
        }
        private bool Equals(Card other)
        {
            return this.suit == other.suit && this.rank == other.rank;
        }
        override public int GetHashCode()
        {
            throw new NotImplementedException();
        }
    }
}