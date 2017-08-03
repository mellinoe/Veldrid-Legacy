using OpenTK.Graphics.ES30;
using System;
using System.Collections.Generic;

namespace Veldrid.Graphics.OpenGLES
{
    public class OpenGLESShaderSet : ShaderSet
    {
        private readonly Dictionary<int, OpenGLESConstantBuffer> _boundConstantBuffers = new Dictionary<int, OpenGLESConstantBuffer>();
        /// <summary>Maps texture/sampler uniform locations to their curretly bound texture unit.</summary>
        private readonly Dictionary<int, int> _boundUniformLocationSlots = new Dictionary<int, int>();

        public OpenGLESVertexInputLayout InputLayout { get; }

        public Shader VertexShader { get; }

        public Shader TessellationControlShader { get; }

        public Shader TessellationEvaluationShader { get; }

        public Shader GeometryShader => null;

        public Shader FragmentShader { get; }

        public int ProgramID { get; }

        public OpenGLESShaderSet(
            OpenGLESVertexInputLayout inputLayout,
            OpenGLESShader vertexShader,
            OpenGLESShader fragmentShader)
        {
            InputLayout = inputLayout;
            VertexShader = vertexShader;
            FragmentShader = fragmentShader;

            ProgramID = GL.CreateProgram();
            Utilities.CheckLastGLES3Error();
            GL.AttachShader(ProgramID, vertexShader.ShaderID);
            Utilities.CheckLastGLES3Error();
            GL.AttachShader(ProgramID, fragmentShader.ShaderID);
            Utilities.CheckLastGLES3Error();

            int slot = 0;
            foreach (var input in inputLayout.InputDescriptions)
            {
                for (int i = 0; i < input.Elements.Length; i++)
                {
                    GL.BindAttribLocation(ProgramID, slot, input.Elements[i].Name);
                    Utilities.CheckLastGLES3Error();
                    slot += 1;
                }
            }

            GL.LinkProgram(ProgramID);
            Utilities.CheckLastGLES3Error();

            int linkStatus;
            GL.GetProgram(ProgramID, GetProgramParameterName.LinkStatus, out linkStatus);
            Utilities.CheckLastGLES3Error();
            if (linkStatus != 1)
            {
                string log = GL.GetProgramInfoLog(ProgramID);
                Utilities.CheckLastGLES3Error();
                throw new VeldridException($"Error linking GL program: {log}");
            }
        }

        public bool BindConstantBuffer(int slot, int blockLocation, OpenGLESConstantBuffer cb)
        {
            // NOTE: slot == uniformBlockIndex

            if (_boundConstantBuffers.TryGetValue(slot, out OpenGLESConstantBuffer boundCB) && boundCB == cb)
            {
                return false;
            }

            GL.UniformBlockBinding(ProgramID, blockLocation, slot);
            Utilities.CheckLastGLES3Error();
            _boundConstantBuffers[slot] = cb;
            return true;
        }

        public void UpdateTextureUniform(int uniformLocation, int textureUnit)
        {
            if (!_boundUniformLocationSlots.TryGetValue(uniformLocation, out int boundSlot) || boundSlot != textureUnit)
            {
                GL.Uniform1(uniformLocation, textureUnit);
                Utilities.CheckLastGLES3Error();
                _boundUniformLocationSlots[uniformLocation] = textureUnit;
            }
        }

        VertexInputLayout ShaderSet.InputLayout => InputLayout;

        public void Dispose()
        {
            InputLayout.Dispose();
            VertexShader.Dispose();
            FragmentShader.Dispose();
            GL.DeleteProgram(ProgramID);
            Utilities.CheckLastGLES3Error();
        }
    }
}
