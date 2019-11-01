namespace PokerAI
{
    using System.Collections.Generic;
    using System.Linq;

    /// <summary>
    /// Class containing helper methods for evaluating and comparing player's hands.
    /// </summary>
    public static class Helpers
    {
        public static int CompareCards(IEnumerable<Card> firstPlayerCards, IEnumerable<Card> secondPlayerCards)
        {
            Hand firstHand = new Hand();
            Hand secondHand = new Hand();
            foreach (Card card in firstPlayerCards)
            {
                firstHand.Add(card);
            }
            foreach (Card card in secondPlayerCards)
            {
                secondHand.Add(card);
            }
            var firstPlayerBestHand = HandEvaluator.getBestHand(firstHand);
            var secondPlayerBestHand = HandEvaluator.getBestHand(secondHand);
            if (firstPlayerBestHand < secondPlayerBestHand) return -1;
            if (firstPlayerBestHand == secondPlayerBestHand) return 0;
            else return 1;
        }
    }
}
