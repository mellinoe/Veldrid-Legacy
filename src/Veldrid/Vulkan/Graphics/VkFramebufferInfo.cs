using System;
using Vulkan;

namespace Veldrid.Graphics.Vulkan
{
    public class VkFramebufferInfo : Framebuffer
    {
        private readonly VkDevice _device;
        private readonly VkPhysicalDevice _physicalDevice;

        public VkFramebufferInfo(VkDevice device, VkPhysicalDevice physicalDevice)
        {
            _device = device;
            _physicalDevice = physicalDevice;
        }

        public VkTexture2D ColorTexture { get; set; }
        public VkTexture2D DepthTexture { get; set; }

        DeviceTexture2D Framebuffer.ColorTexture { get => ColorTexture; set => AttachColorTexture(0, value); }
        DeviceTexture2D Framebuffer.DepthTexture { get => DepthTexture; set => DepthTexture = (VkTexture2D)value; }

        public int Width => ColorTexture.Width;
        public int Height => ColorTexture.Height;

        public void AttachColorTexture(int index, DeviceTexture2D texture)
        {
            if (index != 0)
            {
                throw new NotImplementedException();
            }

            ColorTexture = (VkTexture2D)texture;
        }

        public DeviceTexture2D GetColorTexture(int index)
        {
            if (index != 0)
            {
                throw new NotImplementedException();
            }

            return ColorTexture;
        }

        public void Dispose()
        {
        }
    }
}