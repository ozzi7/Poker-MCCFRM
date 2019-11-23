using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Numerics;
using System.Runtime.Serialization.Formatters.Binary;
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
        public int[] ClusterEMD(float[][] data, int k, int nofRuns, string tempFileName)
        {
            Console.WriteLine("K-means++ (EMD) clustering {0} elements into {1} clusters with {2} runs...", data.Count(), k, nofRuns);

            DateTime start = DateTime.UtcNow;

            double saveToFileMinImprovement = 0.05;
            int[] bestCenters = new int[data.Count()];
            int[] recordCenters = new int[data.Count()]; // we return indices only, the centers are discarded
            double recordDistance = double.MaxValue;

            for (int run = 0; run < nofRuns; ++run)
            {
                bestCenters = new int[data.Count()];
                float[,] centers = FindStartingCentersEMD(data, k);

                Console.WriteLine("K-means++ starting clustering...");
                double lastDistance = double.MaxValue;
                bool distanceChanged = true;
                while (distanceChanged)
                {
                    // find closest cluster for each element
                    long sharedLoopCounter = 0;
                    double totalDistance = 0;
                    using (var progress = new ProgressBar())
                    {
                        Parallel.For(0, Global.NOF_THREADS,
                         i =>
                         {
                             double threadDistance = 0;
                             long iter = 0;
                             for (int j = Util.GetWorkItemsIndices(data.Count(), Global.NOF_THREADS, i).Item1;
                                    j < Util.GetWorkItemsIndices(data.Count(), Global.NOF_THREADS, i).Item2; ++j)
                             { // go through all data
                                 double distance = double.MaxValue;
                                 int bestIndex = 0;
                                 for (int m = 0; m < k; ++m) // go through centers
                                 {
                                     double tempDistance = GetEarthMoverDistance(data, centers, j, m);
                                     if (tempDistance < distance)
                                     {
                                         distance = tempDistance;
                                         bestIndex = m;
                                     }
                                 }
                                 bestCenters[j] = bestIndex;
                                 threadDistance += Math.Sqrt(distance);

                                 iter++;
                                 if (iter % 10000 == 0)
                                 {
                                     Interlocked.Add(ref sharedLoopCounter, 10000);
                                     AddDouble(ref totalDistance, threadDistance);
                                     threadDistance = 0;
                                     progress.Report((double)Interlocked.Read(ref sharedLoopCounter) / data.Count(), sharedLoopCounter);
                                 }
                             }
                             Interlocked.Add(ref sharedLoopCounter, iter % 10000);
                             progress.Report((double)Interlocked.Read(ref sharedLoopCounter) / data.Count(), sharedLoopCounter);

                             AddDouble(ref totalDistance, threadDistance);
                         });
                    }


                    // find new cluster centers // todo: it isnt theoretically sound to take the mean when using EMD distance metric
                    centers = new float[k,  data[0].Count()];
                    int[] occurrences = new int[k];
                    for (int j = 0; j < data.Count(); j++)
                    {
                        for (int m = 0; m <  data[0].Count(); ++m)
                        {
                            centers[bestCenters[j], m] += data[j][m];
                        }
                        occurrences[bestCenters[j]]++;
                    }
                    for (int n = 0; n < k; ++n)
                    {
                        for (int m = 0; m <  data[0].Count(); ++m)
                        {
                            if (occurrences[n] != 0)
                                centers[n, m] /= occurrences[n];
                            else
                                break;
                        }
                    }

                    // check stopping criteria
                    totalDistance /= data.Count();
                    if (totalDistance == lastDistance)
                    {
                        distanceChanged = false;
                    }
                    Console.WriteLine(data.Count());
                    Console.WriteLine(data[0].Count());

                    double diff = lastDistance - totalDistance;
                    if (totalDistance < recordDistance)
                    {
                        recordDistance = totalDistance;
                        Array.Copy(bestCenters, recordCenters, recordCenters.Length);
                    }
                    Console.WriteLine("Current average distance: {0} Improvement: {1}, {2}%", totalDistance, diff,
                        100.0 * (1.0 - totalDistance / lastDistance));

                    if (totalDistance > lastDistance * (1.0 - saveToFileMinImprovement) && totalDistance < lastDistance)
                    {
                        Console.WriteLine("Less than {0}% change in average distance, saving intermediate table to file...",
                           saveToFileMinImprovement * 100.0);

                        Console.WriteLine("Saving cluster index to file {0}", tempFileName);
                        using var fileStream = File.Create(tempFileName);
                        BinaryFormatter bf = new BinaryFormatter();
                        bf.Serialize(fileStream, recordCenters);

                        saveToFileMinImprovement *= 0.5f;
                    }

                    lastDistance = totalDistance;
                }
            }
            Console.WriteLine("Best distance found: " + recordDistance);
            TimeSpan elapsed = DateTime.UtcNow - start;
            Console.WriteLine("K-means++ clustering (EMD) completed in {0}d {1}h {2}m {3}s", elapsed.Days, elapsed.Hours, elapsed.Minutes, elapsed.Seconds);

            // print starting hand chart
            return recordCenters;
        }
        /// <summary>
        /// Returns an array where the element at index i contains the cluster entry associated with the entry
        /// </summary>
        /// <param name="data"></param>
        /// <param name="k"></param>
        /// <returns></returns>
        public int[] ClusterL2(float[][] data, int k, int nofRuns)
        {
            Console.WriteLine("K-means++ clustering (L2) {0} elements into {1} clusters with {2} runs...", data.Count(), k, nofRuns);

            DateTime start = DateTime.UtcNow;

            double saveToFileMinImprovement = 0.05;
            int[] bestCenters = new int[data.Count()];
            int[] recordCenters = new int[data.Count()]; // we return indices only because the centers are discarded
            double recordDistance = double.MaxValue;

            for (int run = 0; run < nofRuns; ++run)
            {
                bestCenters = new int[data.Count()];
                float[,] centers = FindStartingCentersL2(data, k);
                float[,] centerCenterDistances = new float[k,k];

                Console.WriteLine("K-means++ starting clustering...");
                double lastDistance = double.MaxValue;
                bool distanceChanged = true;
                while (distanceChanged)
                {
                    // calculate cluster-cluster distances to use triangle inequality
                    CalculateClusterDistancesL2(centerCenterDistances, centers);

                    // find closest cluster for each element
                    long sharedLoopCounter = 0;
                    double totalDistance = 0;
                    using (var progress = new ProgressBar())
                    {
                        Parallel.For(0, Global.NOF_THREADS,
                         i =>
                         {
                             double threadDistance = 0;
                             long iter = 0;
                             for (int j = Util.GetWorkItemsIndices(data.Count(), Global.NOF_THREADS, i).Item1;
                                    j < Util.GetWorkItemsIndices(data.Count(), Global.NOF_THREADS, i).Item2; ++j)
                             { // go through all data
                                 double distance = double.MaxValue;
                                 int bestIndex = -1;
                                 for (int m = 0; m < k; ++m) // go through centers
                                 {
                                     if (bestIndex == -1 || centerCenterDistances[bestCenters[j], m] < 2 * distance)
                                     {
                                         double tempDistance = GetL2DistanceSquared(data, centers, j, m);
                                         if (tempDistance < distance)
                                         {
                                             distance = tempDistance;
                                             bestIndex = m;
                                         }
                                     }
                                 }
                                 bestCenters[j] = bestIndex;
                                 threadDistance += Math.Sqrt(distance);

                                 iter++;
                                 if(iter % 10000 == 0) 
                                 { 
                                     Interlocked.Add(ref sharedLoopCounter, 10000);
                                     AddDouble(ref totalDistance, threadDistance);
                                     threadDistance = 0;
                                     progress.Report((double)Interlocked.Read(ref sharedLoopCounter) / data.Count(), sharedLoopCounter);
                                 }
                             }
                             Interlocked.Add(ref sharedLoopCounter, iter % 10000);
                             progress.Report((double)Interlocked.Read(ref sharedLoopCounter) / data.Count(), sharedLoopCounter);

                             AddDouble(ref totalDistance, threadDistance);
                         });
                    }

                    // find new cluster centers // todo: it isnt theoretically sound to take the mean when using EMD distance metric
                    centers = new float[k, data[0].Count()];
                    int[] occurrences = new int[k];
                    for (int j = 0; j < data.Count(); j++)
                    {
                        for (int m = 0; m < data[0].Count(); ++m)
                        {
                            centers[bestCenters[j], m] += data[j][m];
                        }
                        occurrences[bestCenters[j]]++;
                    }
                    for (int n = 0; n < k; ++n)
                    {
                        for (int m = 0; m < data[0].Count(); ++m)
                        {
                            if (occurrences[n] != 0)
                                centers[n, m] /= occurrences[n];
                            else
                                break;
                        }
                    }
                    totalDistance /= data.Count();
                    if (totalDistance == lastDistance)
                    {
                        distanceChanged = false;
                    }
                    double diff = lastDistance - totalDistance;
                    if (totalDistance < recordDistance)
                    {
                        recordDistance = totalDistance;
                        Array.Copy(bestCenters, recordCenters, recordCenters.Length);
                    }
                    Console.WriteLine("Current average distance: {0} Improvement: {1}, {2}%", totalDistance, diff,
                        100.0*(1.0-totalDistance/lastDistance));

                    if (totalDistance > lastDistance * (1.0-saveToFileMinImprovement) && totalDistance < lastDistance)
                    {
                        Console.WriteLine("Less than {0}% change in average distance, saving intermediate table to file...",
                           saveToFileMinImprovement*100.0);

                        Console.WriteLine("Saving river cluster index to file riverCluster_temp.txt");
                        using var fileStream = File.Create("riverCluster_temp.txt");
                        BinaryFormatter bf = new BinaryFormatter();
                        bf.Serialize(fileStream, recordCenters);

                        saveToFileMinImprovement *= 0.5f;
                    }

                    lastDistance = totalDistance;
                }
            }
            Console.WriteLine("Best distance found: " + recordDistance);
            TimeSpan elapsed = DateTime.UtcNow - start;
            Console.WriteLine("K-means++ clustering (L2) completed in {0}d {1}h {2}m {3}s", elapsed.Days, elapsed.Hours, elapsed.Minutes, elapsed.Seconds);

            // print starting hand chart
            return recordCenters;
        }
        private void CalculateClusterDistancesL2(float[,] distances, float[,] clusterCenters)
        {
            Parallel.For(0, Global.NOF_THREADS,
            i =>
            {
                for (int j = Util.GetWorkItemsIndices(clusterCenters.GetLength(0), Global.NOF_THREADS, i).Item1;
                    j < Util.GetWorkItemsIndices(clusterCenters.GetLength(0), Global.NOF_THREADS, i).Item2; ++j)
                {
                    for (int m = 0; m < j; ++m) // go through centers
                    {
                        distances[j, m] = GetL2DistanceSquared(clusterCenters, clusterCenters, j, m);
                    }
                }
            });
            for (int j = 0; j < clusterCenters.GetLength(0); ++j)
            {
                for (int m = 0; m < j; ++m) // go through centers
                {
                    distances[m, j] = distances[j,m];
                }
            }
        }
        private void CalculateClusterDistancesEMD(float[][] distances, float[][] clusterCenters)
        {
        }
        /// <summary>
        /// Returns a sample of the data
        /// </summary>
        /// <param name="data"></param>
        /// <param name="nofSamples"></param>
        /// <returns></returns>
        private float[][] GetUniqueRandomNumbers(float[][] data, int nofSamples)
        {
            float[][] tempData = new float[nofSamples][];
            for (int i = 0; i < nofSamples; ++i)
            {
                tempData[i] = new float[data[0].Count()];
            }

            HashSet<int> numbers = new HashSet<int>();

            int numbersLeft = nofSamples;
            int destinationIndex = 0;
            while (numbersLeft > 0)
            {
                int rand = RandomGen.Next(0, data.Count());
                if(!numbers.Contains(rand))
                {
                    numbers.Add(rand);
                    numbersLeft--;
                    CopyArray(data, tempData, rand, destinationIndex);
                    destinationIndex++;
                }
            }
            return tempData;
        }
        private float[,] FindStartingCentersL2(float[][] data, int k)
        {
            Console.WriteLine("K-means++ finding good starting centers...");
            
            // first get some samples of all data to speed up the algorithm
            int maxSamples = k * 100;
            float[][] dataTemp = GetUniqueRandomNumbers(data, maxSamples);
           
            float[,] centers = new float[k, dataTemp[0].Count()];

            // first cluster center is random
            List<int> centerIndices = new List<int>();
            int index = -1;
            using (var progress = new ProgressBar())
            {
                progress.Report((double)(1) / k, 1);

                for (int c = 0; c < k; ++c) // get a new cluster center one by one
                {
                    double[] distancesToBestCenter = Enumerable.Repeat(double.MaxValue, dataTemp.Count()).ToArray();

                    if (c == 0)
                    {
                        index = RandomGen.Next(0, dataTemp.Count());
                        centerIndices.Add(index);
                        CopyArray(dataTemp, centers, index, c);

                        continue;
                    }
                    else
                    {
                        Parallel.For(0, Global.NOF_THREADS,
                        i =>
                        {
                            for (int j = Util.GetWorkItemsIndices(dataTemp.Count(), Global.NOF_THREADS, i).Item1;
                                                        j < Util.GetWorkItemsIndices(dataTemp.Count(), Global.NOF_THREADS, i).Item2; ++j)
                            { // go through all dataTemp
                                for (int m = 0; m < c; ++m) // go through centers
                                {
                                    double tempDistance = GetL2DistanceSquared(dataTemp, centers, j, m);
                                    if (tempDistance < distancesToBestCenter[j])
                                    {
                                        distancesToBestCenter[j] = tempDistance;
                                    }
                                }
                            }
                        });
                        double sum = distancesToBestCenter.Sum();
                        for (int p = 0; p < distancesToBestCenter.Count(); ++p)
                        {
                            distancesToBestCenter[p] /= sum;
                        }
                        int centerIndexSample = Util.SampleDistribution(distancesToBestCenter);
                        while (centerIndices.Contains(centerIndexSample))
                        {
                            centerIndexSample = Util.SampleDistribution(distancesToBestCenter);
                        }
                        CopyArray(dataTemp, centers, centerIndexSample, c);
                        centerIndices.Add(centerIndexSample);
                    }
                    progress.Report((double)(c + 1) / k, c + 1);
                }
            }
            return centers;
        }
        private float[,] FindStartingCentersEMD(float[][] data, int k)
        {
            // select random centers
            Console.WriteLine("K-means++ finding good starting centers...");

            float[,] centers = new float[k, data[0].Count()];

            // first position random
            List<int> centerIndices = new List<int>();
            int index = -1;
            using (var progress = new ProgressBar())
            {
                progress.Report((double)(1) / k, 1);

                for (int c = 0; c < k; ++c) // get a new cluster center one by one
                {
                    double[] distancesToBestCenter = Enumerable.Repeat(double.MaxValue, data.Count()).ToArray();
                    if (c == 0)
                    {
                        index = RandomGen.Next(0, data.Count());
                        centerIndices.Add(index);
                        CopyArray(data, centers, index, c);

                        continue;
                    }
                    Parallel.For(0, Global.NOF_THREADS,
                      i =>
                      {
                          for (int j = Util.GetWorkItemsIndices(data.Count(), Global.NOF_THREADS, i).Item1;
                                                      j < Util.GetWorkItemsIndices(data.Count(), Global.NOF_THREADS, i).Item2; ++j)
                          { // go through all data
                            for (int m = 0; m < c; ++m) // go through centers
                            {
                                double tempDistance = GetEarthMoverDistance(data, centers, j, m);
                                if (tempDistance < distancesToBestCenter[j])
                                {
                                    distancesToBestCenter[j] = tempDistance;
                                }
                            }
                          }
                      });
                    Square(distancesToBestCenter);
                    double sum = distancesToBestCenter.Sum();
                    for (int p = 0; p < distancesToBestCenter.Count(); ++p)
                    {
                        distancesToBestCenter[p] /= sum;
                    }
                    int centerIndexSample = Util.SampleDistribution(distancesToBestCenter);
                    while (centerIndices.Contains(centerIndexSample))
                    {
                        centerIndexSample = Util.SampleDistribution(distancesToBestCenter);
                    }
                    CopyArray(data, centers, centerIndexSample, c);
                    centerIndices.Add(centerIndexSample);
                    
                    progress.Report((double)(c + 1) / k, c + 1);
                }
            }
            return centers;
        }
        private static void Square(double[] a)
        {
            for (int i = 0; i < a.Length; i++)
            {
                a[i] = a[i] * a[i];
            }
        }
        private void CopyArray(float[][] dataSource, float[,] dataDestination, int indexSource, int indexDestination)
        {
            // probably should use buffer.blockcopy (todo)
            for (int i = 0; i < dataSource[0].Count(); ++i)
            {
                dataDestination[indexDestination, i] = dataSource[indexSource][i];
            }
        }
        private void CopyArray(float[][] dataSource, float[][] dataDestination, int indexSource, int indexDestination)
        {
            // probably should use buffer.blockcopy (todo)
            for (int i = 0; i < dataSource[0].Count(); ++i)
            {
                dataDestination[indexDestination][i] = dataSource[indexSource][i];
            }
        }
        private float GetEarthMoverDistance(float[][] data, float[,] centers, int index1, int index2)
        {
            float emd = 0, totalDistance = 0;
            for (int i = 0; i < data[0].Count(); i++)
            {
                emd = (data[index1][i] + emd) - centers[index2, i];
                totalDistance += Math.Abs(emd);
            }
            return totalDistance;
        }
        private double GetL2DistanceSquared(float[][] data, float[,] centers, int index1, int index2)
        {     
            double totalDistance = 0;
            Vector4 v1, v2;
            for (int i = 0; i < data[0].Count() - 4; i +=4)
            {
                v1 = new Vector4(data[index1][i], data[index1][i+1], data[index1][i+2], data[index1][i+3]);
                v2 = new Vector4(centers[index2, i], centers[index2, i+1], centers[index2, i+2], centers[index2, i+3]);
                totalDistance += (v1-v2).LengthSquared();
            }
            for (int i = data[0].Count() - data[0].Count() % 4; i < data[0].Count(); i++) // if the histogram is not a multiple of 4
            {
                totalDistance += (data[index1][i] - centers[index2, i]) * (double)(data[index1][i] - centers[index2, i]);
            }
            return totalDistance;
        }
        private float GetL2DistanceSquared(float[,] data, float[,] centers, int index1, int index2)
        {
            double totalDistance = 0;
            Vector4 v1, v2;
            for (int i = 0; i < data.GetLength(1) - 4; i += 4)
            {
                v1 = new Vector4(data[index1,i], data[index1,i + 1], data[index1,i + 2], data[index1,i + 3]);
                v2 = new Vector4(centers[index2, i], centers[index2, i + 1], centers[index2, i + 2], centers[index2, i + 3]);
                totalDistance += (v1 - v2).LengthSquared();
            }
            for (int i = data.GetLength(1) - data.GetLength(1) % 4; i < data.GetLength(1); i++) // if the histogram is not a multiple of 4
            {
                totalDistance += (data[index1,i] - centers[index2, i]) * (double)(data[index1,i] - centers[index2, i]);
            }
            return (float)totalDistance;
        }
        public static double AddDouble(ref double location1, double value)
        {
            double newCurrentValue = location1; // non-volatile read, so may be stale
            while (true)
            {
                double currentValue = newCurrentValue;
                double newValue = currentValue + value;
                newCurrentValue = Interlocked.CompareExchange(ref location1, newValue, currentValue);
                if (newCurrentValue == currentValue)
                    return newValue;
            }
        }
    }
}
