using System;
using System.Linq;
using Veldrid.Collections;
using Vulkan;
using static Vulkan.VulkanNative;
using static Veldrid.Graphics.Vulkan.VulkanUtil;

namespace Veldrid.Graphics.Vulkan
{
    public unsafe class VkSwapchainInfo : IDisposable
    {
        private VkDevice _device;
        private VkResourceFactory _resourceFactory;

        // Swapchain stuff
        private VkSwapchainKHR _swapchain;
        private RawList<VkImage> _scImages = new RawList<VkImage>();
        private RawList<VkFramebufferInfo> _scFramebuffers = new RawList<VkFramebufferInfo>();
        private VkFormat _scImageFormat;
        private VkExtent2D _scExtent;

        // Depth stuff
        private VkTexture2D _depthTexture;

        public VkExtent2D SwapchainExtent => _scExtent;
        public VkSwapchainKHR Swapchain => _swapchain;
        public VkFormat SwapchainFormat => _scImageFormat;

        public void CreateSwapchain(
            VkDevice device,
            VkPhysicalDevice physicalDevice,
            VkResourceFactory resourceFactory,
            VkSurfaceKHR surface,
            uint graphicsQueueIndex,
            uint presentQueueIndex,
            int width,
            int height)
        {
            _device = device;
            _resourceFactory = resourceFactory;

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

            VkSwapchainCreateInfoKHR swapchainCI = VkSwapchainCreateInfoKHR.New();
            swapchainCI.surface = surface;
            swapchainCI.presentMode = presentMode;
            swapchainCI.imageFormat = surfaceFormat.format;
            swapchainCI.imageColorSpace = surfaceFormat.colorSpace;
            swapchainCI.imageExtent = new VkExtent2D { width = (uint)width, height = (uint)height };
            swapchainCI.minImageCount = imageCount;
            swapchainCI.imageArrayLayers = 1;
            swapchainCI.imageUsage = VkImageUsageFlags.ColorAttachment;

            FixedArray2<uint> queueFamilyIndices = new FixedArray2<uint>(graphicsQueueIndex, presentQueueIndex);

            if (graphicsQueueIndex != presentQueueIndex)
            {
                swapchainCI.imageSharingMode = VkSharingMode.Concurrent;
                swapchainCI.queueFamilyIndexCount = 2;
                swapchainCI.pQueueFamilyIndices = &queueFamilyIndices.First;
            }
            else
            {
                swapchainCI.imageSharingMode = VkSharingMode.Exclusive;
                swapchainCI.queueFamilyIndexCount = 0;
            }

            swapchainCI.preTransform = surfaceCapabilities.currentTransform;
            swapchainCI.compositeAlpha = VkCompositeAlphaFlagsKHR.Opaque;
            swapchainCI.clipped = true;

            VkSwapchainKHR oldSwapchain = _swapchain;
            swapchainCI.oldSwapchain = oldSwapchain;

            VkResult result = vkCreateSwapchainKHR(device, ref swapchainCI, null, out _swapchain);
            CheckResult(result);
            if (oldSwapchain != NullHandle)
            {
                vkDestroySwapchainKHR(device, oldSwapchain, null);
            }

            // Get the images
            uint scImageCount = 0;
            result = vkGetSwapchainImagesKHR(device, _swapchain, ref scImageCount, null);
            CheckResult(result);
            _scImages.Count = scImageCount;
            result = vkGetSwapchainImagesKHR(device, _swapchain, ref scImageCount, out _scImages.Items[0]);
            CheckResult(result);

            _scImageFormat = surfaceFormat.format;
            _scExtent = swapchainCI.imageExtent;

            CreateDepthTexture();
            CreateFramebuffers();
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

        public VkFramebufferInfo GetFramebuffer(uint imageIndex)
        {
            return _scFramebuffers[imageIndex];
        }

        private void CreateDepthTexture()
        {
            if (_depthTexture != null)
            {
                _depthTexture.Dispose();
            }

            _depthTexture = (VkTexture2D)_resourceFactory.CreateTexture(
                1, 
                (int)_scExtent.width, 
                (int)_scExtent.height, 
                PixelFormat.R16_UInt, 
                DeviceTextureCreateOptions.DepthStencil);
        }

        private void CreateFramebuffers()
        {
            if (_scFramebuffers.Count > 0)
            {
                foreach (VkFramebufferInfo fb in _scFramebuffers)
                {
                    fb.Dispose();
                }
                _scFramebuffers.Clear();
            }

            _scFramebuffers.Resize(_scImages.Count);
            for (uint i = 0; i < _scImages.Count; i++)
            {
                VkFramebufferInfo fb = (VkFramebufferInfo)_resourceFactory.CreateFramebuffer();
                VkTexture2D colorTex = new VkTexture2D(_device, 1, (int)_scExtent.width, (int)_scExtent.height, _scImageFormat, _scImages[i]);
                fb.ColorTexture = colorTex;
                fb.DepthTexture = _depthTexture;
                _scFramebuffers[i] = fb;
            }
        }

        public void Dispose()
        {
            vkDestroySwapchainKHR(_device, _swapchain, null);
        }
    }
}
