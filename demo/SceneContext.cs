using System;
using System.Collections.Generic;
using System.Text;
using Veldrid.Graphics;

namespace Veldrid.NeoDemo
{
    public class SceneContext
    {
        public ConstantBuffer ProjectionMatrixBuffer { get; private set; }
        public ConstantBuffer ViewMatrixBuffer { get; private set; }
        public Camera Camera { get; set; }

        public SceneContext()
        {
        }

        public void CreateDeviceObjects(RenderContext rc)
        {
            ProjectionMatrixBuffer = rc.ResourceFactory.CreateConstantBuffer(ShaderConstantType.Matrix4x4);
            ViewMatrixBuffer = rc.ResourceFactory.CreateConstantBuffer(ShaderConstantType.Matrix4x4);
        }

        public void DestroyDeviceObjects()
        {
            ProjectionMatrixBuffer.Dispose();
            ViewMatrixBuffer.Dispose();
        }
    }
}
