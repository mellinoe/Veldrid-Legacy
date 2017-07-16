using System.Collections.Generic;

namespace Veldrid.Graphics.OpenGLES
{
    public class OpenGLESShaderResourceBindingSlots : ShaderResourceBindingSlots
    {
        public ShaderResourceDescription[] Resources { get; }
        public OpenGLESTextureBindingSlots TextureSlots { get; }
        public OpenGLESShaderConstantBindingSlots ConstantBufferSlots { get; }

        public OpenGLESShaderResourceBindingSlots(OpenGLESShaderSet shaderSet, ShaderResourceDescription[] resources)
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
            TextureSlots = new OpenGLESTextureBindingSlots(shaderSet, textures.ToArray());
            ConstantBufferSlots = new OpenGLESShaderConstantBindingSlots(shaderSet, constants.ToArray());
        }
    }
}
