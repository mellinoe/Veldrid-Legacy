using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using System;

namespace Veldrid.Graphics.Direct3D
{
    public abstract class D3DShader<TShader> : Shader where TShader : IDisposable
    {
        private const ShaderFlags DefaultShaderFlags
#if DEBUG
            = ShaderFlags.Debug | ShaderFlags.SkipOptimization;
#else
            = ShaderFlags.OptimizationLevel3;
#endif

        private ShaderReflection _reflection;

        public ShaderStages Type { get; }
        public ShaderBytecode Bytecode { get; }
        public TShader DeviceShader { get; }
        public ShaderReflection Reflection => _reflection ?? (_reflection = new ShaderReflection(Bytecode.Data));

        public D3DShader(Device device, ShaderStages type, ShaderBytecode bytecode)
        {
            Type = type;
            Bytecode = bytecode;
            DeviceShader = CreateDeviceShader(device, Bytecode);
        }

        private static string GetEntryPoint(ShaderStages type)
        {
            switch (type)
            {
                case ShaderStages.Vertex:
                    return "VS";
                case ShaderStages.TessellationControl:
                    return "HS";
                case ShaderStages.TessellationEvaluation:
                    return "DS";
                case ShaderStages.Geometry:
                    return "GS";
                case ShaderStages.Fragment:
                    return "PS";
                default:
                    throw Illegal.Value<ShaderStages>();
            }
        }

        private static string GetProfile(ShaderStages type)
        {
            switch (type)
            {
                case ShaderStages.Vertex:
                    return "vs_5_0";
                case ShaderStages.TessellationControl:
                    return "hs_5_0";
                case ShaderStages.TessellationEvaluation:
                    return "ds_5_0";
                case ShaderStages.Geometry:
                    return "gs_5_0";
                case ShaderStages.Fragment:
                    return "ps_5_0";
                default:
                    throw Illegal.Value<ShaderStages>();
            }
        }

        protected abstract TShader CreateDeviceShader(Device device, ShaderBytecode bytecode);

        public void Dispose()
        {
            DeviceShader.Dispose();
        }

    }

    public class D3DVertexShader : D3DShader<VertexShader>
    {
        public D3DVertexShader(Device device, ShaderBytecode bytecode)
            : base(device, ShaderStages.Vertex, bytecode) { }

        protected override VertexShader CreateDeviceShader(Device device, ShaderBytecode bytecode)
        {
            return new VertexShader(device, bytecode);
        }
    }

    public class D3DTessellationControlShader : D3DShader<HullShader>
    {
        public D3DTessellationControlShader(Device device, ShaderBytecode bytecode)
            : base(device, ShaderStages.TessellationControl, bytecode) { }

        protected override HullShader CreateDeviceShader(Device device, ShaderBytecode bytecode)
        {
            return new HullShader(device, bytecode);
        }
    }

    public class D3DTessellationEvaluationShader : D3DShader<DomainShader>
    {
        public D3DTessellationEvaluationShader(Device device, ShaderBytecode bytecode)
            : base(device, ShaderStages.TessellationEvaluation, bytecode) { }

        protected override DomainShader CreateDeviceShader(Device device, ShaderBytecode bytecode)
        {
            return new DomainShader(device, bytecode);
        }
    }

    public class D3DGeometryShader : D3DShader<GeometryShader>
    {
        public D3DGeometryShader(Device device, ShaderBytecode bytecode)
            : base(device, ShaderStages.Geometry, bytecode) { }

        protected override GeometryShader CreateDeviceShader(Device device, ShaderBytecode bytecode)
        {
            return new GeometryShader(device, bytecode);
        }
    }

    public class D3DFragmentShader : D3DShader<PixelShader>
    {
        public D3DFragmentShader(Device device, ShaderBytecode bytecode)
            : base(device, ShaderStages.Fragment, bytecode) { }

        protected override PixelShader CreateDeviceShader(Device device, ShaderBytecode bytecode)
        {
            return new PixelShader(device, bytecode);
        }
    }
}
