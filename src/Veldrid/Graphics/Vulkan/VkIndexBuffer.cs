using System;
using Vulkan;

namespace Veldrid.Graphics.Vulkan
{
    public class VkIndexBuffer : VkDeviceBuffer, IndexBuffer
    {
        public VkIndexBuffer(
            VkDevice device,
            VkPhysicalDevice physicalDevice,
            ulong size,
            VkMemoryPropertyFlags memoryProperties)
            : base(device, physicalDevice, size, VkBufferUsageFlags.IndexBuffer, memoryProperties)
        {
        }

        public void SetIndices(uint[] indices)
        {
            SetData(indices);
        }

        public void SetIndices(uint[] indices, int stride, int elementOffset)
        {
            SetData(indices, elementOffset * sizeof(uint));
        }

        public void SetIndices(ushort[] indices)
        {
            SetData(indices);
        }

        public void SetIndices(ushort[] indices, int stride, int elementOffset)
        {
            SetData(indices, elementOffset * sizeof(ushort));
        }

        public void SetIndices(IntPtr indices, IndexFormat format, int count)
        {
            SetData(indices, FormatHelpers.GetIndexFormatElementByteSize(format) * count);
        }

        public void SetIndices(IntPtr indices, IndexFormat format, int count, int elementOffset)
        {
            int elementSizeInBytes = FormatHelpers.GetIndexFormatElementByteSize(format);
            SetData(indices, elementSizeInBytes * count, elementOffset * elementSizeInBytes);
        }
    }
}
