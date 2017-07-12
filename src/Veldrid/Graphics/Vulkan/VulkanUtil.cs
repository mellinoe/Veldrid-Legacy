using Vulkan;

namespace Veldrid.Graphics.Vulkan
{
    internal static class VulkanUtil
    {
        public static void CheckResult(VkResult result)
        {
            if (result != VkResult.Success)
            {
                throw new VeldridException("Unsuccessful VkResult: " + result);
            }
        }
    }
}
