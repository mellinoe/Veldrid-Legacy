using Vulkan;
using static Vulkan.VulkanNative;
using static Veldrid.Graphics.Vulkan.VulkanUtil;
using System.Collections.Generic;
using System.Linq;
using System;

namespace Veldrid.Graphics.Vulkan
{
    /// <summary>
    /// Caches immutable resourcesa and manages recycling.
    /// </summary>
    internal unsafe class VkResourceCache
    {
        private readonly VkDevice _device;
        private readonly VkSamplerState _defaultSamplerState;
        private readonly Dictionary<VkPipelineCacheKey, VkPipeline> _pipelines = new Dictionary<VkPipelineCacheKey, VkPipeline>();
        private readonly Dictionary<VkDescriptorSetCacheKey, VkDescriptorSet> _descriptorSets = new Dictionary<VkDescriptorSetCacheKey, VkDescriptorSet>();
        private readonly VkDescriptorPool _descriptorPool;

        public VkResourceCache(VkDevice device, VkSamplerState defaultSamplerState)
        {
            _device = device;
            _defaultSamplerState = defaultSamplerState;

            VkDescriptorPoolSize* sizes = stackalloc VkDescriptorPoolSize[3];
            sizes[0].type = VkDescriptorType.UniformBuffer;
            sizes[0].descriptorCount = 50000;
            sizes[1].type = VkDescriptorType.SampledImage;
            sizes[1].descriptorCount = 15000;
            sizes[2].type = VkDescriptorType.Sampler;
            sizes[2].descriptorCount = 15000;

            VkDescriptorPoolCreateInfo descriptorPoolCI = VkDescriptorPoolCreateInfo.New();
            descriptorPoolCI.flags = VkDescriptorPoolCreateFlags.FreeDescriptorSet;
            descriptorPoolCI.maxSets = 15000;
            descriptorPoolCI.pPoolSizes = sizes;
            descriptorPoolCI.poolSizeCount = 3;

            VkResult result = vkCreateDescriptorPool(_device, ref descriptorPoolCI, null, out _descriptorPool);
            CheckResult(result);
        }

        public VkPipeline GetGraphicsPipeline(ref VkPipelineCacheKey cacheKey)
        {
            if (!_pipelines.TryGetValue(cacheKey, out VkPipeline ret))
            {
                ret = CreateNewGraphicsPipeline(ref cacheKey);
                _pipelines.Add(cacheKey, ret);
            }

            return ret;
        }

        public VkDescriptorSet GetDescriptorSet(ref VkDescriptorSetCacheKey cacheKey)
        {
            if (!_descriptorSets.TryGetValue(cacheKey, out VkDescriptorSet ret))
            {
                ret = CreateNewDescriptorSet(ref cacheKey);

                // Very efficient
                cacheKey.ConstantBuffers = (VkConstantBuffer[])cacheKey.ConstantBuffers.Clone();
                cacheKey.TextureBindings = (VkShaderTextureBinding[])cacheKey.TextureBindings.Clone();
                cacheKey.SamplerStates = (VkSamplerState[])cacheKey.SamplerStates.Clone();
                _descriptorSets.Add(cacheKey, ret);
            }

            return ret;
        }

