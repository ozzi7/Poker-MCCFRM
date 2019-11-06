using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace Poker_MCCFRM
{
    /// <summary>
    /// Cluster the elements in the input array into k distinct buckets and return them 
    /// </summary>
    class Kmeans
    {
        public Kmeans(){ }
        /// <summary>
        /// Returns an array where the element at index i contains the cluster entry associated with the entry
        /// </summary>
        /// <param name="data"></param>
        /// <param name="k"></param>
        /// <returns></returns>
        public int[] ClusterEMD(float[,] data, int k, int nofRuns)
        {
            Console.WriteLine("K-means (EMD) clustering {0} elements into {1} clusters with {2} restarts...", data.GetLength(0), k, nofRuns);

            DateTime start = DateTime.UtcNow;

            int[] bestCenters = new int[data.GetLength(0)];
            int[] recordCenters = new int[data.GetLength(0)]; // we return only indices, the centers are discarded
            long recordDistance = long.MaxValue;

            for (int run = 0; run < nofRuns; ++run)
            {
                bestCenters = new int[data.GetLength(0)];
                float[,] centers = new float[k, data.GetLength(1)];

                // select random centers
                List<int> centerIndices = new List<int>();
                for (int i = 0; i < k; ++i)
                {
                    int index = RandomGen.Next(0, data.GetLength(0));
                    while (centerIndices.Contains(index))
                    {
                        index = RandomGen.Next(0, data.GetLength(0));
                    }
                    CopyArray(data, centers, index, i);
                    centerIndices.Add(index);
                }

                long lastDistance = 0;
                bool distanceChanged = true;
                while(distanceChanged)
                {
                    // find closest cluster for each element
                    long sharedLoopCounter = 0;
                    long totalDistance = 0;
                    using (var progress = new ProgressBar())
                    {
                        Parallel.For(0, Global.NOF_THREADS,
                         i =>
                         {
                             double threadDistance = 0;
                             long iter = 0;

                             for (int j = Util.GetWorkItemsIndices(data.GetLength(0), Global.NOF_THREADS, i).Item1;
                             j < Util.GetWorkItemsIndices(data.GetLength(0), Global.NOF_THREADS, i).Item2; ++j)
                             { // go through all data
                                 double distance = double.MaxValue;
                                 int bestIndex = 0;
                                 for (int m = 0; m < k; ++m) // go through centers
                                 {
                                     float tempEMD = GetEarthMoverDistance(data, centers, j, m);
                                     if (tempEMD < distance)
                                     {
                                         distance = tempEMD;
                                         bestIndex = m;
                                     }
                                 }
                                 bestCenters[j] = bestIndex;
                                 threadDistance += distance;

                                 iter++;
                                 if (iter % 10000 == 0)
                                 {
                                     Interlocked.Add(ref sharedLoopCounter, 10000);
                                     Interlocked.Add(ref totalDistance, (long)(threadDistance * 1000000));
                                     threadDistance = 0;
                                     progress.Report((double)Interlocked.Read(ref sharedLoopCounter) / data.GetLength(0));
                                 }
                             }
                             Interlocked.Add(ref sharedLoopCounter, iter % 10000);
                             progress.Report((double)Interlocked.Read(ref sharedLoopCounter) / data.GetLength(0));

                             Interlocked.Add(ref totalDistance, (long)(threadDistance * 1000000));
                         });
                    }

                    // find new cluster centers // todo: it isnt theoretically sound to take the mean when using EMD distance metric
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
                            if (occurrences[n] != 0)
                                centers[n, m] /= occurrences[n];
                            else
                                break;
                        }
                    }
                    totalDistance /= data.GetLength(0);
                    if (totalDistance == lastDistance)
                    {
                        distanceChanged = false;
                    }
                    long diff = lastDistance - totalDistance;
                    lastDistance = totalDistance;

                    if(totalDistance < recordDistance)
                    {
                        recordDistance = totalDistance;
                        Array.Copy(bestCenters, recordCenters, recordCenters.Length);
                    }
                    Console.WriteLine("Current average distance: {0} Improvement: {1}", (double)totalDistance / 1000000.0,
                        (double)diff / 1000000.0);
                }
            }
            Console.WriteLine("Best distance found: " + (double)recordDistance/ 1000000.0);
            TimeSpan elapsed = DateTime.UtcNow - start;
            Console.WriteLine("K-means clustering (EMD) completed in {0:0.00}s", elapsed.TotalSeconds);

            // print starting hand chart
            return recordCenters;
        }
        /// <summary>
        /// Returns an array where the element at index i contains the cluster entry associated with the entry
        /// </summary>
        /// <param name="data"></param>
        /// <param name="k"></param>
        /// <returns></returns>
        public int[] ClusterL2(float[,] data, int k, int nofRuns)
        {
            Console.WriteLine("K-means clustering (L2) {0} elements into {1} clusters with {2} restarts...", data.GetLength(0), k, nofRuns);

            DateTime start = DateTime.UtcNow;

            int[] bestCenters = new int[data.GetLength(0)];
            int[] recordCenters = new int[data.GetLength(0)]; // we return indices only because the centers are discarded
            long recordDistance = long.MaxValue;

            for (int run = 0; run < nofRuns; ++run)
            {
                bestCenters = new int[data.GetLength(0)];
                float[,] centers = new float[k, data.GetLength(1)];

                // select random centers
                List<int> centerIndices = new List<int>();
                for (int i = 0; i < k; ++i)
                {
                    int index = RandomGen.Next(0, data.GetLength(0));
                    while (centerIndices.Contains(index))
                    {
                        index = RandomGen.Next(0, data.GetLength(0));
                    }
                    CopyArray(data, centers, index, i);
                    centerIndices.Add(index);
                }

                long lastDistance = 0;
                bool distanceChanged = true;
                while (distanceChanged)
                {
                    // find closest cluster for each element
                    long sharedLoopCounter = 0;
                    long totalDistance = 0;
                    using (var progress = new ProgressBar())
                    {
                        Parallel.For(0, Global.NOF_THREADS,
                         i =>
                         {
                             double threadDistance = 0;
                             long iter = 0;
                             for (int j = Util.GetWorkItemsIndices(data.GetLength(0), Global.NOF_THREADS, i).Item1;
                                    j < Util.GetWorkItemsIndices(data.GetLength(0), Global.NOF_THREADS, i).Item2; ++j)
                             { // go through all data
                                 double distance = double.MaxValue;
                                 int bestIndex = 0;
                                 for (int m = 0; m < k; ++m) // go through centers
                                 {
                                     double tempDistance = GetL2Distance(data, centers, j, m);
                                     if (tempDistance < distance)
                                     {
                                         distance = tempDistance;
                                         bestIndex = m;
                                     }
                                 }
                                 bestCenters[j] = bestIndex;
                                 threadDistance += distance;

                                 iter++;
                                 if(iter % 10000 == 0) 
                                 { 
                                     Interlocked.Add(ref sharedLoopCounter, 10000);
                                     Interlocked.Add(ref totalDistance, (long)(threadDistance * 1000000));
                                     threadDistance = 0;
                                     progress.Report((double)Interlocked.Read(ref sharedLoopCounter) / data.GetLength(0));
                                 }
                             }
                             Interlocked.Add(ref sharedLoopCounter, iter % 10000);
                             progress.Report((double)Interlocked.Read(ref sharedLoopCounter) / data.GetLength(0));

                             Interlocked.Add(ref totalDistance, (long)(threadDistance * 1000000));
                         });
                    }

                    // find new cluster centers // todo: it isnt theoretically sound to take the mean when using EMD distance metric
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
                            if (occurrences[n] != 0)
                                centers[n, m] /= occurrences[n];
                            else
                                break;
                        }
                    }
                    totalDistance /= data.GetLength(0);
                    if (totalDistance == lastDistance)
                    {
                        distanceChanged = false;
                    }
                    long diff = lastDistance - totalDistance;
                    lastDistance = totalDistance;

                    if (totalDistance < recordDistance)
                    {
                        recordDistance = totalDistance;
                        Array.Copy(bestCenters, recordCenters, recordCenters.Length);
                    }
                    Console.WriteLine("Current average distance: {0} Improvement: {1}",(double)totalDistance / 1000000.0,
                        (double)diff / 1000000.0);
                }
            }
            Console.WriteLine("Best distance found: " + (double)recordDistance / 1000000.0);
            TimeSpan elapsed = DateTime.UtcNow - start;
            Console.WriteLine("K-means clustering (L2) completed in {0:0.00}s", elapsed.TotalSeconds);

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
        private double GetL2Distance(float[,] data, float[,] centers, int index1, int index2)
        {
            double totalDistance = 0;
            for (int i = 0; i < data.GetLength(1); i++)
            {
                totalDistance += (double)(data[index1, i] - centers[index2, i])* (double)(data[index1, i] - centers[index2, i]);
            }
            return Math.Sqrt(totalDistance);
        }
    }
}
