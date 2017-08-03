using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Veldrid.Graphics.OpenGL
{
    public class OpenGLResourceFactory : ResourceFactory
    {
        public OpenGLResourceFactory()
        {
        }

        protected override GraphicsBackend PlatformGetGraphicsBackend() => GraphicsBackend.OpenGL;

        public override ConstantBuffer CreateConstantBuffer(int sizeInBytes)
        {
            return new OpenGLConstantBuffer(sizeInBytes);
        }

        public override Framebuffer CreateFramebuffer()
        {
            return new OpenGLFramebuffer();
        }

        public override IndexBuffer CreateIndexBuffer(int sizeInBytes, bool isDynamic, IndexFormat format)
        {
            return new OpenGLIndexBuffer(isDynamic, OpenGLFormats.MapIndexFormat(format));
        }

        public override CompiledShaderCode ProcessShaderCode(ShaderStages type, string shaderCode)
        {
            return new OpenGLCompiledShaderCode(shaderCode);
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
                    throw new VeldridException("The byte array provided to LoadProcessedShader was not a valid shader string.");
                }
            }

            return new OpenGLCompiledShaderCode(shaderCode);
        }

        public override Shader CreateShader(ShaderStages type, CompiledShaderCode compiledShaderCode)
        {
            OpenGLCompiledShaderCode glShaderSource = (OpenGLCompiledShaderCode)compiledShaderCode;
            return new OpenGLShader(glShaderSource.ShaderCode, OpenGLFormats.VeldridToGLShaderType(type));
        }

        public override ShaderSet CreateShaderSet(VertexInputLayout inputLayout, Shader vertexShader, Shader fragmentShader)
        {
            return new OpenGLShaderSet((OpenGLVertexInputLayout)inputLayout, (OpenGLShader)vertexShader, null, null, null, (OpenGLShader)fragmentShader);
        }

        public override ShaderSet CreateShaderSet(VertexInputLayout inputLayout, Shader vertexShader, Shader geometryShader, Shader fragmentShader)
        {
            return new OpenGLShaderSet(
                (OpenGLVertexInputLayout)inputLayout,
                (OpenGLShader)vertexShader,
                null,
                null,
                (OpenGLShader)geometryShader,
                (OpenGLShader)fragmentShader);
        }

        public override ShaderSet CreateShaderSet(
            VertexInputLayout inputLayout,
            Shader vertexShader,
            Shader tessellationControlShader,
            Shader tessellationEvaluationShader,
            Shader geometryShader,
            Shader fragmentShader)
        {
            return new OpenGLShaderSet(
                (OpenGLVertexInputLayout)inputLayout,
                (OpenGLShader)vertexShader,
                (OpenGLShader)tessellationControlShader,
                (OpenGLShader)tessellationEvaluationShader,
                (OpenGLShader)geometryShader,
                (OpenGLShader)fragmentShader);
        }

        public override ShaderResourceBindingSlots CreateShaderResourceBindingSlots(
            ShaderSet shaderSet,
            ShaderResourceDescription[] resources)
        {
            return new OpenGLShaderResourceBindingSlots((OpenGLShaderSet)shaderSet, resources);
        }

        public override VertexInputLayout CreateInputLayout(VertexInputDescription[] vertexInputs)
        {
            return new OpenGLVertexInputLayout(vertexInputs);
        }

        public override ShaderTextureBinding CreateShaderTextureBinding(DeviceTexture texture)
        {
            if (texture is OpenGLTexture2D)
            {
                return new OpenGLTextureBinding((OpenGLTexture2D)texture);
            }
            else
            {
                return new OpenGLTextureBinding((OpenGLCubemapTexture)texture);
            }
        }

        public override DeviceTexture2D CreateTexture(
            int mipLevels,
            int width,
            int height,
            PixelFormat format,
            DeviceTextureCreateOptions createOptions)
        {
            OpenTK.Graphics.OpenGL.PixelFormat pixelFormat = OpenGLFormats.MapPixelFormat(format);
            PixelInternalFormat pixelInternalFormat = OpenGLFormats.MapPixelInternalFormat(format);

            if (createOptions == DeviceTextureCreateOptions.DepthStencil)
            {
                if (format != PixelFormat.R16_UInt)
                {
                    throw new NotImplementedException("R16_UInt is the only supported depth texture format.");
                }

                pixelFormat = OpenTK.Graphics.OpenGL.PixelFormat.DepthComponent;
                pixelInternalFormat = PixelInternalFormat.DepthComponent16;
            }

            return new OpenGLTexture2D(
                mipLevels,
                width,
                height,
                format,
                pixelInternalFormat,
                pixelFormat,
                OpenGLFormats.MapPixelType(format));
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
            return new OpenGLSamplerState(addressU, addressV, addressW, filter, maxAnisotropy, borderColor, comparison, minimumLod, maximumLod, lodBias);
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
            return new OpenGLCubemapTexture(
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
            return new OpenGLVertexBuffer(isDynamic);
        }

        protected override BlendState CreateCustomBlendStateCore(
            bool isBlendEnabled,
            Blend srcAlpha, Blend destAlpha, BlendFunction alphaBlendFunc,
            Blend srcColor, Blend destColor, BlendFunction colorBlendFunc,
            RgbaFloat blendFactor)
        {
            return new OpenGLBlendState(isBlendEnabled, srcAlpha, destAlpha, alphaBlendFunc, srcColor, destColor, colorBlendFunc, blendFactor);
        }

        protected override DepthStencilState CreateDepthStencilStateCore(bool isDepthEnabled, DepthComparison comparison, bool isDepthWriteEnabled)
        {
            return new OpenGLDepthStencilState(isDepthEnabled, comparison, isDepthWriteEnabled);
        }

        protected override RasterizerState CreateRasterizerStateCore(
            FaceCullingMode cullMode,
            TriangleFillMode fillMode,
            bool isDepthClipEnabled,
            bool isScissorTestEnabled)
        {
            return new OpenGLRasterizerState(cullMode, fillMode, isDepthClipEnabled, isScissorTestEnabled);
        }
    }
}
