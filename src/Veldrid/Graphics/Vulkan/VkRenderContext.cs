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
        private VkCommandPool _perFrameCommandPool;
        private VkDescriptorPool _perFrameDescriptorPool;
        private VkSemaphore _imageAvailableSemaphore;
        private RawList<VkSemaphore> _renderPassSemaphores = new RawList<VkSemaphore>();
        private VkPrimitiveTopology _primitiveTopology = VkPrimitiveTopology.TriangleList;
        private VkSwapchainInfo _scInfo;
        private VkConstantBuffer[] _constantBuffers = new VkConstantBuffer[20]; // TODO: Real limit.
        private VkShaderTextureBinding[] _textureBindings = new VkShaderTextureBinding[20]; // TODO: Real limit.
        private VkSamplerState[] _samplerStates = new VkSamplerState[20]; // TODO: Real limit.
        private VkRect2D _scissorRect;
        private VkCommandPool _graphicsCommandPool;
        private VkResourceCache _resourceCache;
        private VkDeviceMemoryManager _memoryManager;
        private PFN_vkDebugReportCallbackEXT _debugCallbackFunc;
        private VkDebugReportCallbackEXT _debugCallbackHandle;

        // Draw call tracking
        private List<RenderPassInfo> _renderPassStates = new List<RenderPassInfo>();
        private RenderPassInfo _currentRenderPassState;
        private List<IDisposable> _frameDisposables = new List<IDisposable>();
        private List<VkPipeline> _framePipelines = new List<VkPipeline>();
        private List<VkPipelineLayout> _framePipelineLayouts = new List<VkPipelineLayout>();
        private bool _framebufferChanged;
        private List<VkCommandBuffer> _freeSecondaryCommandBuffers = new List<VkCommandBuffer>();

        public VkDevice Device => _device;
        public VkPhysicalDevice PhysicalDevice => _physicalDevice;
        public VkQueue GraphicsQueue => _graphicsQueue;
        public uint GraphicsQueueIndex => _graphicsQueueIndex;
        public VkCommandPool GraphicsCommandPool => _graphicsCommandPool;
        public VkDeviceMemoryManager MemoryManager => _memoryManager;

        public VkRenderContext(VkSurfaceSource surfaceInfo, int width, int height)
        {
            CreateInstance();
            CreateSurface(surfaceInfo);
            CreatePhysicalDevice();
            CreateLogicalDevice();
            _memoryManager = new VkDeviceMemoryManager(_device, _physicalDevice);
            ResourceFactory = new VkResourceFactory(this);
            _scInfo = new VkSwapchainInfo(_device, _physicalDevice, (VkResourceFactory)ResourceFactory, _surface, _graphicsQueueIndex, _presentQueueIndex, width, height);
            SetFramebuffer(_scInfo);
            CreatePerFrameCommandPool();
            CreatePerFrameDescriptorPool();
            CreateGraphicsCommandPool();
            CreateSemaphores();
            _resourceCache = new VkResourceCache(_device, (VkSamplerState)PointSampler);

            PostContextCreated();
        }

        public VkCommandBuffer BeginOneTimeCommands()
        {
            VkCommandBufferAllocateInfo allocInfo = VkCommandBufferAllocateInfo.New();
            allocInfo.commandBufferCount = 1;
            allocInfo.commandPool = GraphicsCommandPool;
            allocInfo.level = VkCommandBufferLevel.Primary;

            vkAllocateCommandBuffers(_device, ref allocInfo, out VkCommandBuffer cb);

            VkCommandBufferBeginInfo beginInfo = VkCommandBufferBeginInfo.New();
            beginInfo.flags = VkCommandBufferUsageFlags.OneTimeSubmit;

            vkBeginCommandBuffer(cb, ref beginInfo);

            return cb;
        }

        public void EndOneTimeCommands(VkCommandBuffer cb, VkFence fence)
        {
            vkEndCommandBuffer(cb);

            VkSubmitInfo submitInfo = VkSubmitInfo.New();
            submitInfo.commandBufferCount = 1;
            submitInfo.pCommandBuffers = &cb;

            vkQueueSubmit(GraphicsQueue, 1, ref submitInfo, fence);
            vkQueueWaitIdle(GraphicsQueue);

            vkFreeCommandBuffers(_device, GraphicsCommandPool, 1, ref cb);
        }

        private void CreateInstance()
        {
            HashSet<string> availableInstanceLayers = new HashSet<string>(EnumerateInstanceLayers());
            HashSet<string> availableInstanceExtensions = new HashSet<string>(EnumerateInstanceExtensions());

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

            if (!availableInstanceExtensions.Contains(CommonStrings.VK_KHR_SURFACE_EXTENSION_NAME))
            {
                throw new VeldridException($"The required instance extension was not available: {CommonStrings.VK_KHR_SURFACE_EXTENSION_NAME}");
            }

            instanceExtensions.Add(CommonStrings.VK_KHR_SURFACE_EXTENSION_NAME);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                if (!availableInstanceExtensions.Contains(CommonStrings.VK_KHR_WIN32_SURFACE_EXTENSION_NAME))
                {
                    throw new VeldridException($"The required instance extension was not available: {CommonStrings.VK_KHR_WIN32_SURFACE_EXTENSION_NAME}");
                }

                instanceExtensions.Add(CommonStrings.VK_KHR_WIN32_SURFACE_EXTENSION_NAME);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                if (!availableInstanceExtensions.Contains(CommonStrings.VK_KHR_XLIB_SURFACE_EXTENSION_NAME))
                {
                    throw new VeldridException($"The required instance extension was not available: {CommonStrings.VK_KHR_XLIB_SURFACE_EXTENSION_NAME}");
                }

                instanceExtensions.Add(CommonStrings.VK_KHR_XLIB_SURFACE_EXTENSION_NAME);
            }
            else
            {
                throw new NotSupportedException("This platform does not support Vulkan.");
            }

            bool debug = false;
