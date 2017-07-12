using System;
using System.Diagnostics;
using System.Runtime.CompilerServices;
using Vulkan;
using static Veldrid.Graphics.Vulkan.VulkanUtil;
using static Vulkan.VulkanNative;

namespace Veldrid.Graphics.Vulkan
{
    public unsafe class VkDeviceTexture2D : DeviceTexture2D
    {
        private VkImage _image;
        private VkDeviceMemory _memory;
        private VkDevice _device;
        private VkPhysicalDevice _physicalDevice;
        private PixelFormat _veldridFormat;

        public VkDeviceTexture2D(VkDevice device, VkPhysicalDevice physicalDevice, int mipLevels, int width, int height, PixelFormat veldridFormat)
        {
            _device = device;
            _physicalDevice = physicalDevice;
            MipLevels = mipLevels;
            Width = width;
            Height = height;
            Format = VkFormats.VeldridToVkPixelFormat(veldridFormat);
            _veldridFormat = veldridFormat;

            VkImageCreateInfo imageCI = VkImageCreateInfo.New();
            imageCI.mipLevels = (uint)mipLevels;
            imageCI.arrayLayers = 1;
            imageCI.imageType = VkImageType._2d;
            imageCI.extent.width = (uint)width;
            imageCI.extent.height = (uint)height;
            imageCI.extent.depth = 1;
            imageCI.initialLayout = VkImageLayout.General; // TODO: Use proper VkImageLayout values and transitions.
            imageCI.usage = VkImageUsageFlags.Sampled;
            imageCI.tiling = VkImageTiling.Linear;
            imageCI.format = VkFormats.VeldridToVkPixelFormat(veldridFormat);
            imageCI.samples = VkSampleCountFlags._1;

            VkResult result = vkCreateImage(device, ref imageCI, null, out _image);
            CheckResult(result);

            vkGetImageMemoryRequirements(_device, _image, out VkMemoryRequirements memoryRequirements);

            VkMemoryAllocateInfo memoryAI = VkMemoryAllocateInfo.New();
            memoryAI.allocationSize = memoryRequirements.size;
            memoryAI.memoryTypeIndex = FindMemoryType(
                _physicalDevice,
                memoryRequirements.memoryTypeBits,
                VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent);
            vkAllocateMemory(_device, ref memoryAI, null, out _memory);
            vkBindImageMemory(_device, _image, _memory, 0);
        }

        public int Width { get; private set; }

        public int Height { get; private set; }

        public int MipLevels { get; private set; }

        public VkFormat Format { get; }

        public VkImage DeviceImage => _image;

        public void GetTextureData(int mipLevel, IntPtr destination, int storageSizeInBytes)
        {
            throw new NotImplementedException();
        }

        public void GetTextureData<T>(int mipLevel, T[] destination) where T : struct
        {
            throw new NotImplementedException();
        }

        public void SetTextureData(int mipLevel, int x_unused, int y_unused, int width, int height, IntPtr data, int dataSizeInBytes)
        {
            Debug.Assert(x_unused == 0 && y_unused == 0);

            VkImageSubresource subresource = new VkImageSubresource();
            subresource.aspectMask = VkImageAspectFlags.Color;
            subresource.mipLevel = (uint)mipLevel;
            subresource.arrayLayer = 0;
            vkGetImageSubresourceLayout(_device, _image, ref subresource, out VkSubresourceLayout layout);
            ulong rowPitch = layout.rowPitch;

            void* mappedPtr;
            VkResult result = vkMapMemory(_device, _memory, 0, (ulong)dataSizeInBytes, 0, &mappedPtr);
            CheckResult(result);

            if (rowPitch == (ulong)width)
            {
                Buffer.MemoryCopy(data.ToPointer(), mappedPtr, dataSizeInBytes, dataSizeInBytes);
            }
            else
            {
                int pixelSizeInBytes = FormatHelpers.GetPixelSize(_veldridFormat);
                for (uint y = 0; y < height; y++)
                {
                    byte* dstRowStart = ((byte*)mappedPtr) + (rowPitch * y);
                    byte* srcRowStart = ((byte*)data.ToPointer()) + (width * y * pixelSizeInBytes);
                    Unsafe.CopyBlock(dstRowStart, srcRowStart, (uint)(width * pixelSizeInBytes));
                }
            }

            vkUnmapMemory(_device, _memory);
        }

        public void Dispose()
        {
            vkDestroyImage(_device, _image, null);
            vkFreeMemory(_device, _memory, null);
        }
    }
}
