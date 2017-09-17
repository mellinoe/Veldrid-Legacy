using System;
using System.Runtime.CompilerServices;
using Vulkan;

namespace Veldrid.Graphics.Vulkan
{
    public class VkVertexBuffer : VkDeviceBuffer, VertexBuffer
    {
        public uint Stride { get; private set; }

        public VkVertexBuffer(
            VkRenderContext rc,
            ulong size,
            VkMemoryPropertyFlags memoryProperties,
            bool dynamic)
            : base(rc, size, VkBufferUsageFlags.VertexBuffer, memoryProperties, dynamic)
        {
        }

        public void SetVertexData<T>(T[] vertexData, VertexDescriptor descriptor) where T : struct
        {
            Stride = descriptor.VertexSizeInBytes;
            SetData(vertexData);
        }

        public void SetVertexData<T>(T[] vertexData, VertexDescriptor descriptor, int destinationOffsetInVertices) where T : struct
        {
            Stride = descriptor.VertexSizeInBytes;
            int byteOffset = Unsafe.SizeOf<T>() * destinationOffsetInVertices;
            SetData(vertexData, byteOffset);
        }

        public void SetVertexData<T>(ArraySegment<T> vertexData, VertexDescriptor descriptor, int destinationOffsetInVertices) where T : struct
        {
            Stride = descriptor.VertexSizeInBytes;
            int byteOffset = Unsafe.SizeOf<T>() * destinationOffsetInVertices;
            SetData(vertexData, byteOffset);
        }

        public void SetVertexData(IntPtr vertexData, VertexDescriptor descriptor, int numVertices)
        {
            Stride = descriptor.VertexSizeInBytes;
            int dataSizeInBytes = numVertices * descriptor.VertexSizeInBytes;
            SetData(vertexData, dataSizeInBytes);
        }

        public void SetVertexData(IntPtr vertexData, VertexDescriptor descriptor, int numVertices, int destinationOffsetInVertices)
        {
            Stride = descriptor.VertexSizeInBytes;
            int dataSizeInBytes = numVertices * descriptor.VertexSizeInBytes;
            int destinationOffsetInBytes = destinationOffsetInVertices * descriptor.VertexSizeInBytes;
            SetData(vertexData, dataSizeInBytes, destinationOffsetInBytes);
        }
    }
}
