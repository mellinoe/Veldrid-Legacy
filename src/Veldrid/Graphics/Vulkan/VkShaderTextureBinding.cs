using Vulkan;
using static Veldrid.Graphics.Vulkan.VulkanUtil;
using static Vulkan.VulkanNative;

namespace Veldrid.Graphics.Vulkan
{
    public unsafe class VkShaderTextureBinding : ShaderTextureBinding
    {
        private readonly VkDevice _device;
        private VkImageView _imageView;

        public VkShaderTextureBinding(VkDevice device, VkDeviceTexture2D tex2D)
        {
            _device = device;
            BoundTexture = tex2D;
            VkImageViewCreateInfo imageViewCI = VkImageViewCreateInfo.New();
            imageViewCI.format = tex2D.Format;
            imageViewCI.image = tex2D.DeviceImage;
            imageViewCI.viewType = VkImageViewType._2d;
            imageViewCI.subresourceRange.layerCount = 1;
            imageViewCI.subresourceRange.levelCount = (uint)tex2D.MipLevels;

            VkResult result = vkCreateImageView(_device, ref imageViewCI, null, out _imageView);
            CheckResult(result);
        }

        public VkDeviceTexture2D BoundTexture { get; }
        DeviceTexture ShaderTextureBinding.BoundTexture => BoundTexture;

        public void Dispose()
        {
            vkDestroyImageView(_device, _imageView, null);
        }
    }
}
