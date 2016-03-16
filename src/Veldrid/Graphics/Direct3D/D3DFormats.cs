﻿using System;
using SharpDX.DXGI;
using SharpDX.Direct3D11;

namespace Veldrid.Graphics.Direct3D
{
    internal class D3DFormats
    {
        internal static Format ConvertPixelFormat(PixelFormat format)
        {
            switch (format)
            {
                case PixelFormat.R32_G32_B32_A32_Float:
                    return Format.R32G32B32A32_Float;
                case PixelFormat.Alpha_UInt8:
                    return Format.A8_UNorm;
                case PixelFormat.R8_G8_B8_A8:
                    return Format.R8G8B8A8_UNorm;
                default:
                    throw Illegal.Value<PixelFormat>();
            }
        }

        internal static Format ConvertIndexFormat(IndexFormat format)
        {
            switch (format)
            {
                case IndexFormat.UInt32:
                    return Format.R32_UInt;
                case IndexFormat.UInt16:
                    return Format.R16_UInt;
                case IndexFormat.UInt8:
                    return Format.R8_UInt;
                default:
                    throw Illegal.Value<IndexFormat>();
            }
        }

        public static BlendOption ConvertBlend(Blend blendFactor)
        {
            switch (blendFactor)
            {
                case Blend.Zero:
                    return BlendOption.Zero;
                case Blend.One:
                    return BlendOption.One;
                case Blend.SourceAlpha:
                    return BlendOption.SourceAlpha;
                case Blend.InverseSourceAlpha:
                    return BlendOption.InverseSourceAlpha;
                case Blend.DestinationAlpha:
                    return BlendOption.DestinationAlpha;
                case Blend.InverseDestinationAlpha:
                    return BlendOption.InverseDestinationAlpha;
                case Blend.SourceColor:
                    return BlendOption.SourceColor;
                case Blend.InverseSourceColor:
                    return BlendOption.InverseSourceColor;
                case Blend.DestinationColor:
                    return BlendOption.DestinationColor;
                case Blend.InverseDestinationColor:
                    return BlendOption.InverseDestinationColor;
                case Blend.BlendFactor:
                    return BlendOption.BlendFactor;
                case Blend.InverseBlendFactor:
                    return BlendOption.InverseBlendFactor;
                default:
                    throw Illegal.Value<Blend>();
            }
        }

        public static BlendOperation ConvertBlendFunction(BlendFunction function)
        {
            switch (function)
            {
                case BlendFunction.Add:
                    return BlendOperation.Add;
                case BlendFunction.Subtract:
                    return BlendOperation.Subtract;
                case BlendFunction.ReverseSubtract:
                    return BlendOperation.ReverseSubtract;
                case BlendFunction.Minimum:
                    return BlendOperation.Minimum;
                case BlendFunction.Maximum:
                    return BlendOperation.Maximum;
                default:
                    throw Illegal.Value<BlendFunction>();
            }
        }
    }
}