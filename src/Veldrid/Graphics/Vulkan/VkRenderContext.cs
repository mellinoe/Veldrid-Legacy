using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Numerics;
using System.Runtime.InteropServices;
using System.Text;
using Veldrid.Collections;
using Vulkan;
using static Veldrid.Graphics.Vulkan.VulkanUtil;
using static Vulkan.VulkanNative;

namespace Veldrid.Graphics.Vulkan
{
    public unsafe class VkRenderContext : RenderContext
    {
        private static readonly FixedUtf8String s_name = "VkRenderContext";
        private VkInstance _instance;
        private VkSurfaceKHR _surface;
        private VkPhysicalDevice _physicalDevice;
        private VkPhysicalDeviceProperties _physicalDeviceProperties;
        private VkPhysicalDeviceFeatures _physicalDeviceFeatures;
        private uint _graphicsQueueIndex;
        private uint _presentQueueIndex;
        private VkDevice _device;
        private VkQueue _graphicsQueue;
        private VkQueue _presentQueue;

        // Swapchain stuff
        private VkSwapchainKHR _swapchain;
        private RawList<VkImage> _scImages = new RawList<VkImage>();
        private RawList<VkImageView> _scImageViews = new RawList<VkImageView>();
        private RawList<VkFramebuffer> _scFramebuffers = new RawList<VkFramebuffer>();
        private VkFormat _scImageFormat;
        private VkExtent2D _scExtent;

        public VkRenderContext(IntPtr hinstance, IntPtr hwnd, int width, int height)
        {
            CreateInstance();
            CreateSurface(hinstance, hwnd);
            CreatePhysicalDevice();
            CreateLogicalDevice();
            ResourceFactory = new VkResourceFactory(_device, _physicalDevice);
            CreateSwapchain(width, height);
            CreateImageViews();
        }

        private void CreateInstance()
        {
            VkInstanceCreateInfo instanceCI = VkInstanceCreateInfo.New();
            VkApplicationInfo applicationInfo = new VkApplicationInfo();
            applicationInfo.apiVersion = new VkVersion(1, 0, 0);
            applicationInfo.applicationVersion = new VkVersion(1, 0, 0);
            applicationInfo.engineVersion = new VkVersion(1, 0, 0);
            applicationInfo.pApplicationName = s_name;
            applicationInfo.pEngineName = s_name;

            instanceCI.pApplicationInfo = &applicationInfo;

            StackList<IntPtr, Size64Bytes> instanceExtensions = new StackList<IntPtr, Size64Bytes>();
            StackList<IntPtr, Size64Bytes> instanceLayers = new StackList<IntPtr, Size64Bytes>();

            instanceExtensions.Add(CommonStrings.VK_KHR_SURFACE_EXTENSION_NAME);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                instanceExtensions.Add(CommonStrings.VK_KHR_WIN32_SURFACE_EXTENSION_NAME);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                instanceExtensions.Add(CommonStrings.VK_KHR_XCB_SURFACE_EXTENSION_NAME);
            }
            else
            {
                throw new NotSupportedException("This platform does not support Vulkan.");
            }

            bool debug = false;
#if DEBUG
            debug = true;
#endif
            if (debug)
            {
                instanceExtensions.Add(CommonStrings.VK_EXT_DEBUG_REPORT_EXTENSION_NAME);
                instanceLayers.Add(CommonStrings.StandardValidationLayerName);
            }

            instanceCI.enabledExtensionCount = instanceExtensions.Count;
            instanceCI.ppEnabledExtensionNames = (byte**)instanceExtensions.Data;

            instanceCI.enabledLayerCount = instanceLayers.Count;
            instanceCI.ppEnabledLayerNames = (byte**)instanceLayers.Data;

            VkResult result = vkCreateInstance(ref instanceCI, null, out _instance);
            CheckResult(result);
        }

        private void CreateSurface(IntPtr hinstance, IntPtr hwnd)
        {
            VkWin32SurfaceCreateInfoKHR surfaceCI = VkWin32SurfaceCreateInfoKHR.New();
            surfaceCI.hwnd = hwnd;
            surfaceCI.hinstance = hinstance;
            CheckResult(vkCreateWin32SurfaceKHR(_instance, ref surfaceCI, null, out _surface));
        }

