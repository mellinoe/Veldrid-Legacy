using System;
using Veldrid.Graphics;
using Veldrid.Platform;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace Veldrid.NeoDemo
{
    public class NeoDemo
    {
        private Sdl2Window _window;
        private RenderContext _rc;

        public NeoDemo()
        {
            WindowCreateInfo windowCI = new WindowCreateInfo
            {
                WindowWidth = 960,
                WindowHeight = 540,
                WindowInitialState = Platform.WindowState.Normal,
                WindowTitle = "Veldrid NeoDemo"
            };

            RenderContextCreateInfo rcCI = new RenderContextCreateInfo();

            VeldridStartup.CreateWindowAndRenderContext(ref windowCI, ref rcCI, out _window, out _rc);
        }

        public void Run()
        {
            while (_window.Exists)
            {
                InputTracker.UpdateFrameInput(_window.PumpEvents());
                Update();
                Draw();
            }
        }

        private void Update()
        {
            if (InputTracker.GetKeyDown(Key.V))
            {
                ChangeRenderContext(GraphicsBackend.Vulkan);
            }
            else if (InputTracker.GetKeyDown(Key.E))
            {
                ChangeRenderContext(GraphicsBackend.OpenGLES);
            }
            else if (InputTracker.GetKeyDown(Key.O))
            {
                ChangeRenderContext(GraphicsBackend.OpenGL);
            }
            else if (InputTracker.GetKeyDown(Key.D))
            {
                ChangeRenderContext(GraphicsBackend.Direct3D11);
            }

            _window.Title = _rc.BackendType.ToString();
        }

        private void Draw()
        {
            _rc.Viewport = new Viewport(0, 0, _window.Width, _window.Height);
            _rc.ClearBuffer(RgbaFloat.Red);
            _rc.SwapBuffers();
        }

        private void ChangeRenderContext(GraphicsBackend backend)
        {
            _rc.Dispose();
            RenderContextCreateInfo rcCI = new RenderContextCreateInfo
            {
                Backend = backend
            };
            _rc = VeldridStartup.CreateRenderContext(ref rcCI, _window);
        }
    }
}
