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

        public BasicDemoApp(Sdl2Window window, RenderContext rc)
        {
            _window = window;
            _rc = rc;

            _window.Closed += () => _running = false;
            _window.Resized += () => _windowResized = true;

            ResourceFactory factory = _rc.ResourceFactory;
            _vb = factory.CreateVertexBuffer(s_cubeVertices, new VertexDescriptor(VertexPositionColor.SizeInBytes, VertexPositionColor.ElementCount), false);
            _ib = factory.CreateIndexBuffer(s_cubeIndices, false);

            Shader vertexShader = factory.CreateShader(ShaderStages.Vertex, factory.LoadProcessedShader(GetShaderBytecode(factory.BackendType, true)));
            Shader fragmentShader = factory.CreateShader(ShaderStages.Fragment, factory.LoadProcessedShader(GetShaderBytecode(factory.BackendType, false)));
            VertexInputLayout inputLayout = factory.CreateInputLayout(
                new VertexInputElement("vsin_position", VertexSemanticType.Position, VertexElementFormat.Float3),
                new VertexInputElement("vsin_color", VertexSemanticType.Color, VertexElementFormat.Float4));
            _shaderSet = factory.CreateShaderSet(inputLayout, vertexShader, fragmentShader);
            _resourceBindings = factory.CreateShaderResourceBindingSlots(
                _shaderSet,
                new ShaderResourceDescription("WorldBuffer", ShaderConstantType.Matrix4x4),
                new ShaderResourceDescription("ViewBuffer", ShaderConstantType.Matrix4x4),
                new ShaderResourceDescription("ProjectionBuffer", ShaderConstantType.Matrix4x4));
            _worldBuffer = factory.CreateConstantBuffer(ShaderConstantType.Matrix4x4);
            _viewBuffer = factory.CreateConstantBuffer(ShaderConstantType.Matrix4x4);
            _projectionBuffer = factory.CreateConstantBuffer(ShaderConstantType.Matrix4x4);

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
        }

        public void Draw()
        {
            if (_windowResized)
            {
                _windowResized = false;
                _rc.ResizeMainWindow(_window.Width, _window.Height);
            }

            _projectionBuffer.SetData(Matrix4x4.CreatePerspectiveFieldOfView(1f, _window.Width / (float)_window.Height, 1f, 50f));

            _rc.Viewport = new Viewport(0, 0, _window.Width, _window.Height);
            _rc.ClearBuffer(new RgbaFloat((Environment.TickCount / 10000.0f) % 1.0f, (Environment.TickCount / 30000.0f) % 1.0f, (Environment.TickCount / 1000.0f) % 1.0f, 1f));

            _rc.VertexBuffer = _vb;
            _rc.IndexBuffer = _ib;
            _rc.ShaderSet = _shaderSet;
            _rc.ShaderResourceBindingSlots = _resourceBindings;
            _rc.SetConstantBuffer(0, _worldBuffer);
            _rc.SetConstantBuffer(1, _viewBuffer);
            _rc.SetConstantBuffer(2, _projectionBuffer);
            _rc.DrawIndexedPrimitives(s_cubeIndices.Length);

            _rc.SwapBuffers();
        }

        public byte[] GetShaderBytecode(GraphicsBackend backend, bool vertexShader)
        {
            switch (backend)
            {
                case GraphicsBackend.Vulkan:
                    string name = vertexShader ? "simple.vert.spv" : "simple.frag.spv";
                    string path = Path.Combine(AppContext.BaseDirectory, "Shaders", "SPIR-V", name);
                    return File.ReadAllBytes(path);
                default:
                    throw new NotImplementedException();
            }
        }

        private static readonly VertexPositionColor[] s_cubeVertices = new VertexPositionColor[]
        {
            // Top
            new VertexPositionColor(new Vector3(-.5f,.5f,-.5f),    RgbaFloat.Red),
            new VertexPositionColor(new Vector3(.5f,.5f,-.5f),     RgbaFloat.Red),
            new VertexPositionColor(new Vector3(.5f,.5f,.5f),      RgbaFloat.Red),
            new VertexPositionColor(new Vector3(-.5f,.5f,.5f),     RgbaFloat.Red),
            // Bottom
            new VertexPositionColor(new Vector3(-.5f,-.5f,.5f),    RgbaFloat.Grey),
            new VertexPositionColor(new Vector3(.5f,-.5f,.5f),     RgbaFloat.Grey),
            new VertexPositionColor(new Vector3(.5f,-.5f,-.5f),    RgbaFloat.Grey),
            new VertexPositionColor(new Vector3(-.5f,-.5f,-.5f),   RgbaFloat.Grey),
            // Left
            new VertexPositionColor(new Vector3(-.5f,.5f,-.5f),    RgbaFloat.Blue),
            new VertexPositionColor(new Vector3(-.5f,.5f,.5f),     RgbaFloat.Blue),
            new VertexPositionColor(new Vector3(-.5f,-.5f,.5f),    RgbaFloat.Blue),
            new VertexPositionColor(new Vector3(-.5f,-.5f,-.5f),   RgbaFloat.Blue),
            // Right
            new VertexPositionColor(new Vector3(.5f,.5f,.5f),      RgbaFloat.White),
            new VertexPositionColor(new Vector3(.5f,.5f,-.5f),     RgbaFloat.White),
            new VertexPositionColor(new Vector3(.5f,-.5f,-.5f),    RgbaFloat.White),
            new VertexPositionColor(new Vector3(.5f,-.5f,.5f),     RgbaFloat.White),
            // Back
            new VertexPositionColor(new Vector3(.5f,.5f,-.5f),     RgbaFloat.Yellow),
            new VertexPositionColor(new Vector3(-.5f,.5f,-.5f),    RgbaFloat.Yellow),
            new VertexPositionColor(new Vector3(-.5f,-.5f,-.5f),   RgbaFloat.Yellow),
            new VertexPositionColor(new Vector3(.5f,-.5f,-.5f),    RgbaFloat.Yellow),
            // Front
            new VertexPositionColor(new Vector3(-.5f,.5f,.5f),     RgbaFloat.Green),
            new VertexPositionColor(new Vector3(.5f,.5f,.5f),      RgbaFloat.Green),
            new VertexPositionColor(new Vector3(.5f,-.5f,.5f),     RgbaFloat.Green),
            new VertexPositionColor(new Vector3(-.5f,-.5f,.5f),    RgbaFloat.Green)
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
    }
}
