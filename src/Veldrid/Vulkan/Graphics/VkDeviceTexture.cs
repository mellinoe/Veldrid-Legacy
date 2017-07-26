using Vulkan;

namespace Veldrid.Graphics.Vulkan
{
    public abstract class VkDeviceTexture : DeviceTexture
    {
        public abstract int Width { get; }
        public abstract int Height { get; }
        public abstract int MipLevels { get; }

        public abstract VkFormat Format { get; }
        public abstract VkImage DeviceImage { get; }
        public abstract DeviceTextureCreateOptions CreateOptions { get; }

        public abstract void Dispose();
    }
}