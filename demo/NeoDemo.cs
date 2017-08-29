using ImGuiNET;
using System.Collections.Generic;
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
        private readonly List<Renderable> _renderables = new List<Renderable>();
        private readonly List<IUpdateable> _updateables = new List<IUpdateable>();

        public NeoDemo()
        {
            WindowCreateInfo windowCI = new WindowCreateInfo
            {
                WindowWidth = 960,
                WindowHeight = 540,
                WindowInitialState = WindowState.Normal,
                WindowTitle = "Veldrid NeoDemo"
            };
            RenderContextCreateInfo rcCI = new RenderContextCreateInfo();

            VeldridStartup.CreateWindowAndRenderContext(ref windowCI, ref rcCI, out _window, out _rc);

            ImGuiRenderable igRenderable = new ImGuiRenderable(_window);
            igRenderable.CreateDeviceObjects(_rc);
            _renderables.Add(igRenderable);
            _updateables.Add(igRenderable);
        }

        public void Run()
        {
            while (_window.Exists)
            {
                InputTracker.UpdateFrameInput(_window.PumpEvents());
                Update(1f / 60f);
                Draw();
            }
        }

        private void Update(float deltaSeconds)
        {
            foreach (IUpdateable updateable in _updateables)
            {
                updateable.Update(deltaSeconds);
            }

            if (ImGui.BeginMainMenuBar())
            {
                if (ImGui.BeginMenu("Settings"))
                {
                    if (ImGui.BeginMenu("Graphics Backend"))
                    {
                        if (ImGui.MenuItem("Vulkan"))
                        {
                            ChangeRenderContext(GraphicsBackend.Vulkan);
                        }
                        if (ImGui.MenuItem("OpenGL"))
                        {
                            ChangeRenderContext(GraphicsBackend.OpenGL);
                        }
                        if (ImGui.MenuItem("OpenGL ES"))
                        {
                            ChangeRenderContext(GraphicsBackend.OpenGLES);
                        }
                        if (ImGui.MenuItem("Direct3D 11"))
                        {
                            ChangeRenderContext(GraphicsBackend.Direct3D11);
                        }
                        ImGui.EndMenu();
                    }
                    ImGui.EndMenu();
                }
                ImGui.EndMainMenuBar();
            }

            _window.Title = _rc.BackendType.ToString();
        }

        private void Draw()
        {
            _rc.Viewport = new Viewport(0, 0, _window.Width, _window.Height);
            _rc.ClearBuffer(RgbaFloat.Red);

            foreach (Renderable renderable in _renderables)
            {
                renderable.Render(_rc);
            }

            _rc.SwapBuffers();
        }

        private void ChangeRenderContext(GraphicsBackend backend)
        {
            foreach (Renderable renderable in _renderables)
            {
                renderable.DestroyDeviceObjects();
            }

            _rc.Dispose();

            RenderContextCreateInfo rcCI = new RenderContextCreateInfo
            {
                Backend = backend
            };
            _rc = VeldridStartup.CreateRenderContext(ref rcCI, _window);

            foreach (Renderable renderable in _renderables)
            {
                renderable.CreateDeviceObjects(_rc);
            }
        }
    }
}
