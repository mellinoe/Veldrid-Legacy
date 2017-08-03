using System;
using System.Text;

namespace Veldrid.Graphics.OpenGLES
{
    public class OpenGLESResourceFactory : ResourceFactory
    {
        public OpenGLESResourceFactory()
        {
        }

        protected override GraphicsBackend PlatformGetGraphicsBackend() => GraphicsBackend.OpenGLES;

        public override ConstantBuffer CreateConstantBuffer(int sizeInBytes)
        {
            return new OpenGLESConstantBuffer();
        }

        public override Framebuffer CreateFramebuffer()
        {
            return new OpenGLESFramebuffer();
        }

        public override IndexBuffer CreateIndexBuffer(int sizeInBytes, bool isDynamic, IndexFormat format)
        {
            return new OpenGLESIndexBuffer(isDynamic, OpenGLESFormats.MapIndexFormat(format));
        }

        public override CompiledShaderCode ProcessShaderCode(ShaderStages type, string shaderCode)
        {
            return new OpenGLESCompiledShaderCode(shaderCode);
        }

        public override CompiledShaderCode LoadProcessedShader(byte[] bytes)
        {
            string shaderCode;
            try
            {
                shaderCode = Encoding.UTF8.GetString(bytes);
            }
            catch
            {
                try
                {
                    shaderCode = Encoding.ASCII.GetString(bytes);
                }
                catch
                {
                    throw new VeldridException("Byte array provided to LoadProcessedShader was not a valid shader string.");
                }
            }

            return new OpenGLESCompiledShaderCode(shaderCode);
        }

        public override Shader CreateShader(ShaderStages type, CompiledShaderCode compiledShaderCode)
        {
            OpenGLESCompiledShaderCode glShaderSource = (OpenGLESCompiledShaderCode)compiledShaderCode;
            return new OpenGLESShader(glShaderSource.ShaderCode, OpenGLESFormats.VeldridToGLShaderType(type));
        }

        public override ShaderSet CreateShaderSet(VertexInputLayout inputLayout, Shader vertexShader, Shader fragmentShader)
        {
            return new OpenGLESShaderSet((OpenGLESVertexInputLayout)inputLayout, (OpenGLESShader)vertexShader, (OpenGLESShader)fragmentShader);
        }

        public override ShaderSet CreateShaderSet(VertexInputLayout inputLayout, Shader vertexShader, Shader geometryShader, Shader fragmentShader)
        {
            throw new NotSupportedException();
        }

        public override ShaderSet CreateShaderSet(VertexInputLayout inputLayout, Shader vertexShader, Shader tessellationControlShader, Shader tessellationEvaluationShader, Shader geometryShader, Shader fragmentShader)
        {
            throw new NotSupportedException();
        }

        public override ShaderResourceBindingSlots CreateShaderResourceBindingSlots(
            ShaderSet shaderSet,
            ShaderResourceDescription[] resources)
        {
            return new OpenGLESShaderResourceBindingSlots((OpenGLESShaderSet)shaderSet, resources);
        }

        public override VertexInputLayout CreateInputLayout(VertexInputDescription[] vertexInputs)
        {
            return new OpenGLESVertexInputLayout(vertexInputs);
        }

        public override ShaderTextureBinding CreateShaderTextureBinding(DeviceTexture texture)
        {
            if (texture is OpenGLESTexture2D)
            {
                return new OpenGLESTextureBinding((OpenGLESTexture2D)texture);
            }
            else
            {
                return new OpenGLESTextureBinding((OpenGLESCubemapTexture)texture);
            }
        }

        public override DeviceTexture2D CreateTexture(
            int mipLevels,
            int width,
            int height,
            PixelFormat format,
            DeviceTextureCreateOptions createOptions)
        {
            int pixelSizeInBytes = FormatHelpers.GetPixelSizeInBytes(format);
            OpenTK.Graphics.ES30.PixelFormat pixelFormat = OpenGLESFormats.MapPixelFormat(format);

            if (createOptions == DeviceTextureCreateOptions.DepthStencil)
            {
                if (format != PixelFormat.R16_UInt)
                {
                    throw new NotImplementedException("R16_UInt is the only supported depth texture format.");
                }

                pixelFormat = OpenTK.Graphics.ES30.PixelFormat.DepthComponent;
            }

            return new OpenGLESTexture2D(mipLevels, width, height, format, pixelFormat, OpenGLESFormats.MapPixelType(format));
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
            return new OpenGLESSamplerState(addressU, addressV, addressW, filter, maxAnisotropy, borderColor, comparison, minimumLod, maximumLod, lodBias);
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
            return new OpenGLESCubemapTexture(
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

        public override VertexBuffer CreateVertexBuffer(int sizeInBytes, bool isDynamic)
        {
            return new OpenGLESVertexBuffer(isDynamic);
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
            return new OpenGLESBlendState(isBlendEnabled, srcAlpha, destAlpha, alphaBlendFunc, srcColor, destColor, colorBlendFunc, blendFactor);
        }

        protected override DepthStencilState CreateDepthStencilStateCore(bool isDepthEnabled, DepthComparison comparison, bool isDepthWriteEnabled)
        {
            return new OpenGLESDepthStencilState(isDepthEnabled, comparison, isDepthWriteEnabled);
        }

        protected override RasterizerState CreateRasterizerStateCore(
            FaceCullingMode cullMode,
            TriangleFillMode fillMode,
            bool isDepthClipEnabled,
            bool isScissorTestEnabled)
        {
            return new OpenGLESRasterizerState(cullMode, fillMode, isDepthClipEnabled, isScissorTestEnabled);
        }
    }
}
