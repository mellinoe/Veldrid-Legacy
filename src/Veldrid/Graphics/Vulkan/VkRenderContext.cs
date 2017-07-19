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
        private VkPrimitiveTopology _primitiveTopology = VkPrimitiveTopology.TriangleList;

        private VkSwapchainInfo _scInfo;

        // Draw call tracking
        private List<RenderPassInfo> _renderPassStates = new List<RenderPassInfo>();
        private RenderPassInfo _currentRenderPassState;
        private bool _needsNewRenderPass = true;
        private bool _clearBuffer;
        private RgbaFloat _cachedClearColor;

        public VkRenderContext(IntPtr hinstance, IntPtr hwnd, int width, int height)
        {
            CreateInstance();
            CreateSurface(hinstance, hwnd);
            CreatePhysicalDevice();
            CreateLogicalDevice();
            ResourceFactory = new VkResourceFactory(_device, _physicalDevice);
            _scInfo = new VkSwapchainInfo();
            _scInfo.CreateSwapchain(_device, _physicalDevice, (VkResourceFactory)ResourceFactory, _surface, _graphicsQueueIndex, _presentQueueIndex, width, height);
            SetFramebuffer(_scInfo.GetFramebuffer(0));
            CreateCommandPool();
            CreateSemaphores();

            PostContextCreated();
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
            RenderPassInfo renderPassState = GetCurrentRenderPass();
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
            _cachedClearColor = ClearColor;
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
            _primitiveTopology = VkFormats.VeldridToVkPrimitiveTopology(primitiveTopology);
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
            VkFramebufferInfo fbInfo = _scInfo.GetFramebuffer(imageIndex);
            renderPassBeginInfo.framebuffer = fbInfo.Framebuffer;
            renderPassBeginInfo.renderPass = fbInfo.RenderPass;
            if (_clearBuffer)
            {
                _clearBuffer = false;
                VkClearColorValue colorClear = new VkClearColorValue
                {
                    float32_0 = _cachedClearColor.R,
                    float32_1 = _cachedClearColor.G,
                    float32_2 = _cachedClearColor.B,
                    float32_3 = _cachedClearColor.A
                };
                VkClearDepthStencilValue depthClear = new VkClearDepthStencilValue() { depth = 1f, stencil = 0 };
                FixedArray2<VkClearValue> clearValues = new FixedArray2<VkClearValue>();
                clearValues.First.color = colorClear;
                clearValues.Second.depthStencil = depthClear;

                renderPassBeginInfo.clearValueCount = 2;
                renderPassBeginInfo.pClearValues = &clearValues.First;
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

            _renderPassStates.Clear();
            _needsNewRenderPass = true;
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

        private RenderPassInfo GetCurrentRenderPass()
        {
            EnsureRenderPassCreated();
            return _currentRenderPassState;
        }

        private void EnsureRenderPassCreated()
        {
            if (_needsNewRenderPass)
            {
                _needsNewRenderPass = false;
                _currentRenderPassState = new RenderPassInfo() { Framebuffer = CurrentFramebuffer };
                _renderPassStates.Add(_currentRenderPassState);
            }
        }

        private VkPipeline GetCurrentGraphicsPipeline(out VkPipelineLayout layout)
        {
            VkGraphicsPipelineCreateInfo pipelineCI = VkGraphicsPipelineCreateInfo.New();

            // RenderPass
            pipelineCI.renderPass = GetCurrentRenderPass().Framebuffer.RenderPass;
            pipelineCI.subpass = 0;

            // Layout
            layout = GetCurrentPipelineLayout();
            pipelineCI.layout = layout;

            // DynamicState
            VkPipelineDynamicStateCreateInfo dynamicStateCI = VkPipelineDynamicStateCreateInfo.New();
            VkDynamicState* dynamicStates = stackalloc VkDynamicState[2];
            dynamicStates[0] = VkDynamicState.Viewport;
            dynamicStates[1] = VkDynamicState.Scissor;
            dynamicStateCI.dynamicStateCount = 2;
            dynamicStateCI.pDynamicStates = dynamicStates;
            pipelineCI.pDynamicState = &dynamicStateCI;

            // ColorBlendState
            VkPipelineColorBlendAttachmentState colorBlendAttachementState = new VkPipelineColorBlendAttachmentState();
            // TODO : Respect blend state options.
            colorBlendAttachementState.colorWriteMask = VkColorComponentFlags.R | VkColorComponentFlags.G | VkColorComponentFlags.B | VkColorComponentFlags.A;
            colorBlendAttachementState.blendEnable = false;
            VkPipelineColorBlendStateCreateInfo colorBlendStateCI = VkPipelineColorBlendStateCreateInfo.New();
            colorBlendStateCI.attachmentCount = 1;
            colorBlendStateCI.pAttachments = &colorBlendAttachementState;
            pipelineCI.pColorBlendState = &colorBlendStateCI;

            // DepthStencilState
            VkPipelineDepthStencilStateCreateInfo depthStencilStateCI = VkPipelineDepthStencilStateCreateInfo.New();
            depthStencilStateCI.depthCompareOp = VkFormats.VeldridToVkDepthComparison(DepthStencilState.DepthComparison);
            depthStencilStateCI.depthWriteEnable = DepthStencilState.IsDepthWriteEnabled;
            depthStencilStateCI.depthTestEnable = DepthStencilState.IsDepthEnabled;
            pipelineCI.pDepthStencilState = &depthStencilStateCI;

            // MultisampleState
            VkPipelineMultisampleStateCreateInfo multisampleStateCI = VkPipelineMultisampleStateCreateInfo.New();
            multisampleStateCI.rasterizationSamples = VkSampleCountFlags.Count1;
            pipelineCI.pMultisampleState = &multisampleStateCI;

            // RasterizationState
            VkPipelineRasterizationStateCreateInfo rasterizationStateCI = ((VkRasterizerState)RasterizerState).RasterizerStateCreateInfo;
            rasterizationStateCI.lineWidth = 1f;
            pipelineCI.pRasterizationState = &rasterizationStateCI;

            // ViewportState
            VkPipelineViewportStateCreateInfo viewportStateCI = VkPipelineViewportStateCreateInfo.New();
            viewportStateCI.viewportCount = 1;
            viewportStateCI.scissorCount = 1;
            pipelineCI.pViewportState = &viewportStateCI;

            // InputAssemblyState
            VkPipelineInputAssemblyStateCreateInfo inputAssemblyStateCI = VkPipelineInputAssemblyStateCreateInfo.New();
            inputAssemblyStateCI.topology = _primitiveTopology;
            pipelineCI.pInputAssemblyState = &inputAssemblyStateCI;

            // VertexInputState
            VkPipelineVertexInputStateCreateInfo vertexInputStateCI = VkPipelineVertexInputStateCreateInfo.New();
            VertexInputDescription[] inputDescriptions = ShaderSet.InputLayout.InputDescriptions;
            uint bindingCount = (uint)inputDescriptions.Length;
            uint attributeCount = (uint)inputDescriptions.Sum(desc => desc.Elements.Length);
            VkVertexInputBindingDescription* bindingDescs = stackalloc VkVertexInputBindingDescription[(int)bindingCount];
            VkVertexInputAttributeDescription* attributeDescs = stackalloc VkVertexInputAttributeDescription[(int)attributeCount];

            int targetIndex = 0;
            for (int binding = 0; binding < inputDescriptions.Length; binding++)
            {
                VertexInputDescription inputDesc = inputDescriptions[binding];
                bindingDescs[targetIndex] = new VkVertexInputBindingDescription()
                {
                    binding = (uint)binding,
                    inputRate = (inputDesc.Elements[0].StorageClassifier == VertexElementInputClass.PerInstance) ? VkVertexInputRate.Instance : VkVertexInputRate.Vertex,
                    stride = (uint)inputDesc.VertexSizeInBytes
                };

                for (int location = 0; location < inputDesc.Elements.Length; location++)
                {
                    VertexInputElement inputElement = inputDesc.Elements[location];

                    attributeDescs[targetIndex] = new VkVertexInputAttributeDescription()
                    {
                        format = VkFormats.VeldridToVkVertexElementFormat(inputElement.ElementFormat),
                        binding = (uint)binding,
                        location = (uint)location
                    };

                    targetIndex += 1;
                }
            }

            vertexInputStateCI.vertexBindingDescriptionCount = bindingCount;
            vertexInputStateCI.pVertexBindingDescriptions = bindingDescs;
            vertexInputStateCI.vertexAttributeDescriptionCount = attributeCount;
            vertexInputStateCI.pVertexAttributeDescriptions = attributeDescs;
            pipelineCI.pVertexInputState = &vertexInputStateCI;

            // ShaderStage
            VkPipelineShaderStageCreateInfo* shaderStageCIs = stackalloc VkPipelineShaderStageCreateInfo[2];
            VkPipelineShaderStageCreateInfo vertexStage = VkPipelineShaderStageCreateInfo.New();
            vertexStage.stage = VkShaderStageFlags.Vertex;
            vertexStage.module = ShaderSet.VertexShader.ShaderModule;
            vertexStage.pName = CommonStrings.main;
            VkPipelineShaderStageCreateInfo fragmentStage = VkPipelineShaderStageCreateInfo.New();
            fragmentStage.stage = VkShaderStageFlags.Fragment;
            fragmentStage.module = ShaderSet.FragmentShader.ShaderModule;
            fragmentStage.pName = CommonStrings.main;
            shaderStageCIs[0] = vertexStage;
            shaderStageCIs[1] = fragmentStage;
            pipelineCI.stageCount = 2;// TODO: NOT REALLY
            pipelineCI.pStages = shaderStageCIs;

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
            VkDescriptorSetAllocateInfo descriptorSetAI = VkDescriptorSetAllocateInfo.New();
            vkAllocateDescriptorSets(_device, ref descriptorSetAI, out VkDescriptorSet ret);
            return ret;
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
        private new VkShaderSet ShaderSet => (VkShaderSet)base.ShaderSet;
        private new VkFramebufferInfo CurrentFramebuffer => (VkFramebufferInfo)base.CurrentFramebuffer;
        private new VkShaderResourceBindingSlots ShaderResourceBindingSlots
            => (VkShaderResourceBindingSlots)base.ShaderResourceBindingSlots;

    }

    internal class RenderPassInfo
    {
        public VkFramebufferInfo Framebuffer { get; set; }
        public RawList<VkCommandBuffer> SecondaryCommandBuffers { get; set; } = new RawList<VkCommandBuffer>();
    }
}