        private void CreatePhysicalDevice()
        {
            uint deviceCount = 0;
            vkEnumeratePhysicalDevices(_instance, ref deviceCount, null);
            if (deviceCount == 0)
            {
                throw new InvalidOperationException("No physical devices exist.");
            }

            VkPhysicalDevice[] physicalDevices = new VkPhysicalDevice[deviceCount];
            vkEnumeratePhysicalDevices(_instance, ref deviceCount, ref physicalDevices[0]);
            // Just use the first one.
            _physicalDevice = physicalDevices[0];

            vkGetPhysicalDeviceProperties(_physicalDevice, out _physicalDeviceProperties);
            string deviceName;
            fixed (byte* utf8NamePtr = _physicalDeviceProperties.deviceName)
            {
                deviceName = Encoding.UTF8.GetString(utf8NamePtr, (int)MaxPhysicalDeviceNameSize);
            }

            vkGetPhysicalDeviceFeatures(_physicalDevice, out _physicalDeviceFeatures);

            Console.WriteLine($"Using device: {deviceName}");
        }

        private void CreateLogicalDevice()
        {
            GetQueueFamilyIndices();

            HashSet<uint> familyIndices = new HashSet<uint> { _graphicsQueueIndex, _presentQueueIndex };
            RawList<VkDeviceQueueCreateInfo> queueCreateInfos = new RawList<VkDeviceQueueCreateInfo>();

            foreach (uint index in familyIndices)
            {
                VkDeviceQueueCreateInfo queueCreateInfo = VkDeviceQueueCreateInfo.New();
                queueCreateInfo.queueFamilyIndex = _graphicsQueueIndex;
                queueCreateInfo.queueCount = 1;
                float priority = 1f;
                queueCreateInfo.pQueuePriorities = &priority;
                queueCreateInfos.Add(queueCreateInfo);
            }

            VkPhysicalDeviceFeatures deviceFeatures = new VkPhysicalDeviceFeatures();

            VkDeviceCreateInfo deviceCreateInfo = VkDeviceCreateInfo.New();

            fixed (VkDeviceQueueCreateInfo* qciPtr = &queueCreateInfos.Items[0])
            {
                deviceCreateInfo.pQueueCreateInfos = qciPtr;
                deviceCreateInfo.queueCreateInfoCount = queueCreateInfos.Count;

                deviceCreateInfo.pEnabledFeatures = &deviceFeatures;

                byte* layerNames = CommonStrings.StandardValidationLayerName;
                deviceCreateInfo.enabledLayerCount = 1;
                deviceCreateInfo.ppEnabledLayerNames = &layerNames;

                byte* extensionNames = CommonStrings.VK_KHR_SWAPCHAIN_EXTENSION_NAME;
                deviceCreateInfo.enabledExtensionCount = 1;
                deviceCreateInfo.ppEnabledExtensionNames = &extensionNames;

                vkCreateDevice(_physicalDevice, ref deviceCreateInfo, null, out _device);
            }

            vkGetDeviceQueue(_device, _graphicsQueueIndex, 0, out _graphicsQueue);
            vkGetDeviceQueue(_device, _presentQueueIndex, 0, out _presentQueue);
        }

        private void GetQueueFamilyIndices()
        {
            uint queueFamilyCount = 0;
            vkGetPhysicalDeviceQueueFamilyProperties(_physicalDevice, ref queueFamilyCount, null);
            VkQueueFamilyProperties[] qfp = new VkQueueFamilyProperties[queueFamilyCount];
            vkGetPhysicalDeviceQueueFamilyProperties(_physicalDevice, ref queueFamilyCount, out qfp[0]);

            bool foundGraphics = false;
            bool foundPresent = false;

            for (uint i = 0; i < qfp.Length; i++)
            {
                if ((qfp[i].queueFlags & VkQueueFlags.Graphics) != 0)
                {
                    _graphicsQueueIndex = i;
                    foundGraphics = true;
                }

                vkGetPhysicalDeviceSurfaceSupportKHR(_physicalDevice, i, _surface, out VkBool32 presentSupported);
                if (presentSupported)
                {
                    _presentQueueIndex = i;
                    foundPresent = true;
                }

                if (foundGraphics && foundPresent)
                {
                    break;
                }
            }
        }

        private void CreateSwapchain(int width, int height)
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

            VkSwapchainCreateInfoKHR sci = VkSwapchainCreateInfoKHR.New();
            sci.surface = _surface;
            sci.presentMode = presentMode;
            sci.imageFormat = surfaceFormat.format;
            sci.imageColorSpace = surfaceFormat.colorSpace;
            sci.imageExtent = new VkExtent2D { width = (uint)width, height = (uint)height };
            sci.minImageCount = imageCount;
            sci.imageArrayLayers = 1;
            sci.imageUsage = VkImageUsageFlags.ColorAttachment;

            FixedArray2<uint> queueFamilyIndices = new FixedArray2<uint>(_graphicsQueueIndex, _presentQueueIndex);

