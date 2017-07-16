using SharpDX.Direct3D11;
using System.Collections.Generic;

namespace Veldrid.Graphics.Direct3D
{
    public class D3DShaderResourceBindingSlots : ShaderResourceBindingSlots
    {
        public ShaderResourceDescription[] Resources { get; }

        public D3DShaderTextureBindingSlots TextureSlots { get; }

        public D3DShaderConstantBindingSlots ConstantBufferSlots { get; }

        public D3DShaderResourceBindingSlots(Device device, D3DShaderSet shaderSet, ShaderResourceDescription[] resources)
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
            TextureSlots = new D3DShaderTextureBindingSlots(shaderSet, textures.ToArray());
            ConstantBufferSlots = new D3DShaderConstantBindingSlots(device, shaderSet, constants.ToArray());
        }
    }
}
