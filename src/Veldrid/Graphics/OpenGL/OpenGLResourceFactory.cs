using OpenTK.Graphics.OpenGL;
using System;
using System.IO;
using System.Threading.Tasks;
using System.Threading;
using OpenTK;
using OpenTK.Graphics;
using System.Collections.Concurrent;
using System.Collections.Generic;

namespace Veldrid.Graphics.OpenGL
{
    public class OpenGLResourceFactory : ResourceFactory
    {
        private static readonly string s_shaderDirectory = "GLSL";
        private static readonly string s_shaderFileExtension = "glsl";

        private readonly OpenGLResourceTaskScheduler _taskScheduler;

        public OpenGLResourceFactory(GraphicsMode mode, int numResourceThreads, int mainThreadID)
        {
            _taskScheduler = new OpenGLResourceTaskScheduler(mode, numResourceThreads, mainThreadID);
        }

        public override ConstantBuffer CreateConstantBuffer(int sizeInBytes)
        {
            return new OpenGLConstantBuffer();
        }

        public override Framebuffer CreateFramebuffer()
        {
            return new OpenGLFramebuffer();
        }

        public override Framebuffer CreateFramebuffer(int width, int height)
        {
            OpenGLTexture2D colorTexture = new OpenGLTexture2D(
                width, height,
                PixelInternalFormat.Rgba32f,
                OpenTK.Graphics.OpenGL.PixelFormat.Rgba,
                PixelType.Float);
            OpenGLTexture2D depthTexture = new OpenGLTexture2D(
                width,
                height,
                PixelInternalFormat.DepthComponent16,
                OpenTK.Graphics.OpenGL.PixelFormat.DepthComponent,
                PixelType.UnsignedShort);

            return new OpenGLFramebuffer(colorTexture, depthTexture);
        }

        public override IndexBuffer CreateIndexBuffer(int sizeInBytes, bool isDynamic)
        {
            return new OpenGLIndexBuffer(isDynamic);
        }
        public override IndexBuffer CreateIndexBuffer(int sizeInBytes, bool isDynamic, IndexFormat format)
        {
            return new OpenGLIndexBuffer(isDynamic, OpenGLFormats.MapIndexFormat(format));
        }

        public override Material CreateMaterial(
            RenderContext rc,
            string vertexShaderName,
            string pixelShaderName,
            MaterialVertexInput inputs,
            MaterialInputs<MaterialGlobalInputElement> globalInputs,
            MaterialInputs<MaterialPerObjectInputElement> perObjectInputs,
            MaterialTextureInputs textureInputs)
        {
            string vertexShaderPath = GetShaderPathFromName(vertexShaderName);
            string pixelShaderPath = GetShaderPathFromName(pixelShaderName);

            if (!File.Exists(vertexShaderPath))
            {
                throw new FileNotFoundException($"The shader file '{vertexShaderName}' was not found at {vertexShaderPath}.");
            }
            string vsSource = File.ReadAllText(vertexShaderPath);

            if (!File.Exists(pixelShaderPath))
            {
                throw new FileNotFoundException($"The shader file '{pixelShaderPath}' was not found at {pixelShaderPath}.");
            }
            string psSource = File.ReadAllText(pixelShaderPath);

            OpenGLShader vertexShader = new OpenGLShader(vsSource, ShaderType.VertexShader);
            OpenGLShader fragmentShader = new OpenGLShader(psSource, ShaderType.FragmentShader);

            return new OpenGLMaterial((OpenGLRenderContext)rc, vertexShader, fragmentShader, inputs, globalInputs, perObjectInputs, textureInputs);
        }

        public override DeviceTexture2D CreateTexture(IntPtr pixelData, int width, int height, int pixelSizeInBytes, PixelFormat format)
        {
            return new OpenGLTexture2D(width, height, format, pixelData);
        }

        public override DeviceTexture2D CreateTexture<T>(T[] pixelData, int width, int height, int pixelSizeInBytes, PixelFormat format)
        {
            return OpenGLTexture2D.Create(pixelData, width, height, pixelSizeInBytes, format);
        }

        public override DeviceTexture2D CreateDepthTexture(int width, int height, int pixelSizeInBytes, PixelFormat format)
        {
            if (format != PixelFormat.Alpha_UInt16)
            {
                throw new NotImplementedException("Alpha_UInt16 is the only supported depth texture format.");
            }

            return new OpenGLTexture2D(
                width,
                height,
                PixelInternalFormat.DepthComponent16,
                OpenTK.Graphics.OpenGL.PixelFormat.DepthComponent,
                PixelType.UnsignedShort);
        }

        public override ShaderTextureBinding CreateShaderTextureBinding(DeviceTexture texture)
        {
            if (texture is OpenGLTexture2D)
            {
                return new OpenGLTextureBinding((OpenGLTexture2D)texture);
            }
            else
            {
                return new OpenGLTextureBinding((OpenGLCubemapTexture)texture);
            }
        }

        public override CubemapTexture CreateCubemapTexture(
            IntPtr pixelsFront,
            IntPtr pixelsBack,
            IntPtr pixelsLeft,
            IntPtr pixelsRight,
            IntPtr pixelsTop,
            IntPtr pixelsBottom,
            int width,
            int height,
            int pixelSizeinBytes,
            PixelFormat format)
        {
            return new OpenGLCubemapTexture(
                pixelsFront,
                pixelsBack,
                pixelsLeft,
                pixelsRight,
                pixelsTop,
                pixelsBottom,
                width,
                height,
                format);
        }

        public override VertexBuffer CreateVertexBuffer(int sizeInBytes, bool isDynamic)
        {
            return new OpenGLVertexBuffer(isDynamic);
        }

