using System.Collections.Generic;

namespace Veldrid.Graphics.Direct3D
{
    public class D3DShaderResourceBindingSlots : ShaderResourceBindingSlots
    {
        /// <summary>Maps from generic slot to device slot.</summary>
        private Dictionary<int, D3DResourceBindingSlotInfo> _textureDeviceSlots = new Dictionary<int, D3DResourceBindingSlotInfo>();
        /// <summary>Maps from generic slot to device slot.</summary>
        private Dictionary<int, D3DResourceBindingSlotInfo> _samplerDeviceSlots = new Dictionary<int, D3DResourceBindingSlotInfo>();
        /// <summary>Maps from generic slot to device slot.</summary>
        private Dictionary<int, D3DResourceBindingSlotInfo> _constantBufferDeviceSlots = new Dictionary<int, D3DResourceBindingSlotInfo>();

        public ShaderResourceDescription[] Resources { get; }

        public ShaderResourceDescription[] TextureInputs { get; }
        public ShaderResourceDescription[] SamplerInputs { get; }
        public ShaderResourceDescription[] ConstantBufferInputs { get; }

        public D3DShaderResourceBindingSlots(ShaderResourceDescription[] resourceInputs)
        {
            Resources = resourceInputs;

            List<ShaderResourceDescription> textures = new List<ShaderResourceDescription>();
            List<ShaderResourceDescription> samplers = new List<ShaderResourceDescription>();
            List<ShaderResourceDescription> constants = new List<ShaderResourceDescription>();
            int currentTextureSlot = 0;
            int currentSamplerSlot = 0;
            int currentConstantBufferSlot = 0;
            for (int i = 0; i < resourceInputs.Length; i++)
            {
                ShaderResourceDescription resource = resourceInputs[i];
                if (resource.Type == ShaderResourceType.Texture)
                {
                    textures.Add(resource);
                    _textureDeviceSlots[i] = new D3DResourceBindingSlotInfo { DeviceSlot = currentTextureSlot, Stages = resource.Stages };
                    currentTextureSlot += 1;
                }
                else if (resource.Type == ShaderResourceType.Sampler)
                {
                    samplers.Add(resource);
                    _samplerDeviceSlots[i] = new D3DResourceBindingSlotInfo { DeviceSlot = currentSamplerSlot, Stages = resource.Stages };
                    currentSamplerSlot += 1;
                }
                else
                {
                    constants.Add(resource);
                    _constantBufferDeviceSlots[i] = new D3DResourceBindingSlotInfo { DeviceSlot = currentConstantBufferSlot, Stages = resource.Stages };
                    currentConstantBufferSlot += 1;
                }
            }

            TextureInputs = textures.ToArray();
            SamplerInputs = samplers.ToArray();
            ConstantBufferInputs = constants.ToArray();
        }

        public D3DResourceBindingSlotInfo GetTextureSlotInfo(int resourceBindingSlot)
        {
            if (!_textureDeviceSlots.TryGetValue(resourceBindingSlot, out D3DResourceBindingSlotInfo ret))
            {
                throw new VeldridException("There is no texture resource in slot " + resourceBindingSlot);
            }

            return ret;
        }

        public D3DResourceBindingSlotInfo GetSamplerSlotInfo(int resourceBindingSlot)
        {
            if (!_samplerDeviceSlots.TryGetValue(resourceBindingSlot, out D3DResourceBindingSlotInfo ret))
            {
                throw new VeldridException("There is no sampler resource in slot " + resourceBindingSlot);
            }

            return ret;
        }


        public D3DResourceBindingSlotInfo GetConstantBufferInfo(int resourceBindingSlot)
        {
            if (!_constantBufferDeviceSlots.TryGetValue(resourceBindingSlot, out D3DResourceBindingSlotInfo ret))
            {
                throw new VeldridException("There is no constant buffer resource in slot " + resourceBindingSlot);
            }

            return ret;
        }
    }

    public struct D3DResourceBindingSlotInfo
    {
        /// <summary>
        /// The device slot which the binding applies to.
        /// </summary>
        public int DeviceSlot;

        /// <summary>
        /// The shader stages in which the binding should be active.
        /// </summary>
        public ShaderStages Stages;
    }
}