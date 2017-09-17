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
            if (Camera != null)
            {
                ProjectionMatrixBuffer.SetData(Camera.ProjectionMatrix);
                ViewMatrixBuffer.SetData(Camera.ViewMatrix);
            }
        }

        public void DestroyDeviceObjects()
        {
            ProjectionMatrixBuffer.Dispose();
            ViewMatrixBuffer.Dispose();
        }

        public void SetCurrentScene(Scene scene)
        {
            Camera = scene.Camera;
            scene.Camera.ViewChanged += view => ViewMatrixBuffer.SetData(ref view);
            ViewMatrixBuffer.SetData(scene.Camera.ViewMatrix);
            scene.Camera.ProjectionChanged += proj => ProjectionMatrixBuffer.SetData(ref proj);
            ProjectionMatrixBuffer.SetData(scene.Camera.ProjectionMatrix);
        }
    }
}
