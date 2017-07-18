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
        private VkCommandPool _commandPool;
        private VkSemaphore _imageAvailableSemaphore;
        private VkSemaphore _renderCompleteSemaphore;

        private VkSwapchainInfo _scInfo;

        // Draw call tracking
        private List<RenderPassState> _renderPassStates = new List<RenderPassState>();
        private RenderPassState _currentRenderPassState;
        private bool _needsNewRenderPass = true;
        private bool _clearBuffer;

        public VkRenderContext(IntPtr hinstance, IntPtr hwnd, int width, int height)
        {
            CreateInstance();
            CreateSurface(hinstance, hwnd);
            CreatePhysicalDevice();
            CreateLogicalDevice();
            ResourceFactory = new VkResourceFactory(_device, _physicalDevice);
            _scInfo = new VkSwapchainInfo();
            _scInfo.CreateSwapchain(_device, _physicalDevice, _surface, _graphicsQueueIndex, _presentQueueIndex, width, height);
            _scInfo.CreateImageViews(_device);
            CreateCommandPool();
            CreateSemaphores();
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

        private void CreateCommandPool()
        {
            VkCommandPoolCreateInfo commandPoolCI = VkCommandPoolCreateInfo.New();
            commandPoolCI.flags = VkCommandPoolCreateFlags.ResetCommandBuffer;
            commandPoolCI.queueFamilyIndex = _graphicsQueueIndex;
            vkCreateCommandPool(_device, ref commandPoolCI, null, out _commandPool);
        }

        private void CreateSemaphores()
        {
            VkSemaphoreCreateInfo semaphoreCI = VkSemaphoreCreateInfo.New();
            vkCreateSemaphore(_device, ref semaphoreCI, null, out _imageAvailableSemaphore);
            vkCreateSemaphore(_device, ref semaphoreCI, null, out _renderCompleteSemaphore);
        }

        public override ResourceFactory ResourceFactory { get; }

        public override RenderCapabilities RenderCapabilities => throw new NotImplementedException();

        public override void DrawIndexedPrimitives(int count, int startingIndex) => DrawIndexedPrimitives(count, startingIndex, 0);
        public override void DrawIndexedPrimitives(int count, int startingIndex, int startingVertex)
        {
            RenderPassState renderPassState = GetCurrentRenderPass();
            VkPipeline graphicsPipeline = GetCurrentGraphicsPipeline(out VkPipelineLayout layout);
            VkDescriptorSet descriptorSet = GetCurrentDescriptorSet();

            VkCommandBuffer cb = GetCommandBuffer();
            VkCommandBufferBeginInfo beginInfo = VkCommandBufferBeginInfo.New();
            beginInfo.flags = VkCommandBufferUsageFlags.OneTimeSubmit;
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
            VkBuffer vb = VertexBuffer.DeviceBuffer;
            vkCmdBindVertexBuffers(cb, 0, 1, ref vb, null);
            vkCmdBindIndexBuffer(cb, IndexBuffer.DeviceBuffer, 0, IndexBuffer.IndexType);
            vkCmdDrawIndexed(cb, (uint)count, 0, (uint)startingIndex, startingVertex, 0);
            vkEndCommandBuffer(cb);

            renderPassState.SecondaryCommandBuffers.Add(cb);
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
            _clearBuffer = true;
        }

        protected override void PlatformClearMaterialResourceBindings()
        {
        }

        protected override void PlatformDispose()
        {
        }

        protected override GraphicsBackend PlatformGetGraphicsBackend() => GraphicsBackend.Vulkan;

        protected override void PlatformResize(int width, int height)
        {
        }

        protected override void PlatformSetBlendstate(BlendState blendState)
        {
        }

        protected override void PlatformSetConstantBuffer(int slot, ConstantBuffer cb)
        {
        }

        protected override void PlatformSetDefaultFramebuffer()
        {
        }

        protected override void PlatformSetDepthStencilState(DepthStencilState depthStencilState)
        {
        }

        protected override void PlatformSetFramebuffer(Framebuffer framebuffer)
        {
            _needsNewRenderPass = true;
        }

        protected override void PlatformSetIndexBuffer(IndexBuffer ib)
        {
        }

        protected override void PlatformSetPrimitiveTopology(PrimitiveTopology primitiveTopology)
        {
        }

        protected override void PlatformSetRasterizerState(RasterizerState rasterizerState)
        {
        }

        protected override void PlatformSetSamplerState(int slot, SamplerState samplerState, bool mipmapped)
        {
        }

        protected override void PlatformSetScissorRectangle(Rectangle rectangle)
        {
        }

        protected override void PlatformSetShaderResourceBindingSlots(ShaderResourceBindingSlots shaderConstantBindings)
        {
        }

        protected override void PlatformSetShaderSet(ShaderSet shaderSet)
        {
        }

        protected override void PlatformSetTexture(int slot, ShaderTextureBinding textureBinding)
        {
        }

        protected override void PlatformSetVertexBuffer(int slot, VertexBuffer vb)
        {
        }

        protected override void PlatformSetViewport(int x, int y, int width, int height)
        {
        }

        protected override void PlatformSwapBuffers()
        {
            // Submit command buffers and present.
            uint imageIndex = _scInfo.AcquireNextImage(_device, _imageAvailableSemaphore);

            EnsureRenderPassCreated();
            if (_renderPassStates.Count > 1)
            {
                throw new NotImplementedException();
            }

            VkCommandBuffer primaryCommandBuffer = GetPrimaryCommandBuffer(imageIndex);
            VkCommandBufferBeginInfo beginInfo = VkCommandBufferBeginInfo.New();
            beginInfo.flags = VkCommandBufferUsageFlags.OneTimeSubmit;
            vkBeginCommandBuffer(primaryCommandBuffer, ref beginInfo);
            VkRenderPassBeginInfo renderPassBeginInfo = VkRenderPassBeginInfo.New();
            renderPassBeginInfo.framebuffer = _scInfo.GetFramebuffer(imageIndex);
            renderPassBeginInfo.renderPass = _renderPassStates[0].RenderPass;
            if (_clearBuffer)
            {
                _clearBuffer = false;
                renderPassBeginInfo.clearValueCount = 1;
                VkClearColorValue colorClear = new VkClearColorValue
                {
                    float32_0 = ClearColor.R,
                    float32_1 = ClearColor.G,
                    float32_2 = ClearColor.B,
                    float32_3 = ClearColor.A
                };
                VkClearDepthStencilValue depthClear = new VkClearDepthStencilValue() { depth = float.MaxValue, stencil = 0 };
                VkClearValue clearValue = new VkClearValue() { color = colorClear, depthStencil = depthClear };
                renderPassBeginInfo.pClearValues = &clearValue;
            }
            renderPassBeginInfo.renderArea.extent = _scInfo.SwapchainExtent;

            vkCmdBeginRenderPass(primaryCommandBuffer, ref renderPassBeginInfo, VkSubpassContents.SecondaryCommandBuffers);
            RawList<VkCommandBuffer> secondaryCBs = _renderPassStates[0].SecondaryCommandBuffers;
            if (secondaryCBs.Count > 0)
            {
                vkCmdExecuteCommands(primaryCommandBuffer, secondaryCBs.Count, ref secondaryCBs[0]);
            }
            vkCmdEndRenderPass(primaryCommandBuffer);
            vkEndCommandBuffer(primaryCommandBuffer);

            VkSubmitInfo submitInfo = VkSubmitInfo.New();
            VkSemaphore waitSemaphore = _imageAvailableSemaphore;
            VkPipelineStageFlags waitStages = VkPipelineStageFlags.ColorAttachmentOutput;
            submitInfo.waitSemaphoreCount = 1;
            submitInfo.pWaitSemaphores = &waitSemaphore;
            submitInfo.pWaitDstStageMask = &waitStages;
            VkCommandBuffer cb = primaryCommandBuffer;
            submitInfo.commandBufferCount = 1;
            submitInfo.pCommandBuffers = &cb;
            VkSemaphore signalSemaphore = _renderCompleteSemaphore;
            submitInfo.signalSemaphoreCount = 1;
            submitInfo.pSignalSemaphores = &signalSemaphore;
            vkQueueSubmit(_graphicsQueue, 1, ref submitInfo, VkFence.Null);

            VkPresentInfoKHR presentInfo = VkPresentInfoKHR.New();
            presentInfo.waitSemaphoreCount = 1;
            presentInfo.pWaitSemaphores = &signalSemaphore;

            VkSwapchainKHR swapchain = _scInfo.Swapchain;
            presentInfo.swapchainCount = 1;
            presentInfo.pSwapchains = &swapchain;
            presentInfo.pImageIndices = &imageIndex;

            vkQueuePresentKHR(_presentQueue, ref presentInfo);
        }

        private VkCommandBuffer GetPrimaryCommandBuffer(uint imageIndex)
        {
            VkCommandBufferAllocateInfo commandBufferAI = VkCommandBufferAllocateInfo.New();
            commandBufferAI.commandBufferCount = 1;
            commandBufferAI.commandPool = _commandPool;
            commandBufferAI.level = VkCommandBufferLevel.Primary;

            VkResult result = vkAllocateCommandBuffers(_device, ref commandBufferAI, out VkCommandBuffer ret);
            CheckResult(result);
            return ret;
        }

        private RenderPassState GetCurrentRenderPass()
        {
            EnsureRenderPassCreated();
            return _currentRenderPassState;
        }

        private void EnsureRenderPassCreated()
        {
            if (_needsNewRenderPass)
            {
                _needsNewRenderPass = false;
                VkRenderPassCreateInfo renderPassCI = VkRenderPassCreateInfo.New();

                VkAttachmentDescription colorAttachmentDesc = new VkAttachmentDescription();
                colorAttachmentDesc.format = _scInfo.SwapchainFormat;
                colorAttachmentDesc.samples = VkSampleCountFlags._1;
                colorAttachmentDesc.loadOp = VkAttachmentLoadOp.Clear;
                colorAttachmentDesc.storeOp = VkAttachmentStoreOp.Store;
                colorAttachmentDesc.stencilLoadOp = VkAttachmentLoadOp.DontCare;
                colorAttachmentDesc.stencilStoreOp = VkAttachmentStoreOp.DontCare;
                colorAttachmentDesc.initialLayout = VkImageLayout.Undefined;
                colorAttachmentDesc.finalLayout = VkImageLayout.PresentSrc;

                VkAttachmentReference colorAttachmentRef = new VkAttachmentReference();
                colorAttachmentRef.attachment = 0;
                colorAttachmentRef.layout = VkImageLayout.ColorAttachmentOptimal;

                VkAttachmentDescription depthAttachmentDesc = new VkAttachmentDescription();
                VkAttachmentReference depthAttachmentRef = new VkAttachmentReference();
                if (CurrentFramebuffer.DepthTexture != null)
                {
                    depthAttachmentDesc.format = CurrentFramebuffer.DepthTexture.Format;
                    depthAttachmentDesc.samples = VkSampleCountFlags._1;
                    depthAttachmentDesc.loadOp = VkAttachmentLoadOp.Clear;
                    depthAttachmentDesc.storeOp = VkAttachmentStoreOp.Store;
                    depthAttachmentDesc.stencilLoadOp = VkAttachmentLoadOp.DontCare;
                    depthAttachmentDesc.stencilStoreOp = VkAttachmentStoreOp.DontCare;
                    depthAttachmentDesc.initialLayout = VkImageLayout.Undefined;
                    depthAttachmentDesc.finalLayout = VkImageLayout.DepthStencilAttachmentOptimal;

                    depthAttachmentRef.attachment = 1;
                    depthAttachmentRef.layout = VkImageLayout.DepthStencilAttachmentOptimal;
                }

                VkSubpassDescription subpass = new VkSubpassDescription();
                subpass.pipelineBindPoint = VkPipelineBindPoint.Graphics;
                subpass.colorAttachmentCount = 1;
                subpass.pColorAttachments = &colorAttachmentRef;

                StackList<VkAttachmentDescription, Size2IntPtr> attachments = new StackList<VkAttachmentDescription, Size2IntPtr>();
                attachments.Add(colorAttachmentDesc);

                if (CurrentFramebuffer.DepthTexture != null)
                {
                    subpass.pDepthStencilAttachment = &depthAttachmentRef;
                    attachments.Add(depthAttachmentDesc);
                }

                VkSubpassDependency subpassDependency = new VkSubpassDependency();
                subpassDependency.srcSubpass = SubpassExternal;
                subpassDependency.srcStageMask = VkPipelineStageFlags.ColorAttachmentOutput;
                subpassDependency.dstStageMask = VkPipelineStageFlags.ColorAttachmentOutput;
                subpassDependency.dstAccessMask = VkAccessFlags.ColorAttachmentRead | VkAccessFlags.ColorAttachmentWrite;
                if (CurrentFramebuffer.DepthTexture != null)
                {
                    subpassDependency.dstAccessMask |= VkAccessFlags.DepthStencilAttachmentRead | VkAccessFlags.DepthStencilAttachmentWrite;
                }

                renderPassCI.attachmentCount = attachments.Count;
                renderPassCI.pAttachments = (VkAttachmentDescription*)attachments.Data;
                renderPassCI.subpassCount = 1;
                renderPassCI.pSubpasses = &subpass;
                renderPassCI.dependencyCount = 1;
                renderPassCI.pDependencies = &subpassDependency;


                vkCreateRenderPass(_device, ref renderPassCI, null, out VkRenderPass newRenderPass);
                _currentRenderPassState = new RenderPassState() { RenderPass = newRenderPass };
                _renderPassStates.Add(_currentRenderPassState);
            }
        }

        private VkPipeline GetCurrentGraphicsPipeline(out VkPipelineLayout layout)
        {
            VkGraphicsPipelineCreateInfo pipelineCI = VkGraphicsPipelineCreateInfo.New();
            pipelineCI.renderPass = GetCurrentRenderPass().RenderPass;
            pipelineCI.subpass = 0;
            layout = GetCurrentPipelineLayout();
            pipelineCI.layout = layout;

            VkPipelineDynamicStateCreateInfo dynamicStateCI = VkPipelineDynamicStateCreateInfo.New();
            VkDynamicState* dynamicStates = stackalloc VkDynamicState[2];
            dynamicStates[0] = VkDynamicState.Viewport;
            dynamicStates[1] = VkDynamicState.Scissor;
            dynamicStateCI.dynamicStateCount = 2;
            dynamicStateCI.pDynamicStates = dynamicStates;
            pipelineCI.pDynamicState = &dynamicStateCI;

            VkResult result = vkCreateGraphicsPipelines(_device, VkPipelineCache.Null, 1, ref pipelineCI, null, out VkPipeline ret);
            CheckResult(result);
            return ret;
        }

        private VkPipelineLayout GetCurrentPipelineLayout()
        {
            VkPipelineLayoutCreateInfo pipelineLayoutCI = VkPipelineLayoutCreateInfo.New();
            VkDescriptorSetLayout layout = ((VkShaderResourceBindingSlots)ShaderResourceBindingSlots).DescriptorSetLayout;
            pipelineLayoutCI.setLayoutCount = 1;
            pipelineLayoutCI.pSetLayouts = &layout;
            vkCreatePipelineLayout(_device, ref pipelineLayoutCI, null, out VkPipelineLayout ret);
            return ret;
        }

        private VkDescriptorSet GetCurrentDescriptorSet()
        {
            throw new NotImplementedException();
        }

        private VkCommandBuffer GetCommandBuffer()
        {
            VkCommandBufferAllocateInfo commandBufferAI = VkCommandBufferAllocateInfo.New();
            commandBufferAI.commandBufferCount = 1;
            commandBufferAI.commandPool = _commandPool;
            commandBufferAI.level = VkCommandBufferLevel.Secondary;

            vkAllocateCommandBuffers(_device, ref commandBufferAI, out VkCommandBuffer ret);
            return ret;
        }

        private new VkVertexBuffer VertexBuffer => (VkVertexBuffer)base.VertexBuffer;
        private new VkIndexBuffer IndexBuffer => (VkIndexBuffer)base.IndexBuffer;
        private new VkFramebufferInfo CurrentFramebuffer => (VkFramebufferInfo)base.CurrentFramebuffer;
        private new VkShaderResourceBindingSlots ShaderResourceBindingSlots
            => (VkShaderResourceBindingSlots)base.ShaderResourceBindingSlots;

        private class RenderPassState
        {
            public VkRenderPass RenderPass { get; set; }
            public RawList<VkCommandBuffer> SecondaryCommandBuffers { get; set; } = new RawList<VkCommandBuffer>();
        }
    }
}
