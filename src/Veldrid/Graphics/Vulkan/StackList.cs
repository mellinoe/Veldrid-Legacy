using System.Diagnostics;
using System.Runtime.CompilerServices;

namespace Veldrid.Graphics.Vulkan
{
    /// <summary>
    /// A super-dangerous stack-only list which can hold up to 256 bytes of blittable data.
    /// </summary>
    /// <typeparam name="T">The type of element held in the list. Must be blittable.</typeparam>
    internal unsafe struct StackList<T> where T : struct
    {
        public const int CapacityInBytes = 256;
        private static readonly int s_sizeofT = Unsafe.SizeOf<T>();

        private fixed byte _storage[CapacityInBytes];

        public uint Count { get; private set; }
        public void* Data => Unsafe.AsPointer(ref this);

        public void Add(T item)
        {
            byte* basePtr = (byte*)Data;
            int offset = (int)(Count * s_sizeofT);
#if DEBUG
            Debug.Assert((offset + s_sizeofT) <= CapacityInBytes);
#endif
            Unsafe.Write(basePtr + offset, item);

            Count += 1;
        }
    }

    /// <summary>
    /// A super-dangerous stack-only list which can hold a number of bytes determined by the second type parameter.
    /// </summary>
    /// <typeparam name="T">The type of element held in the list. Must be blittable.</typeparam>
    /// <typeparam name="TSize">A type parameter dictating the capacity of the list.</typeparam>
    internal unsafe struct StackList<T, TSize> where T : struct where TSize : struct
    {
        private static readonly int s_sizeofT = Unsafe.SizeOf<T>();

#pragma warning disable 0169 // Unused field. This is used implicity because it controls the size of the structure on the stack.
        private TSize _storage;
#pragma warning restore 0169
        private uint _count;

        public uint Count { get; private set; }
        public void* Data => Unsafe.AsPointer(ref this);

        public void Add(T item)
        {
            byte* basePtr = (byte*)Data;
            int offset = (int)(_count * s_sizeofT);
#if DEBUG
            Debug.Assert((offset + s_sizeofT) <= Unsafe.SizeOf<TSize>());
#endif
            Unsafe.Write(basePtr + offset, item);

            _count += 1;
        }
    }

    internal unsafe struct Size64Bytes { public fixed byte Data[64]; }
    internal unsafe struct Size128Bytes { public fixed byte Data[64]; }
    internal unsafe struct Size512Bytes { public fixed byte Data[1024]; }
    internal unsafe struct Size1024Bytes { public fixed byte Data[1024]; }
    internal unsafe struct Size2048Bytes { public fixed byte Data[2048]; }
}
