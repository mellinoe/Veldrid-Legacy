using System;
using Vulkan;
using static Vulkan.VulkanNative;
using static Veldrid.Graphics.Vulkan.VulkanUtil;

namespace Veldrid.Graphics.Vulkan
{
    public unsafe class VkShaderResourceBindingSlots : ShaderResourceBindingSlots
    {
        public VkPipelineLayout PipelineLayout { get; }

        public VkDescriptorSetLayout DescriptorSetLayout { get; }

        public ShaderResourceDescription[] Resources { get; }

        public VkShaderResourceBindingSlots(VkDevice device, ShaderResourceDescription[] resources)
        {
            Resources = resources;
            VkDescriptorSetLayoutCreateInfo descriptorSetLayoutCI = VkDescriptorSetLayoutCreateInfo.New();
            descriptorSetLayoutCI.bindingCount = (uint)resources.Length;

            VkDescriptorSetLayoutBinding* bindings = stackalloc VkDescriptorSetLayoutBinding[resources.Length];
            for (int i = 0; i < resources.Length; i++)
            {
                ShaderResourceDescription desc = resources[i];
                bindings[i].binding = (uint)i;
                bindings[i].descriptorType = MapDescriptorType(desc.Type);
                bindings[i].descriptorCount = 1;
                bindings[i].stageFlags = MapStageFlags(desc.Stages);
            }
            descriptorSetLayoutCI.pBindings = bindings;

            vkCreateDescriptorSetLayout(device, ref descriptorSetLayoutCI, null, out VkDescriptorSetLayout descriptorSetLayout);
            DescriptorSetLayout = descriptorSetLayout;

            VkPipelineLayoutCreateInfo pipelineLayoutCI = VkPipelineLayoutCreateInfo.New();
            pipelineLayoutCI.setLayoutCount = 1;
            pipelineLayoutCI.pSetLayouts = &descriptorSetLayout;
            VkResult result = vkCreatePipelineLayout(device, ref pipelineLayoutCI, null, out VkPipelineLayout layout);
            CheckResult(result);
            PipelineLayout = layout;
        }

        private VkDescriptorType MapDescriptorType(ShaderResourceType type)
        {
            switch (type)
            {
                case ShaderResourceType.ConstantBuffer:
                    return VkDescriptorType.UniformBuffer;
                case ShaderResourceType.Texture:
                    return VkDescriptorType.SampledImage;
                case ShaderResourceType.Sampler:
                    return VkDescriptorType.Sampler;
                default:
                    throw Illegal.Value<ShaderResourceType>();
            }
        }

        private VkShaderStageFlags MapStageFlags(ShaderStages stages)
        {
            VkShaderStageFlags flags = VkShaderStageFlags.None;
            if ((stages & ShaderStages.Vertex) == ShaderStages.Vertex)
            {
                flags |= VkShaderStageFlags.Vertex;
            }
            if ((stages & ShaderStages.Geometry) == ShaderStages.Geometry)
            {
                flags |= VkShaderStageFlags.Geometry;
            }
            if ((stages & ShaderStages.Fragment) == ShaderStages.Fragment)
            {
                flags |= VkShaderStageFlags.Fragment;
            }

            return flags;
        }
    }
}
