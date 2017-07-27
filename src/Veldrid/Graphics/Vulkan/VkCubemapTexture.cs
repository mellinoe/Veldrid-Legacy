using System;
using System.Runtime.CompilerServices;
using Vulkan;
using static Veldrid.Graphics.Vulkan.VulkanUtil;
using static Vulkan.VulkanNative;

namespace Veldrid.Graphics.Vulkan
{
    public unsafe class VkCubemapTexture : VkDeviceTexture, CubemapTexture
    {
        private readonly VkRenderContext _rc;
        private readonly VkPhysicalDevice _physicalDevice;
        private readonly VkDevice _device;
        private readonly PixelFormat _format;
        private readonly VkDeviceMemoryManager _memoryManager;

        private VkImage _image;
        private VkMemoryBlock _memory;
        private readonly VkFormat _vkFormat;
        private VkImageLayout _imageLayout;

        public VkCubemapTexture(
            VkDevice device,
            VkPhysicalDevice physicalDevice,
            VkDeviceMemoryManager memoryManager,
            VkRenderContext rc,
            IntPtr pixelsFront,
            IntPtr pixelsBack,
            IntPtr pixelsLeft,
            IntPtr pixelsRight,
            IntPtr pixelsTop,
            IntPtr pixelsBottom,
            int width,
            int height,
            PixelFormat format)
        {
            _device = device;
            _physicalDevice = physicalDevice;
            _rc = rc;
            _memoryManager = memoryManager;

            Width = width;
            Height = height;
            _format = format;
            _vkFormat = VkFormats.VeldridToVkPixelFormat(_format);

            VkImageCreateInfo imageCI = VkImageCreateInfo.New();
            imageCI.imageType = VkImageType.Image2D;
            imageCI.flags = VkImageCreateFlags.CubeCompatible;
            imageCI.format = _vkFormat;
            imageCI.extent.width = (uint)width;
            imageCI.extent.height = (uint)height;
            imageCI.extent.depth = 1;
            imageCI.mipLevels = 1;
            imageCI.arrayLayers = 6;
            imageCI.samples = VkSampleCountFlags.Count1;
            imageCI.tiling = VkImageTiling.Optimal;
            imageCI.usage = VkImageUsageFlags.Sampled | VkImageUsageFlags.TransferDst;
            imageCI.initialLayout = VkImageLayout.Undefined;

            VkResult result = vkCreateImage(_device, ref imageCI, null, out _image);
            CheckResult(result);

            vkGetImageMemoryRequirements(_device, _image, out VkMemoryRequirements memReqs);
            uint memoryType = FindMemoryType(_physicalDevice, memReqs.memoryTypeBits, VkMemoryPropertyFlags.DeviceLocal);

            _memory = memoryManager.Allocate(memoryType, memReqs.size, memReqs.alignment);
            vkBindImageMemory(_device, _image, _memory.DeviceMemory, _memory.Offset);

            // Copy data into image.

            CreateImage(
                _device,
                _physicalDevice,
                memoryManager,
                (uint)width,
                (uint)height,
                6,
                _vkFormat,
                VkImageTiling.Linear,
                VkImageUsageFlags.TransferSrc,
                VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent,
                out VkImage stagingImage,
                out VkMemoryBlock stagingMemory);

            int pixelSizeInBytes = FormatHelpers.GetPixelSizeInBytes(_format);
            int dataSizeInBytes = width * height * pixelSizeInBytes;

            vkGetImageMemoryRequirements(_device, stagingImage, out VkMemoryRequirements stagingMemReqs);
            void* mappedPtr;
            result = vkMapMemory(_device, stagingMemory.DeviceMemory, stagingMemory.Offset, stagingMemReqs.size, 0, &mappedPtr);
            CheckResult(result);

            StackList<IntPtr, Size6IntPtr> faces = new StackList<IntPtr, Size6IntPtr>();
            faces.Add(pixelsFront);
            faces.Add(pixelsBack);
            faces.Add(pixelsLeft);
            faces.Add(pixelsRight);
            faces.Add(pixelsTop);
            faces.Add(pixelsBottom);

            for (uint i = 0; i < 6; i++)
            {
                VkImageSubresource subresource;
                subresource.mipLevel = 0;
                subresource.arrayLayer = i;
                subresource.aspectMask = VkImageAspectFlags.Color;
                vkGetImageSubresourceLayout(_device, _image, ref subresource, out VkSubresourceLayout layout);

                ulong rowPitch = layout.rowPitch;
                IntPtr data = faces[i];

                if (rowPitch == (ulong)width)
                {
                    Buffer.MemoryCopy(data.ToPointer(), mappedPtr, dataSizeInBytes, dataSizeInBytes);
                }
                else
                {
                    for (uint yy = 0; yy < height; yy++)
                    {
                        byte* dstRowStart = ((byte*)mappedPtr) + (rowPitch * yy);
                        byte* srcRowStart = ((byte*)data.ToPointer()) + (width * yy * pixelSizeInBytes);
                        Unsafe.CopyBlock(dstRowStart, srcRowStart, (uint)(width * pixelSizeInBytes));
                    }
                }
            }

            vkUnmapMemory(_device, stagingMemory.DeviceMemory);

            TransitionImageLayout(stagingImage, 1, VkImageLayout.Preinitialized, VkImageLayout.TransferSrcOptimal);
            TransitionImageLayout(_image, (uint)MipLevels, _imageLayout, VkImageLayout.TransferDstOptimal);
            CopyImage(stagingImage, 0, _image, 0, (uint)width, (uint)height);
            TransitionImageLayout(_image, (uint)MipLevels, VkImageLayout.TransferDstOptimal, VkImageLayout.ShaderReadOnlyOptimal);
            _imageLayout = VkImageLayout.ShaderReadOnlyOptimal;

            vkDestroyImage(_device, stagingImage, null);
            _memoryManager.Free(stagingMemory);
        }

