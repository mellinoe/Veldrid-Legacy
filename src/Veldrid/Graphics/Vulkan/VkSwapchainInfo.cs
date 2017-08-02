using System;
using System.Linq;
using Veldrid.Collections;
using Vulkan;
using static Vulkan.VulkanNative;
using static Veldrid.Graphics.Vulkan.VulkanUtil;

namespace Veldrid.Graphics.Vulkan
{
    public unsafe class VkSwapchainInfo : VkFramebufferBase, IDisposable
    {
        private readonly VkDevice _device;
        private readonly VkResourceFactory _resourceFactory;
        private readonly VkSurfaceKHR _surface;
        private readonly VkPhysicalDevice _physicalDevice;
        private readonly uint _graphicsQueueIndex;
        private readonly uint _presentQueueIndex;
        // Swapchain stuff
        private VkSwapchainKHR _swapchain;
        private RawList<VkImage> _scImages = new RawList<VkImage>();
        private RawList<VkRegularFramebuffer> _scFramebuffers = new RawList<VkRegularFramebuffer>();
        private RawList<VkTexture2D> _scColorTextures = new RawList<VkTexture2D>();
        private VkFormat _scImageFormat;
        private VkExtent2D _scExtent;
        private uint _scImageIndex;

        // Depth stuff
        private VkTexture2D _depthTexture;

        public VkExtent2D SwapchainExtent => _scExtent;
        public VkSwapchainKHR Swapchain => _swapchain;
        public VkFormat SwapchainFormat => _scImageFormat;
        public uint ImageIndex => _scImageIndex;

        public override VkFramebuffer VkFramebuffer => GetCurrentFramebuffer().VkFramebuffer;

        public override VkTexture2D ColorTexture { get => GetColorTexture(0); set => AttachColorTexture(0, value); }
        public override VkTexture2D DepthTexture
        {
            get => _depthTexture;
            set => throw new VeldridException("Cannot modify the depth texture of a Vulkan swapchain.");
        }

        public override int Width => (int)_scExtent.width;

        public override int Height => (int)_scExtent.height;

        public override VkRenderPass RenderPassClearBuffer => _scFramebuffers[0].RenderPassClearBuffer;
        public override VkRenderPass RenderPassNoClear => _scFramebuffers[0].RenderPassNoClear;

        public VkSwapchainInfo(
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
            _physicalDevice = physicalDevice;
            _resourceFactory = resourceFactory;
            _surface = surface;
            _graphicsQueueIndex = graphicsQueueIndex;
            _presentQueueIndex = presentQueueIndex;
            CreateSwapchain(width, height);
        }

        public void AcquireNextImage(VkDevice device, VkSemaphore semaphore)
        {
            VkResult result = vkAcquireNextImageKHR(
                device,
                _swapchain,
                ulong.MaxValue,
                semaphore,
                VkFence.Null,
                ref _scImageIndex);
            if (result == VkResult.ErrorOutOfDate || result == VkResult.Suboptimal)
            {
                // RecreateSwapChain();
            }
            else if (result != VkResult.Success)
            {
                throw new VeldridException("Could not acquire next image from the Vulkan swapchain.");
            }
        }

        public VkRegularFramebuffer GetCurrentFramebuffer()
        {
            return _scFramebuffers[_scImageIndex];
        }

        public VkRegularFramebuffer GetFramebuffer(uint imageIndex)
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
                foreach (VkRegularFramebuffer fb in _scFramebuffers)
                {
                    fb.Dispose();
                }
                _scFramebuffers.Clear();
            }