        private VkPipeline CreateNewGraphicsPipeline(ref VkPipelineCacheKey cacheKey)
        {
            VkGraphicsPipelineCreateInfo pipelineCI = VkGraphicsPipelineCreateInfo.New();

            // RenderPass
            pipelineCI.renderPass = cacheKey.RenderPass;
            pipelineCI.subpass = 0;

            pipelineCI.layout = cacheKey.PipelineLayout;

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
            colorBlendAttachementState.colorWriteMask = VkColorComponentFlags.R | VkColorComponentFlags.G | VkColorComponentFlags.B | VkColorComponentFlags.A;
            colorBlendAttachementState.blendEnable = cacheKey.BlendState.IsBlendEnabled;
            colorBlendAttachementState.srcColorBlendFactor = VkFormats.VeldridToVkBlendFactor(cacheKey.BlendState.SourceColorBlend);
            colorBlendAttachementState.dstColorBlendFactor = VkFormats.VeldridToVkBlendFactor(cacheKey.BlendState.DestinationColorBlend);
            colorBlendAttachementState.colorBlendOp = VkFormats.VeldridToVkBlendOp(cacheKey.BlendState.ColorBlendFunction);
            colorBlendAttachementState.srcAlphaBlendFactor = VkFormats.VeldridToVkBlendFactor(cacheKey.BlendState.SourceAlphaBlend);
            colorBlendAttachementState.dstAlphaBlendFactor = VkFormats.VeldridToVkBlendFactor(cacheKey.BlendState.DestinationAlphaBlend);
            colorBlendAttachementState.alphaBlendOp = VkFormats.VeldridToVkBlendOp(cacheKey.BlendState.AlphaBlendFunction);

            VkPipelineColorBlendStateCreateInfo colorBlendStateCI = VkPipelineColorBlendStateCreateInfo.New();
            if (cacheKey.Framebuffer.ColorTexture != null)
            {
                colorBlendStateCI.attachmentCount = 1;
                colorBlendStateCI.pAttachments = &colorBlendAttachementState;
                colorBlendStateCI.blendConstants_0 = cacheKey.BlendState.BlendFactor.R;
                colorBlendStateCI.blendConstants_1 = cacheKey.BlendState.BlendFactor.G;
                colorBlendStateCI.blendConstants_2 = cacheKey.BlendState.BlendFactor.B;
                colorBlendStateCI.blendConstants_3 = cacheKey.BlendState.BlendFactor.A;
                pipelineCI.pColorBlendState = &colorBlendStateCI;
            }

            // DepthStencilState
            VkPipelineDepthStencilStateCreateInfo depthStencilStateCI = VkPipelineDepthStencilStateCreateInfo.New();
            depthStencilStateCI.depthCompareOp = VkFormats.VeldridToVkDepthComparison(cacheKey.DepthStencilState.DepthComparison);
            depthStencilStateCI.depthWriteEnable = cacheKey.DepthStencilState.IsDepthWriteEnabled;
            depthStencilStateCI.depthTestEnable = cacheKey.DepthStencilState.IsDepthEnabled;
            pipelineCI.pDepthStencilState = &depthStencilStateCI;

            // MultisampleState
            VkPipelineMultisampleStateCreateInfo multisampleStateCI = VkPipelineMultisampleStateCreateInfo.New();
            multisampleStateCI.rasterizationSamples = VkSampleCountFlags.Count1;
            pipelineCI.pMultisampleState = &multisampleStateCI;

            // RasterizationState
            VkPipelineRasterizationStateCreateInfo rasterizationStateCI = ((VkRasterizerState)cacheKey.RasterizerState).RasterizerStateCreateInfo;
            rasterizationStateCI.lineWidth = 1f;
            pipelineCI.pRasterizationState = &rasterizationStateCI;

            // ViewportState
            VkPipelineViewportStateCreateInfo viewportStateCI = VkPipelineViewportStateCreateInfo.New();
            viewportStateCI.viewportCount = 1;
            viewportStateCI.scissorCount = 1;
            pipelineCI.pViewportState = &viewportStateCI;

            // InputAssemblyState
            VkPipelineInputAssemblyStateCreateInfo inputAssemblyStateCI = VkPipelineInputAssemblyStateCreateInfo.New();
            inputAssemblyStateCI.topology = cacheKey.PrimitiveTopology;
            pipelineCI.pInputAssemblyState = &inputAssemblyStateCI;

            // VertexInputState
            VkPipelineVertexInputStateCreateInfo vertexInputStateCI = VkPipelineVertexInputStateCreateInfo.New();
            VertexInputDescription[] inputDescriptions = cacheKey.ShaderSet.InputLayout.InputDescriptions;
            uint bindingCount = (uint)inputDescriptions.Length;
            uint attributeCount = (uint)inputDescriptions.Sum(desc => desc.Elements.Length);
            VkVertexInputBindingDescription* bindingDescs = stackalloc VkVertexInputBindingDescription[(int)bindingCount];
            VkVertexInputAttributeDescription* attributeDescs = stackalloc VkVertexInputAttributeDescription[(int)attributeCount];

            int targetIndex = 0;
            int targetLocation = 0;
            for (int binding = 0; binding < inputDescriptions.Length; binding++)
            {
                VertexInputDescription inputDesc = inputDescriptions[binding];
                bindingDescs[targetIndex] = new VkVertexInputBindingDescription()
                {
                    binding = (uint)binding,
                    inputRate = (inputDesc.Elements[0].StorageClassifier == VertexElementInputClass.PerInstance) ? VkVertexInputRate.Instance : VkVertexInputRate.Vertex,
                    stride = (uint)inputDesc.VertexSizeInBytes
                };

                uint currentOffset = 0;
                for (int location = 0; location < inputDesc.Elements.Length; location++)
                {
                    VertexInputElement inputElement = inputDesc.Elements[location];

                    attributeDescs[targetIndex] = new VkVertexInputAttributeDescription()
                    {
                        format = VkFormats.VeldridToVkVertexElementFormat(inputElement.ElementFormat),
                        binding = (uint)binding,
                        location = (uint)(targetLocation + location),
                        offset = currentOffset
                    };

                    targetIndex += 1;
                    currentOffset += inputElement.SizeInBytes;
                }

                targetLocation += inputDesc.Elements.Length;
            }

            vertexInputStateCI.vertexBindingDescriptionCount = bindingCount;
            vertexInputStateCI.pVertexBindingDescriptions = bindingDescs;
            vertexInputStateCI.vertexAttributeDescriptionCount = attributeCount;
            vertexInputStateCI.pVertexAttributeDescriptions = attributeDescs;
            pipelineCI.pVertexInputState = &vertexInputStateCI;

            // ShaderStage
            StackList<VkPipelineShaderStageCreateInfo> shaderStageCIs = new StackList<VkPipelineShaderStageCreateInfo>();

            VkPipelineShaderStageCreateInfo vertexStage = VkPipelineShaderStageCreateInfo.New();
            vertexStage.stage = VkShaderStageFlags.Vertex;
            vertexStage.module = cacheKey.ShaderSet.VertexShader.ShaderModule;
            vertexStage.pName = CommonStrings.main;
            shaderStageCIs.Add(vertexStage);

            VkPipelineShaderStageCreateInfo fragmentStage = VkPipelineShaderStageCreateInfo.New();
            fragmentStage.stage = VkShaderStageFlags.Fragment;
            fragmentStage.module = cacheKey.ShaderSet.FragmentShader.ShaderModule;
            fragmentStage.pName = CommonStrings.main;
            shaderStageCIs.Add(fragmentStage);

            if (cacheKey.ShaderSet.TessellationControlShader != null)
            {
                VkPipelineShaderStageCreateInfo tcStage = VkPipelineShaderStageCreateInfo.New();
                tcStage.stage = VkShaderStageFlags.TessellationControl;
                tcStage.module = cacheKey.ShaderSet.TessellationControlShader.ShaderModule;
                tcStage.pName = CommonStrings.main;
                shaderStageCIs.Add(tcStage);
            }

            if (cacheKey.ShaderSet.TessellationEvaluationShader != null)
            {
                VkPipelineShaderStageCreateInfo teStage = VkPipelineShaderStageCreateInfo.New();
                teStage.stage = VkShaderStageFlags.TessellationEvaluation;
                teStage.module = cacheKey.ShaderSet.TessellationEvaluationShader.ShaderModule;
                teStage.pName = CommonStrings.main;
                shaderStageCIs.Add(teStage);
            }

            if (cacheKey.ShaderSet.GeometryShader != null)
            {
                VkPipelineShaderStageCreateInfo geometryStage = VkPipelineShaderStageCreateInfo.New();
                geometryStage.stage = VkShaderStageFlags.Geometry;
                geometryStage.module = cacheKey.ShaderSet.GeometryShader.ShaderModule;
                geometryStage.pName = CommonStrings.main;
                shaderStageCIs.Add(geometryStage);
            }

            pipelineCI.stageCount = shaderStageCIs.Count;
            pipelineCI.pStages = (VkPipelineShaderStageCreateInfo*)shaderStageCIs.Data;

            VkResult result = vkCreateGraphicsPipelines(_device, VkPipelineCache.Null, 1, ref pipelineCI, null, out VkPipeline ret);
            CheckResult(result);
            return ret;
        }

