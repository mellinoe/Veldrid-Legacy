using Veldrid.Graphics;

namespace Veldrid.RenderDemo
{
    public static class ResourceFactoryEx
    {
        public static Material CreateMaterial(
            this ResourceFactory factory,
            RenderContext rc,
            string vertexShaderName,
            string fragmentShaderName,
            VertexInputDescription vertexInputs,
            ShaderResourceDescription[] resources)
        {
            return CreateMaterial(factory, rc, vertexShaderName, fragmentShaderName, new[] { vertexInputs }, resources);
        }

        public static Material CreateMaterial(
            this ResourceFactory factory,
            RenderContext rc,
            string vertexShaderName,
            string fragmentShaderName,
            VertexInputDescription vertexInputs0,
            VertexInputDescription vertexInputs1,
            ShaderResourceDescription[] resources)
        {
            return CreateMaterial(
                factory,
                rc,
                vertexShaderName,
                fragmentShaderName,
                new[] { vertexInputs0, vertexInputs1 },
                resources);
        }

        public static Material CreateMaterial(
            this ResourceFactory factory,
            RenderContext rc,
            string vertexShaderName,
            string fragmentShaderName,
            VertexInputDescription[] vertexInputs,
            ShaderResourceDescription[] resources)

        {
            Shader vs = factory.CreateShader(ShaderStages.Vertex, ShaderHelper.LoadShaderCode(vertexShaderName, ShaderStages.Vertex, rc.ResourceFactory));
            Shader fs = factory.CreateShader(ShaderStages.Fragment, ShaderHelper.LoadShaderCode(fragmentShaderName, ShaderStages.Fragment, rc.ResourceFactory));
            VertexInputLayout inputLayout = factory.CreateInputLayout(vertexInputs);
            ShaderSet shaderSet = factory.CreateShaderSet(inputLayout, vs, fs);
            ShaderResourceBindingSlots resourceBindings = factory.CreateShaderResourceBindingSlots(shaderSet, resources);

            return new Material(shaderSet, resourceBindings);
        }
    }
}
