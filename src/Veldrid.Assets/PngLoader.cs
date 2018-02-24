using SixLabors.ImageSharp;
using System.IO;
using Veldrid.Graphics;

namespace Veldrid.Assets
{
    public class PngLoader : ConcreteLoader<ImageSharpTexture>
    {
        public override string FileExtension => "png";

        public override ImageSharpTexture Load(Stream s)
        {
            return new ImageSharpTexture(Image.Load<Rgba32>(s));
        }
    }
}