            _scFramebuffers.Resize(_scImages.Count);
            _scColorTextures.Resize(_scImages.Count);
            for (uint i = 0; i < _scImages.Count; i++)
            {
                VkRegularFramebuffer fb = (VkRegularFramebuffer)_resourceFactory.CreateFramebuffer();
                VkTexture2D colorTex = new VkTexture2D(_device, 1, (int)_scExtent.width, (int)_scExtent.height, _scImageFormat, _scImages[i]);
                fb.ColorTexture = colorTex;
                fb.DepthTexture = _depthTexture;
                _scFramebuffers[i] = fb;
                _scColorTextures[i] = colorTex;
            }
        }

        public override void Dispose()
        {
            vkDestroySwapchainKHR(_device, _swapchain, null);
        }

        public override void AttachColorTexture(int index, VkTexture2D texture)
        {
            throw new VeldridException("Cannot attach a color texture to a Vulkan Swapchain.");
        }

        public override VkTexture2D GetColorTexture(int index)
        {
            return _scColorTextures[0];
        }

        public void Resize(int width, int height)
        {
            CreateSwapchain(width, height);
        }

        private void CreateSwapchain(
            int width,
            int height)
        {
            uint surfaceFormatCount = 0;
            vkGetPhysicalDeviceSurfaceFormatsKHR(_physicalDevice, _surface, ref surfaceFormatCount, null);
            VkSurfaceFormatKHR[] formats = new VkSurfaceFormatKHR[surfaceFormatCount];
            vkGetPhysicalDeviceSurfaceFormatsKHR(_physicalDevice, _surface, ref surfaceFormatCount, out formats[0]);

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
            vkGetPhysicalDeviceSurfacePresentModesKHR(_physicalDevice, _surface, ref presentModeCount, null);
            VkPresentModeKHR[] presentModes = new VkPresentModeKHR[presentModeCount];
            vkGetPhysicalDeviceSurfacePresentModesKHR(_physicalDevice, _surface, ref presentModeCount, out presentModes[0]);

            VkPresentModeKHR presentMode = VkPresentModeKHR.Fifo;
            if (presentModes.Contains(VkPresentModeKHR.Mailbox))
            {
                presentMode = VkPresentModeKHR.Mailbox;
            }
            else if (presentModes.Contains(VkPresentModeKHR.Immediate))
            {
                presentMode = VkPresentModeKHR.Immediate;
            }

            vkGetPhysicalDeviceSurfaceCapabilitiesKHR(_physicalDevice, _surface, out VkSurfaceCapabilitiesKHR surfaceCapabilities);
            uint imageCount = surfaceCapabilities.minImageCount + 1;

            VkSwapchainCreateInfoKHR swapchainCI = VkSwapchainCreateInfoKHR.New();
            swapchainCI.surface = _surface;
            swapchainCI.presentMode = presentMode;
            swapchainCI.imageFormat = surfaceFormat.format;
            swapchainCI.imageColorSpace = surfaceFormat.colorSpace;
            swapchainCI.imageExtent = new VkExtent2D { width = (uint)width, height = (uint)height };
            swapchainCI.minImageCount = imageCount;
            swapchainCI.imageArrayLayers = 1;
            swapchainCI.imageUsage = VkImageUsageFlags.ColorAttachment;

            FixedArray2<uint> queueFamilyIndices = new FixedArray2<uint>(_graphicsQueueIndex, _presentQueueIndex);

            if (_graphicsQueueIndex != _presentQueueIndex)
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

            VkResult result = vkCreateSwapchainKHR(_device, ref swapchainCI, null, out _swapchain);
            CheckResult(result);
            if (oldSwapchain != NullHandle)
            {
                vkDestroySwapchainKHR(_device, oldSwapchain, null);
            }

            // Get the images
            uint scImageCount = 0;
            result = vkGetSwapchainImagesKHR(_device, _swapchain, ref scImageCount, null);
            CheckResult(result);
            _scImages.Count = scImageCount;
            result = vkGetSwapchainImagesKHR(_device, _swapchain, ref scImageCount, out _scImages.Items[0]);
            CheckResult(result);

            _scImageFormat = surfaceFormat.format;
            _scExtent = swapchainCI.imageExtent;

            CreateDepthTexture();
            CreateFramebuffers();
        }
    }
}
