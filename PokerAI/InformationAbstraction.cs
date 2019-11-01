using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace PokerAI
{
    class InformationAbstraction
    {
        private int publicBuckets = 60;
        private int publicStates = 1755;
        private int setsPerPublicState = 1176;
        private int priorBuckets = 5000;
        private List<int> TransitionTable;

        public InformationAbstraction()
        {
            CalculateTransitionTable();
        }
        private void CalculateTransitionTable()
        {
            TransitionTable = new List<int>();
            int[,] PublicFlopHands = new int[publicStates, 3];


        }
    }
}
