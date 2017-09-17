using System;
using System.Diagnostics;
using System.Numerics;
using Veldrid.Graphics;
using Veldrid.Platform;

namespace Veldrid.NeoDemo
{
    public class ImGuiRenderable : Renderable, IUpdateable
    {
        private ImGuiRenderer _imguiRenderer;
        private int _width;
        private int _height;

        public ImGuiRenderable(int width, int height)
        {
            _width = width;
            _height = height;
        }

        public void WindowResized(int width, int height) => _imguiRenderer.WindowResized(width, height);

        public override void CreateDeviceObjects(RenderContext rc)
        {
            if (_imguiRenderer == null)
            {
                _imguiRenderer = new ImGuiRenderer(rc, _width, _height);
            }
            else
            {
                _imguiRenderer.CreateDeviceResources(rc);
            }
        }

        public override void DestroyDeviceObjects()
        {
            _imguiRenderer.Dispose();
        }

        public override RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition)
        {
            return new RenderOrderKey();
        }

        public override void Render(RenderContext rc, SceneContext sc, RenderPasses renderPass)
        {
            Debug.Assert(renderPass == RenderPasses.Overlay);
            _imguiRenderer.Render(rc);
        }

        public override RenderPasses RenderPasses => RenderPasses.Overlay;

        public void Update(float deltaSeconds)
        {
            _imguiRenderer.Update(deltaSeconds);
            _imguiRenderer.OnInputUpdated(InputTracker.FrameSnapshot);
        }
    }
}
