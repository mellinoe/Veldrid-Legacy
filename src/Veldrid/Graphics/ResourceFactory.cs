using System;
using System.Threading.Tasks;

namespace Veldrid.Graphics
{
    public abstract class ResourceFactory : DeviceTextureCreator
    {
        public abstract VertexBuffer CreateVertexBuffer(int sizeInBytes, bool isDynamic);
        public abstract Task<VertexBuffer> CreateVertexBufferAsync(int sizeInBytes, bool isDynamic);

        public abstract IndexBuffer CreateIndexBuffer(int sizeInBytes, bool isDynamic);
        public abstract Task<IndexBuffer> CreateIndexBufferAsync(int sizeInBytes, bool isDynamic);
        public abstract IndexBuffer CreateIndexBuffer(int sizeInBytes, bool isDynamic, IndexFormat format);
        public abstract Task<IndexBuffer> CreateIndexBufferAsync(int sizeInBytes, bool isDynamic, IndexFormat format);

        public abstract Material CreateMaterial(
            RenderContext rc,
            string vertexShaderName,
            string pixelShaderName,
            MaterialVertexInput vertexInputs,
            MaterialInputs<MaterialGlobalInputElement> globalInputs,
            MaterialInputs<MaterialPerObjectInputElement> perObjectInputs,
            MaterialTextureInputs textureInputs);
        public abstract Task<Material> CreateMaterialAsync(
            RenderContext rc,
            string vertexShaderName,
            string pixelShaderName,
            MaterialVertexInput vertexInputs,
            MaterialInputs<MaterialGlobalInputElement> globalInputs,
            MaterialInputs<MaterialPerObjectInputElement> perObjectInputs,
            MaterialTextureInputs textureInputs);

        public abstract ConstantBuffer CreateConstantBuffer(int sizeInBytes);
        public abstract Task<ConstantBuffer> CreateConstantBufferAsync(int sizeInBytes);

        public abstract Framebuffer CreateFramebuffer();

        public abstract Framebuffer CreateFramebuffer(int width, int height);

        public abstract DeviceTexture2D CreateTexture<T>(T[] pixelData, int width, int height, int pixelSizeInBytes, PixelFormat format) where T : struct;
        public abstract Task<DeviceTexture2D> CreateTextureAsync<T>(T[] pixelData, int width, int height, int pixelSizeInBytes, PixelFormat format) where T : struct;

        public abstract DeviceTexture2D CreateTexture(IntPtr pixelData, int width, int height, int pixelSizeInBytes, PixelFormat format);
        public abstract Task<DeviceTexture2D> CreateTextureAsync(IntPtr pixelData, int width, int height, int pixelSizeInBytes, PixelFormat format);
        public abstract ShaderTextureBinding CreateShaderTextureBinding(DeviceTexture texture);
        public abstract DeviceTexture2D CreateDepthTexture(int width, int height, int pixelSizeInBytes, PixelFormat format);
        public abstract CubemapTexture CreateCubemapTexture(
            IntPtr pixelsFront,
            IntPtr pixelsBack,
            IntPtr pixelsLeft,
            IntPtr pixelsRight,
            IntPtr pixelsTop,
            IntPtr pixelsBottom,
            int width,
            int height,
            int pixelSizeinBytes,
            PixelFormat format);

        public abstract BlendState CreateCustomBlendState(
            bool isBlendEnabled,
            Blend srcBlend, Blend destBlend, BlendFunction blendFunc);


        public abstract BlendState CreateCustomBlendState(
            bool isBlendEnabled,
            Blend srcAlpha, Blend destAlpha, BlendFunction alphaBlendFunc,
            Blend srcColor, Blend destColor, BlendFunction colorBlendFunc);

        public abstract DepthStencilState CreateDepthStencilState(bool isDepthEnabled, DepthComparison comparison);

        public abstract RasterizerState CreateRasterizerState(
            FaceCullingMode cullMode,
            TriangleFillMode fillMode,
            bool isDepthClipEnabled,
            bool isScissorTestEnabled);

        public abstract Task<T> RunAsync<T>(Func<T> func);
    }
}
