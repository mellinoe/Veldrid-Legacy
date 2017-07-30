using System;
using Vulkan;

namespace Veldrid.Graphics.Vulkan
{
    public class VkIndexBuffer : VkDeviceBuffer, IndexBuffer
    {
        public VkIndexBuffer(
            VkRenderContext rc,
            ulong size,
            VkMemoryPropertyFlags memoryProperties,
            bool dynamic)
            : base(rc, size, VkBufferUsageFlags.IndexBuffer, memoryProperties, dynamic)
        {
        }

        public VkIndexType IndexType { get; private set; } = VkIndexType.Uint16;

        public void SetIndices(uint[] indices)
        {
            SetData(indices);
            IndexType = VkIndexType.Uint32;
        }

        public void SetIndices(uint[] indices, int stride, int elementOffset)
        {
            SetData(indices, elementOffset * sizeof(uint));
            IndexType = VkIndexType.Uint32;
        }

        public void SetIndices(ushort[] indices)
        {
            SetData(indices);
            IndexType = VkIndexType.Uint16;
        }

        public void SetIndices(ushort[] indices, int stride, int elementOffset)
        {
            SetData(indices, elementOffset * sizeof(ushort));
            IndexType = VkIndexType.Uint16;
        }

        public void SetIndices(IntPtr indices, IndexFormat format, int count)
        {
            SetData(indices, FormatHelpers.GetIndexFormatElementByteSize(format) * count);
            IndexType = VkFormats.VeldridToVkIndexFormat(format);
        }

        public void SetIndices(IntPtr indices, IndexFormat format, int count, int elementOffset)
        {
            int elementSizeInBytes = FormatHelpers.GetIndexFormatElementByteSize(format);
            SetData(indices, elementSizeInBytes * count, elementOffset * elementSizeInBytes);
            IndexType = VkFormats.VeldridToVkIndexFormat(format);
        }
    }
}
