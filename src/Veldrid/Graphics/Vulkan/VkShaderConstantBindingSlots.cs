namespace Veldrid.Graphics.Vulkan
{
    public class VkShaderConstantBindingSlots : ShaderConstantBindingSlots
    {
        public VkShaderConstantBindingSlots(VkShaderSet shaderSet, ShaderConstantDescription[] constants)
        {
            ShaderSet = shaderSet;
            Constants = constants;
        }

        public VkShaderSet ShaderSet { get; }

        public ShaderConstantDescription[] Constants { get; }
    }
}