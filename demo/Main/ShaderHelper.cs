using System;
using System.IO;
using Veldrid.Graphics;

namespace Veldrid.NeoDemo
{
    public static class ShaderHelper
    {
        public static Shader LoadShader(ResourceFactory factory, string name, ShaderStages stage)
        {
            return factory.CreateShader(stage, LoadBytecode(factory, name, stage));
        }

        public static CompiledShaderCode LoadBytecode(ResourceFactory factory, string name, ShaderStages stage)
        {
            GraphicsBackend backend = factory.BackendType;
            string folder = GetFolderName(backend);
            string extension = GetExtension(backend);
            string path = Path.Combine(AppContext.BaseDirectory, "Assets", folder, name + extension);
            if (backend == GraphicsBackend.Vulkan)
            {
                return factory.LoadProcessedShader(File.ReadAllBytes(path));
            }
            else
            {
                return factory.ProcessShaderCode(stage, File.ReadAllText(path));
            }
        }

        private static string GetFolderName(GraphicsBackend backend)
        {
            switch (backend)
            {
                case GraphicsBackend.Direct3D11: return "HLSL";
                case GraphicsBackend.Vulkan: return "SPIR-V";
                case GraphicsBackend.OpenGL:
                case GraphicsBackend.OpenGLES:
                    return "GLSL";
                default: throw new InvalidOperationException("Invalid Graphics backend: " + backend);
            }
        }

        private static string GetExtension(GraphicsBackend backend)
        {
            switch (backend)
            {
                case GraphicsBackend.Direct3D11: return ".hlsl";
                case GraphicsBackend.Vulkan: return ".spv";
                case GraphicsBackend.OpenGL:
                case GraphicsBackend.OpenGLES:
                    return ".glsl";
                default: throw new InvalidOperationException("Invalid Graphics backend: " + backend);
            }
        }
    }
}
