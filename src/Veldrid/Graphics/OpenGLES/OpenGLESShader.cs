using OpenTK.Graphics.ES30;

namespace Veldrid.Graphics.OpenGLES
{
    public class OpenGLESShader : Shader
    {
        public int ShaderID { get; private set; }

        public ShaderStages Type { get; }

        public OpenGLESShader(string source, ShaderType type)
        {
            LoadShader(source, type);
        }

        private void LoadShader(string source, ShaderType type)
        {
            ShaderID = GL.CreateShader(type);
            Utilities.CheckLastGLES3Error();
            GL.ShaderSource(ShaderID, source);
            Utilities.CheckLastGLES3Error();
            GL.CompileShader(ShaderID);
            Utilities.CheckLastGLES3Error();
            GL.GetShader(ShaderID, ShaderParameter.CompileStatus, out int compileStatus);
            Utilities.CheckLastGLES3Error();
            if (compileStatus != 1)
            {
                string shaderLog = GL.GetShaderInfoLog(ShaderID);
                Utilities.CheckLastGLES3Error();
                throw new VeldridException($"Error compiling {type} shader. {shaderLog}");
            }
        }

        public void Dispose()
        {
            GL.DeleteShader(ShaderID);
            Utilities.CheckLastGLES3Error();
        }
    }
}
