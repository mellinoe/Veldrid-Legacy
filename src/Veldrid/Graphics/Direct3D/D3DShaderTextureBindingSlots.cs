using SharpDX.D3DCompiler;
using System;
using System.Diagnostics;

namespace Veldrid.Graphics.Direct3D
{
    public class D3DShaderTextureBindingSlots
    {
        private readonly ShaderStages[] _applicabilities;

        public ShaderResourceDescription[] TextureInputs { get; }

        public D3DShaderTextureBindingSlots(D3DShaderSet shaderSet, ShaderResourceDescription[] textureInputs)
        {
            TextureInputs = textureInputs;
            _applicabilities = ComputeStageApplicabilities(shaderSet, textureInputs);
        }

        public ShaderStages GetApplicabilityForSlot(int slot)
        {
            return _applicabilities[slot];
        }

        private ShaderStages[] ComputeStageApplicabilities(D3DShaderSet shaderSet, ShaderResourceDescription[] textureInputs)
        {
            ShaderStages[] stageFlagsBySlot = new ShaderStages[textureInputs.Length];
            for (int i = 0; i < stageFlagsBySlot.Length; i++)
            {
                ShaderResourceDescription element = textureInputs[i];
                ShaderStages flags = ShaderStages.None;

                if (IsTextureSlotUsedInShader(shaderSet.VertexShader, i
#if DEBUG
                    , element.Name
#endif
                ))
                {
                    flags |= ShaderStages.Vertex;
                }

                if (IsTextureSlotUsedInShader(shaderSet.FragmentShader, i
#if DEBUG
                    , element.Name
#endif
                ))
                {
                    flags |= ShaderStages.Fragment;
                }


                if (shaderSet.GeometryShader != null && IsTextureSlotUsedInShader(shaderSet.GeometryShader, i
#if DEBUG
                    , element.Name
#endif
                ))
                {
                    flags |= ShaderStages.Geometry;
                }

                stageFlagsBySlot[i] = flags;
            }

            return stageFlagsBySlot;
        }

        private bool IsTextureSlotUsedInShader<TShader>(D3DShader<TShader> shader, int slot
#if DEBUG
            , string name)
#else
            )
#endif
            where TShader : IDisposable
        {
            ShaderReflection reflection = shader.Reflection;
            int numResources = reflection.Description.BoundResources;
            for (int i = 0; i < numResources; i++)
            {
                InputBindingDescription desc = reflection.GetResourceBindingDescription(i);
                if (desc.Type == ShaderInputType.Texture && desc.BindPoint == slot)
                {
#if DEBUG
                    if (desc.Name != name)
                    {
                        Debug.WriteLine($"The texture resource in slot {slot} had an unexpected name. Expected: {name} Actual: {desc.Name}");
                    }
#endif
                    return true;
                }
            }

            return false;
        }
    }
}