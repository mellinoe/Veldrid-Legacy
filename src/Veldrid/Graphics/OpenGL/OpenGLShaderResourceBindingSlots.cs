using System.Collections.Generic;

namespace Veldrid.Graphics.OpenGL
{
    public class OpenGLShaderResourceBindingSlots : ShaderResourceBindingSlots
    {
        public ShaderResourceDescription[] Resources { get; }
        public OpenGLTextureBindingSlots TextureSlots { get; }
        public OpenGLShaderConstantBindingSlots ConstantBufferSlots { get; }

        public OpenGLShaderResourceBindingSlots(OpenGLShaderSet shaderSet, ShaderResourceDescription[] resources)
        {
            Resources = resources;

            List<ShaderResourceDescription> textures = new List<ShaderResourceDescription>();
            List<ShaderResourceDescription> constants = new List<ShaderResourceDescription>();

            for (int i = 0; i < resources.Length; i++)
            {
                ShaderResourceDescription resource = resources[i];
                if (resource.Type == ShaderResourceType.ConstantBuffer)
                {
                    constants.Add(resource);
                }
                else if (resource.Type == ShaderResourceType.Texture)
                {
                    textures.Add(resource);
                }
            }

            // TODO Allow lists to be passed in or otherwise avoid allocations.
            TextureSlots = new OpenGLTextureBindingSlots(shaderSet, textures.ToArray());
            ConstantBufferSlots = new OpenGLShaderConstantBindingSlots(shaderSet, constants.ToArray());
        }
    }
}
