namespace Veldrid.Graphics.Vulkan
{
    public class VkSamplerState : SamplerState
    {
        public VkSamplerState(SamplerAddressMode addressU, SamplerAddressMode addressV, SamplerAddressMode addressW, SamplerFilter filter, int maxAnisotropy, RgbaFloat borderColor, DepthComparison comparison, int minimumLod, int maximumLod, int lodBias)
        {
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
        }

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