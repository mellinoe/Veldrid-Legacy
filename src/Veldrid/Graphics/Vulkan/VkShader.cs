using System;
using Vulkan;
using static Veldrid.Graphics.Vulkan.VulkanUtil;
using static Vulkan.VulkanNative;

namespace Veldrid.Graphics.Vulkan
{
    public class VkShader : Shader
    {
        private readonly VkDevice _device;
        private readonly VkShaderModule _shaderModule;

        public unsafe VkShader(VkDevice device, ShaderStages type, VkShaderBytecode bytecode)
        {
            _device = device;
            Type = type;

            VkShaderModuleCreateInfo shaderModuleCI = VkShaderModuleCreateInfo.New();
            fixed (byte* codePtr = bytecode.ShaderBytes)
            {
                shaderModuleCI.codeSize = (UIntPtr)bytecode.ShaderBytes.Length;
                shaderModuleCI.pCode = (uint*)codePtr;
                VkResult result = vkCreateShaderModule(_device, ref shaderModuleCI, null, out _shaderModule);
                CheckResult(result);
            }
        }

        public ShaderStages Type { get; }
        public VkShaderModule ShaderModule => _shaderModule;

        public unsafe void Dispose()
        {
            vkDestroyShaderModule(_device, ShaderModule, null);
        }
    }
}