#if DEBUG
            debug = true;
#endif
            bool debugReportExtensionAvailable = false;
            if (debug)
            {
                if (availableInstanceExtensions.Contains(CommonStrings.VK_EXT_DEBUG_REPORT_EXTENSION_NAME))
                {
                    debugReportExtensionAvailable = true;
                    instanceExtensions.Add(CommonStrings.VK_EXT_DEBUG_REPORT_EXTENSION_NAME);
                }
                if (availableInstanceLayers.Contains(CommonStrings.StandardValidationLayerName))
                {
                    instanceLayers.Add(CommonStrings.StandardValidationLayerName);
                }
            }

            instanceCI.enabledExtensionCount = instanceExtensions.Count;
            instanceCI.ppEnabledExtensionNames = (byte**)instanceExtensions.Data;

            instanceCI.enabledLayerCount = instanceLayers.Count;
            instanceCI.ppEnabledLayerNames = (byte**)instanceLayers.Data;

            VkResult result = vkCreateInstance(ref instanceCI, null, out _instance);
            CheckResult(result);

            if (debug && debugReportExtensionAvailable)
            {
                EnableDebugCallback(VkDebugReportFlagsEXT.Warning | VkDebugReportFlagsEXT.Error | VkDebugReportFlagsEXT.PerformanceWarning);
            }
        }

        public void EnableDebugCallback(VkDebugReportFlagsEXT flags = VkDebugReportFlagsEXT.Warning | VkDebugReportFlagsEXT.Error)
        {
            _debugCallbackFunc = DebugCallback;
            IntPtr debugFunctionPtr = Marshal.GetFunctionPointerForDelegate(_debugCallbackFunc);
            VkDebugReportCallbackCreateInfoEXT debugCallbackCI = VkDebugReportCallbackCreateInfoEXT.New();
            debugCallbackCI.flags = flags;
            debugCallbackCI.pfnCallback = debugFunctionPtr;
            FixedUtf8String debugExtFnName = "vkCreateDebugReportCallbackEXT";
            IntPtr createFnPtr = vkGetInstanceProcAddr(_instance, debugExtFnName);
            vkCreateDebugReportCallbackEXT_d createDelegate = Marshal.GetDelegateForFunctionPointer<vkCreateDebugReportCallbackEXT_d>(createFnPtr);
            createDelegate(_instance, &debugCallbackCI, IntPtr.Zero, out _debugCallbackHandle);
        }

        private uint DebugCallback(
            uint flags,
            VkDebugReportObjectTypeEXT objectType,
            ulong @object,
            UIntPtr location,
            int messageCode,
            byte* pLayerPrefix,
            byte* pMessage,
            void* pUserData)
        {
            Console.WriteLine($"[{(VkDebugReportFlagsEXT)flags}] ({objectType}) {Utilities.GetString(pMessage)}");
            return 0;
        }

        private void CreateSurface(VkSurfaceSource surfaceInfo)
        {
            _surface = surfaceInfo.CreateSurface(_instance);
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
            deviceFeatures.samplerAnisotropy = true;
            deviceFeatures.fillModeNonSolid = true;
            deviceFeatures.geometryShader = true;
            deviceFeatures.depthClamp = true;

            VkDeviceCreateInfo deviceCreateInfo = VkDeviceCreateInfo.New();

            fixed (VkDeviceQueueCreateInfo* qciPtr = &queueCreateInfos.Items[0])
            {
                deviceCreateInfo.pQueueCreateInfos = qciPtr;
                deviceCreateInfo.queueCreateInfoCount = queueCreateInfos.Count;

                deviceCreateInfo.pEnabledFeatures = &deviceFeatures;

                StackList<IntPtr> layerNames = new StackList<IntPtr>();
                layerNames.Add(CommonStrings.StandardValidationLayerName);
                deviceCreateInfo.enabledLayerCount = layerNames.Count;
                deviceCreateInfo.ppEnabledLayerNames = (byte**)layerNames.Data;

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

        private void CreatePerFrameCommandPool()
        {
            VkCommandPoolCreateInfo commandPoolCI = VkCommandPoolCreateInfo.New();
            commandPoolCI.flags = VkCommandPoolCreateFlags.ResetCommandBuffer | VkCommandPoolCreateFlags.Transient;
            commandPoolCI.queueFamilyIndex = _graphicsQueueIndex;
            vkCreateCommandPool(_device, ref commandPoolCI, null, out _perFrameCommandPool);
        }

        private void CreatePerFrameDescriptorPool()
        {
            VkDescriptorPoolSize* sizes = stackalloc VkDescriptorPoolSize[3];
            sizes[0].type = VkDescriptorType.UniformBuffer;
            sizes[0].descriptorCount = 5000;
            sizes[1].type = VkDescriptorType.SampledImage;
            sizes[1].descriptorCount = 5000;
            sizes[2].type = VkDescriptorType.Sampler;
            sizes[2].descriptorCount = 5000;

            VkDescriptorPoolCreateInfo descriptorPoolCI = VkDescriptorPoolCreateInfo.New();
            descriptorPoolCI.flags = VkDescriptorPoolCreateFlags.FreeDescriptorSet;
            descriptorPoolCI.maxSets = 5000;
            descriptorPoolCI.pPoolSizes = sizes;
            descriptorPoolCI.poolSizeCount = 3;

            VkResult result = vkCreateDescriptorPool(_device, ref descriptorPoolCI, null, out _perFrameDescriptorPool);
            CheckResult(result);
        }

        private void CreateGraphicsCommandPool()
        {
            VkCommandPoolCreateInfo commandPoolCI = VkCommandPoolCreateInfo.New();
            commandPoolCI.flags = VkCommandPoolCreateFlags.None;
            commandPoolCI.queueFamilyIndex = _graphicsQueueIndex;
            VkResult result = vkCreateCommandPool(_device, ref commandPoolCI, null, out _graphicsCommandPool);
            CheckResult(result);
        }

        private void CreateSemaphores()
        {
            VkSemaphoreCreateInfo semaphoreCI = VkSemaphoreCreateInfo.New();
            vkCreateSemaphore(_device, ref semaphoreCI, null, out _imageAvailableSemaphore);
            const int MaxRenderPasses = 10;
            _renderPassSemaphores.Resize(MaxRenderPasses);
            for (int i = 0; i < MaxRenderPasses; i++)
            {
                vkCreateSemaphore(_device, ref semaphoreCI, null, out _renderPassSemaphores[i]);
            }
        }

        public override ResourceFactory ResourceFactory { get; }

        public override RenderCapabilities RenderCapabilities => new RenderCapabilities(true, true);

        public override void DrawIndexedPrimitives(int count, int startingIndex)
            => DrawPrimitives(count, 1, startingIndex, 0);
        public override void DrawIndexedPrimitives(int count, int startingIndex, int startingVertex)
            => DrawPrimitives(count, 1, startingIndex, startingVertex);

        public override void DrawInstancedPrimitives(int indexCount, int instanceCount, int startingIndex)
            => DrawPrimitives(indexCount, instanceCount, startingIndex, 0);
        public override void DrawInstancedPrimitives(int indexCount, int instanceCount, int startingIndex, int startingVertex)
            => DrawPrimitives(indexCount, instanceCount, startingIndex, startingVertex);

        private void DrawPrimitives(int indexCount, int instanceCount, int startingIndex, int startingVertex)
        {
            RenderPassInfo renderPassState = GetCurrentRenderPass();
            VkPipelineLayout layout = ShaderResourceBindingSlots.PipelineLayout;
            VkPipelineCacheKey pipelineCacheKey = new VkPipelineCacheKey();
            pipelineCacheKey.RenderPass = renderPassState.Framebuffer.RenderPassClearBuffer;
            pipelineCacheKey.PipelineLayout = layout;
            pipelineCacheKey.BlendState = (VkBlendState)BlendState;
            pipelineCacheKey.Framebuffer = renderPassState.Framebuffer;
            pipelineCacheKey.DepthStencilState = (VkDepthStencilState)DepthStencilState;
            pipelineCacheKey.RasterizerState = (VkRasterizerState)RasterizerState;
            pipelineCacheKey.PrimitiveTopology = _primitiveTopology;
            pipelineCacheKey.ShaderSet = ShaderSet;
            VkPipeline graphicsPipeline = _resourceCache.GetGraphicsPipeline(ref pipelineCacheKey);

            VkDescriptorSetCacheKey descriptorSetCacheKey = new VkDescriptorSetCacheKey();
            descriptorSetCacheKey.ShaderResourceBindingSlots = ShaderResourceBindingSlots;
            descriptorSetCacheKey.ConstantBuffers = _constantBuffers;
            descriptorSetCacheKey.TextureBindings = _textureBindings;
            descriptorSetCacheKey.SamplerStates = _samplerStates;
            VkDescriptorSet descriptorSet = _resourceCache.GetDescriptorSet(ref descriptorSetCacheKey);

            VkCommandBuffer cb = GetCommandBuffer();
            VkCommandBufferBeginInfo beginInfo = VkCommandBufferBeginInfo.New();
            beginInfo.flags = VkCommandBufferUsageFlags.OneTimeSubmit | VkCommandBufferUsageFlags.RenderPassContinue;
            VkCommandBufferInheritanceInfo inheritanceInfo = VkCommandBufferInheritanceInfo.New();
            inheritanceInfo.renderPass = renderPassState.Framebuffer.RenderPassClearBuffer;
            beginInfo.pInheritanceInfo = &inheritanceInfo;
            vkBeginCommandBuffer(cb, ref beginInfo);

            vkCmdBindPipeline(cb, VkPipelineBindPoint.Graphics, graphicsPipeline);
            vkCmdBindDescriptorSets(
                cb,
                VkPipelineBindPoint.Graphics,
                layout,
                0,
                1,
                ref descriptorSet,
                0,
                IntPtr.Zero);

            int vbCount = ShaderSet.InputLayout.InputDescriptions.Length;
            StackList<VkBuffer, Size512Bytes> vbs = new StackList<VkBuffer, Size512Bytes>();
            for (int vbIndex = 0; vbIndex < vbCount; vbIndex++)
            {
                vbs.Add(((VkVertexBuffer)VertexBuffers[vbIndex]).DeviceBuffer);
            }

            StackList<VkBuffer, Size512Bytes> offsets = new StackList<VkBuffer, Size512Bytes>();
            vkCmdBindVertexBuffers(cb, 0, vbs.Count, (IntPtr)vbs.Data, (IntPtr)offsets.Data);
            vkCmdBindIndexBuffer(cb, IndexBuffer.DeviceBuffer, 0, IndexBuffer.IndexType);
            VkViewport viewport = new VkViewport()
            {
                x = Viewport.X,
                y = Viewport.Y,
                width = Viewport.Width,
                height = Viewport.Height,
                minDepth = 0,
                maxDepth = 1
            };
            vkCmdSetViewport(cb, 0, 1, ref viewport);
            vkCmdSetScissor(cb, 0, 1, ref _scissorRect);
            vkCmdDrawIndexed(cb, (uint)indexCount, (uint)instanceCount, (uint)startingIndex, startingVertex, 0);
            vkEndCommandBuffer(cb);

            renderPassState.SecondaryCommandBuffers.Add(cb);
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
            RenderPassInfo renderPassInfo = GetCurrentRenderPass();
            renderPassInfo.ClearBuffer = true;
            renderPassInfo.ClearColor = ClearColor;
        }

        protected override void PlatformClearMaterialResourceBindings()
        {
        }

        protected override void PlatformDispose()
        {
            _scInfo.Dispose();
            if (_debugCallbackFunc != null)
            {
                _debugCallbackFunc = null;
                FixedUtf8String debugExtFnName = "vkDestroyDebugReportCallbackEXT";
                IntPtr destroyFuncPtr = vkGetInstanceProcAddr(_instance, debugExtFnName);
                vkDestroyDebugReportCallbackEXT_d destroyDel
                    = Marshal.GetDelegateForFunctionPointer<vkDestroyDebugReportCallbackEXT_d>(destroyFuncPtr);
                destroyDel.Invoke(_instance, _debugCallbackHandle, null);
            }
            vkDestroyInstance(_instance, null);
        }

        protected override GraphicsBackend PlatformGetGraphicsBackend() => GraphicsBackend.Vulkan;

        protected override void PlatformResize(int width, int height)
        {
            vkDeviceWaitIdle(_device);
            _scInfo.Resize(width, height);
        }

        protected override void PlatformSetBlendstate(BlendState blendState)
        {
        }

        protected override void PlatformSetConstantBuffer(int slot, ConstantBuffer cb)
        {
            _constantBuffers[slot] = (VkConstantBuffer)cb;
        }

        protected override void PlatformSetDefaultFramebuffer()
        {
            SetFramebuffer(_scInfo);
        }

        protected override void PlatformSetDepthStencilState(DepthStencilState depthStencilState)
        {
        }

        protected override void PlatformSetFramebuffer(Framebuffer framebuffer)
        {
            _framebufferChanged = true;
        }

        protected override void PlatformSetIndexBuffer(IndexBuffer ib)
        {
        }

        protected override void PlatformSetPrimitiveTopology(PrimitiveTopology primitiveTopology)
        {
            _primitiveTopology = VkFormats.VeldridToVkPrimitiveTopology(primitiveTopology);
        }

        protected override void PlatformSetRasterizerState(RasterizerState rasterizerState)
        {
        }

        protected override void PlatformSetSamplerState(int slot, SamplerState samplerState)
        {
            _samplerStates[slot] = (VkSamplerState)samplerState;
        }

        protected override void PlatformSetScissorRectangle(Rectangle rectangle)
        {
            _scissorRect = new VkRect2D(rectangle.X, rectangle.Y, rectangle.Width, rectangle.Height);
        }

        protected override void PlatformSetShaderResourceBindingSlots(ShaderResourceBindingSlots shaderConstantBindings)
        {
        }

        protected override void PlatformSetShaderSet(ShaderSet shaderSet)
        {
        }

        protected override void PlatformSetTexture(int slot, ShaderTextureBinding textureBinding)
        {
            _textureBindings[slot] = (VkShaderTextureBinding)textureBinding;
        }

        protected override void PlatformSetVertexBuffer(int slot, VertexBuffer vb)
        {
        }

        protected override void PlatformSetViewport(int x, int y, int width, int height)
        {
        }

        protected override void PlatformSwapBuffers()
        {
            // First, ensure all pending draws are submitted to the proper render passes, and are completed.
            FlushFrameDrawCommands();

            // Then, present the swapchain.
            VkPresentInfoKHR presentInfo = VkPresentInfoKHR.New();
            presentInfo.waitSemaphoreCount = 1;
            // Wait on the last render pass to complete before presenting.
            VkSemaphore signalSemaphore = _renderPassSemaphores[_renderPassStates.Count - 1];
            presentInfo.pWaitSemaphores = &signalSemaphore;

            VkSwapchainKHR swapchain = _scInfo.Swapchain;
            presentInfo.swapchainCount = 1;
            presentInfo.pSwapchains = &swapchain;
            uint imageIndex = _scInfo.ImageIndex;
            presentInfo.pImageIndices = &imageIndex;

            vkQueuePresentKHR(_presentQueue, ref presentInfo);

            ClearFrameObjects();
            _framebufferChanged = true;
        }

        private void FlushFrameDrawCommands()
        {
            _scInfo.AcquireNextImage(_device, _imageAvailableSemaphore);
            for (int i = 0; i < _renderPassStates.Count; i++)
            {
                RenderPassInfo renderPassState = _renderPassStates[i];

                VkCommandBuffer primaryCommandBuffer = GetPrimaryCommandBuffer();
                renderPassState.PrimaryCommandBuffer = primaryCommandBuffer;
                VkCommandBufferBeginInfo beginInfo = VkCommandBufferBeginInfo.New();
                beginInfo.flags = VkCommandBufferUsageFlags.OneTimeSubmit;
                vkBeginCommandBuffer(primaryCommandBuffer, ref beginInfo);
                VkRenderPassBeginInfo renderPassBeginInfo = VkRenderPassBeginInfo.New();
                VkFramebufferBase fbInfo = renderPassState.Framebuffer;
                renderPassBeginInfo.framebuffer = fbInfo.VkFramebuffer;
                renderPassBeginInfo.renderPass = renderPassState.ClearBuffer
                    ? fbInfo.RenderPassClearBuffer
                    : fbInfo.RenderPassNoClear;

                if (renderPassState.ClearBuffer)
                {
                    VkClearColorValue colorClear = new VkClearColorValue
                    {
                        float32_0 = renderPassState.ClearColor.R,
                        float32_1 = renderPassState.ClearColor.G,
                        float32_2 = renderPassState.ClearColor.B,
                        float32_3 = renderPassState.ClearColor.A
                    };
                    VkClearDepthStencilValue depthClear = new VkClearDepthStencilValue() { depth = 1f, stencil = 0 };
                    StackList<VkClearValue, Size512Bytes> clearValues = new StackList<VkClearValue, Size512Bytes>();
                    if (fbInfo.ColorTexture != null)
                    {
                        clearValues.Add(new VkClearValue() { color = colorClear });
                    }
                    if (fbInfo.DepthTexture != null)
                    {
                        clearValues.Add(new VkClearValue() { depthStencil = depthClear });
                    }

                    renderPassBeginInfo.clearValueCount = clearValues.Count;
                    renderPassBeginInfo.pClearValues = (VkClearValue*)clearValues.Data;
                }
                renderPassBeginInfo.renderArea.extent = new VkExtent2D(fbInfo.Width, fbInfo.Height);

                vkCmdBeginRenderPass(primaryCommandBuffer, ref renderPassBeginInfo, VkSubpassContents.SecondaryCommandBuffers);
                RawList<VkCommandBuffer> secondaryCBs = renderPassState.SecondaryCommandBuffers;
                if (secondaryCBs.Count > 0)
                {
                    vkCmdExecuteCommands(primaryCommandBuffer, secondaryCBs.Count, ref secondaryCBs[0]);
                }
                vkCmdEndRenderPass(primaryCommandBuffer);
                vkEndCommandBuffer(primaryCommandBuffer);

                VkSubmitInfo submitInfo = VkSubmitInfo.New();
                VkSemaphore waitSemaphore = (i == 0) ? _imageAvailableSemaphore : _renderPassSemaphores[i - 1];
                VkPipelineStageFlags waitStages = VkPipelineStageFlags.ColorAttachmentOutput;
                submitInfo.waitSemaphoreCount = 1;
                submitInfo.pWaitSemaphores = &waitSemaphore;
                submitInfo.pWaitDstStageMask = &waitStages;
                VkCommandBuffer cb = primaryCommandBuffer;
                submitInfo.commandBufferCount = 1;
                submitInfo.pCommandBuffers = &cb;
                VkSemaphore signalSemaphore = _renderPassSemaphores[i];
                submitInfo.signalSemaphoreCount = 1;
                submitInfo.pSignalSemaphores = &signalSemaphore;
                vkQueueSubmit(_graphicsQueue, 1, ref submitInfo, VkFence.Null);
            }
        }

        private void ClearFrameObjects()
        {
            vkQueueWaitIdle(_graphicsQueue);
            foreach (RenderPassInfo rps in _renderPassStates)
            {
                vkFreeCommandBuffers(_device, _perFrameCommandPool, 1, ref rps.PrimaryCommandBuffer);
                if (rps.SecondaryCommandBuffers.Count > 0)
                {
                    _freeSecondaryCommandBuffers.AddRange(rps.SecondaryCommandBuffers);
                }
            }
            _renderPassStates.Clear();

            vkResetCommandPool(_device, _perFrameCommandPool, VkCommandPoolResetFlags.None);
            vkResetDescriptorPool(_device, _perFrameDescriptorPool, 0);

            foreach (VkPipeline pipeline in _framePipelines)
            {
                vkDestroyPipeline(_device, pipeline, null);
            }
            _framePipelines.Clear();

            foreach (VkPipelineLayout layout in _framePipelineLayouts)
            {
                vkDestroyPipelineLayout(_device, layout, null);
            }
            _framePipelineLayouts.Clear();
        }

        private VkCommandBuffer GetPrimaryCommandBuffer()
        {
            VkCommandBufferAllocateInfo commandBufferAI = VkCommandBufferAllocateInfo.New();
            commandBufferAI.commandBufferCount = 1;
            commandBufferAI.commandPool = _perFrameCommandPool;
            commandBufferAI.level = VkCommandBufferLevel.Primary;

            VkResult result = vkAllocateCommandBuffers(_device, ref commandBufferAI, out VkCommandBuffer ret);
            CheckResult(result);
            return ret;
        }

        private RenderPassInfo GetCurrentRenderPass()
        {
            if (_framebufferChanged)
            {
                _framebufferChanged = false;
                CreateNextRenderPass();
            }

            return _currentRenderPassState;
        }

        private void CreateNextRenderPass()
        {
            _currentRenderPassState = new RenderPassInfo() { Framebuffer = CurrentFramebuffer };
            _renderPassStates.Add(_currentRenderPassState);
        }

        private VkCommandBuffer GetCommandBuffer()
        {
            if (_freeSecondaryCommandBuffers.Count > 0)
            {
                int lastIndex = _freeSecondaryCommandBuffers.Count - 1;
                VkCommandBuffer freeCB = _freeSecondaryCommandBuffers[lastIndex];
                _freeSecondaryCommandBuffers.RemoveAt(lastIndex);
                return freeCB;

            }
            VkCommandBufferAllocateInfo commandBufferAI = VkCommandBufferAllocateInfo.New();
            commandBufferAI.commandBufferCount = 1;
            commandBufferAI.commandPool = _perFrameCommandPool;
            commandBufferAI.level = VkCommandBufferLevel.Secondary;

            VkResult result = vkAllocateCommandBuffers(_device, ref commandBufferAI, out VkCommandBuffer ret);
            CheckResult(result);

            return ret;
        }

        private new VkVertexBuffer VertexBuffer => (VkVertexBuffer)base.VertexBuffer;
        private new VkIndexBuffer IndexBuffer => (VkIndexBuffer)base.IndexBuffer;
        private new VkShaderSet ShaderSet => (VkShaderSet)base.ShaderSet;
        private new VkFramebufferBase CurrentFramebuffer => (VkFramebufferBase)base.CurrentFramebuffer;
        private new VkShaderResourceBindingSlots ShaderResourceBindingSlots
            => (VkShaderResourceBindingSlots)base.ShaderResourceBindingSlots;

    }

    internal class RenderPassInfo
    {
        public VkFramebufferBase Framebuffer;
        public VkCommandBuffer PrimaryCommandBuffer;
        public RawList<VkCommandBuffer> SecondaryCommandBuffers { get; set; } = new RawList<VkCommandBuffer>();
        public bool ClearBuffer;
        public RgbaFloat ClearColor;
    }

    internal unsafe delegate VkResult vkCreateDebugReportCallbackEXT_d(
        VkInstance instance,
        VkDebugReportCallbackCreateInfoEXT* createInfo,
        IntPtr allocatorPtr,
        out VkDebugReportCallbackEXT ret);

    internal unsafe delegate VkResult vkDestroyDebugReportCallbackEXT_d(VkInstance instance, VkDebugReportCallbackEXT callback, VkAllocationCallbacks* pAllocator);
}
