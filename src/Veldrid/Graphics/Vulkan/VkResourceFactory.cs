using System;
using Vulkan;

namespace Veldrid.Graphics.Vulkan
{
    public class VkResourceFactory : ResourceFactory
    {
        private readonly VkDevice _device;
        private readonly VkPhysicalDevice _physicalDevice;

        public VkResourceFactory(VkDevice device, VkPhysicalDevice physicalDevice)
        {
            _device = device;
            _physicalDevice = physicalDevice;
        }

        public override ConstantBuffer CreateConstantBuffer(int sizeInBytes)
        {
            return new VkConstantBuffer(
                _device,
                _physicalDevice,
                (ulong)sizeInBytes,
                VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent);
        }

        public override CubemapTexture CreateCubemapTexture(IntPtr pixelsFront, IntPtr pixelsBack, IntPtr pixelsLeft, IntPtr pixelsRight, IntPtr pixelsTop, IntPtr pixelsBottom, int width, int height, int pixelSizeinBytes, PixelFormat format)
        {
            throw new NotImplementedException();
        }

        public override Framebuffer CreateFramebuffer()
        {
            return new VkFramebufferInfo(_device, _physicalDevice);
        }

        public override IndexBuffer CreateIndexBuffer(int sizeInBytes, bool isDynamic, IndexFormat format)
        {
            return new VkIndexBuffer(
                _device,
                _physicalDevice,
                (ulong)sizeInBytes,
                VkMemoryPropertyFlags.HostCoherent | VkMemoryPropertyFlags.HostVisible);
        }

        public override VertexInputLayout CreateInputLayout(params VertexInputDescription[] vertexInputs)
        {
            return new VKInputLayout(vertexInputs);
        }

        public override Shader CreateShader(ShaderType type, CompiledShaderCode compiledShaderCode)
        {
            return new VkShader(_device, type, (VkShaderBytecode)compiledShaderCode);
        }

        public override ShaderResourceBindingSlots CreateShaderConstantBindingSlots(ShaderSet shaderSet, params ShaderResourceDescription[] resources)
        {
            throw new NotImplementedException();
        }

        public override ShaderSet CreateShaderSet(VertexInputLayout inputLayout, Shader vertexShader, Shader fragmentShader)
        {
            return new VkShaderSet((VKInputLayout)inputLayout, (VkShader)vertexShader, null, (VkShader)fragmentShader);
        }

        public override ShaderSet CreateShaderSet(VertexInputLayout inputLayout, Shader vertexShader, Shader geometryShader, Shader fragmentShader)
        {
            return new VkShaderSet((VKInputLayout)inputLayout, (VkShader)vertexShader, (VkShader)geometryShader, (VkShader)fragmentShader);
        }

        public override ShaderTextureBinding CreateShaderTextureBinding(DeviceTexture texture)
        {
            return new VkShaderTextureBinding(_device, (VkTexture2D)texture);
        }

        public override DeviceTexture2D CreateTexture(
            int mipLevels,
            int width,
            int height,
            PixelFormat format,
            DeviceTextureCreateOptions createOptions)
        {
            return new VkTexture2D(_device, _physicalDevice, mipLevels, width, height, format, createOptions);
        }

        public override VertexBuffer CreateVertexBuffer(int sizeInBytes, bool isDynamic)
        {
            return new VkVertexBuffer(_device, _physicalDevice, (ulong)sizeInBytes, VkMemoryPropertyFlags.HostVisible | VkMemoryPropertyFlags.HostCoherent);
        }

        public override CompiledShaderCode LoadProcessedShader(byte[] data)
        {
            return new VkShaderBytecode(data);
        }

        public override CompiledShaderCode ProcessShaderCode(ShaderType type, string shaderCode)
        {
            return new VkShaderBytecode(type, shaderCode);
        }

        protected override BlendState CreateCustomBlendStateCore(bool isBlendEnabled, Blend srcAlpha, Blend destAlpha, BlendFunction alphaBlendFunc, Blend srcColor, Blend destColor, BlendFunction colorBlendFunc, RgbaFloat blendFactor)
        {
            throw new NotImplementedException();
        }

        protected override DepthStencilState CreateDepthStencilStateCore(bool isDepthEnabled, DepthComparison comparison, bool isDepthWriteEnabled)
        {
            throw new NotImplementedException();
        }

        protected override RasterizerState CreateRasterizerStateCore(FaceCullingMode cullMode, TriangleFillMode fillMode, bool isDepthClipEnabled, bool isScissorTestEnabled)
        {
            throw new NotImplementedException();
        }

        protected override SamplerState CreateSamplerStateCore(SamplerAddressMode addressU, SamplerAddressMode addressV, SamplerAddressMode addressW, SamplerFilter filter, int maxAnisotropy, RgbaFloat borderColor, DepthComparison comparison, int minimumLod, int maximumLod, int lodBias)
        {
            throw new NotImplementedException();
        }

        protected override string GetShaderFileExtension()
        {
            throw new NotImplementedException();
        }

        protected override GraphicsBackend PlatformGetGraphicsBackend()
        {
            throw new NotImplementedException();
        }
    }
}
