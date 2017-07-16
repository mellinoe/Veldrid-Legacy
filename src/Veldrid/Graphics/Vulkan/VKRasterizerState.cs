using Vulkan;

namespace Veldrid.Graphics.Vulkan
{
    public class VkRasterizerState : RasterizerState
    {
        public FaceCullingMode CullMode { get; }

        public TriangleFillMode FillMode { get; }

        public bool IsDepthClipEnabled { get; }

        public bool IsScissorTestEnabled { get; }

        public VkPipelineRasterizationStateCreateInfo RasterizerStateCreateInfo { get; }

        public VkRasterizerState(FaceCullingMode cullMode, TriangleFillMode fillMode, bool isDepthClipEnabled, bool isScissorTestEnabled)
        {
            CullMode = cullMode;
            FillMode = fillMode;
            IsDepthClipEnabled = isDepthClipEnabled;
            IsScissorTestEnabled = isScissorTestEnabled;

            VkPipelineRasterizationStateCreateInfo rasterizerStateCI = VkPipelineRasterizationStateCreateInfo.New();
            rasterizerStateCI.cullMode = VkFormats.VeldridToVkCullMode(cullMode);
            rasterizerStateCI.polygonMode = VkFormats.VeldridToVkFillMode(fillMode);
            rasterizerStateCI.depthClampEnable = !isDepthClipEnabled; // TODO: Same as OpenGL (?)
            rasterizerStateCI.frontFace = VkFrontFace.Clockwise;

            RasterizerStateCreateInfo = rasterizerStateCI;
        }

        public void Dispose()
        {
        }
    }
}
