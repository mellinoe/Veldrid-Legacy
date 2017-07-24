using System;
using Vulkan;

namespace Veldrid.Graphics.Vulkan
{
    public static class VkFormats
    {
        public static VkFormat VeldridToVkPixelFormat(PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.R32_G32_B32_A32_Float:
                    return VkFormat.R32g32b32a32Sfloat;
                case PixelFormat.R8_UInt:
                    return VkFormat.R8Uint;
                case PixelFormat.R16_UInt:
                    return VkFormat.R16Uint;
                case PixelFormat.R8_G8_B8_A8_UInt:
                    return VkFormat.R8g8b8a8Unorm;
                case PixelFormat.B8_G8_R8_A8_UInt:
                    return VkFormat.B8g8r8a8Unorm;
                default:
                    throw Illegal.Value<PixelFormat>();
            }
        }

        public static PixelFormat VkToVeldridPixelFormat(VkFormat vkFormat)
        {
            switch (vkFormat)
            {
                case VkFormat.R32g32b32a32Sfloat:
                    return PixelFormat.R32_G32_B32_A32_Float;
                case VkFormat.R8Uint:
                    return PixelFormat.R8_UInt;
                case VkFormat.R16Uint:
                    return PixelFormat.R16_UInt;
                case VkFormat.R8g8b8a8Uint:
                    return PixelFormat.R8_G8_B8_A8_UInt;
                case VkFormat.B8g8r8a8Unorm:
                    return PixelFormat.B8_G8_R8_A8_UInt;
                default:
                    throw Illegal.Value<VkFormat>();
            }
        }

        public static void GetFilterProperties(
            SamplerFilter filter,
            out VkFilter minFilter,
            out VkFilter magFilter,
            out VkSamplerMipmapMode mipMode,
            out bool anisotropyEnable,
            out bool compareEnable)
        {
            anisotropyEnable = false;
            compareEnable = false;

            switch (filter)
            {
                case SamplerFilter.MinMagMipPoint:
                    minFilter = VkFilter.Nearest;
                    magFilter = VkFilter.Nearest;
                    mipMode = VkSamplerMipmapMode.Nearest;
                    break;
                case SamplerFilter.MinMagPointMipLinear:
                    minFilter = VkFilter.Nearest;
                    magFilter = VkFilter.Nearest;
                    mipMode = VkSamplerMipmapMode.Linear;
                    break;
                case SamplerFilter.MinPointMagLinearMipPoint:
                    minFilter = VkFilter.Nearest;
                    magFilter = VkFilter.Linear;
                    mipMode = VkSamplerMipmapMode.Nearest;
                    break;
                case SamplerFilter.MinPointMagMipLinear:
                    minFilter = VkFilter.Nearest;
                    magFilter = VkFilter.Linear;
                    mipMode = VkSamplerMipmapMode.Linear;
                    break;
                case SamplerFilter.MinLinearMagMipPoint:
                    minFilter = VkFilter.Linear;
                    magFilter = VkFilter.Nearest;
                    mipMode = VkSamplerMipmapMode.Nearest;
                    break;
                case SamplerFilter.MinLinearMagPointMipLinear:
                    minFilter = VkFilter.Linear;
                    magFilter = VkFilter.Nearest;
                    mipMode = VkSamplerMipmapMode.Linear;
                    break;
                case SamplerFilter.MinMagLinearMipPoint:
                    minFilter = VkFilter.Linear;
                    magFilter = VkFilter.Linear;
                    mipMode = VkSamplerMipmapMode.Nearest;
                    break;
                case SamplerFilter.MinMagMipLinear:
                    minFilter = VkFilter.Linear;
                    magFilter = VkFilter.Linear;
                    mipMode = VkSamplerMipmapMode.Linear;
                    break;
                case SamplerFilter.Anisotropic:
                    anisotropyEnable = true;
                    minFilter = VkFilter.Nearest;
                    magFilter = VkFilter.Nearest;
                    mipMode = VkSamplerMipmapMode.Nearest;
                    break;
                case SamplerFilter.ComparisonMinMagMipPoint:
                    compareEnable = true;
                    minFilter = VkFilter.Nearest;
                    magFilter = VkFilter.Nearest;
                    mipMode = VkSamplerMipmapMode.Nearest;
                    break;
                case SamplerFilter.ComparisonMinMagPointMipLinear:
                    compareEnable = true;
                    minFilter = VkFilter.Nearest;
                    magFilter = VkFilter.Nearest;
                    mipMode = VkSamplerMipmapMode.Linear;
                    break;
                case SamplerFilter.ComparisonMinPointMagLinearMipPoint:
                    compareEnable = true;
                    minFilter = VkFilter.Nearest;
                    magFilter = VkFilter.Linear;
                    mipMode = VkSamplerMipmapMode.Nearest;
                    break;
                case SamplerFilter.ComparisonMinPointMagMipLinear:
                    compareEnable = true;
                    minFilter = VkFilter.Nearest;
                    magFilter = VkFilter.Linear;
                    mipMode = VkSamplerMipmapMode.Linear;
                    break;
                case SamplerFilter.ComparisonMinLinearMagMipPoint:
                    compareEnable = true;
                    minFilter = VkFilter.Linear;
                    magFilter = VkFilter.Nearest;
                    mipMode = VkSamplerMipmapMode.Nearest;
                    break;
                case SamplerFilter.ComparisonMinLinearMagPointMipLinear:
                    compareEnable = true;
                    minFilter = VkFilter.Linear;
                    magFilter = VkFilter.Nearest;
                    mipMode = VkSamplerMipmapMode.Linear;
                    break;
                case SamplerFilter.ComparisonMinMagLinearMipPoint:
                    compareEnable = true;
                    minFilter = VkFilter.Linear;
                    magFilter = VkFilter.Linear;
                    mipMode = VkSamplerMipmapMode.Nearest;
                    break;
                case SamplerFilter.ComparisonMinMagMipLinear:
                    compareEnable = true;
                    minFilter = VkFilter.Linear;
                    magFilter = VkFilter.Linear;
                    mipMode = VkSamplerMipmapMode.Linear;
                    break;
                case SamplerFilter.ComparisonAnisotropic:
                    compareEnable = true;
                    anisotropyEnable = true;
                    minFilter = VkFilter.Nearest;
                    magFilter = VkFilter.Nearest;
                    mipMode = VkSamplerMipmapMode.Nearest;
                    break;
                default:
                    throw Illegal.Value<SamplerFilter>();
            }
        }

