using Vulkan;
using static Veldrid.Graphics.Vulkan.VulkanUtil;
using static Vulkan.VulkanNative;

namespace Veldrid.Graphics.Vulkan
{
    public unsafe class VkSamplerState : SamplerState
    {
        private readonly VkDevice _device;

        public VkSamplerState(
            VkDevice device,
            SamplerAddressMode addressU,
            SamplerAddressMode addressV,
            SamplerAddressMode addressW,
            SamplerFilter filter,
            int maxAnisotropy,
            RgbaFloat borderColor,
            DepthComparison comparison,
            int minimumLod,
            int maximumLod,
            int lodBias)
        {
            _device = device;
            AddressU = addressU;
            AddressV = addressV;
            AddressW = addressW;
            Filter = filter;
            MaximumAnisotropy = maxAnisotropy;
            BorderColor = borderColor;
            Comparison = comparison;
            MinimumLod = minimumLod;
            MaximumLod = maximumLod;
            LodBias = lodBias;

            VkSamplerCreateInfo samplerCI = VkSamplerCreateInfo.New();
            samplerCI.addressModeU = VkFormats.VeldridToVkSamplerAddressMode(addressU);
            samplerCI.addressModeV = VkFormats.VeldridToVkSamplerAddressMode(addressV);
            samplerCI.addressModeW = VkFormats.VeldridToVkSamplerAddressMode(addressW);
            VkFormats.GetFilterProperties(
                filter,
                out VkFilter minFilter,
                out VkFilter magFilter,
                out VkSamplerMipmapMode mipmapMode,
                out bool anisotropyEnable,
                out bool compareEnable);
            samplerCI.minFilter = minFilter;
            samplerCI.magFilter = magFilter;
            samplerCI.mipmapMode = mipmapMode;
            samplerCI.maxAnisotropy = maxAnisotropy;
            samplerCI.anisotropyEnable = anisotropyEnable;
            samplerCI.compareEnable = compareEnable;
            samplerCI.minLod = minimumLod;
            samplerCI.maxLod = maximumLod;
            samplerCI.mipLodBias = lodBias;
            samplerCI.compareOp = VkFormats.VeldridToVkDepthComparison(comparison);
            samplerCI.borderColor = VkBorderColor.FloatOpaqueWhite;
            VkResult result = vkCreateSampler(_device, ref samplerCI, null, out VkSampler sampler);
            CheckResult(result);
            Sampler = sampler;
        }

        public VkSampler Sampler { get; private set; }

        public SamplerAddressMode AddressU { get; }

        public SamplerAddressMode AddressV { get; }

        public SamplerAddressMode AddressW { get; }

        public SamplerFilter Filter { get; }

        public int MaximumAnisotropy { get; }

        public RgbaFloat BorderColor { get; }

        public DepthComparison Comparison { get; }

        public int MinimumLod { get; }

        public int MaximumLod { get; }

        public int LodBias { get; }

        public void Dispose() { }
    }
}