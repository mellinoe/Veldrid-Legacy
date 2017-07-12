using Vulkan;

namespace Veldrid.Graphics.Vulkan
{
    public static class VkFormats
    {
        public static VkFormat VeldridToVkPixelFormat(PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.R32_G32_B32_A32_Float:
                    return VkFormat.R32g32b32a32Sfloat;
                case PixelFormat.R8_UInt:
                    return VkFormat.R8Uint;
                case PixelFormat.R16_UInt:
                    return VkFormat.R16Uint;
                case PixelFormat.R8_G8_B8_A8_UInt:
                    return VkFormat.R8g8b8a8Uint;
                default:
                    throw Illegal.Value<PixelFormat>();
            }
        }
    }
}
