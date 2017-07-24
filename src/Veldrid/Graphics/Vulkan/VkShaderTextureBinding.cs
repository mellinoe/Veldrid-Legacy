using Vulkan;
using static Veldrid.Graphics.Vulkan.VulkanUtil;
using static Vulkan.VulkanNative;

namespace Veldrid.Graphics.Vulkan
{
    public unsafe class VkShaderTextureBinding : ShaderTextureBinding
    {
        private readonly VkDevice _device;

        public VkShaderTextureBinding(VkDevice device, VkTexture2D tex2D)
        {
            _device = device;
            BoundTexture = tex2D;
            VkImageViewCreateInfo imageViewCI = VkImageViewCreateInfo.New();
            imageViewCI.format = tex2D.Format;
            imageViewCI.image = tex2D.DeviceImage;
            imageViewCI.viewType = VkImageViewType.Image2D;
            imageViewCI.subresourceRange.aspectMask = tex2D.CreateOptions == DeviceTextureCreateOptions.DepthStencil ? VkImageAspectFlags.Depth : VkImageAspectFlags.Color;
            imageViewCI.subresourceRange.layerCount = 1;
            imageViewCI.subresourceRange.levelCount = (uint)tex2D.MipLevels;

            VkResult result = vkCreateImageView(_device, ref imageViewCI, null, out VkImageView imageView);
            CheckResult(result);
            ImageView = imageView;
        }

        public VkImageView ImageView { get; }
        public VkTexture2D BoundTexture { get; }
        public VkImageLayout ImageLayout { get; internal set; }

        DeviceTexture ShaderTextureBinding.BoundTexture => BoundTexture;

        public void Dispose()
        {
            vkDestroyImageView(_device, ImageView, null);
        }
    }
}
