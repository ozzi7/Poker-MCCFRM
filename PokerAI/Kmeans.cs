using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace PokerAI
{
    /// <summary>
    /// Cluster the elements in the input array into k distinct buckets and return them 
    /// </summary>
    class Kmeans
    {
        int nofRuns = 4;
       
        public Kmeans(){ }
        /// <summary>
        /// Returns an array where the element at index i contains the cluster entry associated with the entry
        /// </summary>
        /// <param name="data"></param>
        /// <param name="k"></param>
        /// <returns></returns>
        public int[] Cluster(float[,] data, int k)
        {
            Console.WriteLine("K-means clustering {0} elements into {1} clusters with {2} restarts...", data.GetLength(0), k, nofRuns);

            DateTime start = DateTime.UtcNow;

            int[] bestCenters = new int[data.GetLength(0)];
            int[] recordCenters = new int[data.GetLength(0)]; // we return indices only because the centers are discarded
            float recordDistance = float.MaxValue;

            for (int run = 0; run < nofRuns; ++run)
            {
                bestCenters = new int[data.GetLength(0)];
                float[,] centers = new float[k, data.GetLength(1)];

                // select random centers
                List<int> centerIndices = new List<int>();
                for (int i = 0; i < k; ++i)
                {
                    int index = RandomGen.Next(0, data.GetLength(1));
                    while (centerIndices.Contains(index))
                    {
                        index = RandomGen.Next(0, data.GetLength(0));
                    }
                    CopyArray(data, centers, index, i);
                    centerIndices.Add(index);
                }

                Int64 lastDistance = 0;
                bool distanceChanged = true;
                while(distanceChanged)
                {
                    // find closest cluster for each element
                    long sharedLoopCounter = 0;
                    Int64 totalDistance = 0;
                    using (var progress = new ProgressBar())
                    {
                        Parallel.For(0, 24,
                         i =>
                         {
                             for (int j = i * data.GetLength(0) / 24; j < i * data.GetLength(0) / 24 + data.GetLength(0) / 24; j++)
                             { // go through all data
                                 float distance = float.MaxValue;
                                 int bestIndex = 0;
                                 for (int m = 0; m < k; ++m) // go through centers
                                 {
                                     if (GetEarthMoverDistance(data, centers, j, m) < distance)
                                     {
                                         distance = GetEarthMoverDistance(data, centers, j, m);
                                         bestIndex = m;
                                     }
                                 }
                                 bestCenters[j] = bestIndex;
                                 Interlocked.Add(ref totalDistance, (Int64)(distance * 1000000));
                             }
                             Interlocked.Add(ref sharedLoopCounter, 1);
                             progress.Report((double)Interlocked.Read(ref sharedLoopCounter) / (24 * 100));
                         });
                    }


                    // find new cluster centers
                    centers = new float[k, data.GetLength(1)];
                    int[] occurrences = new int[k];
                    for (int j = 0; j < data.GetLength(0); j++)
                    {
                        for (int m = 0; m < data.GetLength(1); ++m)
                        {
                            centers[bestCenters[j], m] += data[j, m];
                        }
                        occurrences[bestCenters[j]]++;
                    }
                    for (int n = 0; n < k; ++n)
                    {
                        for (int m = 0; m < data.GetLength(1); ++m)
                        {
                            centers[n, m] /= occurrences[n];
                        }
                    }
                    if(totalDistance == lastDistance)
                    {
                        distanceChanged = false;
                    }
                    lastDistance = totalDistance;

                    if(totalDistance < recordDistance)
                    {
                        recordDistance = totalDistance;
                        Array.Copy(bestCenters, recordCenters, recordCenters.Length);
                    }
                    Console.WriteLine("Current total distance: " + (double)totalDistance / 1000000.0);
                }
            }
            Console.WriteLine("Best distance found: " + (double)recordDistance/ 1000000.0);
            TimeSpan elapsed = DateTime.UtcNow - start;
            Console.WriteLine("K-means clustering completed in {0:0.00}s", elapsed.TotalSeconds);

            // print starting hand chart
            return recordCenters;
        }
        private void CopyArray(float[,] data, float[,] centers, int indexData, int indexCenter)
        {
            // probably should use buffer.blockcopy (todo)
            for (int i = 0; i < data.GetLength(1); ++i)
            {
                centers[indexCenter, i] = data[indexData, i]; 
            }
        }
        private float GetEarthMoverDistance(float[,] data, float[,] centers, int index1, int index2)
        {
            float emd = 0, totalDistance = 0;
            for (int i = 0; i < data.GetLength(1); i++)
            {
                emd = (data[index1,i] + emd) - centers[index2, i];
                totalDistance += Math.Abs(emd);
            }
            return totalDistance;
        }
    }
}
