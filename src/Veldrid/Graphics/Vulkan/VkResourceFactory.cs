using System;
using Vulkan;

namespace Veldrid.Graphics.Vulkan
{
    public class VkResourceFactory : ResourceFactory
    {
        private readonly VkDevice _device;
        private readonly VkPhysicalDevice _physicalDevice;

        public VkCommandPool CommandPool { get; }

        public VkRenderContext RenderContext { get; }

        public VkResourceFactory(VkRenderContext rc)
        {
            RenderContext = rc;
            _device = rc.Device;
            _physicalDevice = rc.PhysicalDevice;

            VkCommandPoolCreateInfo commandPoolCI = VkCommandPoolCreateInfo.New();
            commandPoolCI.flags = VkCommandPoolCreateFlags.None;
            commandPoolCI.queueFamilyIndex = rc.GraphicsQueueIndex;
        }

        protected override GraphicsBackend PlatformGetGraphicsBackend() => GraphicsBackend.Vulkan;

        public override ConstantBuffer CreateConstantBuffer(int sizeInBytes)
        {
            return new VkConstantBuffer(
                RenderContext,
                (ulong)sizeInBytes,
                VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent,
                true);
        }

        public override CubemapTexture CreateCubemapTexture(
            IntPtr pixelsFront,
            IntPtr pixelsBack,
            IntPtr pixelsLeft,
            IntPtr pixelsRight,
            IntPtr pixelsTop,
            IntPtr pixelsBottom,
            int width,
            int height,
            int pixelSizeinBytes,
            PixelFormat format)
        {
            return new VkCubemapTexture(
                _device,
                _physicalDevice,
                RenderContext.MemoryManager,
                RenderContext,
                pixelsFront,
                pixelsBack,
                pixelsLeft,
                pixelsRight,
                pixelsTop,
                pixelsBottom,
                width,
                height,
                format);
        }

        public override Framebuffer CreateFramebuffer()
        {
            return new VkRegularFramebuffer(_device, _physicalDevice);
        }

        public override IndexBuffer CreateIndexBuffer(int sizeInBytes, bool isDynamic, IndexFormat format)
        {
            return new VkIndexBuffer(
                RenderContext,
                (ulong)sizeInBytes,
                VkMemoryPropertyFlags.HostCoherent | VkMemoryPropertyFlags.HostVisible,
                isDynamic);
        }

        public override VertexInputLayout CreateInputLayout(params VertexInputDescription[] vertexInputs)
        {
            return new VKInputLayout(vertexInputs);
        }

        public override Shader CreateShader(ShaderStages type, CompiledShaderCode compiledShaderCode)
        {
            return new VkShader(_device, type, (VkShaderBytecode)compiledShaderCode);
        }

        public override ShaderResourceBindingSlots CreateShaderResourceBindingSlots(ShaderSet shaderSet, params ShaderResourceDescription[] resources)
        {
            return new VkShaderResourceBindingSlots(_device, resources);
        }

        public override ShaderSet CreateShaderSet(VertexInputLayout inputLayout, Shader vertexShader, Shader fragmentShader)
        {
            return new VkShaderSet((VKInputLayout)inputLayout, (VkShader)vertexShader, null, null, null, (VkShader)fragmentShader);
        }

        public override ShaderSet CreateShaderSet(VertexInputLayout inputLayout, Shader vertexShader, Shader geometryShader, Shader fragmentShader)
        {
            return new VkShaderSet((VKInputLayout)inputLayout, (VkShader)vertexShader, null, null, (VkShader)geometryShader, (VkShader)fragmentShader);
        }

        public override ShaderSet CreateShaderSet(
            VertexInputLayout inputLayout,
            Shader vertexShader,
            Shader tessellationControlShader,
            Shader tessellationEvaluationShader,
            Shader geometryShader,
            Shader fragmentShader)
        {
            return new VkShaderSet(
                (VKInputLayout)inputLayout,
                (VkShader)vertexShader,
                (VkShader)tessellationControlShader,
                (VkShader)tessellationEvaluationShader,
                (VkShader)geometryShader,
                (VkShader)fragmentShader);
        }

        public override ShaderTextureBinding CreateShaderTextureBinding(DeviceTexture texture)
        {
            return new VkShaderTextureBinding(_device, (VkDeviceTexture)texture);
        }

        public override DeviceTexture2D CreateTexture(
            int mipLevels,
            int width,
            int height,
            PixelFormat format,
            DeviceTextureCreateOptions createOptions)
        {
            return new VkTexture2D(
                _device,
                _physicalDevice,
                RenderContext.MemoryManager,
                RenderContext,
                mipLevels,
                width,
                height,
                format,
                createOptions);
        }

        public override VertexBuffer CreateVertexBuffer(int sizeInBytes, bool isDynamic)
        {
            return new VkVertexBuffer(
                RenderContext,
                (ulong)sizeInBytes,
                VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent,
                isDynamic);
        }

        public override CompiledShaderCode LoadProcessedShader(byte[] data)
        {
            return new VkShaderBytecode(data);
        }

        public override CompiledShaderCode ProcessShaderCode(ShaderStages type, string shaderCode)
        {
            return new VkShaderBytecode(type, shaderCode);
        }

        protected override BlendState CreateCustomBlendStateCore(
            bool isBlendEnabled,
            Blend srcAlpha,
            Blend destAlpha,
            BlendFunction alphaBlendFunc,
            Blend srcColor,
            Blend destColor,
            BlendFunction colorBlendFunc,
            RgbaFloat blendFactor)
        {
            return new VkBlendState(isBlendEnabled, srcAlpha, destAlpha, alphaBlendFunc, srcColor, destColor, colorBlendFunc, blendFactor);
        }

        protected override DepthStencilState CreateDepthStencilStateCore(bool isDepthEnabled, DepthComparison comparison, bool isDepthWriteEnabled)
        {
            return new VkDepthStencilState(isDepthEnabled, comparison, isDepthWriteEnabled);
        }

        protected override RasterizerState CreateRasterizerStateCore(FaceCullingMode cullMode, TriangleFillMode fillMode, bool isDepthClipEnabled, bool isScissorTestEnabled)
        {
            return new VkRasterizerState(cullMode, fillMode, isDepthClipEnabled, isScissorTestEnabled);
        }

        protected override SamplerState CreateSamplerStateCore(
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
            return new VkSamplerState(
                _device,
                addressU,
                addressV,
                addressW,
                filter,
                maxAnisotropy,
                borderColor,
                comparison,
                minimumLod,
                maximumLod,
                lodBias);
        }
    }
}
