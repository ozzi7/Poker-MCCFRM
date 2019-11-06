using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poker_MCCFRM
{
	public class Deck
	{
		private ulong[] cards;
		private ulong removedCards;
		private int position;

		// TODO: this metric doesn't account for removed cards
		public int CardsRemaining {
            get { return 52 - position; } }

		public Deck(ulong removedCards = 0)
		{
			this.removedCards = removedCards;
			cards = new ulong[52];
			for (int i = 0; i < 52; i++) cards[i] = 1ul << i;
			position = 0;
		}

		public void Shuffle(int from = 0)
		{
            for (int i = from; i < 52 - 1; i++) // from =position maybe
            {
                int n = RandomGen.Next(position, 52);
                ulong temp = cards[i]; // could skip if n == i
                cards[i] = cards[n];
                cards[n] = temp;
            }
        }

        public ulong Draw_(int count)
		{
			ulong hand = 0;
			for (int i = 0; i < count; i++)
			{
				while ((cards[position] & removedCards) != 0) position++;
				hand |= cards[position];
				position++;
			}
			return hand;
		}
        public ulong Draw(int position)
        {
            position++;
            return cards[position];
        }
    }
}
