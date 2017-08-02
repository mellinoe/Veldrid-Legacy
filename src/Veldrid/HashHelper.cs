using System;
using Veldrid.Graphics.Vulkan;

namespace Veldrid
{
    internal static class HashHelper
    {
        public static int Combine(int value1, int value2)
        {
            uint rol5 = ((uint)value1 << 5) | ((uint)value1 >> 27);
            return ((int)rol5 + value1) ^ value2;
        }

        public static int Combine(int value1, int value2, int value3)
        {
            return Combine(value1, Combine(value2, value3));
        }

        public static int Combine(int value1, int value2, int value3, int value4)
        {
            return Combine(value1, Combine(value2, Combine(value3, value4)));
        }

        public static int Combine(int value1, int value2, int value3, int value4, int value5)
        {
            return Combine(value1, Combine(value2, Combine(value3, Combine(value4, value5))));
        }

        public static int Combine(int value1, int value2, int value3, int value4, int value5, int value6, int value7, int value8)
        {
            return Combine(value1, Combine(value2, Combine(value3, Combine(value4, Combine(value5, Combine(value6, Combine(value7, value8)))))));
        }

        public static int Array<T>(T[] items)
        {
            int hash = items[0]?.GetHashCode() ?? 0;
            for (int i = 1; i < items.Length; i++)
            {
                hash = Combine(hash, items[i]?.GetHashCode() ?? i);
            }

            return hash;
        }
    }
}
