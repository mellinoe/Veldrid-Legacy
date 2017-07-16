namespace Veldrid.Graphics.OpenGLES
{
    public class OpenGLESShaderResourceBindingSlots : ShaderResourceBindingSlots
    {
        public ShaderResourceDescription[] Resources { get; }

        public OpenGLESTextureBindingSlots TextureSlots { get; }

        public OpenGLESShaderConstantBindingSlots ConstantSlots { get; }
    }
}
