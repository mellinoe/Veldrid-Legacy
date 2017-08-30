using System;
using Veldrid.Graphics;

namespace Veldrid.NeoDemo
{
    public abstract class Renderable : IDisposable
    {
        public abstract void Render(RenderContext rc, SceneContext sc);
        public abstract void CreateDeviceObjects(RenderContext rc);
        public abstract void DestroyDeviceObjects();

        public void Dispose()
        {
            DestroyDeviceObjects();
        }
    }

    public abstract class CullRenderable : Renderable
    {
        public abstract bool Cull(ref BoundingFrustum visibleFrustum);
    }
}
