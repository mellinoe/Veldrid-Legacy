using System.Collections.Generic;
using Veldrid.Graphics;

namespace Veldrid.RenderDemo.ForwardRendering
{
    public static class TextureCache
    {
        private static readonly Dictionary<TextureData, DeviceTexture2D> s_deviceTextures = new Dictionary<TextureData, DeviceTexture2D>();
        private static RenderContext s_previousRC;

        public static void Clear()
        {
            s_deviceTextures.Clear();
        }

        public static DeviceTexture2D GetCachedTexture(RenderContext rc, TextureData texData)
        {
            if (s_previousRC != rc)
            {
                s_previousRC = rc;
                Clear();
            }

            if (!s_deviceTextures.TryGetValue(texData, out DeviceTexture2D ret))
            {
                ret = texData.CreateDeviceTexture(rc.ResourceFactory);
                s_deviceTextures.Add(texData, ret);
            }

            return ret;
        }
    }
}
