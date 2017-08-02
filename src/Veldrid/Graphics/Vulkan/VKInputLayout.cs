namespace Veldrid.Graphics.Vulkan
{
    public class VKInputLayout : VertexInputLayout
    {
        public VKInputLayout(VertexInputDescription[] vertexInputs)
        {
            InputDescriptions = vertexInputs;
        }

        public VertexInputDescription[] InputDescriptions { get; }

        public void Dispose()
        {
        }
    }
}