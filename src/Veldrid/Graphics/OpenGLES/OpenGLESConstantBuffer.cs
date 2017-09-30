using System;
using OpenTK.Graphics.ES30;

namespace Veldrid.Graphics.OpenGLES
{
    public class OpenGLESConstantBuffer : OpenGLESBuffer, ConstantBuffer
    {
        public OpenGLESConstantBuffer(int sizeInBytes)
            : base(BufferTarget.UniformBuffer, sizeInBytes, BufferUsageHint.DynamicDraw)
        { }

        internal void BindToBlock(int program, int uniformBlockIndex, int dataSize, int uniformBindingIndex)
        {
            Bind();
            GL.BindBufferRange(BufferRangeTarget.UniformBuffer, uniformBindingIndex, BufferID, IntPtr.Zero, dataSize);
            Utilities.CheckLastGLES3Error();
            GL.UniformBlockBinding(program, uniformBlockIndex, uniformBindingIndex);
            Utilities.CheckLastGLES3Error();
        }
    }
}
