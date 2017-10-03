using System.Numerics;
using ShaderGen;
using static ShaderGen.ShaderBuiltins;

[assembly: ShaderSet("Simple2D", "Shaders.Simple2D.VS", "Shaders.Simple2D.FS")]

namespace Shaders
{
    public class Simple2D
    {
        public Texture2DResource Tex;
        public SamplerResource TexSampler;

        [VertexShader]
        FragmentIn VS(VertexIn input)
        {
            FragmentIn output;
            output.Position = new Vector4(input.Position, 0, 1);
            output.TexCoord = input.TexCoord;
            return output;
        }

        [FragmentShader]
        Vector4 FS(FragmentIn input)
        {
            return Sample(Tex, TexSampler, input.TexCoord);
        }

        public struct VertexIn
        {
            [PositionSemantic] public Vector2 Position;
            [TextureCoordinateSemantic] public Vector2 TexCoord;
        }

        public struct FragmentIn
        {
            [PositionSemantic] public Vector4 Position;
            [TextureCoordinateSemantic] public Vector2 TexCoord;
        }
    }
}
