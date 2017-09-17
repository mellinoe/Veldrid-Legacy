using System;

namespace Veldrid.Graphics
{
    /// <summary>
    /// A structure describing the overall shape of a vertex element.
    /// </summary>
    public struct VertexDescriptor
    {
        /// <summary>
        /// The size of a single vertex.
        /// </summary>
        public readonly byte VertexSizeInBytes;
        /// <summary>
        /// The number of vertices stored in a buffer.
        /// </summary>
        public readonly byte ElementCount;
        /// <summary>
        /// Indicates that vertex data starts at a given byte offset from the beginning of the buffer.
        /// </summary>
        public readonly IntPtr Offset;

        /// <summary>
        /// Constructs a new <see cref="VertexDescriptor"/>.
        /// </summary>
        /// <param name="vertexSizeInBytes">The total size of an individual vertex.</param>
        /// <param name="elementCount">The number of distinct elements (position, normal, color, etc.) in a vertex element.</param>
        public VertexDescriptor(byte vertexSizeInBytes, byte elementCount)
            : this(vertexSizeInBytes, elementCount, IntPtr.Zero) { }

        /// <summary>
        /// Constructs a new <see cref="VertexDescriptor"/>.
        /// </summary>
        /// <param name="vertexSizeInBytes">The total size of an individual vertex.</param>
        /// <param name="elementCount">The number of distinct elements (position, normal, color, etc.) in a vertex element.</param>
        /// <param name="offset">Indicates that vertex data starts at a given byte offset from the beginning of the buffer.</param>
        public VertexDescriptor(byte vertexSizeInBytes, byte elementCount, IntPtr offset)
        {
            VertexSizeInBytes = vertexSizeInBytes;
            ElementCount = elementCount;
            Offset = offset;
        }
    }
}
