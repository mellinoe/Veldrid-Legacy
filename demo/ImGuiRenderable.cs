using System;
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

        public override void Render(RenderContext rc)
        {
            _imguiRenderer.Render(rc);
        }

        public void Update(float deltaSeconds)
        {
            _imguiRenderer.Update(_window, deltaSeconds);
            _imguiRenderer.OnInputUpdated(_window, InputTracker.FrameSnapshot);
        }
    }
}
