using System;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using Veldrid.Graphics;
using Veldrid.Platform;
using Veldrid.RenderDemo;
using Veldrid.Sdl2;

namespace BasicDemo
{
    public class BasicDemoApp
    {
        private readonly Sdl2Window _window;
        private readonly RenderContext _rc;
        private double _desiredFrameTimeSeconds = 1.0 / 60.0;
        private bool _running = true;
        private bool _windowResized = false;

        // Device objects
        private VertexBuffer _vb;
        private IndexBuffer _ib;
        private ShaderSet _shaderSet;
        private ShaderResourceBindingSlots _resourceBindings;
        private ConstantBuffer _worldBuffer;
        private ConstantBuffer _viewBuffer;
        private ConstantBuffer _projectionBuffer;
        private DeviceTexture2D _deviceTexture;
        private ShaderTextureBinding _textureBinding;

        public BasicDemoApp(Sdl2Window window, RenderContext rc)
        {
            _window = window;
            _rc = rc;

            _window.Closed += () => _running = false;
            _window.Resized += () => _windowResized = true;

            ResourceFactory factory = _rc.ResourceFactory;
            _vb = factory.CreateVertexBuffer(s_cubeVertices, new VertexDescriptor(VertexPositionTexture.SizeInBytes, VertexPositionTexture.ElementCount), false);
            _ib = factory.CreateIndexBuffer(s_cubeIndices, false);

            Shader vertexShader = factory.CreateShader(ShaderStages.Vertex, factory.LoadProcessedShader(GetShaderBytecode(factory.BackendType, true)));
            Shader fragmentShader = factory.CreateShader(ShaderStages.Fragment, factory.LoadProcessedShader(GetShaderBytecode(factory.BackendType, false)));
            VertexInputLayout inputLayout = factory.CreateInputLayout(
                new VertexInputElement("vsin_position", VertexSemanticType.Position, VertexElementFormat.Float3),
                new VertexInputElement("vsin_texCoord", VertexSemanticType.TextureCoordinate, VertexElementFormat.Float2));
            _shaderSet = factory.CreateShaderSet(inputLayout, vertexShader, fragmentShader);
            _resourceBindings = factory.CreateShaderResourceBindingSlots(
                _shaderSet,
                new ShaderResourceDescription("WorldViewProjectionBuffer", ShaderConstantType.Matrix4x4),
                new ShaderResourceDescription("SurfaceTexture", ShaderResourceType.Texture, ShaderStages.Fragment),
                new ShaderResourceDescription("Sampler", ShaderResourceType.Sampler, ShaderStages.Fragment));
            _worldBuffer = factory.CreateConstantBuffer(ShaderConstantType.Matrix4x4);
            _viewBuffer = factory.CreateConstantBuffer(ShaderConstantType.Matrix4x4);
            _projectionBuffer = factory.CreateConstantBuffer(ShaderConstantType.Matrix4x4);
            TextureData textureData = new ImageSharpMipmapChain(
                Path.Combine(AppContext.BaseDirectory, "Textures", "Sponza_Bricks.png"));
            _deviceTexture = textureData.CreateDeviceTexture(factory);
            _textureBinding = factory.CreateShaderTextureBinding(_deviceTexture);

            _worldBuffer.SetData(Matrix4x4.Identity);
            _viewBuffer.SetData(Matrix4x4.CreateLookAt(new Vector3(0, 0, -5), Vector3.Zero, Vector3.UnitY));
        }

        public void Run()
        {
            Stopwatch sw = Stopwatch.StartNew();
            long previousFrameTicks = 0;
            long currentFrameTicks;
            long swFrequency = Stopwatch.Frequency;
            long desiredFramerateTicks = (long)(_desiredFrameTimeSeconds * swFrequency);
            while (_running)
            {
                while ((currentFrameTicks = sw.ElapsedTicks) < desiredFramerateTicks)
                { }
                double elapsed = (currentFrameTicks - previousFrameTicks) * swFrequency;

                Update(elapsed);
                Draw();

                previousFrameTicks = currentFrameTicks;
            }
        }

        public void Update(double deltaSeconds)
        {
            // Poll input
            InputSnapshot snapshot = _window.PumpEvents();
            InputTracker.UpdateFrameInput(snapshot);
            float circleWidth = 4f;
            float timeFactor = (float)DateTime.UtcNow.TimeOfDay.TotalMilliseconds / 1000;
            Vector3 position = new Vector3(
                (float)(Math.Cos(timeFactor) * circleWidth),
                3 + (float)Math.Sin(timeFactor) * 2,
                (float)(Math.Sin(timeFactor) * circleWidth));
            Vector3 lookDirection = -position;
            _viewMatrix = Matrix4x4.CreateLookAt(position, position + lookDirection, Vector3.UnitY);
            _viewBuffer.SetData(_viewMatrix);
        }

