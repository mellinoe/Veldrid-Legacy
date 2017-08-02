using OpenTK.Graphics.OpenGL;
using System;
using System.Collections.Generic;
using System.Diagnostics;

namespace Veldrid.Graphics.OpenGL
{
    public class OpenGLShaderResourceBindingSlots : ShaderResourceBindingSlots
    {
        private Dictionary<int, OpenGLTextureBindingSlotInfo> _textureBindings = new Dictionary<int, OpenGLTextureBindingSlotInfo>();
        private Dictionary<int, OpenGLTextureBindingSlotInfo> _samplerBindings = new Dictionary<int, OpenGLTextureBindingSlotInfo>();
        private Dictionary<int, OpenGLUniformBinding> _constantBindings = new Dictionary<int, OpenGLUniformBinding>();

        public ShaderResourceDescription[] Resources { get; }

        public OpenGLShaderResourceBindingSlots(OpenGLShaderSet shaderSet, ShaderResourceDescription[] resources)
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
                    if (blockIndex != -1)
                    {
                        ValidateBlockSize(programID, blockIndex, resource.DataSizeInBytes, resource.Name);
                        _constantBindings[i] = new OpenGLUniformBinding(programID, blockIndex, resource.DataSizeInBytes);
                    }
                    else
                    {
                        int uniformLocation = GL.GetUniformLocation(programID, resource.Name);
                        if (uniformLocation == -1)
                        {
                            throw new VeldridException($"No uniform or uniform block with name {resource.Name} was found.");
                        }

                        OpenGLUniformStorageAdapter storageAdapter = new OpenGLUniformStorageAdapter(programID, uniformLocation);
                        _constantBindings[i] = new OpenGLUniformBinding(programID, storageAdapter);
                    }
                }
                else if (resource.Type == ShaderResourceType.Texture )
                {
                    int location = GL.GetUniformLocation(shaderSet.ProgramID, resource.Name);
                    if (location == -1)
                    {
                        throw new VeldridException($"No sampler was found with the name {resource.Name}");
                    }

                    relativeTextureIndex += 1;
                    _textureBindings[i] = new OpenGLTextureBindingSlotInfo() { RelativeIndex = relativeTextureIndex, UniformLocation = location };
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

                    _samplerBindings[i] = new OpenGLTextureBindingSlotInfo() { RelativeIndex = relativeTextureIndex, UniformLocation = lastTextureLocation };
                }
            }

        }

        public OpenGLTextureBindingSlotInfo GetTextureBindingInfo(int slot)
        {
            if (!_textureBindings.TryGetValue(slot, out OpenGLTextureBindingSlotInfo binding))
            {
                throw new VeldridException("There is no texture in slot " + slot);
            }

            return binding;
        }

        public OpenGLTextureBindingSlotInfo GetSamplerBindingInfo(int slot)
        {
            if (!_samplerBindings.TryGetValue(slot, out OpenGLTextureBindingSlotInfo binding))
            {
                throw new VeldridException("There is no sampler in slot " + slot);
            }

            return binding;
        }

        public OpenGLUniformBinding GetUniformBindingForSlot(int slot)
        {
            if (!_constantBindings.TryGetValue(slot, out OpenGLUniformBinding binding))
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

            bool sizeMismatched = (blockSize != providerSize);

            if (sizeMismatched)
            {
                string nameInProgram = GL.GetActiveUniformName(programID, blockIndex);
                bool nameMismatched = nameInProgram != elementName;
                string errorMessage = $"Uniform block validation failed for Program {programID}.";
                if (nameMismatched)
                {
                    errorMessage += Environment.NewLine + $"Expected name: {elementName}, Actual name: {nameInProgram}.";
                }
                if (sizeMismatched)
                {
                    errorMessage += Environment.NewLine + $"Provider size in bytes: {providerSize}, Actual buffer size in bytes: {blockSize}.";
                }

                throw new VeldridException(errorMessage);
            }
        }
    }

    public struct OpenGLTextureBindingSlotInfo
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
