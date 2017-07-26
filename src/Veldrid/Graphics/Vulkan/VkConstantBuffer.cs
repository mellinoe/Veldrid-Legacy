using Vulkan;

namespace Veldrid.Graphics.Vulkan
{
    public class VkConstantBuffer : VkDeviceBuffer, ConstantBuffer
    {
        public VkConstantBuffer(
            VkDevice device,
            VkPhysicalDevice physicalDevice,
            ulong size,
            VkMemoryPropertyFlags memoryProperties,
            bool dynamic)
            : base(device, physicalDevice, size, VkBufferUsageFlags.UniformBuffer, memoryProperties, dynamic)
        {
        }
    }
}
