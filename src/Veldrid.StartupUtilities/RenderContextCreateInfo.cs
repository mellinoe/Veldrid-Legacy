using System;
using Veldrid.Graphics;

namespace Veldrid.StartupUtilities
{
    public struct RenderContextCreateInfo
    {
        public GraphicsBackend? Backend;
        public bool DebugContext;
    }
}
