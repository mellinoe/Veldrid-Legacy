namespace Veldrid.Graphics.Vulkan
{
    public class VkDepthStencilState : DepthStencilState
    {
        public VkDepthStencilState(bool isDepthEnabled, DepthComparison comparison, bool isDepthWriteEnabled)
        {
            IsDepthEnabled = isDepthEnabled;
            IsDepthWriteEnabled = isDepthWriteEnabled;
            DepthComparison = comparison;
        }

        public bool IsDepthEnabled { get; }

        public bool IsDepthWriteEnabled { get; }

        public DepthComparison DepthComparison { get; }

        public void Dispose() { }
    }
}