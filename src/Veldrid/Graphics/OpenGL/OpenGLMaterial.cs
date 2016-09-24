﻿using OpenTK.Graphics.OpenGL;
using System;
using System.Diagnostics;
using System.Linq;

namespace Veldrid.Graphics.OpenGL
{
    public class OpenGLMaterial : Material, IDisposable
    {
        private readonly OpenGLShader _vertexShader;
        private readonly OpenGLShader _fragmentShader;
        private readonly int _programID;
        private readonly OpenGLVertexInputLayout _inputLayout;
        private readonly GlobalBindingPair[] _globalUniformBindings;
        private readonly UniformBinding[] _perObjectBindings;
        private readonly OpenGLProgramTextureBinding[] _textureBindings;

        private static int s_vertexAttribSlotsBound = 0;

        public OpenGLMaterial(
            OpenGLRenderContext rc,
            OpenGLShader vertexShader,
            OpenGLShader fragmentShader,
            MaterialVertexInput[] vertexInputs,
            MaterialInputs<MaterialGlobalInputElement> globalInputs,
            MaterialInputs<MaterialPerObjectInputElement> perObjectInputs,
            MaterialTextureInputs textureInputs)
        {
            _vertexShader = vertexShader;
            _fragmentShader = fragmentShader;
            _inputLayout = new OpenGLVertexInputLayout(vertexInputs);

            _programID = GL.CreateProgram();
            GL.AttachShader(_programID, _vertexShader.ShaderID);
            GL.AttachShader(_programID, _fragmentShader.ShaderID);

            int slot = 0;
            foreach (var input in vertexInputs)
            {
                for (int i = 0; i < input.Elements.Length; i++)
                {
                    GL.BindAttribLocation(_programID, slot, input.Elements[i].Name);
                    slot += 1;
                }
            }

            GL.LinkProgram(_programID);

            int linkStatus;
            GL.GetProgram(_programID, GetProgramParameterName.LinkStatus, out linkStatus);
            if (linkStatus != 1)
            {
                string log = GL.GetProgramInfoLog(_programID);
                throw new InvalidOperationException($"Error linking GL program: {log}");
            }

            int globalInputsCount = globalInputs.Elements.Length;
            int bindingIndex = 0;
            _globalUniformBindings = new GlobalBindingPair[globalInputsCount];
            for (int i = 0; i < globalInputsCount; i++)
            {
                var element = globalInputs.Elements[i];

                ConstantBufferDataProvider dataProvider = element.UseGlobalNamedBuffer
                    ? rc.GetNamedGlobalBufferProviderPair(element.GlobalProviderName).DataProvider
                    : element.DataProvider;
                int blockIndex = GL.GetUniformBlockIndex(_programID, element.Name);
                if (blockIndex != -1)
                {
                    ValidateBlockSize(_programID, blockIndex, dataProvider.DataSizeInBytes);
                    _globalUniformBindings[i] = new GlobalBindingPair(
                        new UniformBlockBinding(
                            _programID,
                            blockIndex,
                            bindingIndex,
                            new OpenGLConstantBuffer(dataProvider),
                            dataProvider.DataSizeInBytes),
                        dataProvider);
                    bindingIndex += 1;
                }
                else
                {
                    int uniformLocation = GL.GetUniformLocation(_programID, element.Name);
                    if (uniformLocation == -1)
                    {
                        throw new InvalidOperationException($"No uniform or uniform block with name {element.Name} was found.");
                    }

                    _globalUniformBindings[i] = new GlobalBindingPair(
                        new UniformLocationBinding(
                            _programID,
                            uniformLocation),
                        dataProvider);
                }
            }

            int perObjectInputsCount = perObjectInputs.Elements.Length;
            _perObjectBindings = new UniformBinding[perObjectInputsCount];
            for (int i = 0; i < perObjectInputsCount; i++)
            {
                var element = perObjectInputs.Elements[i];

                int blockIndex = GL.GetUniformBlockIndex(_programID, element.Name);
                if (blockIndex != -1)
                {
                    _perObjectBindings[i] = new UniformBlockBinding(
                        _programID,
                        blockIndex,
                        bindingIndex,
                        new OpenGLConstantBuffer(),
                        element.BufferSizeInBytes);
                    bindingIndex += 1;
                }
                else
                {
                    int uniformLocation = GL.GetUniformLocation(_programID, element.Name);
                    if (uniformLocation == -1)
                    {
                        throw new InvalidOperationException($"No uniform or uniform block with name {element.Name} was found.");
                    }

                    _perObjectBindings[i] = new UniformLocationBinding(
                        _programID,
                        uniformLocation);
                }
            }

            _textureBindings = new OpenGLProgramTextureBinding[textureInputs.Elements.Length];
            for (int i = 0; i < textureInputs.Elements.Length; i++)
            {
                var element = textureInputs.Elements[i];
                int location = GL.GetUniformLocation(_programID, element.Name);
                if (location == -1)
                {
                    throw new InvalidOperationException($"No sampler was found with the name {element.Name}");
                }
                OpenGLTexture2D deviceTexture = (OpenGLTexture2D)element.GetDeviceTexture(rc);
                _textureBindings[i] = new OpenGLProgramTextureBinding(location, deviceTexture);
            }
        }