            if (_graphicsQueueIndex != _presentQueueIndex)
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

            vkCreateSwapchainKHR(_device, ref sci, null, out _swapchain);
            if (oldSwapchain != NullHandle)
            {
                vkDestroySwapchainKHR(_device, oldSwapchain, null);
            }

            // Get the images
            uint scImageCount = 0;
            vkGetSwapchainImagesKHR(_device, _swapchain, ref scImageCount, null);
            _scImages.Count = scImageCount;
            vkGetSwapchainImagesKHR(_device, _swapchain, ref scImageCount, out _scImages.Items[0]);

            _scImageFormat = surfaceFormat.format;
            _scExtent = sci.imageExtent;
        }

        private void CreateImageViews()
        {
            _scImageViews.Resize(_scImages.Count);
            for (int i = 0; i < _scImages.Count; i++)
            {
                CreateImageView(_scImages[i], _scImageFormat, out _scImageViews[i]);
            }
        }

        private void CreateImageView(VkImage image, VkFormat format, out VkImageView imageView)
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

            vkCreateImageView(_device, ref imageViewCI, null, out imageView);
        }

        public override ResourceFactory ResourceFactory { get; }

        public override RenderCapabilities RenderCapabilities => throw new NotImplementedException();

        public override void DrawIndexedPrimitives(int count, int startingIndex) => DrawIndexedPrimitives(count, startingIndex, 0);
        public override void DrawIndexedPrimitives(int count, int startingIndex, int startingVertex)
        {
            throw new NotImplementedException();
        }

        public override void DrawInstancedPrimitives(int indexCount, int instanceCount, int startingIndex)
            => DrawInstancedPrimitives(indexCount, instanceCount, startingIndex, 0);

        public override void DrawInstancedPrimitives(int indexCount, int instanceCount, int startingIndex, int startingVertex)
        {
            throw new NotImplementedException();
        }

        protected override Vector2 GetBottomRightUvCoordinate()
        {
            return Vector2.One;
        }

        protected override Vector2 GetTopLeftUvCoordinate()
        {
            return Vector2.Zero;
        }

        protected override void PlatformClearBuffer()
        {
            throw new NotImplementedException();
        }

        protected override void PlatformClearMaterialResourceBindings()
        {
            throw new NotImplementedException();
        }

        protected override void PlatformDispose()
        {
            throw new NotImplementedException();
        }

        protected override GraphicsBackend PlatformGetGraphicsBackend()
        {
            throw new NotImplementedException();
        }

        protected override void PlatformResize(int width, int height)
        {
            throw new NotImplementedException();
        }

        protected override void PlatformSetBlendstate(BlendState blendState)
        {
            throw new NotImplementedException();
        }

        protected override void PlatformSetConstantBuffer(int slot, ConstantBuffer cb)
        {
            throw new NotImplementedException();
        }

        protected override void PlatformSetDefaultFramebuffer()
        {
            throw new NotImplementedException();
        }

        protected override void PlatformSetDepthStencilState(DepthStencilState depthStencilState)
        {
            throw new NotImplementedException();
        }

        protected override void PlatformSetFramebuffer(Framebuffer framebuffer)
        {
            throw new NotImplementedException();
        }

        protected override void PlatformSetIndexBuffer(IndexBuffer ib)
        {
            throw new NotImplementedException();
        }

        protected override void PlatformSetPrimitiveTopology(PrimitiveTopology primitiveTopology)
        {
            throw new NotImplementedException();
        }

        protected override void PlatformSetRasterizerState(RasterizerState rasterizerState)
        {
            throw new NotImplementedException();
        }

        protected override void PlatformSetSamplerState(int slot, SamplerState samplerState, bool mipmapped)
        {
            throw new NotImplementedException();
        }

        protected override void PlatformSetScissorRectangle(Rectangle rectangle)
        {
            throw new NotImplementedException();
        }

        protected override void PlatformSetShaderResourceBindingSlots(ShaderResourceBindingSlots shaderConstantBindings)
        {
            throw new NotImplementedException();
        }

        protected override void PlatformSetShaderSet(ShaderSet shaderSet)
        {
            throw new NotImplementedException();
        }

        protected override void PlatformSetTexture(int slot, ShaderTextureBinding textureBinding)
        {
            throw new NotImplementedException();
        }

        protected override void PlatformSetVertexBuffer(int slot, VertexBuffer vb)
        {
            throw new NotImplementedException();
        }

        protected override void PlatformSetViewport(int x, int y, int width, int height)
        {
            throw new NotImplementedException();
        }

        protected override void PlatformSwapBuffers()
        {
            throw new NotImplementedException();
        }
    }
}