        public void Draw()
        {
            if (_windowResized)
            {
                _windowResized = false;
                _rc.ResizeMainWindow(_window.Width, _window.Height);
            }

            Matrix4x4 proj = Matrix4x4.CreatePerspectiveFieldOfView(0.5f, _window.Width / (float)_window.Height, 1f, 50f);
            _projectionBuffer.SetData(ref proj);

            _rc.Viewport = new Viewport(0, 0, _window.Width, _window.Height);
            RgbaFloat clearColor = new RgbaFloat(
                (float)Math.Sin(Environment.TickCount / 2000.0f),
                (float)Math.Cos(Environment.TickCount / 3000.0f),
                (float)Math.Sin(Environment.TickCount / 1000.0f),
                1f);
            _rc.ClearBuffer(clearColor);

            var worldViewProjection =
                Matrix4x4.Identity // World
                * _viewMatrix // View
                * proj; // Projection

            _worldBuffer.SetData(ref worldViewProjection);

            _rc.VertexBuffer = _vb;
            _rc.IndexBuffer = _ib;
            _rc.ShaderSet = _shaderSet;
            _rc.ShaderResourceBindingSlots = _resourceBindings;
            _rc.SetConstantBuffer(0, _worldBuffer);
            _rc.SetTexture(1, _textureBinding);
            _rc.SetSamplerState(2, _rc.LinearSampler);
            _rc.DrawIndexedPrimitives(s_cubeIndices.Length);

            _rc.SwapBuffers();
        }

        public byte[] GetShaderBytecode(GraphicsBackend backend, bool vertexShader)
        {
            string name = vertexShader ? "simple-vertex" : "simple-frag";
            switch (backend)
            {
                case GraphicsBackend.Vulkan:
                    {
                        name += ".spv";
                        string path = Path.Combine(AppContext.BaseDirectory, "Shaders", "SPIR-V", name);
                        return File.ReadAllBytes(path);
                    }
                case GraphicsBackend.Direct3D11:
                    {
                        name += ".hlsl";
                        string path = Path.Combine(AppContext.BaseDirectory, "Shaders", "HLSL", name);
                        string text = File.ReadAllText(path);
                        CompiledShaderCode bytecode = _rc.ResourceFactory.ProcessShaderCode(vertexShader ? ShaderStages.Vertex : ShaderStages.Fragment, text);
                        return ((Veldrid.Graphics.Direct3D.D3DShaderBytecode)bytecode).Bytecode.Data; // wtf
                    }
                case GraphicsBackend.OpenGL:
                case GraphicsBackend.OpenGLES:
                    {
                        name += ".glsl";
                        string path = Path.Combine(AppContext.BaseDirectory, "Shaders", "GLSL", name);
                        return File.ReadAllBytes(path);
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        public static readonly VertexPositionTexture[] s_cubeVertices = new VertexPositionTexture[]
        {
            // Top
            new VertexPositionTexture(new Vector3(-.5f,.5f,-.5f),     new Vector2(0, 0)),
            new VertexPositionTexture(new Vector3(.5f,.5f,-.5f),      new Vector2(1, 0)),
            new VertexPositionTexture(new Vector3(.5f,.5f,.5f),       new Vector2(1, 1)),
            new VertexPositionTexture(new Vector3(-.5f,.5f,.5f),      new Vector2(0, 1)),
            // Bottom                                                 
            new VertexPositionTexture(new Vector3(-.5f,-.5f,.5f),     new Vector2(0, 0)),
            new VertexPositionTexture(new Vector3(.5f,-.5f,.5f),      new Vector2(1, 0)),
            new VertexPositionTexture(new Vector3(.5f,-.5f,-.5f),     new Vector2(1, 1)),
            new VertexPositionTexture(new Vector3(-.5f,-.5f,-.5f),    new Vector2(0, 1)),
            // Left                                                   
            new VertexPositionTexture(new Vector3(-.5f,.5f,-.5f),     new Vector2(0, 0)),
            new VertexPositionTexture(new Vector3(-.5f,.5f,.5f),      new Vector2(1, 0)),
            new VertexPositionTexture(new Vector3(-.5f,-.5f,.5f),     new Vector2(1, 1)),
            new VertexPositionTexture(new Vector3(-.5f,-.5f,-.5f),    new Vector2(0, 1)),
            // Right                                                  
            new VertexPositionTexture(new Vector3(.5f,.5f,.5f),       new Vector2(0, 0)),
            new VertexPositionTexture(new Vector3(.5f,.5f,-.5f),      new Vector2(1, 0)),
            new VertexPositionTexture(new Vector3(.5f,-.5f,-.5f),     new Vector2(1, 1)),
            new VertexPositionTexture(new Vector3(.5f,-.5f,.5f),      new Vector2(0, 1)),
            // Back                                                   
            new VertexPositionTexture(new Vector3(.5f,.5f,-.5f),      new Vector2(0, 0)),
            new VertexPositionTexture(new Vector3(-.5f,.5f,-.5f),     new Vector2(1, 0)),
            new VertexPositionTexture(new Vector3(-.5f,-.5f,-.5f),    new Vector2(1, 1)),
            new VertexPositionTexture(new Vector3(.5f,-.5f,-.5f),     new Vector2(0, 1)),
            // Front                                                  
            new VertexPositionTexture(new Vector3(-.5f,.5f,.5f),      new Vector2(0, 0)),
            new VertexPositionTexture(new Vector3(.5f,.5f,.5f),       new Vector2(1, 0)),
            new VertexPositionTexture(new Vector3(.5f,-.5f,.5f),      new Vector2(1, 1)),
            new VertexPositionTexture(new Vector3(-.5f,-.5f,.5f),     new Vector2(0, 1)),
        };

        private static readonly ushort[] s_cubeIndices = new ushort[]
        {
            0,1,2, 0,2,3,
            4,5,6, 4,6,7,
            8,9,10, 8,10,11,
            12,13,14, 12,14,15,
            16,17,18, 16,18,19,
            20,21,22, 20,22,23,
        };
        private Matrix4x4 _viewMatrix;
    }
}
