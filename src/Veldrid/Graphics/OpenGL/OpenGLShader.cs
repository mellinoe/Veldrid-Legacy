using OpenTK.Graphics.OpenGL;

namespace Veldrid.Graphics.OpenGL
{
    public class OpenGLShader : Shader
    {
        public int ShaderID { get; private set; }

        public ShaderStages Type { get; }

        public OpenGLShader(string source, ShaderType type)
        {
            LoadShader(source, type);
        }

        private void LoadShader(string source, ShaderType type)
        {
            ShaderID = GL.CreateShader(type);
            GL.ShaderSource(ShaderID, source);
            GL.CompileShader(ShaderID);
            int compileStatus;
            GL.GetShader(ShaderID, ShaderParameter.CompileStatus, out compileStatus);
            if (compileStatus != 1)
            {
                string shaderLog = GL.GetShaderInfoLog(ShaderID);
                throw new VeldridException($"Error compiling {type} shader. {shaderLog}");
            }
        }

        public void Dispose()
        {
            GL.DeleteShader(ShaderID);
        }
    }
}
