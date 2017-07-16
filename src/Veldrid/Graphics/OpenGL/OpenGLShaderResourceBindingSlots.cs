namespace Veldrid.Graphics.OpenGL
{
    public class OpenGLShaderResourceBindingSlots : ShaderResourceBindingSlots
    {
        public ShaderResourceDescription[] Resources { get; }
        public OpenGLTextureBindingSlots TextureSlots { get; }
        public OpenGLShaderConstantBindingSlots ConstantSlots { get; }
    }
}
