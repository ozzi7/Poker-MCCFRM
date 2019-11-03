using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Poker_MCCFRM
{
    /// <summary>
    /// standard fair deck of 52 cards
    /// </summary>
    public class Deck
    {
        private List<Card> deck = new List<Card>();

        public void Create()
        {
            for (int i = 2; i <= 14; i++)
            {
                for (int j = 1; j <= 4; j++)
                {
                    deck.Add(new Card(i, j));
                }
            }
        }
        public void Shuffle(int startIndex = 0)
        {
            for (int i = startIndex; i < 52 - 1; i++)
            {
                int n = RandomGen.Next(i, 52);
                Card temp = deck[i]; // could skip if n == i
                deck[i] = deck[n];
                deck[n] = temp;
            }
        }
        public string Print()
        {
            string output = "";
            foreach (Card card in deck)
            {
                output += card.ToString() + " ";
            }
            return output;
        }
        public Card Deal(int cardIndex)
        {
            return deck.ElementAt(cardIndex);
        }
        public List<Card> ToList()
        {
            return deck;
        }
    }
}
