namespace Veldrid.Graphics.Vulkan
{
    public class VkShaderSet : ShaderSet
    {
        public VkShaderSet(
            VKInputLayout inputLayout,
            VkShader vertexShader,
            VkShader geometryShader,
            VkShader fragmentShader)
        {
            InputLayout = inputLayout;
            VertexShader = vertexShader;
            GeometryShader = geometryShader;
            FragmentShader = fragmentShader;
        }

        public VKInputLayout InputLayout { get; }
        public VkShader VertexShader { get; }
        public VkShader GeometryShader { get; }
        public VkShader FragmentShader { get; }

        VertexInputLayout ShaderSet.InputLayout => InputLayout;
        Shader ShaderSet.VertexShader => VertexShader;
        Shader ShaderSet.GeometryShader => GeometryShader;
        Shader ShaderSet.FragmentShader => FragmentShader;

        public void Dispose()
        {
            InputLayout.Dispose();
            VertexShader.Dispose();
            GeometryShader?.Dispose();
            FragmentShader.Dispose();
        }
    }
}