        public static VkSamplerAddressMode VeldridToVkSamplerAddressMode(SamplerAddressMode mode)
        {
            switch (mode)
            {
                case SamplerAddressMode.Wrap:
                    return VkSamplerAddressMode.Repeat;
                case SamplerAddressMode.Mirror:
                    return VkSamplerAddressMode.MirroredRepeat;
                case SamplerAddressMode.Clamp:
                    return VkSamplerAddressMode.ClampToEdge;
                case SamplerAddressMode.Border:
                    return VkSamplerAddressMode.ClampToBorder;
                default:
                    throw Illegal.Value<SamplerAddressMode>();
            }
        }

        internal static VkCullModeFlags VeldridToVkCullMode(FaceCullingMode cullMode)
        {
            switch (cullMode)
            {
                case FaceCullingMode.Back:
                    return VkCullModeFlags.Back;
                case FaceCullingMode.Front:
                    return VkCullModeFlags.Front;
                case FaceCullingMode.None:
                    return VkCullModeFlags.None;
                default:
                    throw Illegal.Value<FaceCullingMode>();
            }
        }

        internal static VkPolygonMode VeldridToVkFillMode(TriangleFillMode fillMode)
        {
            switch (fillMode)
            {
                case TriangleFillMode.Solid:
                    return VkPolygonMode.Fill;
                case TriangleFillMode.Wireframe:
                    return VkPolygonMode.Line;
                default:
                    throw Illegal.Value<TriangleFillMode>();
            }
        }

        public static VkIndexType VeldridToVkIndexFormat(IndexFormat format)
        {
            switch (format)
            {
                case IndexFormat.UInt16:
                    return VkIndexType.Uint16;
                case IndexFormat.UInt32:
                    return VkIndexType.Uint32;
                default:
                    throw Illegal.Value<IndexFormat>();
            }
        }

