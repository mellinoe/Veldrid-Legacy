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

            vkGetPhysicalDeviceImageFormatProperties(
                _physicalDevice,
                _vkFormat,
                VkImageType.Image2D,
                VkImageTiling.Linear,
                VkImageUsageFlags.Sampled,
                VkImageCreateFlags.CubeCompatible,
                out VkImageFormatProperties linearProps);

            vkGetPhysicalDeviceImageFormatProperties(
                _physicalDevice,
                _vkFormat,
                VkImageType.Image2D,
                VkImageTiling.Optimal,
                VkImageUsageFlags.Sampled,
                VkImageCreateFlags.CubeCompatible,
                out VkImageFormatProperties optimalProps);

            bool useSingleStagingBuffer = linearProps.maxArrayLayers >= 6;

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
            imageCI.tiling = useSingleStagingBuffer ? VkImageTiling.Linear : VkImageTiling.Optimal;
            imageCI.usage = VkImageUsageFlags.Sampled | VkImageUsageFlags.TransferDst;
            imageCI.initialLayout = VkImageLayout.Preinitialized;

            VkResult result = vkCreateImage(_device, ref imageCI, null, out _image);
            CheckResult(result);

            vkGetImageMemoryRequirements(_device, _image, out VkMemoryRequirements memReqs);
            uint memoryType = FindMemoryType(_physicalDevice, memReqs.memoryTypeBits, VkMemoryPropertyFlags.DeviceLocal);

            _memory = memoryManager.Allocate(memoryType, memReqs.size, memReqs.alignment);
            vkBindImageMemory(_device, _image, _memory.DeviceMemory, _memory.Offset);

            if (useSingleStagingBuffer)
            {
                CopyDataSingleStagingBuffer(pixelsFront, pixelsBack, pixelsLeft, pixelsRight, pixelsTop, pixelsBottom, memReqs);
            }
            else
            {
                CopyDataMultiImage(pixelsFront, pixelsBack, pixelsLeft, pixelsRight, pixelsTop, pixelsBottom);
            }

            TransitionImageLayout(_image, (uint)MipLevels, 0, 6, VkImageLayout.TransferDstOptimal, VkImageLayout.ShaderReadOnlyOptimal);
            _imageLayout = VkImageLayout.ShaderReadOnlyOptimal;
        }

        private void CopyDataSingleStagingBuffer(IntPtr pixelsFront, IntPtr pixelsBack, IntPtr pixelsLeft, IntPtr pixelsRight, IntPtr pixelsTop, IntPtr pixelsBottom, VkMemoryRequirements memReqs)
        {
            VkBufferCreateInfo bufferCI = VkBufferCreateInfo.New();
            bufferCI.size = memReqs.size;
            bufferCI.usage = VkBufferUsageFlags.TransferSrc;
            vkCreateBuffer(_device, ref bufferCI, null, out VkBuffer stagingBuffer);

            vkGetBufferMemoryRequirements(_device, stagingBuffer, out VkMemoryRequirements stagingMemReqs);
            VkMemoryBlock stagingMemory = _memoryManager.Allocate(
                FindMemoryType(
                    _physicalDevice,
                    stagingMemReqs.memoryTypeBits,
                    VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent),
                stagingMemReqs.size,
                stagingMemReqs.alignment);

            VkResult result = vkBindBufferMemory(_device, stagingBuffer, stagingMemory.DeviceMemory, 0);
            CheckResult(result);

            StackList<IntPtr, Size6IntPtr> faces = new StackList<IntPtr, Size6IntPtr>();
            faces.Add(pixelsRight);
            faces.Add(pixelsLeft);
            faces.Add(pixelsTop);
            faces.Add(pixelsBottom);
            faces.Add(pixelsBack);
            faces.Add(pixelsFront);

            for (uint i = 0; i < 6; i++)
            {
                VkImageSubresource subresource;
                subresource.aspectMask = VkImageAspectFlags.Color;
                subresource.arrayLayer = i;
                subresource.mipLevel = 0;
                vkGetImageSubresourceLayout(_device, _image, ref subresource, out VkSubresourceLayout faceLayout);
                void* mappedPtr;
                result = vkMapMemory(_device, stagingMemory.DeviceMemory, faceLayout.offset, faceLayout.size, 0, &mappedPtr);
                CheckResult(result);
                Buffer.MemoryCopy((void*)faces[i], mappedPtr, faceLayout.size, faceLayout.size);
                vkUnmapMemory(_device, stagingMemory.DeviceMemory);
            }

            StackList<VkBufferImageCopy, Size512Bytes> copyRegions = new StackList<VkBufferImageCopy, Size512Bytes>();
            for (uint i = 0; i < 6; i++)
            {
                VkImageSubresource subres;
                subres.aspectMask = VkImageAspectFlags.Color;
                subres.mipLevel = 0;
                subres.arrayLayer = i;
                vkGetImageSubresourceLayout(_device, _image, ref subres, out VkSubresourceLayout layout);

                VkBufferImageCopy copyRegion;
                copyRegion.bufferOffset = layout.offset;
                copyRegion.bufferImageHeight = 0;
                copyRegion.bufferRowLength = 0;
                copyRegion.imageExtent.width = (uint)Width;
                copyRegion.imageExtent.height = (uint)Height;
                copyRegion.imageExtent.depth = 1;
                copyRegion.imageOffset.x = 0;
                copyRegion.imageOffset.y = 0;
                copyRegion.imageOffset.z = 0;
                copyRegion.imageSubresource.baseArrayLayer = i;
                copyRegion.imageSubresource.aspectMask = VkImageAspectFlags.Color;
                copyRegion.imageSubresource.layerCount = 1;
                copyRegion.imageSubresource.mipLevel = 0;

                copyRegions.Add(copyRegion);
            }

            VkFenceCreateInfo fenceCI = VkFenceCreateInfo.New();
            result = vkCreateFence(_device, ref fenceCI, null, out VkFence copyFence);
            CheckResult(result);

            TransitionImageLayout(_image, (uint)MipLevels, 0, 6, _imageLayout, VkImageLayout.TransferDstOptimal);

            VkCommandBuffer copyCmd = _rc.BeginOneTimeCommands();
            vkCmdCopyBufferToImage(copyCmd, stagingBuffer, _image, VkImageLayout.TransferDstOptimal, copyRegions.Count, (IntPtr)copyRegions.Data);
            _rc.EndOneTimeCommands(copyCmd, copyFence);
            result = vkWaitForFences(_device, 1, ref copyFence, true, ulong.MaxValue);
            CheckResult(result);

            vkDestroyBuffer(_device, stagingBuffer, null);
            _memoryManager.Free(stagingMemory);
        }

        private void CopyDataMultiImage(
            IntPtr pixelsFront,
            IntPtr pixelsBack,
            IntPtr pixelsLeft,
            IntPtr pixelsRight,
            IntPtr pixelsTop,
            IntPtr pixelsBottom)
        {
            StackList<IntPtr, Size6IntPtr> faces = new StackList<IntPtr, Size6IntPtr>();
            faces.Add(pixelsRight);
            faces.Add(pixelsLeft);
            faces.Add(pixelsTop);
            faces.Add(pixelsBottom);
            faces.Add(pixelsBack);
            faces.Add(pixelsFront);

            TransitionImageLayout(_image, (uint)MipLevels, 0, 6, _imageLayout, VkImageLayout.TransferDstOptimal);

            for (uint i = 0; i < 6; i++)
            {
                CreateImage(
                    _device,
                    _physicalDevice,
                    _memoryManager,
                    (uint)Width,
                    (uint)Height,
                    1,
                    _vkFormat,
                    VkImageTiling.Linear,
                    VkImageUsageFlags.TransferSrc,
                    VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent,
                    out VkImage stagingImage, out VkMemoryBlock stagingMemory);

                VkImageSubresource subresource;
                subresource.aspectMask = VkImageAspectFlags.Color;
                subresource.arrayLayer = 0;
                subresource.mipLevel = 0;

                vkGetImageSubresourceLayout(_device, stagingImage, ref subresource, out VkSubresourceLayout stagingLayout);
                void* mappedPtr;
                VkResult result = vkMapMemory(_device, stagingMemory.DeviceMemory, stagingMemory.Offset, stagingLayout.size, 0, &mappedPtr);
                CheckResult(result);
                IntPtr data = faces[i];
                ulong dataSizeInBytes = (ulong)(Width * Height * FormatHelpers.GetPixelSizeInBytes(_format));
                ulong rowPitch = stagingLayout.rowPitch;
                if (rowPitch == (ulong)Width)
                {
                    Buffer.MemoryCopy(data.ToPointer(), mappedPtr, dataSizeInBytes, dataSizeInBytes);
                }
                else
                {
                    int pixelSizeInBytes = FormatHelpers.GetPixelSizeInBytes(_format);
                    for (uint yy = 0; yy < Height; yy++)
                    {
                        byte* dstRowStart = ((byte*)mappedPtr) + (rowPitch * yy);
                        byte* srcRowStart = ((byte*)data.ToPointer()) + (Width * yy * pixelSizeInBytes);
                        Unsafe.CopyBlock(dstRowStart, srcRowStart, (uint)(Width * pixelSizeInBytes));
                    }
                }

                vkUnmapMemory(_device, stagingMemory.DeviceMemory);

                TransitionImageLayout(stagingImage, 1, 0, 1, VkImageLayout.Preinitialized, VkImageLayout.TransferSrcOptimal);
                CopyImage(stagingImage, 0, 0, _image, 0, i, (uint)Width, (uint)Height);

                vkDestroyImage(_device, stagingImage, null);
                _memoryManager.Free(stagingMemory);
            }
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

        protected void TransitionImageLayout(VkImage image, uint mipLevels, uint baseArrayLayer, uint layerCount, VkImageLayout oldLayout, VkImageLayout newLayout)
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
            barrier.subresourceRange.baseArrayLayer = baseArrayLayer;
            barrier.subresourceRange.layerCount = layerCount;

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

        protected void CopyImage(VkImage srcImage, uint srcMipLevel, uint srcArrayLayer, VkImage dstImage, uint dstMipLevel, uint dstArrayLayer, uint width, uint height)
        {
            VkImageSubresourceLayers srcSubresource = new VkImageSubresourceLayers();
            srcSubresource.mipLevel = srcMipLevel;
            srcSubresource.aspectMask = VkImageAspectFlags.Color;
            srcSubresource.baseArrayLayer = srcArrayLayer;
            srcSubresource.layerCount = 1;

            VkImageSubresourceLayers dstSubresource = new VkImageSubresourceLayers();
            dstSubresource.mipLevel = dstMipLevel;
            dstSubresource.aspectMask = VkImageAspectFlags.Color;
            dstSubresource.baseArrayLayer = dstArrayLayer;
            dstSubresource.layerCount = 1;

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
