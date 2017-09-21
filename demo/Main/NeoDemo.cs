using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using Veldrid.Graphics;
using Veldrid.NeoDemo.Objects;
using Veldrid.Platform;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace Veldrid.NeoDemo
{
    public class NeoDemo
    {
        private Sdl2Window _window;
        private RenderContext _rc;
        private Scene _scene;
        private readonly ImGuiRenderable _igRenderable;
        private readonly SceneContext _sc = new SceneContext();
        private bool _windowResized;
        private RenderOrderKeyComparer _renderOrderKeyComparer = new RenderOrderKeyComparer();

        private event Action<int, int> _resizeHandled;

        public NeoDemo()
        {
            WindowCreateInfo windowCI = new WindowCreateInfo
            {
                X = 50,
                Y = 50,
                WindowWidth = 960,
                WindowHeight = 540,
                WindowInitialState = WindowState.Normal,
                WindowTitle = "Veldrid NeoDemo"
            };
            RenderContextCreateInfo rcCI = new RenderContextCreateInfo();
            rcCI.Backend = GraphicsBackend.Direct3D11; // TODO REMOVE

            VeldridStartup.CreateWindowAndRenderContext(ref windowCI, ref rcCI, out _window, out _rc);
            _window.Resized += () => _windowResized = true;

            _sc.CreateDeviceObjects(_rc);

            _scene = new Scene(_window.Width, _window.Height);

            _sc.SetCurrentScene(_scene);

            _igRenderable = new ImGuiRenderable(_window.Width, _window.Height);
            _resizeHandled += (w, h) => _igRenderable.WindowResized(w, h);
            _igRenderable.CreateDeviceObjects(_rc);
            _scene.AddRenderable(_igRenderable);
            _scene.AddUpdateable(_igRenderable);

            InfiniteGrid grid = new InfiniteGrid();
            grid.CreateDeviceObjects(_rc);
            _scene.AddRenderable(grid);

            Skybox skybox = Skybox.LoadDefaultSkybox();
            skybox.CreateDeviceObjects(_rc);
            _scene.AddRenderable(skybox);

            ImageSharpTexture texData = new ImageSharpTexture(AssetHelper.GetPath("Textures/cloudtop/cloudtop_up.png"));
            TexturedMesh cube = new TexturedMesh(
                new SimpleMeshDataProvider(CubeModel.Vertices, CubeModel.Indices),
                texData);
            cube.Transform.Scale = new Vector3(10);
            cube.CreateDeviceObjects(_rc);
            _scene.AddRenderable(cube);
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
            _scene.Update(deltaSeconds);

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
            int width = _window.Width;
            int height = _window.Height;

            if (_windowResized)
            {
                _windowResized = false;
                _rc.ResizeMainWindow(width, height);
                _scene.Camera.WindowResized(width, height);
                _resizeHandled?.Invoke(width, height);
            }

            _rc.Viewport = new Viewport(0, 0, width, height);
            _rc.ClearBuffer(RgbaFloat.CornflowerBlue);

            _scene.Render(_rc, _sc, RenderPasses.Standard, null);
            _scene.Render(_rc, _sc, RenderPasses.AlphaBlend, null);
            _scene.Render(_rc, _sc, RenderPasses.Overlay, null);

            _rc.SwapBuffers();
        }

        private void ChangeRenderContext(GraphicsBackend backend)
        {
            _sc.DestroyDeviceObjects();
            _scene.DestroyAllDeviceObjects();

            _rc.Dispose();

            WindowCreateInfo windowCI = new WindowCreateInfo
            {
                X = _window.X,
                Y  = _window.Y,
                WindowWidth = _window.Width,
                WindowHeight = _window.Height,
                WindowInitialState = _window.WindowState,
                WindowTitle = "Veldrid NeoDemo"
            };

            _window.Close();

            RenderContextCreateInfo rcCI = new RenderContextCreateInfo
            {
                Backend = backend
            };
            VeldridStartup.CreateWindowAndRenderContext(ref windowCI, ref rcCI, out _window, out _rc);
            _window.Resized += () => _windowResized = true;

            _sc.CreateDeviceObjects(_rc);
            _scene.CreateAllDeviceObjects(_rc);
        }
    }
}
