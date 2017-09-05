using System;
using System.Diagnostics;
using System.Numerics;
using Veldrid.Graphics;
using Veldrid.Platform;

namespace Veldrid.NeoDemo
{
    public class ImGuiRenderable : Renderable, IUpdateable
    {
        private readonly Window _window;

        private ImGuiRenderer _imguiRenderer;

        public ImGuiRenderable(Window window)
        {
            _window = window;
        }

        public override void CreateDeviceObjects(RenderContext rc)
        {
            if (_imguiRenderer == null)
            {
                _imguiRenderer = new ImGuiRenderer(rc, _window);
            }
            else
            {
                _imguiRenderer.SetRenderContext(rc);
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
            _imguiRenderer.Update(_window, deltaSeconds);
            _imguiRenderer.OnInputUpdated(_window, InputTracker.FrameSnapshot);
        }
    }
}