        private VkDescriptorSet CreateNewDescriptorSet(ref VkDescriptorSetCacheKey cacheKey)
        {
            {
                VkDescriptorSetAllocateInfo descriptorSetAI = VkDescriptorSetAllocateInfo.New();
                descriptorSetAI.descriptorPool = _descriptorPool;
                descriptorSetAI.descriptorSetCount = 1;
                VkDescriptorSetLayout layout = cacheKey.ShaderResourceBindingSlots.DescriptorSetLayout;
                descriptorSetAI.pSetLayouts = &layout;
                VkResult result = vkAllocateDescriptorSets(_device, ref descriptorSetAI, out VkDescriptorSet descriptorSet);
                CheckResult(result);

                int resourceCount = cacheKey.ShaderResourceBindingSlots.Resources.Length;
                VkWriteDescriptorSet[] descriptorWrites = new VkWriteDescriptorSet[resourceCount];
                VkDescriptorBufferInfo* bufferInfos = stackalloc VkDescriptorBufferInfo[resourceCount]; // TODO: Fix this.
                VkDescriptorImageInfo* imageInfos = stackalloc VkDescriptorImageInfo[resourceCount]; // TODO: Fix this.

                for (uint binding = 0; binding < resourceCount; binding++)
                {
                    descriptorWrites[binding].sType = VkStructureType.WriteDescriptorSet;
                    descriptorWrites[binding].descriptorCount = 1;
                    descriptorWrites[binding].dstBinding = binding;
                    descriptorWrites[binding].dstSet = descriptorSet;

                    ShaderResourceDescription resource = cacheKey.ShaderResourceBindingSlots.Resources[binding];
                    switch (resource.Type)
                    {
                        case ShaderResourceType.ConstantBuffer:
                            {
                                descriptorWrites[binding].descriptorType = VkDescriptorType.UniformBuffer;
                                VkConstantBuffer cb = cacheKey.ConstantBuffers[binding];
                                if (cb == null)
                                {
                                    throw new VeldridException($"No constant buffer bound to required binding slot {binding}.");
                                }
                                VkDescriptorBufferInfo* cbInfo = &bufferInfos[binding];
                                cbInfo->buffer = cb.DeviceBuffer;
                                cbInfo->offset = 0;
                                cbInfo->range = (ulong)resource.DataSizeInBytes;
                                descriptorWrites[binding].pBufferInfo = cbInfo;
                                break;
                            }
                        case ShaderResourceType.Texture:
                            {
                                descriptorWrites[binding].descriptorType = VkDescriptorType.SampledImage;
                                VkShaderTextureBinding textureBinding = cacheKey.TextureBindings[binding];
                                if (textureBinding == null)
                                {
                                    throw new VeldridException($"No texture bound to required binding slot {binding}.");
                                }
                                VkDescriptorImageInfo* imageInfo = &imageInfos[binding];
                                imageInfo->imageLayout = textureBinding.ImageLayout;
                                imageInfo->imageView = textureBinding.ImageView;
                                descriptorWrites[binding].pImageInfo = imageInfo;
                            }
                            break;
                        case ShaderResourceType.Sampler:
                            {
                                descriptorWrites[binding].descriptorType = VkDescriptorType.Sampler;
                                VkSamplerState samplerState = cacheKey.SamplerStates[binding] ?? (VkSamplerState)_defaultSamplerState;
                                VkDescriptorImageInfo* imageInfo = &imageInfos[binding];
                                imageInfo->sampler = samplerState.Sampler;
                                descriptorWrites[binding].pImageInfo = imageInfo;
                            }
                            break;
                        default:
                            throw Illegal.Value<ShaderResourceType>();
                    }
                }

                vkUpdateDescriptorSets(_device, (uint)resourceCount, ref descriptorWrites[0], 0, null);

                return descriptorSet;
            }
        }
    }

