using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poker_MCCFRM
{
    public static class Utilities
    {
        public static uint? BinarySearch<T>(this IList<T> list, IComparable<T> item)
        {
            uint low = 0;
            uint high = (uint)list.Count - 1;

            while (true)
            {
                if (low > high)
                {
                    return null;
                }
                uint index = ((low + high) / 2);
                var comparison = item.CompareTo(list.ElementAt((int)index));
                if (comparison > 0) low = index + 1;
                else if (comparison < 0) high = index - 1;
                else return index;
            }
        }

        public static int? BinaryInsert<T>(this IList<T> list, IComparable<T> item)
        {
            int low = 0;
            int high = list.Count - 1;

            while (true)
            {
                if (low > high)
                {
                    list.Insert(low, (T)item);
                    return low;
                }
                int index = (int)((low + high) / 2);
                var comparison = item.CompareTo(list.ElementAt(index));
                if (comparison > 0) low = index + 1;
                else if (comparison < 0) high = index - 1;
                else return null;
            }
        }
        public static List<Hand> GetStartingHandChart()
        {
            List<Hand> result = new List<Hand>();

            for (int i = 0; i < 169; ++i) {
                string firstCardRank = "";
                switch (i / 13)
                {
                    case 0: firstCardRank = "2"; break;
                    case 1: firstCardRank = "3"; break;
                    case 2: firstCardRank = "4"; break;
                    case 3: firstCardRank = "5"; break;
                    case 4: firstCardRank = "6"; break;
                    case 5: firstCardRank = "7"; break;
                    case 6: firstCardRank = "8"; break;
                    case 7: firstCardRank = "9"; break;
                    case 8: firstCardRank = "T"; break;
                    case 9: firstCardRank = "J"; break;
                    case 10: firstCardRank = "Q"; break;
                    case 11: firstCardRank = "K"; break;
                    case 12: firstCardRank = "A"; break;
                }
                string secondCardRank = "";
                switch(i % 13)
                {
                    case 0: secondCardRank = "2"; break;
                    case 1: secondCardRank = "3"; break;
                    case 2: secondCardRank = "4"; break;
                    case 3: secondCardRank = "5"; break;
                    case 4: secondCardRank = "6"; break;
                    case 5: secondCardRank = "7"; break;
                    case 6: secondCardRank = "8"; break;
                    case 7: secondCardRank = "9"; break;
                    case 8: secondCardRank = "T"; break;
                    case 9: secondCardRank = "J"; break;
                    case 10: secondCardRank = "Q"; break;
                    case 11: secondCardRank = "K"; break;
                    case 12: secondCardRank = "A"; break;
                }
                string firstCardSuit = "S";
                string secondCardSuit = "";
                if(i%13 > i/13)
                {
                    secondCardSuit = "S";
                }
                else
                {
                    secondCardSuit = "H";
                }
                Hand hand = new Hand();
                hand.Cards.Add(new Card(firstCardRank + firstCardSuit));
                hand.Cards.Add(new Card(secondCardRank + secondCardSuit));
                result.Add(hand);
            }
            return result;
        }
    }
}