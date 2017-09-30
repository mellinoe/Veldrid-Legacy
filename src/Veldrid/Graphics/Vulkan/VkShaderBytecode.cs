using System;

namespace Veldrid.Graphics.Vulkan
{
    public class VkShaderBytecode : CompiledShaderCode
    {
        public VkShaderBytecode(byte[] shaderBytes)
        {
            ShaderBytes = shaderBytes;
        }

        public VkShaderBytecode(ShaderStages type, string shaderCode, string entryPoint)
        {
            if (!GlslangValidatorTool.IsAvailable())
            {
                throw new VeldridException("glslangValidator is not available to compile GLSL to SPIR-V.");
            }

            ShaderBytes = GlslangValidatorTool.CompileBytecode(type, shaderCode, entryPoint);
        }

        public byte[] ShaderBytes { get; }
    }
}
