namespace Veldrid.Graphics.OpenGLES
{
    public class OpenGLESTextureBinding : ShaderTextureBinding
    {
        private readonly OpenGLESTexture _texture;
        public OpenGLESTexture BoundTexture => _texture;
        DeviceTexture ShaderTextureBinding.BoundTexture => _texture;

        public OpenGLESTextureBinding(OpenGLESTexture texture)
        {
            _texture = texture;
        }

        public void Dispose()
        {
        }
    }
}
