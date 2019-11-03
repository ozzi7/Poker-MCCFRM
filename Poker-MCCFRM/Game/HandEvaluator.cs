using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace PokerMCCFRM
{
    /// <summary>
    /// the most enjoyable class to program, evaluate the best 5 cards out of 7 cards
    /// also determines information needed to compare hands
    /// all hands are sorted
    /// </summary>
    public static class HandEvaluator
    {
        //get best class without running isRoyalFlush, since straightflush covers the royal flush
        public static Hand getBestHand(Hand hand)
        {
            if (hand.Count() < 5)
            {
                hand.Clear();
                return hand;
            }
            if (isStraightFlush(hand))
                return getStraightFlush(hand);
            if (isFourOfAKind(hand))
                return getFourOfAKind(hand);
            if (isFullHouse(hand))
                return getFullHouse(hand);
            if (isFlush(hand))
                return getFlush(hand);
            if (isStraight(hand))
                return getStraight(hand);
            if (isThreeOfAKind(hand))
                return getThreeOfAKind(hand);
            if (isTwoPair(hand))
                return getTwoPair(hand);
            if (isOnePair(hand))
                return getOnePair(hand);
            return getHighCard(hand);
        }
        
        //use recursion to get rid of pairs, then evaluate straight flush
        public static bool isStraightFlush(Hand hand)
        {
            if(!isFlush(hand))
            {
                return false;
            }
            Hand simplifiedhand1, simplifiedhand2; //to be set the same as hand - cards are removed from this hand to evaluate straights separately without the interference of pairs or three-of-a-kind
            hand.sortByRank();
            for (int i = 0; i <= hand.Count() - 2; i++)
            {
                if (hand.getCard(i) == hand.getCard(i + 1))
                {
                    simplifiedhand1 = new Hand(hand);
                    simplifiedhand1.Remove(i);
                    simplifiedhand2 = new Hand(hand);
                    simplifiedhand2.Remove(i + 1);
                    if (isStraightFlush(simplifiedhand1))
                        return true;
                    if (isStraightFlush(simplifiedhand2))
                        return true;
                }
            }
            for (int i = 0; i <= hand.Count() - 5; i++) // if 5 cards, check once, if 6 check another time starting at 1
            {
                int currentrank = hand.getCard(i).getRank(), currentsuit = hand.getCard(i).getSuit();
                if (currentrank == hand.getCard(i + 1).getRank() + 1 
                    && currentrank == hand.getCard(i + 2).getRank() + 2 
                    && currentrank == hand.getCard(i + 3).getRank() + 3 
                    && currentrank == hand.getCard(i + 4).getRank() + 4

                    && currentsuit == hand.getCard(i + 1).getSuit() 
                    && currentsuit == hand.getCard(i + 2).getSuit() 
                    && currentsuit == hand.getCard(i + 3).getSuit() 
                    && currentsuit == hand.getCard(i + 4).getSuit())
                    return true;
                
            }
            for (int i = 1; i <= hand.Count() - 4; i++) // if flush with A.. 
            {
                int currentrank = hand.getCard(i).getRank(), currentsuit = hand.getCard(i).getSuit();
                if (currentrank == 5
                    && hand.getCard(i + 1).getRank() == 4 
                    && hand.getCard(i + 2).getRank() == 3 
                    && hand.getCard(i + 3).getRank() == 2 
                    && hand.getCard(0).getRank() == 14 

                    && currentsuit == hand.getCard(i + 1).getSuit() 
                    && currentsuit == hand.getCard(i + 2).getSuit() 
                    && currentsuit == hand.getCard(i + 3).getSuit() 
                    && currentsuit == hand.getCard(0).getSuit())
                    return true;
            }
            return false;
        }
        //get straight flush using two pointer variable and taking care of all cases
        public static Hand getStraightFlush(Hand hand)
        {
            hand.sortByRank();
            Hand straightflush = new Hand();
            straightflush.setValue(9);

            // get suit
            int detectedSuit = -1;
            for (int suit = 1; suit <= 4; ++suit)
            {
                int count = 0;
                for (int i = 0; i < hand.Count(); ++i)
                {
                    if (hand.getCard(i).getSuit() == suit)
                    {
                        count++;
                    }
                }
                if (count >= 5)
                {
                    detectedSuit = suit;
                    break;
                }
            }

            // add 5 cards of the detected suit
            int cardsAdded = 0;
            Card tempAce = null;
            for (int i = 0; i < hand.Count(); ++i)
            {
                if (hand.getCard(i).getSuit() == detectedSuit)
                {
                    if (hand.getCard(i).getRank() == 14)
                        tempAce = hand.getCard(i);
                    else
                    {
                        straightflush.Add(hand.getCard(i));
                        cardsAdded++;
                    }
                }
                if (cardsAdded == 5) break;
            }
            if (cardsAdded == 4)
            {
                straightflush.Add(tempAce);
            }
            straightflush.setValue(straightflush.getCard(0).getRank());

            return straightflush;
        }
        //easy algorithm to understand, just loop through the array and check for a certain amount of pairs
        //same for 3 of a kind, full house, 2 pair and 1 pair
        public static bool isFourOfAKind(Hand hand)
        {
            hand.sortByRank();
            for (int i = 0; i <= hand.Count() - 4; i++)
            {
                if (hand.getCard(i) == hand.getCard(i + 1) && hand.getCard(i) == hand.getCard(i + 2) && hand.getCard(i) == hand.getCard(i + 3))
                    return true;
            }
            return false;
        }
        //same as above except return the cards themselves
        public static Hand getFourOfAKind(Hand hand)
        {
            Hand fourofakind = new Hand();
            fourofakind.setValue(8);
            hand.sortByRank();
            for (int i = 0; i <= hand.Count() - 4; i++)
            {
                if (hand.getCard(i) == hand.getCard(i + 1) && hand.getCard(i) == hand.getCard(i + 2) && hand.getCard(i) == hand.getCard(i + 3))
                {
                    fourofakind.Add(hand.getCard(i));
                    fourofakind.Add(hand.getCard(i + 1));
                    fourofakind.Add(hand.getCard(i + 2));
                    fourofakind.Add(hand.getCard(i + 3));
                    fourofakind.setValue(hand.getCard(i).getRank());
                    break;
                }
            }
            return getKickers(hand,fourofakind);
        }
        
        public static bool isFullHouse(Hand hand)
        {
            hand.sortByRank();
            bool threeofakind = false, pair = false;
            int threeofakindRank = 0;
            for (int i = 0; i <= hand.Count() - 3; i++)
            {
                if (hand.getCard(i) == hand.getCard(i + 1) && hand.getCard(i) == hand.getCard(i + 2))
                {
                    threeofakind = true;
                    threeofakindRank = hand.getCard(i).getRank();
                    break;
                }
            }
            for (int i = 0; i <= hand.Count() - 2; i++)
            {
                if (hand.getCard(i) == hand.getCard(i + 1) && hand.getCard(i).getRank() != threeofakindRank)
                {
                    pair = true;
                    break;
                }
            }
            if (threeofakind == true && pair == true)
                return true;
            else
                return false;
        }
        public static Hand getFullHouse(Hand hand)
        {
            hand.sortByRank();
            Hand fullhouse = new Hand();
            fullhouse.setValue(7);
            bool threeofakind = false, pair = false;
            int threeofakindRank = 0;
            for (int i = 0; i <= hand.Count() - 3; i++)
            {
                if (hand.getCard(i) == hand.getCard(i + 1) && hand.getCard(i) == hand.getCard(i + 2))
                {
                    threeofakind = true;
                    threeofakindRank = hand.getCard(i).getRank();
                    fullhouse.Add(hand.getCard(i));
                    fullhouse.Add(hand.getCard(i + 1));
                    fullhouse.Add(hand.getCard(i + 2));
                    fullhouse.setValue(hand.getCard(i).getRank());
                    break;
                }
            }
            for (int i = 0; i <= hand.Count() - 2; i++)
            {
                if (hand.getCard(i) == hand.getCard(i + 1) && hand.getCard(i).getRank() != threeofakindRank)
                {
                    pair = true;
                    fullhouse.Add(hand.getCard(i));
                    fullhouse.Add(hand.getCard(i + 1));
                    fullhouse.setValue(hand.getCard(i).getRank());
                    break;
                }
            }
            if (threeofakind == true && pair == true)
                return fullhouse;
            else
            {
                fullhouse.Clear();
                return fullhouse;
            }
        }
        //use a counter, if a counter reaches five, a flush is detected
        public static bool isFlush(Hand hand)
        {
            int diamondCount = 0, clubCount = 0, heartCount = 0, spadeCount = 0;
            for (int i = 0; i < hand.Count(); i++)
            {
                if ((SUIT)hand.getCard(i).getSuit() == SUIT.DIAMONDS)
                    diamondCount++;
                else if ((SUIT)hand.getCard(i).getSuit() == SUIT.CLUBS)
                    clubCount++;
                else if ((SUIT)hand.getCard(i).getSuit() == SUIT.HEARTS)
                    heartCount++;
                else if ((SUIT)hand.getCard(i).getSuit() == SUIT.SPADES)
                    spadeCount++;
            }
            if (diamondCount >= 5)
                return true;
            else if (clubCount >= 5)
                return true;
            else if (heartCount >= 5)
                return true;
            else if (spadeCount >= 5)
                return true;
            return false;
        }
        //use a counter to determine with suit forms a flush
        //then get all cards from the suit
        public static Hand getFlush(Hand hand)
        {
            hand.sortByRank();
            Hand flush = new Hand();
            flush.setValue(6);
            int diamondCount = 0, clubCount = 0, heartCount = 0, spadeCount = 0;
            for (int i = 0; i < hand.Count(); i++)
            {
                if ((SUIT)hand.getCard(i).getSuit() == SUIT.DIAMONDS)
                    diamondCount++;
                else if ((SUIT)hand.getCard(i).getSuit() == SUIT.CLUBS)
                    clubCount++;
                else if ((SUIT)hand.getCard(i).getSuit() == SUIT.HEARTS)
                    heartCount++;
                else if ((SUIT)hand.getCard(i).getSuit() == SUIT.SPADES)
                    spadeCount++;
            }
            if (diamondCount >= 5)
            {
                for (int i = 0; i < hand.Count(); i++)
                {
                    if (hand.getCard(i).getSuit() == 1)
                    {
                        flush.Add(hand.getCard(i));
                        flush.setValue(hand.getCard(i).getRank());
                    }
                    if (flush.Count() == 5)
                        break;
                }
                //return flush;
            }
            else if (clubCount >= 5)
            {
                for (int i = 0; i <= hand.Count(); i++)
                {
                    if (hand.getCard(i).getSuit() == 2)
                    {
                        flush.Add(hand.getCard(i));
                        flush.setValue(hand.getCard(i).getRank());
                    }
                    if (flush.Count() == 5)
                        break;
                }
                //return flush;
            }
            else if (heartCount >= 5)
            {
                for (int i = 0; i <= hand.Count(); i++)
                {
                    if (hand.getCard(i).getSuit() == 3)
                    {
                        flush.Add(hand.getCard(i));
                        flush.setValue(hand.getCard(i).getRank());
                    }
                    if (flush.Count() == 5)
                        break;
                }
                //return flush;
            }
            else if (spadeCount >= 5)
            {
                for (int i = 0; i <= hand.Count(); i++)
                {
                    if (hand.getCard(i).getSuit() == 4)
                    {
                        flush.Add(hand.getCard(i));
                        flush.setValue(hand.getCard(i).getRank());
                    }
                    if (flush.Count() == 5)
                        break;
                }
                //return flush;
            }
            return flush;
        }
        //explanation below
        public static bool isStraight(Hand hand)
        {
            hand.sortByRank();
            if(hand.getCard(0).getRank()==14)
                hand.Add(new Card((int)RANK.ACE,hand.getCard(0).getSuit()));
            int straightCount=1;
            for (int i = 0; i <= hand.Count() - 2; i++)
            {
                //if 5 cards are found to be straights, break out of the loop
                if (straightCount == 5)
                    break;
                int currentrank = hand.getCard(i).getRank();
                //if cards suit differ by 1, increment straight
                if (currentrank - hand.getCard(i + 1).getRank() == 1)
                    straightCount++;
                //specific condition for 2-A
                else if (currentrank == 2 && hand.getCard(i + 1).getRank() == 14)
                    straightCount++;
                //if cards suit differ by more than 1, reset straight to 1
                else if (currentrank - hand.getCard(i + 1).getRank() > 1)
                    straightCount = 1;
                //if card suits does not differ, do nothing
            }
            if (hand.getCard(0).getRank() == 14)
                hand.Remove(hand.Count() - 1);
            //depending on the straight count, return true or false
            if (straightCount == 5)
                return true;
            return false;
        }
        //explaination below, same as isStraight except return cards
        public static Hand getStraight(Hand hand)
        {
            hand.sortByRank();
            Hand straight = new Hand();
            straight.setValue(5);
            if (hand.getCard(0).getRank() == 14)
                hand.Add(new Card((int)RANK.ACE, hand.getCard(0).getSuit()));
            int straightCount = 1;
            straight.Add(hand.getCard(0));
            for (int i = 0; i <= hand.Count() - 2; i++)
            {
                //if 5 cards are found to be straights, break out of the loop
                if (straightCount == 5)
                    break;
                int currentrank = hand.getCard(i).getRank();
                //if cards suit differ by 1, increment straight
                if (currentrank - hand.getCard(i + 1).getRank() == 1)
                {
                    straightCount++;
                    straight.Add(hand.getCard(i+1));
                }
                //specific condition for 2-A
                else if (currentrank == 2 && hand.getCard(i + 1).getRank() == 14)
                {
                    straightCount++;
                    straight.Add(hand.getCard(i+1));
                }
                //if cards suit differ by more than 1, reset straight to 1
                else if (currentrank - hand.getCard(i + 1).getRank() > 1)
                {
                    straightCount = 1;
                    straight.Clear();
                    straight.setValue(5);
                    straight.Add(hand.getCard(i+1));
                }
                //if card suits does not differ, do nothing
            }
            //depending on the straight count, return true or false
            if (hand.getCard(0).getRank() == 14)
                hand.Remove(hand.Count() - 1);
            if (straightCount != 5)
                straight.Clear();
            straight.setValue(straight.getCard(0).getRank());
            return straight;
        }

        public static bool isThreeOfAKind(Hand hand)
        {
            hand.sortByRank();
            for (int i = 0; i <= hand.Count() - 3; i++)
            {
                if (hand.getCard(i) == hand.getCard(i + 1) && hand.getCard(i) == hand.getCard(i + 2))
                    return true;
            }
            return false;
        }
        public static Hand getThreeOfAKind(Hand hand)
        {
            hand.sortByRank();
            Hand threeofakind = new Hand();
            threeofakind.setValue(4);
            for (int i = 0; i <= hand.Count() - 3; i++)
            {
                if (hand.getCard(i) == hand.getCard(i + 1) && hand.getCard(i) == hand.getCard(i + 2))
                {
                    threeofakind.setValue(hand.getCard(i).getRank());
                    threeofakind.Add(hand.getCard(i));
                    threeofakind.Add(hand.getCard(i + 1));
                    threeofakind.Add(hand.getCard(i + 2));
                    break;
                }
            }
            return getKickers(hand, threeofakind);
        }

        public static bool isTwoPair(Hand hand)
        {
            hand.sortByRank();
            int pairCount = 0;
            for (int i = 0; i <= hand.Count() - 2; i++)
            {
                if (hand.getCard(i) == hand.getCard(i + 1))
                {
                    pairCount++;
                    i++; //the pair has already been checked, i must be incremented an additional time to avoid using a card in this pair again. This prevents the program from identifying 3 of a kind as 2 pairs.
                }
            }
            if (pairCount >= 2)
                return true;
            else
                return false;
        }
        public static Hand getTwoPair(Hand hand)
        {
            hand.sortByRank();
            Hand twopair = new Hand();
            twopair.setValue(3);
            int pairCount = 0;
            for (int i = 0; i <= hand.Count() - 2; i++)
            {
                if (hand.getCard(i) == hand.getCard(i + 1))
                {
                    twopair.setValue(hand.getCard(i).getRank());
                    twopair.Add(hand.getCard(i));
                    twopair.Add(hand.getCard(i+1));
                    pairCount++;
                    if (pairCount == 2)
                        break;
                    i++; //the pair has already been checked, i must be incremented an additional time to avoid using a card in this pair again. This prevents the program from identifying 3 of a kind as 2 pairs.
                }
            }
            if (pairCount == 2)
                return getKickers(hand,twopair);
            else
                twopair.Clear();
                return twopair;
        }

        public static bool isOnePair(Hand hand)
        {
            hand.sortByRank();
            for (int i = 0; i <= hand.Count() - 2; i++)
            {
                if (hand.getCard(i) == hand.getCard(i + 1))
                    return true;
            }
            return false;
        }
        public static Hand getOnePair(Hand hand)
        {
            hand.sortByRank();
            Hand onepair = new Hand();
            onepair.setValue(2);
            for (int i = 0; i <= hand.Count() - 2; i++)
            {
                if (hand.getCard(i) == hand.getCard(i + 1))
                {
                    onepair.setValue(hand.getCard(i).getRank());
                    onepair.Add(hand.getCard(i));
                    onepair.Add(hand.getCard(i + 1));
                    break;
                }
            }
            return getKickers(hand, onepair);
        }

        public static bool isHighCard(Hand hand)
        {
            return true;
        }
        //get highest cards after sorting
        public static Hand getHighCard(Hand hand)
        {
            hand.sortByRank();
            Hand highcard = new Hand();
            highcard.setValue(1);
            highcard.Add(hand.getCard(0));
            highcard.setValue(hand.getCard(0).getRank());
            return getKickers(hand, highcard);
        }
        //get all remaining cards, if necessary, to form 5 cards
        private static Hand getKickers(Hand hand, Hand specialCards)
        {
            if (specialCards.Count() == 0)
                return specialCards;
            for (int i = 0; i < specialCards.Count(); i++)
            {
                hand.Remove(specialCards.getCard(i));
            }
            for (int i = 0; i < hand.Count();i++)
            {
                if (specialCards.Count() >= 5)
                    break;
                specialCards.Add(hand.getCard(i));
                specialCards.setValue(hand.getCard(i).getRank());
            }
            return specialCards;
        }
    }
}