    internal struct VkPipelineCacheKey : IEquatable<VkPipelineCacheKey>
    {
        public VkRenderPass RenderPass;
        public VkPipelineLayout PipelineLayout;
        public VkBlendState BlendState;
        public VkFramebufferBase Framebuffer;
        public VkDepthStencilState DepthStencilState;
        public VkRasterizerState RasterizerState;
        public VkPrimitiveTopology PrimitiveTopology;
        public VkShaderSet ShaderSet;

        public bool Equals(VkPipelineCacheKey other)
        {
            return RenderPass.Equals(other.RenderPass) && PipelineLayout.Equals(other.PipelineLayout)
                && BlendState.Equals(other.BlendState) && Framebuffer.Equals(other.Framebuffer)
                && DepthStencilState.Equals(other.DepthStencilState) && RasterizerState.Equals(other.RasterizerState)
                && PrimitiveTopology == other.PrimitiveTopology && ShaderSet.Equals(other.ShaderSet);
        }

        public override int GetHashCode()
        {
            return HashHelper.Combine(
                RenderPass.GetHashCode(), PipelineLayout.GetHashCode(), BlendState.GetHashCode(),
                Framebuffer.GetHashCode(), DepthStencilState.GetHashCode(), RasterizerState.GetHashCode(),
                (int)PrimitiveTopology, ShaderSet.GetHashCode());
        }
    }

    internal struct VkDescriptorSetCacheKey : IEquatable<VkDescriptorSetCacheKey>
    {
        public VkShaderResourceBindingSlots ShaderResourceBindingSlots;
        public VkConstantBuffer[] ConstantBuffers;
        public VkShaderTextureBinding[] TextureBindings;
        public VkSamplerState[] SamplerStates;

        public bool Equals(VkDescriptorSetCacheKey other)
        {
            return ShaderResourceBindingSlots.Equals(other.ShaderResourceBindingSlots)
                && ArrayEquals(ConstantBuffers, other.ConstantBuffers)
                && ArrayEquals(TextureBindings, other.TextureBindings)
                && ArrayEquals(SamplerStates, other.SamplerStates);
        }

        private static bool ArrayEquals<T>(T[] left, T[] right)
        {
            if (left.Length != right.Length)
            {
                return false;
            }

            for (int i = 0; i < left.Length; i++)
            {
                if (!ReferenceEquals(left[i], right[i]))
                {
                    return false;
                }
            }

            return true;
        }

        public override int GetHashCode()
        {
            return HashHelper.Combine(
                ShaderResourceBindingSlots.GetHashCode(),
                HashHelper.Array(ConstantBuffers),
                HashHelper.Array(TextureBindings),
                HashHelper.Array(SamplerStates));
        }

        public override bool Equals(object obj) => obj is VkDescriptorSetCacheKey other && Equals(other);
    }
}
