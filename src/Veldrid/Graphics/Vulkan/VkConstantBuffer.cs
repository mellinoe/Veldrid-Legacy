using Vulkan;

namespace Veldrid.Graphics.Vulkan
{
    public class VkConstantBuffer : VkDeviceBuffer, ConstantBuffer
    {
        public VkConstantBuffer(
            VkRenderContext rc,
            ulong size,
            VkMemoryPropertyFlags memoryProperties,
            bool dynamic)
            : base(rc, size, VkBufferUsageFlags.UniformBuffer, memoryProperties, dynamic)
        {
        }
    }
}