        private OpenGLMaterialVertexInput[] GetInputsBySlot(MaterialVertexInput[] vertexInputs)
        {
            return vertexInputs.Select(mvi => new OpenGLMaterialVertexInput(mvi)).ToArray();
        }

        [Conditional("DEBUG")]
        private void ValidateBlockSize(int programID, int blockIndex, int providerSize)
        {
            int blockSize;
            GL.GetActiveUniformBlock(programID, blockIndex, ActiveUniformBlockParameter.UniformBlockDataSize, out blockSize);

            if (blockSize != providerSize)
            {
                throw new InvalidOperationException(
                    $"Declared shader uniform block size does not match provider's data size. The provider has size {providerSize}, but the buffer has size {blockSize}.");
            }
        }

        public void Apply(VertexBuffer[] vertexBuffers)
        {
            int totalSlotsBound = 0;
            for (int i = 0; i < _inputLayout.VBLayoutsBySlot.Length; i++)
            {
                OpenGLMaterialVertexInput input = _inputLayout.VBLayoutsBySlot[i];
                ((OpenGLVertexBuffer)vertexBuffers[i]).Apply();
                for (int slot = 0; slot < input.Elements.Length; slot++)
                {
                    OpenGLMaterialVertexInputElement element = input.Elements[slot];
                    int actualSlot = totalSlotsBound + slot;
                    GL.EnableVertexAttribArray(actualSlot);
                    GL.VertexAttribPointer(actualSlot, element.ElementCount, element.Type, element.Normalized, input.VertexSizeInBytes, element.Offset);
                    GL.VertexAttribDivisor(actualSlot, element.InstanceStepRate);
                }

                totalSlotsBound += input.Elements.Length;
            }

            for (int extraSlot = totalSlotsBound; extraSlot < s_vertexAttribSlotsBound; extraSlot++)
            {
                GL.DisableVertexAttribArray(extraSlot);
            }

            s_vertexAttribSlotsBound = totalSlotsBound;

            GL.UseProgram(_programID);

            foreach (var globalBinding in _globalUniformBindings)
            {
                globalBinding.Bind();
            }
            foreach (var perObjectBinding in _perObjectBindings)
            {
                perObjectBinding.Bind();
            }

            ApplyDefaultTextureBindings();
        }

        private void ApplyDefaultTextureBindings()
        {
            for (int i = 0; i < _textureBindings.Length; i++)
            {
                var binding = _textureBindings[i];
                if (binding.DeviceTexture != null)
                {
                    GL.ActiveTexture(TextureUnit.Texture0 + i);
                    binding.DeviceTexture.Bind();
                    GL.Uniform1(binding.UniformLocation, i);
                }
            }
        }

        public void ApplyPerObjectInput(ConstantBufferDataProvider dataProvider)
        {
            _perObjectBindings[0].SetData(dataProvider);
        }

        public void ApplyPerObjectInputs(ConstantBufferDataProvider[] dataProviders)
        {
            for (int i = 0; i < dataProviders.Length; i++)
            {
                _perObjectBindings[i].SetData(dataProviders[i]);
            }
        }

        public void UseDefaultTextures()
        {
            ApplyDefaultTextureBindings();
        }

        public void UseTexture(int slot, ShaderTextureBinding binding)
        {
            if (!(binding is OpenGLTextureBinding))
            {
                throw new InvalidOperationException("Illegal binding type.");
            }

            if (binding.BoundTexture != null)
            {
                BindTexture(slot, (OpenGLTexture)binding.BoundTexture);
            }
        }

