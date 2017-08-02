using System;
using Vulkan;

namespace Veldrid.Graphics.Vulkan
{
    public abstract class VkFramebufferBase : Framebuffer
    {
        public abstract VkFramebuffer VkFramebuffer { get; }
        public abstract VkRenderPass RenderPassClearBuffer { get; }
        public abstract VkRenderPass RenderPassNoClear { get; }

        public abstract VkTexture2D ColorTexture { get; set; }
        public abstract VkTexture2D DepthTexture { get; set; }
        public abstract int Width { get; }
        public abstract int Height { get; }

        public abstract void AttachColorTexture(int index, VkTexture2D texture);
        public abstract VkTexture2D GetColorTexture(int index);

        public abstract void Dispose();

        DeviceTexture2D Framebuffer.ColorTexture { get => ColorTexture; set => ColorTexture = (VkTexture2D)value; }
        DeviceTexture2D Framebuffer.DepthTexture { get => DepthTexture; set => DepthTexture = (VkTexture2D)value; }
        DeviceTexture2D Framebuffer.GetColorTexture(int index) => GetColorTexture(index);
        void Framebuffer.AttachColorTexture(int index, DeviceTexture2D texture) => AttachColorTexture(index, (VkTexture2D)texture);
    }
}
