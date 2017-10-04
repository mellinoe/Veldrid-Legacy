﻿using ImGuiNET;
using System;
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

            AddTexturedMesh(
                "Textures/spnza_bricks_a_diff.png", 
                PrimitiveShapes.Box(10, 10, 10, 10),
                new Vector3(0, 0, -5),
                Quaternion.Identity,
                Vector3.One);

            AddTexturedMesh(
                "Textures/spnza_bricks_a_diff.png",
                PrimitiveShapes.Box(5, 5, 5, 5f),
                new Vector3(-3, -9, 2),
                Quaternion.Identity,
                Vector3.One);

            AddTexturedMesh(
                "Textures/spnza_bricks_a_diff.png",
                PrimitiveShapes.Box(27, 3, 27, 27f),
                new Vector3(-5, -16, 5),
                Quaternion.Identity,
                Vector3.One);

            AddTexturedMesh(
                "Textures/spnza_bricks_a_diff.png",
                PrimitiveShapes.Plane(100, 100, 5),
                new Vector3(0, -20, 0),
                Quaternion.Identity,
                Vector3.One);

            ShadowmapDrawer texDrawer = new ShadowmapDrawer(_window);
            texDrawer.CreateDeviceObjects(_rc);
            texDrawer.Position = new Vector2(10, 80);
            _scene.AddRenderable(texDrawer);
        }

        private void AddTexturedMesh(string texPath, MeshData meshData, Vector3 position, Quaternion rotation, Vector3 scale)
        {
            ImageSharpMipmapChain texData = new ImageSharpMipmapChain(AssetHelper.GetPath(texPath));
            TexturedMesh mesh = new TexturedMesh(meshData, texData);
            mesh.Transform.Position = position;
            mesh.Transform.Rotation = rotation;
            mesh.Transform.Scale = scale;
            mesh.CreateDeviceObjects(_rc);
            _scene.AddRenderable(mesh);
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
                    bool isFullscreen = _window.WindowState == WindowState.BorderlessFullScreen;
                    if (ImGui.MenuItem("Fullscreen", "F11", isFullscreen, true))
                    {
                        ToggleFullscreenState();
                    }

                    ImGui.EndMenu();
                }
                ImGui.EndMainMenuBar();
            }

            if (InputTracker.GetKeyDown(Key.F11))
            {
                ToggleFullscreenState();
            }

            _window.Title = _rc.BackendType.ToString();
        }

        private void ToggleFullscreenState()
        {
            bool isFullscreen = _window.WindowState == WindowState.BorderlessFullScreen;
            _window.WindowState = isFullscreen ? WindowState.Normal : WindowState.BorderlessFullScreen;
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

            _scene.RenderAllStages(_rc, _sc);

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
                Y = _window.Y,
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