        public override int Width { get; }

        public override int Height { get; }

        public override int MipLevels => 1; // Probably

        public override VkFormat Format => _vkFormat;

        public override VkImage DeviceImage => _image;

        public override DeviceTextureCreateOptions CreateOptions => DeviceTextureCreateOptions.Default;

        public override void Dispose()
        {
            vkDestroyImage(_device, _image, null);
            _memoryManager.Free(_memory);
        }

        protected void TransitionImageLayout(VkImage image, uint mipLevels, VkImageLayout oldLayout, VkImageLayout newLayout)
        {
            VkCommandBuffer cb = BeginOneTimeCommands();

            VkImageMemoryBarrier barrier = VkImageMemoryBarrier.New();
            barrier.oldLayout = oldLayout;
            barrier.newLayout = newLayout;
            barrier.srcQueueFamilyIndex = QueueFamilyIgnored;
            barrier.dstQueueFamilyIndex = QueueFamilyIgnored;
            barrier.image = image;
            barrier.subresourceRange.aspectMask = VkImageAspectFlags.Color;
            barrier.subresourceRange.baseMipLevel = 0;
            barrier.subresourceRange.levelCount = mipLevels;
            barrier.subresourceRange.baseArrayLayer = 0;
            barrier.subresourceRange.layerCount = 1;

            vkCmdPipelineBarrier(
                cb,
                VkPipelineStageFlags.TopOfPipe,
                VkPipelineStageFlags.TopOfPipe,
                VkDependencyFlags.None,
                0, null,
                0, null,
                1, &barrier);

            EndOneTimeCommands(cb);
        }

        protected void CopyImage(VkImage srcImage, uint srcMipLevel, VkImage dstImage, uint dstMipLevel, uint width, uint height)
        {
            VkImageSubresourceLayers srcSubresource = new VkImageSubresourceLayers();
            srcSubresource.mipLevel = srcMipLevel;
            srcSubresource.layerCount = 1;
            srcSubresource.aspectMask = VkImageAspectFlags.Color;
            srcSubresource.baseArrayLayer = 0;

            VkImageSubresourceLayers dstSubresource = new VkImageSubresourceLayers();
            dstSubresource.mipLevel = dstMipLevel;
            dstSubresource.layerCount = 1;
            dstSubresource.aspectMask = VkImageAspectFlags.Color;
            dstSubresource.baseArrayLayer = 0;

            VkImageCopy region = new VkImageCopy();
            region.dstSubresource = dstSubresource;
            region.srcSubresource = srcSubresource;
            region.extent.width = width;
            region.extent.height = height;
            region.extent.depth = 1;

            VkCommandBuffer copyCmd = BeginOneTimeCommands();
            vkCmdCopyImage(copyCmd, srcImage, VkImageLayout.TransferSrcOptimal, dstImage, VkImageLayout.TransferDstOptimal, 1, ref region);
            EndOneTimeCommands(copyCmd);
        }

        protected VkCommandBuffer BeginOneTimeCommands()
        {
            VkCommandBufferAllocateInfo allocInfo = VkCommandBufferAllocateInfo.New();
            allocInfo.commandBufferCount = 1;
            allocInfo.commandPool = _rc.GraphicsCommandPool;
            allocInfo.level = VkCommandBufferLevel.Primary;

            vkAllocateCommandBuffers(_device, ref allocInfo, out VkCommandBuffer cb);

            VkCommandBufferBeginInfo beginInfo = VkCommandBufferBeginInfo.New();
            beginInfo.flags = VkCommandBufferUsageFlags.OneTimeSubmit;

            vkBeginCommandBuffer(cb, ref beginInfo);

            return cb;
        }

        protected void EndOneTimeCommands(VkCommandBuffer cb)
        {
            vkEndCommandBuffer(cb);

            VkSubmitInfo submitInfo = VkSubmitInfo.New();
            submitInfo.commandBufferCount = 1;
            submitInfo.pCommandBuffers = &cb;

            vkQueueSubmit(_rc.GraphicsQueue, 1, ref submitInfo, VkFence.Null);
            vkQueueWaitIdle(_rc.GraphicsQueue);

            vkFreeCommandBuffers(_device, _rc.GraphicsCommandPool, 1, ref cb);
        }
    }
}
