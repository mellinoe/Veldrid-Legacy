namespace Veldrid.Graphics.Vulkan
{
    public class VkShaderTextureBindingSlots : ShaderTextureBindingSlots
    {

        public VkShaderTextureBindingSlots(VkShaderSet shaderSet, ShaderTextureInput[] textureInputs)
        {
            ShaderSet = shaderSet;
            TextureInputs = textureInputs;
        }

        public VkShaderSet ShaderSet { get; }

        public ShaderTextureInput[] TextureInputs { get; }
    }
}