        public void SetVertexAttributes(int vertexBufferSlot, OpenGLVertexBuffer vb)
        {
            // TODO: Related to OpenGLRenderContext.PlatformSetVertexBuffer()
            // These attributes should be lazily set on a draw call or something.
            if (vertexBufferSlot <= _inputLayout.VBLayoutsBySlot.Length)
            {
                return;
            }

            int baseSlot = GetSlotBaseIndex(vertexBufferSlot);
            OpenGLMaterialVertexInput input = _inputLayout.VBLayoutsBySlot[vertexBufferSlot];
            vb.Apply();
            for (int i = 0; i < input.Elements.Length; i++)
            {
                OpenGLMaterialVertexInputElement element = input.Elements[i];
                int slot = baseSlot + i;
                GL.EnableVertexAttribArray(slot);
                GL.VertexAttribPointer(slot, element.ElementCount, element.Type, element.Normalized, input.VertexSizeInBytes, element.Offset);
            }
        }

        private int GetSlotBaseIndex(int vertexBufferSlot)
        {
            int index = 0;
            for (int i = 0; i < vertexBufferSlot; i++)
            {
                index += _inputLayout.VBLayoutsBySlot[i].Elements.Length;
            }

            return index;
        }

        private void BindTexture(int slot, OpenGLTexture texture)
        {
            GL.ActiveTexture(TextureUnit.Texture0 + slot);
            texture.Bind();
            GL.Uniform1(GetTextureUniformLocation(slot), slot);
        }

        private int GetTextureUniformLocation(int slot)
        {
            if (_textureBindings.Length <= slot)
            {
                throw new InvalidOperationException("Illegal slot value. There are only  " + _textureBindings.Length + " texture bindings.");
            }

            return _textureBindings[slot].UniformLocation;
        }

        public void Dispose()
        {
            GL.DeleteProgram(_programID);
            _vertexShader.Dispose();
            _fragmentShader.Dispose();

            foreach (var textureBinding in _textureBindings)
            {
                textureBinding.DeviceTexture?.Dispose();
            }

            foreach (var globalBinding in _globalUniformBindings)
            {
                globalBinding.Binding.Dispose();
            }

            foreach (var perObjectbinding in _perObjectBindings)
            {
                perObjectbinding.Dispose();
            }
        }

        private abstract class UniformBinding : IDisposable
        {
            public int ProgramID { get; }

            public UniformBinding(int programID)
            {
                ProgramID = programID;
            }

            public abstract void Bind();
            public abstract void SetData(ConstantBufferDataProvider dataProvider);
            public abstract void Dispose();
        }

        [DebuggerDisplay("Prog:{ProgramID} BlockInd:{BlockIndex} BindingInd:{BindingIndex}")]
        private class UniformBlockBinding : UniformBinding
        {
            private readonly int _dataSizeInBytes;

            public int BlockIndex { get; }
            public int BindingIndex { get; }
            public OpenGLConstantBuffer ConstantBuffer { get; }

            public UniformBlockBinding(
                int programID,
                int blockIndex,
                int bindingIndex,
                OpenGLConstantBuffer constantBuffer,
                int dataSizeInBytes)
                : base(programID)
            {
                _dataSizeInBytes = dataSizeInBytes;
                BlockIndex = blockIndex;
                BindingIndex = bindingIndex;
                ConstantBuffer = constantBuffer;
            }

            public override void Bind()
            {
                ConstantBuffer.BindToBlock(ProgramID, BlockIndex, _dataSizeInBytes, BindingIndex);
            }

            public override void SetData(ConstantBufferDataProvider dataProvider)
            {
                dataProvider.SetData(ConstantBuffer);
            }

            public override void Dispose()
            {
                ConstantBuffer.Dispose();
            }
        }

        private class UniformLocationBinding : UniformBinding
        {
            public OpenGLUniformStorageAdapter StorageAdapter { get; }

            public UniformLocationBinding(
                int programID,
                int uniformLocation) : base(programID)
            {
                StorageAdapter = new OpenGLUniformStorageAdapter(ProgramID, uniformLocation);
            }

            public override void Bind()
            {
            }

            public override void SetData(ConstantBufferDataProvider dataProvider)
            {
                dataProvider.SetData(StorageAdapter);
            }

            public override void Dispose()
            {
            }
        }

        private struct GlobalBindingPair
        {
            public UniformBinding Binding { get; }
            public ConstantBufferDataProvider DataProvider { get; }

            public GlobalBindingPair(UniformBinding binding, ConstantBufferDataProvider dataProvider)
            {
                Binding = binding;
                DataProvider = dataProvider;
            }

            public void Bind()
            {
                Binding.Bind();
                Binding.SetData(DataProvider);
            }
        }

        private struct OpenGLProgramTextureBinding
        {
            public readonly int UniformLocation;
            public readonly OpenGLTexture DeviceTexture;

            public OpenGLProgramTextureBinding(int location, OpenGLTexture deviceTexture)
            {
                UniformLocation = location;
                DeviceTexture = deviceTexture;
            }
        }
    }
}
