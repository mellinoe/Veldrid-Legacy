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

            if (backend == GraphicsBackend.Vulkan || backend == GraphicsBackend.Direct3D11)
            {
                string bytecodeExtension = GetBytecodeExtension(backend);
                string bytecodePath = AssetHelper.GetPath(Path.Combine("Shaders.Generated", name + bytecodeExtension));
                if (File.Exists(bytecodePath))
                {
                    return factory.LoadProcessedShader(File.ReadAllBytes(bytecodePath));
                }
            }

            string extension = GetSourceExtension(backend);
            string path = AssetHelper.GetPath(Path.Combine("Shaders.Generated", name + extension));
            return factory.ProcessShaderCode(stage, File.ReadAllText(path));
        }

        private static string GetBytecodeExtension(GraphicsBackend backend)
        {
            switch (backend)
            {
                case GraphicsBackend.Direct3D11: return ".hlsl.bytes";
                case GraphicsBackend.Vulkan: return ".450.glsl.spv";
                case GraphicsBackend.OpenGL:
                case GraphicsBackend.OpenGLES:
                    throw new InvalidOperationException("OpenGL and OpenGLES do not support shader bytecode.");
                default: throw new InvalidOperationException("Invalid Graphics backend: " + backend);
            }
        }

        private static string GetSourceExtension(GraphicsBackend backend)
        {
            switch (backend)
            {
                case GraphicsBackend.Direct3D11: return ".hlsl";
                case GraphicsBackend.Vulkan: return ".450.glsl";
                case GraphicsBackend.OpenGL:
                case GraphicsBackend.OpenGLES:
                    return ".330.glsl";
                default: throw new InvalidOperationException("Invalid Graphics backend: " + backend);
            }
        }
    }
}
