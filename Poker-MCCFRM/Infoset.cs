using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Poker_MCCFRM
{
    public class Infoset
    {
        public float[] regret;
        public float[] strategy;
        public float[] actionCounter;

        public Infoset()
        {
        }
        public Infoset(int actions)
        {
            if (actions != 0)
            {
                // chance node
                regret = new float[actions];
                strategy = new float[actions];
                actionCounter = new float[actions];
            }
        }
        public List<float> CalculateStrategy()
        {
            float sum = 0;
            List<float> moveProbs = new List<float>(new float[regret.Length]);
            for (int a = 0; a < regret.Length; ++a)
            {
                sum += Math.Max(0, regret[a]);
            }
            for (int a = 0; a < regret.Length; ++a)
            {
                if (sum > 0)
                {
                    moveProbs[a] = Math.Max(0, regret[a]) / sum;
                }
                else
                {
                    moveProbs[a] = 1.0f / regret.Length;
                }
            }
            return moveProbs;
        }
        public List<float> GetFinalStrategy()
        {
            float sum = 0;
            List<float> moveProbs = new List<float>(new float[regret.Length]);
            for (int a = 0; a < regret.Length; ++a)
            {
                sum += actionCounter[a];
            }
            for (int a = 0; a < regret.Length; ++a)
            {
                if (sum > 0)
                {
                    moveProbs[a] = actionCounter[a] / sum;
                }
                else
                {
                    moveProbs[a] = 1.0f / regret.Length;
                }
            }
            return moveProbs;
        }
    }
}
