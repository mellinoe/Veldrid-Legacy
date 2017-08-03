namespace Veldrid.Graphics.Direct3D
{
    public class D3DShaderSet : ShaderSet
    {
        public D3DVertexInputLayout InputLayout { get; }

        public D3DVertexShader VertexShader { get; }

        public D3DTessellationControlShader TessellationControlShader { get; }

        public D3DTessellationEvaluationShader TessellationEvaluationShader { get; }

        public D3DGeometryShader GeometryShader { get; }

        public D3DFragmentShader FragmentShader { get; }

        public D3DShaderSet(
            VertexInputLayout inputLayout,
            Shader vertexShader,
            Shader tessellationControlShader,
            Shader tessellationEvaluationShader,
            Shader geometryShader,
            Shader fragmentShader)
        {
            InputLayout = (D3DVertexInputLayout)inputLayout;
            VertexShader = (D3DVertexShader)vertexShader;
            TessellationControlShader = (D3DTessellationControlShader)tessellationControlShader;
            TessellationEvaluationShader = (D3DTessellationEvaluationShader)tessellationEvaluationShader;
            GeometryShader = (D3DGeometryShader)geometryShader;
            FragmentShader = (D3DFragmentShader)fragmentShader;
        }

        VertexInputLayout ShaderSet.InputLayout => InputLayout;
        Shader ShaderSet.VertexShader => VertexShader;
        Shader ShaderSet.TessellationControlShader => TessellationControlShader;
        Shader ShaderSet.TessellationEvaluationShader => TessellationEvaluationShader;
        Shader ShaderSet.GeometryShader => GeometryShader;
        Shader ShaderSet.FragmentShader => FragmentShader;

        public void Dispose()
        {
            InputLayout.Dispose();
            VertexShader.Dispose();
            TessellationControlShader?.Dispose();
            TessellationEvaluationShader?.Dispose();
            GeometryShader?.Dispose();
            FragmentShader.Dispose();
        }
    }
}