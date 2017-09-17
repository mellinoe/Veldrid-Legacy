using System;
using OpenTK.Graphics.OpenGL;

namespace Veldrid.Graphics.OpenGL
{
    public class OpenGLVertexBuffer : OpenGLBuffer, VertexBuffer
    {
        public int Stride { get; private set; }

        public OpenGLVertexBuffer(bool isDynamic)
            : base(BufferTarget.ArrayBuffer)
        { }

        public void Apply()
        {
            Bind();
        }

        public void SetVertexData<T>(T[] vertexData, VertexDescriptor descriptor) where T : struct
        {
            Stride = descriptor.VertexSizeInBytes;
            SetData(vertexData);
        }

        public void SetVertexData<T>(T[] vertexData, VertexDescriptor descriptor, int destinationOffsetInVertices) where T : struct
        {
            Stride = descriptor.VertexSizeInBytes;
            SetData(vertexData, descriptor.VertexSizeInBytes * destinationOffsetInVertices);
        }

        public void SetVertexData<T>(ArraySegment<T> vertexData, VertexDescriptor descriptor, int destinationOffsetInVertices) where T : struct
        {
            Stride = descriptor.VertexSizeInBytes;
            SetData(vertexData, descriptor.VertexSizeInBytes * destinationOffsetInVertices);
        }

        public void SetVertexData(IntPtr vertexData, VertexDescriptor descriptor, int numVertices)
        {
            Stride = descriptor.VertexSizeInBytes;
            SetData(vertexData, descriptor.VertexSizeInBytes * numVertices);
        }

        public void SetVertexData(IntPtr vertexData, VertexDescriptor descriptor, int numVertices, int destinationOffsetInVertices)
        {
            Stride = descriptor.VertexSizeInBytes;
            SetData(vertexData, descriptor.VertexSizeInBytes * numVertices, descriptor.VertexSizeInBytes * destinationOffsetInVertices);
        }
    }
}