        public override BlendState CreateCustomBlendState(bool isBlendEnabled, Blend srcBlend, Blend destBlend, BlendFunction blendFunc)
        {
            return new OpenGLBlendState(isBlendEnabled, srcBlend, destBlend, blendFunc, srcBlend, destBlend, blendFunc);
        }

        public override BlendState CreateCustomBlendState(bool isBlendEnabled, Blend srcAlpha, Blend destAlpha, BlendFunction alphaBlendFunc, Blend srcColor, Blend destColor, BlendFunction colorBlendFunc)
        {
            return new OpenGLBlendState(isBlendEnabled, srcAlpha, destAlpha, alphaBlendFunc, srcColor, destColor, colorBlendFunc);
        }

        private string GetShaderPathFromName(string shaderName)
        {
            return Path.Combine(AppContext.BaseDirectory, s_shaderDirectory, shaderName + "." + s_shaderFileExtension);
        }

        public override DepthStencilState CreateDepthStencilState(bool isDepthEnabled, DepthComparison comparison)
        {
            return new OpenGLDepthStencilState(isDepthEnabled, comparison);
        }

        public override RasterizerState CreateRasterizerState(
            FaceCullingMode cullMode,
            TriangleFillMode fillMode,
            bool isDepthClipEnabled,
            bool isScissorTestEnabled)
        {
            return new OpenGLRasterizerState(cullMode, fillMode, isDepthClipEnabled, isScissorTestEnabled);
        }

        public override Task<VertexBuffer> CreateVertexBufferAsync(int sizeInBytes, bool isDynamic)
        {
            return Task.Factory.StartNew(
                () => CreateVertexBuffer(sizeInBytes, isDynamic),
                CancellationToken.None,
                TaskCreationOptions.None,
                _taskScheduler);
        }

        public override Task<IndexBuffer> CreateIndexBufferAsync(int sizeInBytes, bool isDynamic)
        {
            return Task.Factory.StartNew(
                () => CreateIndexBuffer(sizeInBytes, isDynamic),
                CancellationToken.None,
                TaskCreationOptions.None,
                _taskScheduler);
        }

        public override Task<IndexBuffer> CreateIndexBufferAsync(int sizeInBytes, bool isDynamic, IndexFormat format)
        {
            return Task.Factory.StartNew(
                () => CreateIndexBuffer(sizeInBytes, isDynamic, format),
                CancellationToken.None,
                TaskCreationOptions.None,
                _taskScheduler);
        }

        public override Task<Material> CreateMaterialAsync(
            RenderContext rc,
            string vertexShaderName,
            string pixelShaderName,
            MaterialVertexInput vertexInputs,
            MaterialInputs<MaterialGlobalInputElement> globalInputs,
            MaterialInputs<MaterialPerObjectInputElement> perObjectInputs,
            MaterialTextureInputs textureInputs)
        {
            return Task.Factory.StartNew(
                () => CreateMaterial(
                    rc,
                    vertexShaderName,
                    pixelShaderName,
                    vertexInputs,
                    globalInputs,
                    perObjectInputs,
                    textureInputs),
                CancellationToken.None,
                TaskCreationOptions.None,
                _taskScheduler);
        }

        public override Task<ConstantBuffer> CreateConstantBufferAsync(int sizeInBytes)
        {
            return Task.Factory.StartNew(
                () => CreateConstantBuffer(sizeInBytes),
                CancellationToken.None,
                TaskCreationOptions.None,
                _taskScheduler);
        }

        public override Task<DeviceTexture2D> CreateTextureAsync<T>(
            T[] pixelData,
            int width,
            int height,
            int pixelSizeInBytes,
            PixelFormat format)
        {
            return Task.Factory.StartNew(
                () => CreateTexture(pixelData, width, height, pixelSizeInBytes, format),
                CancellationToken.None,
                TaskCreationOptions.None,
                _taskScheduler);
        }

        public override Task<DeviceTexture2D> CreateTextureAsync(IntPtr pixelData, int width, int height, int pixelSizeInBytes, PixelFormat format)
        {
            return Task.Factory.StartNew(
                () => CreateTexture(pixelData, width, height, pixelSizeInBytes, format),
                CancellationToken.None,
                TaskCreationOptions.None,
                _taskScheduler);
        }

        public override Task<T> RunAsync<T>(Func<T> func)
        {
            return Task.Factory.StartNew(
                func,
                CancellationToken.None,
                TaskCreationOptions.None,
                _taskScheduler);
        }

        private class OpenGLResourceTaskScheduler : TaskScheduler
        {
            private readonly BlockingCollection<Task> _tasks = new BlockingCollection<Task>();
            private readonly int _mainThreadID;

            public OpenGLResourceTaskScheduler(GraphicsMode mode, int numThreads, int mainThreadID)
            {
                _mainThreadID = mainThreadID;

                for (int i = 0; i < numThreads; i++)
                {
                    new Thread(() =>
                    {
                        NativeWindow window = new NativeWindow();
                        GraphicsContext context = new GraphicsContext(mode, window.WindowInfo);
                        context.MakeCurrent(window.WindowInfo);
                        context.LoadAll();

                        foreach (Task t in _tasks.GetConsumingEnumerable())
                        {
                            TryExecuteTask(t);
                        }

                        context.Dispose();
                    }).Start();
                }
            }

            protected override IEnumerable<Task> GetScheduledTasks()
            {
                return _tasks.ToArray();
            }

            protected override void QueueTask(Task task)
            {
                _tasks.Add(task);
            }

            protected override bool TryExecuteTaskInline(Task task, bool taskWasPreviouslyQueued)
            {
                return Environment.CurrentManagedThreadId == _mainThreadID && TryExecuteTask(task);
            }

            public void Shutdown()
            {
                _tasks.CompleteAdding();
            }
        }
    }
}