        public static VkCompareOp VeldridToVkDepthComparison(DepthComparison depthComparison)
        {
            switch (depthComparison)
            {
                case DepthComparison.Never:
                    return VkCompareOp.Never;
                case DepthComparison.Less:
                    return VkCompareOp.Less;
                case DepthComparison.Equal:
                    return VkCompareOp.Equal;
                case DepthComparison.LessEqual:
                    return VkCompareOp.LessOrEqual;
                case DepthComparison.Greater:
                    return VkCompareOp.Greater;
                case DepthComparison.NotEqual:
                    return VkCompareOp.NotEqual;
                case DepthComparison.GreaterEqual:
                    return VkCompareOp.GreaterOrEqual;
                case DepthComparison.Always:
                    return VkCompareOp.Always;
                default:
                    throw Illegal.Value<DepthComparison>();
            }
        }

        public static VkPrimitiveTopology VeldridToVkPrimitiveTopology(PrimitiveTopology primitiveTopology)
        {
            switch (primitiveTopology)
            {
                case PrimitiveTopology.TriangleList:
                    return VkPrimitiveTopology.TriangleList;
                case PrimitiveTopology.TriangleStrip:
                    return VkPrimitiveTopology.TriangleStrip;
                case PrimitiveTopology.LineList:
                    return VkPrimitiveTopology.LineList;
                case PrimitiveTopology.LineStrip:
                    return VkPrimitiveTopology.LineStrip;
                case PrimitiveTopology.PointList:
                    return VkPrimitiveTopology.PointList;
                default:
                    throw Illegal.Value<PrimitiveTopology>();
            }
        }

        public static VkFormat VeldridToVkVertexElementFormat(VertexElementFormat elementFormat)
        {
            switch (elementFormat)
            {
                case VertexElementFormat.Float1:
                    return VkFormat.R32Sfloat;
                case VertexElementFormat.Float2:
                    return VkFormat.R32g32Sfloat;
                case VertexElementFormat.Float3:
                    return VkFormat.R32g32b32Sfloat;
                case VertexElementFormat.Float4:
                    return VkFormat.R32g32b32a32Sfloat;
                case VertexElementFormat.Byte1:
                    return VkFormat.R8Uint;
                case VertexElementFormat.Byte4:
                    return VkFormat.R8g8b8a8Unorm;
                default:
                    throw Illegal.Value<VertexElementFormat>();
            }
        }

        public static VkBlendFactor VeldridToVkBlendFactor(Blend blend)
        {
            switch (blend)
            {
                case Blend.Zero:
                    return VkBlendFactor.Zero;
                case Blend.One:
                    return VkBlendFactor.One;
                case Blend.SourceAlpha:
                    return VkBlendFactor.SrcAlpha;
                case Blend.InverseSourceAlpha:
                    return VkBlendFactor.OneMinusSrcAlpha;
                case Blend.DestinationAlpha:
                    return VkBlendFactor.DstAlpha;
                case Blend.InverseDestinationAlpha:
                    return VkBlendFactor.OneMinusDstAlpha;
                case Blend.SourceColor:
                    return VkBlendFactor.SrcColor;
                case Blend.InverseSourceColor:
                    return VkBlendFactor.OneMinusSrcColor;
                case Blend.DestinationColor:
                    return VkBlendFactor.DstColor;
                case Blend.InverseDestinationColor:
                    return VkBlendFactor.OneMinusDstColor;
                case Blend.BlendFactor:
                    return VkBlendFactor.ConstantColor;
                case Blend.InverseBlendFactor:
                    return VkBlendFactor.OneMinusConstantColor;
                default:
                    throw Illegal.Value<Blend>();
            }
        }

        public static VkBlendOp VeldridToVkBlendOp(BlendFunction func)
        {
            switch (func)
            {
                case BlendFunction.Add:
                    return VkBlendOp.Add;
                case BlendFunction.Subtract:
                    return VkBlendOp.Subtract;
                case BlendFunction.ReverseSubtract:
                    return VkBlendOp.ReverseSubtract;
                case BlendFunction.Minimum:
                    return VkBlendOp.Min;
                case BlendFunction.Maximum:
                    return VkBlendOp.Max;
                default:
                    throw Illegal.Value<BlendFunction>();
            }
        }
    }
}
