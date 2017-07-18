using System;
using System.Linq;
using Veldrid.Collections;
using Vulkan;
using static Vulkan.VulkanNative;

namespace Veldrid.Graphics.Vulkan
{
    public unsafe class VkSwapchainInfo
    {
        // Swapchain stuff
        private VkSwapchainKHR _swapchain;
        private RawList<VkImage> _scImages = new RawList<VkImage>();
        private RawList<VkImageView> _scImageViews = new RawList<VkImageView>();
        private RawList<VkFramebuffer> _scFramebuffers = new RawList<VkFramebuffer>();
        private VkFormat _scImageFormat;
        private VkExtent2D _scExtent;

        public VkExtent2D SwapchainExtent => _scExtent;
        public VkSwapchainKHR Swapchain => _swapchain;
        public VkFormat SwapchainFormat => _scImageFormat;

        public void CreateSwapchain(
            VkDevice device,
            VkPhysicalDevice physicalDevice,
            VkSurfaceKHR surface,
            uint graphicsQueueIndex,
            uint presentQueueIndex,
            int width,
            int height)
        {
            uint surfaceFormatCount = 0;
            vkGetPhysicalDeviceSurfaceFormatsKHR(physicalDevice, surface, ref surfaceFormatCount, null);
            VkSurfaceFormatKHR[] formats = new VkSurfaceFormatKHR[surfaceFormatCount];
            vkGetPhysicalDeviceSurfaceFormatsKHR(physicalDevice, surface, ref surfaceFormatCount, out formats[0]);

            VkSurfaceFormatKHR surfaceFormat = new VkSurfaceFormatKHR();
            if (formats.Length == 1 && formats[0].format == VkFormat.Undefined)
            {
                surfaceFormat = new VkSurfaceFormatKHR { colorSpace = VkColorSpaceKHR.SrgbNonlinear, format = VkFormat.B8g8r8a8Unorm };
            }
            else
            {
                foreach (VkSurfaceFormatKHR format in formats)
                {
                    if (format.colorSpace == VkColorSpaceKHR.SrgbNonlinear && format.format == VkFormat.B8g8r8a8Unorm)
                    {
                        surfaceFormat = format;
                        break;
                    }
                }
                if (surfaceFormat.format == VkFormat.Undefined)
                {
                    surfaceFormat = formats[0];
                }
            }

            uint presentModeCount = 0;
            vkGetPhysicalDeviceSurfacePresentModesKHR(physicalDevice, surface, ref presentModeCount, null);
            VkPresentModeKHR[] presentModes = new VkPresentModeKHR[presentModeCount];
            vkGetPhysicalDeviceSurfacePresentModesKHR(physicalDevice, surface, ref presentModeCount, out presentModes[0]);

            VkPresentModeKHR presentMode = VkPresentModeKHR.Fifo;
            if (presentModes.Contains(VkPresentModeKHR.Mailbox))
            {
                presentMode = VkPresentModeKHR.Mailbox;
            }
            else if (presentModes.Contains(VkPresentModeKHR.Immediate))
            {
                presentMode = VkPresentModeKHR.Immediate;
            }

            vkGetPhysicalDeviceSurfaceCapabilitiesKHR(physicalDevice, surface, out VkSurfaceCapabilitiesKHR surfaceCapabilities);
            uint imageCount = surfaceCapabilities.minImageCount + 1;

            VkSwapchainCreateInfoKHR sci = VkSwapchainCreateInfoKHR.New();
            sci.surface = surface;
            sci.presentMode = presentMode;
            sci.imageFormat = surfaceFormat.format;
            sci.imageColorSpace = surfaceFormat.colorSpace;
            sci.imageExtent = new VkExtent2D { width = (uint)width, height = (uint)height };
            sci.minImageCount = imageCount;
            sci.imageArrayLayers = 1;
            sci.imageUsage = VkImageUsageFlags.ColorAttachment;

            FixedArray2<uint> queueFamilyIndices = new FixedArray2<uint>(graphicsQueueIndex, presentQueueIndex);

            if (graphicsQueueIndex != presentQueueIndex)
            {
                sci.imageSharingMode = VkSharingMode.Concurrent;
                sci.queueFamilyIndexCount = 2;
                sci.pQueueFamilyIndices = &queueFamilyIndices.First;
            }
            else
            {
                sci.imageSharingMode = VkSharingMode.Exclusive;
                sci.queueFamilyIndexCount = 0;
            }

            sci.preTransform = surfaceCapabilities.currentTransform;
            sci.compositeAlpha = VkCompositeAlphaFlagsKHR.Opaque;
            sci.clipped = true;

            VkSwapchainKHR oldSwapchain = _swapchain;
            sci.oldSwapchain = oldSwapchain;

            vkCreateSwapchainKHR(device, ref sci, null, out _swapchain);
            if (oldSwapchain != NullHandle)
            {
                vkDestroySwapchainKHR(device, oldSwapchain, null);
            }

            // Get the images
            uint scImageCount = 0;
            vkGetSwapchainImagesKHR(device, _swapchain, ref scImageCount, null);
            _scImages.Count = scImageCount;
            vkGetSwapchainImagesKHR(device, _swapchain, ref scImageCount, out _scImages.Items[0]);

            _scImageFormat = surfaceFormat.format;
            _scExtent = sci.imageExtent;
        }

