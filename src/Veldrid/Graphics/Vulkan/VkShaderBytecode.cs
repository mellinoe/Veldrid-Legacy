using System;

namespace Veldrid.Graphics.Vulkan
{
    public class VkShaderBytecode : CompiledShaderCode
    {
        public VkShaderBytecode(byte[] shaderBytes)
        {
            ShaderBytes = shaderBytes;
        }

        public VkShaderBytecode(ShaderStages type, string shaderCode)
        {
            // TODO: Try to use glslangvalidator if it exists.
            // glslangValidator -H -V -o <tempfile> <inputfile>
            throw new NotImplementedException();
        }

        public byte[] ShaderBytes { get; }
    }
}
