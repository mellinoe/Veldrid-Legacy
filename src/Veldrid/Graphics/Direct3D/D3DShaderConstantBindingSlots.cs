using SharpDX.D3DCompiler;
using SharpDX.Direct3D11;
using System;

namespace Veldrid.Graphics.Direct3D
{
    public class D3DShaderConstantBindingSlots
    {
        private readonly Device _device;
        private readonly ShaderStages[] _applicabilityFlagsBySlot;
        
        public ShaderResourceDescription[] Constants { get; }

        public D3DShaderConstantBindingSlots(
            Device device,
            ShaderSet shaderSet,
            ShaderResourceDescription[] constants)
        {
            _device = device;
            Constants = constants;

            D3DShaderSet d3dShaderSet = (D3DShaderSet)shaderSet;

            ShaderReflection vsReflection = d3dShaderSet.VertexShader.Reflection;
            ShaderReflection psReflection = d3dShaderSet.FragmentShader.Reflection;
            ShaderReflection gsReflection = null;
            if (shaderSet.GeometryShader != null)
            {
                gsReflection = d3dShaderSet.GeometryShader.Reflection;
            }

            int numConstants = constants.Length;
            _applicabilityFlagsBySlot = new ShaderStages[numConstants];
            for (int i = 0; i < numConstants; i++)
            {
                var genericElement = constants[i];
                bool isVsBuffer = DoesConstantBufferExist(vsReflection, i, genericElement.Name);
                bool isPsBuffer = DoesConstantBufferExist(psReflection, i, genericElement.Name);
                bool isGsBuffer = false;
                if (gsReflection != null)
                {
                    isGsBuffer = DoesConstantBufferExist(gsReflection, i, genericElement.Name);
                }

                ShaderStages applicabilityFlags = ShaderStages.None;
                if (isVsBuffer)
                {
                    applicabilityFlags |= ShaderStages.Vertex;
                }
                if (isPsBuffer)
                {
                    applicabilityFlags |= ShaderStages.Fragment;
                }
                if (isGsBuffer)
                {
                    applicabilityFlags |= ShaderStages.Geometry;
                }

                _applicabilityFlagsBySlot[i] = applicabilityFlags;
            }
        }

        public ShaderStages GetApplicability(int slot)
        {
            if (slot >= _applicabilityFlagsBySlot.Length)
            {
                throw new ArgumentOutOfRangeException($"Invalid slot. There are {_applicabilityFlagsBySlot.Length} slots bound.");
            }

            return _applicabilityFlagsBySlot[slot];
        }

        private static bool DoesConstantBufferExist(ShaderReflection reflection, int slot, string name)
        {
            InputBindingDescription bindingDesc;
            try
            {
                bindingDesc = reflection.GetResourceBindingDescription(name);

                if (bindingDesc.BindPoint != slot)
                {
                    throw new VeldridException($"Mismatched binding slot for {name}. Expected: {slot}, Actual: {bindingDesc.BindPoint}");
                }

                return true;
            }
            catch (SharpDX.SharpDXException)
            {
                for (int i = 0; i < reflection.Description.BoundResources; i++)
                {
                    var desc = reflection.GetResourceBindingDescription(i);
                    if (desc.Type == ShaderInputType.ConstantBuffer && desc.BindPoint == slot)
                    {
                        System.Diagnostics.Debug.WriteLine("Buffer in slot " + slot + " has wrong name. Expected: " + name + ", Actual: " + desc.Name);
                        bindingDesc = desc;
                        return true;
                    }
                }
            }

            return false;
        }
    }
}