        public void CreateImageViews(VkDevice device)
        {
            _scImageViews.Resize(_scImages.Count);
            for (int i = 0; i < _scImages.Count; i++)
            {
                CreateImageView(device, _scImages[i], _scImageFormat, out _scImageViews[i]);
            }
        }

        public uint AcquireNextImage(VkDevice device, VkSemaphore semaphore)
        {
            uint imageIndex = 0;
            VkResult result = vkAcquireNextImageKHR(device, _swapchain, ulong.MaxValue, semaphore, VkFence.Null, ref imageIndex);
            if (result == VkResult.ErrorOutOfDate || result == VkResult.Suboptimal)
            {
                // RecreateSwapChain();
            }
            else if (result != VkResult.Success)
            {
                throw new VeldridException("Could not acquire next image from the Vulkan swapchain.");
            }

            return imageIndex;
        }

        public VkFramebuffer GetFramebuffer(uint imageIndex)
        {
            return _scFramebuffers[imageIndex];
        }

        private void CreateImageView(VkDevice device, VkImage image, VkFormat format, out VkImageView imageView)
        {
            VkImageViewCreateInfo imageViewCI = VkImageViewCreateInfo.New();
            imageViewCI.image = image;
            imageViewCI.viewType = VkImageViewType._2d;
            imageViewCI.format = format;
            imageViewCI.subresourceRange.aspectMask = VkImageAspectFlags.Color;
            imageViewCI.subresourceRange.baseMipLevel = 0;
            imageViewCI.subresourceRange.levelCount = 1;
            imageViewCI.subresourceRange.baseArrayLayer = 0;
            imageViewCI.subresourceRange.layerCount = 1;

            vkCreateImageView(device, ref imageViewCI, null, out imageView);
        }

        private void CreateFramebuffers()
        {
            _scFramebuffers.Resize(_scImageViews.Count);
            for (uint i = 0; i < _scImageViews.Count; i++)
            {
                VkImageView attachment = _scImageViews[i];
                VkFramebufferCreateInfo framebufferCI = VkFramebufferCreateInfo.New();
                framebufferCI.renderPass = _renderPass;
                framebufferCI.attachmentCount = 1;
                framebufferCI.pAttachments = &attachment;
                framebufferCI.width = _scExtent.width;
                framebufferCI.height = _scExtent.height;
                framebufferCI.layers = 1;

                vkCreateFramebuffer(_device, ref framebufferCI, null, out _scFramebuffers[i]);
            }
        }
    }
}
