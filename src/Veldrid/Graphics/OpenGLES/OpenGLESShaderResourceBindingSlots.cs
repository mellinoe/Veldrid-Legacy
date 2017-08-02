using OpenTK.Graphics.ES30;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Veldrid.Graphics.OpenGLES
{
    public class OpenGLESShaderResourceBindingSlots : ShaderResourceBindingSlots
    {
        private Dictionary<int, OpenGLESTextureBindingSlotInfo> _textureBindings = new Dictionary<int, OpenGLESTextureBindingSlotInfo>();
        private Dictionary<int, OpenGLESTextureBindingSlotInfo> _samplerBindings = new Dictionary<int, OpenGLESTextureBindingSlotInfo>();
        private Dictionary<int, OpenGLESUniformBinding> _constantBindings = new Dictionary<int, OpenGLESUniformBinding>();

        public ShaderResourceDescription[] Resources { get; }

        public OpenGLESShaderResourceBindingSlots(OpenGLESShaderSet shaderSet, ShaderResourceDescription[] resources)
        {
            Resources = resources;
            int programID = shaderSet.ProgramID;

            int lastTextureLocation = -1;
            int relativeTextureIndex = -1;
            for (int i = 0; i < resources.Length; i++)
            {
                ShaderResourceDescription resource = resources[i];
                if (resource.Type == ShaderResourceType.ConstantBuffer)
                {
                    int blockIndex = GL.GetUniformBlockIndex(programID, resource.Name);
                    Utilities.CheckLastGLES3Error();
                    if (blockIndex != -1)
                    {
                        ValidateBlockSize(programID, blockIndex, resource.DataSizeInBytes, resource.Name);
                        _constantBindings[i] = new OpenGLESUniformBinding(programID, blockIndex, resource.DataSizeInBytes);
                    }
                    else
                    {
                        int uniformLocation = GL.GetUniformLocation(programID, resource.Name);
                        Utilities.CheckLastGLES3Error();
                        if (uniformLocation == -1)
                        {
                            throw new VeldridException($"No uniform or uniform block with name {resource.Name} was found.");
                        }

                        OpenGLESUniformStorageAdapter storageAdapter = new OpenGLESUniformStorageAdapter(programID, uniformLocation);
                        _constantBindings[i] = new OpenGLESUniformBinding(programID, storageAdapter);
                    }
                }
                else if (resource.Type == ShaderResourceType.Texture)
                {
                    int location = GL.GetUniformLocation(shaderSet.ProgramID, resource.Name);
                    Utilities.CheckLastGLES3Error();
                    if (location == -1)
                    {
                        throw new VeldridException($"No sampler was found with the name {resource.Name}");
                    }

                    relativeTextureIndex += 1;
                    _textureBindings[i] = new OpenGLESTextureBindingSlotInfo() { RelativeIndex = relativeTextureIndex, UniformLocation = location };
                    lastTextureLocation = location;
                }
                else
                {
                    Debug.Assert(resource.Type == ShaderResourceType.Sampler);
                    if (lastTextureLocation == -1)
                    {
                        throw new VeldridException(
                            "OpenGL Shaders must specify at least one texture before a sampler. Samplers are implicity linked with the closest-previous texture resource in the binding list.");
                    }

                    _samplerBindings[i] = new OpenGLESTextureBindingSlotInfo() { RelativeIndex = relativeTextureIndex, UniformLocation = lastTextureLocation };
                }
            }

        }

        public OpenGLESTextureBindingSlotInfo GetTextureBindingInfo(int slot)
        {
            if (!_textureBindings.TryGetValue(slot, out OpenGLESTextureBindingSlotInfo binding))
            {
                throw new VeldridException("There is no texture in slot " + slot);
            }

            return binding;
        }

        public OpenGLESTextureBindingSlotInfo GetSamplerBindingInfo(int slot)
        {
            if (!_samplerBindings.TryGetValue(slot, out OpenGLESTextureBindingSlotInfo binding))
            {
                throw new VeldridException("There is no sampler in slot " + slot);
            }

            return binding;
        }

        public OpenGLESUniformBinding GetUniformBindingForSlot(int slot)
        {
            if (!_constantBindings.TryGetValue(slot, out OpenGLESUniformBinding binding))
            {
                throw new VeldridException("There is no constant buffer in slot " + slot);
            }

            return binding;
        }

        [Conditional("DEBUG")]
        private void ValidateBlockSize(int programID, int blockIndex, int providerSize, string elementName)
        {
            int blockSize;
            GL.GetActiveUniformBlock(programID, blockIndex, ActiveUniformBlockParameter.UniformBlockDataSize, out blockSize);
            Utilities.CheckLastGLES3Error();

            bool sizeMismatched = (blockSize != providerSize);

            if (sizeMismatched)
            {
                string errorMessage = $"Uniform block validation failed for Program {programID}.";
                if (sizeMismatched)
                {
                    errorMessage += Environment.NewLine + $"Provider size in bytes: {providerSize}, Actual buffer size in bytes: {blockSize}.";
                }

                throw new VeldridException(errorMessage);
            }
        }
    }

    public struct OpenGLESTextureBindingSlotInfo
    {
        /// <summary>
        /// The relative index of this binding with relation to the other textures used by a shader.
        /// Generally, this is the texture unit that the binding will be placed into.
        /// </summary>
        public int RelativeIndex;
        /// <summary>
        /// The uniform location of the binding in the shader program.
        /// </summary>
        public int UniformLocation;
    }
}
