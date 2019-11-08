using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Xml.Serialization;

namespace SnapCall
{
    [Serializable]
    public class HashMapEntry
    {
        public ulong Key { get; set; }
        public uint Value { get; set; }

        public static byte[] ToBytes(HashMapEntry entry)
        {
            byte[] bytes = new byte[12];
            return bytes;
        }
    }

    [Serializable]
    public class ArrayWrapper
    {
        public ulong[] Array { get; set; }
    }

    /// <summary>
    /// High performance, quickly serializable dictionary implementation.
    /// Several warnings:
    /// - Can't use zero as a key
    /// - There's no duplicate key checking, it will fuck you up
    /// - No delete operation
    /// </summary>
    [Serializable]
    public class HashMap
    {
        public uint Size { get; set; }
        public uint Count { get; set; }
        public uint TotalSize { get; set; }
        public List<ArrayWrapper> Data { get; set; }
        public int Misses { get; set; }

        public HashMap() { }

        public HashMap(uint size)
        {
            TotalSize = size * 2;
            Count = 1;
            while (TotalSize / Count > 10000000) Count *= 2;
            Size = TotalSize / Count;
            if (Size % 2 == 1) Size++;
            Data = new List<ArrayWrapper>();
            TotalSize = Size * Count;
            for (int i = 0; i < Count; i++)
            {
                var wrapper = new ArrayWrapper();
                wrapper.Array = new ulong[Size];
                Data.Add(wrapper);
            }
            Misses = 0;
        }

        public ulong this[ulong key]
        {
            get
            {
                ulong index = (key * 2) % TotalSize;
                int subarray = (int)(index / Size);
                while (true)
                {
                    if (Data[subarray].Array[index % Size] == key) return Data[subarray].Array[index % Size + 1];
                    index += 2;
                }
            }
            set
            {
                ulong index = (key * 2) % TotalSize;
                int subarray = (int)(index / Size);
                while (true)
                {
                    if (Data[subarray].Array[index % Size] == 0)
                    {
                        Data[subarray].Array[index % Size] = key;
                        Data[subarray].Array[index % Size + 1] = value;
                        break;
                    }
                    index += 2;
                    Misses++;
                }
            }
        }
    }
}
