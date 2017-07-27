using Vulkan;

namespace Veldrid.Graphics.Vulkan
{
    public class VkConstantBuffer : VkDeviceBuffer, ConstantBuffer
    {
        public VkConstantBuffer(
            VkDevice device,
            VkPhysicalDevice physicalDevice,
            VkDeviceMemoryManager memoryManager,
            ulong size,
            VkMemoryPropertyFlags memoryProperties,
            bool dynamic)
            : base(device, physicalDevice, memoryManager, size, VkBufferUsageFlags.UniformBuffer, memoryProperties, dynamic)
        {
        }
    }
}
