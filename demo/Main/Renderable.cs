using System;
using System.Numerics;
using Veldrid.Graphics;

namespace Veldrid.NeoDemo
{
    public abstract class Renderable : IDisposable
    {
        public abstract void Render(RenderContext rc, SceneContext sc, RenderPasses renderPass);
        public abstract void CreateDeviceObjects(RenderContext rc);
        public abstract void DestroyDeviceObjects();
        public abstract RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition);
        public virtual RenderPasses RenderPasses => RenderPasses.Standard;

        public void Dispose()
        {
            DestroyDeviceObjects();
        }
    }

    public abstract class CullRenderable : Renderable
    {
        public abstract bool Cull(ref BoundingFrustum visibleFrustum);
        public abstract BoundingBox BoundingBox { get; }
    }
}
