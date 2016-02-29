﻿namespace Veldrid.Graphics
{
    public interface Texture
    {
        int Width { get; }
        int Height { get; }
        float[] Pixels { get; }
        PixelFormat Format { get; }
    }
}