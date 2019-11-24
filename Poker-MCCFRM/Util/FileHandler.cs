using System;
using System.Collections.Generic;
using System.IO;
using System.Runtime.Serialization.Formatters.Binary;
using System.Text;

namespace Poker_MCCFRM
{
    public static class FileHandler
    {
        public static void SaveToFile(float[][] data, string filename)
        {
            Console.WriteLine("Saving to file {0}...", filename);
            BinaryWriter bin = new BinaryWriter(File.Open(filename, FileMode.Create));
            int dim1 = data.Length;
            int dim2 = data[0].Length;

            bin.Write(dim1);
            bin.Write(dim2);
            for (int i = 0; i < dim1; ++i)
            {
                for (int j = 0; j < dim2; ++j)
                {
                    bin.Write(data[i][j]);
                }
            }
            bin.Close();
        }
        public static void SaveToFile(int[] data, string filename)
        {
            Console.WriteLine("Saving to file {0}...", filename);
            BinaryWriter bin = new BinaryWriter(File.Open(filename, FileMode.Create));
            int dim1 = data.Length;

            bin.Write(dim1);
            for (int i = 0; i < dim1; ++i)
            { 
                bin.Write(data[i]);
            }
            bin.Close();
        }
        public static float[][] LoadFromFile(string filename)
        {
            try
            {
                Console.WriteLine("Loading from file {0}...", filename);
                BinaryReader binR = new BinaryReader(File.OpenRead(filename));
                int dim1 = binR.ReadInt32();
                int dim2 = binR.ReadInt32();
                float[][] data = new float[dim1][];
                for (int i = 0; i < dim1; ++i)
                {
                    data[i] = new float[dim2];
                }

                for (int i = 0; i < dim1; ++i)
                {
                    for (int j = 0; j < dim2; ++j)
                    {
                        data[i][j] = binR.ReadSingle();
                    }
                }
                return data;
            }
            catch {
                return null;
            }
        }
        public static int[] LoadFromFileIndex(string filename)
        {
            try
            {
                BinaryReader binR = new BinaryReader(File.OpenRead(filename));
                int dim1 = binR.ReadInt32();
                int[] data = new int[dim1];

                for (int i = 0; i < dim1; ++i)
                {
                    data[i] = binR.ReadInt32();
                }
                return data;
            }
            catch
            {
                return null;
            }
        }
        public static int[] LoadFromFileIndexSerialized(string filename)
        {
            try
            {
                using var fileStream2 = File.OpenRead(filename);
                var binForm = new BinaryFormatter();
                int[] data = (int[])binForm.Deserialize(fileStream2);

                return data;
            }
            catch
            {
                return null;
            }
        }
    }
}
