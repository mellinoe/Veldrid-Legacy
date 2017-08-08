using System;
using Veldrid.Graphics;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;

namespace Veldrid.NeoDemo
{
    class Program
    {
        static void Main(string[] args)
        {
            WindowCreateInfo windowCI = new WindowCreateInfo
            {
                WindowWidth = 960,
                WindowHeight = 540,
                WindowInitialState = Platform.WindowState.Normal,
                WindowTitle = "Veldrid NeoDemo"
            };

            RenderContextCreateInfo rcCI = new RenderContextCreateInfo();

            VeldridStartup.CreateWindowAndRenderContext(ref windowCI, ref rcCI, out Sdl2Window window, out RenderContext rc);

            while (window.Exists)
            {
                rc.Viewport = new Viewport(0, 0, window.Width, window.Height);
                rc.ClearBuffer(RgbaFloat.Red);
                rc.SwapBuffers();
            }
        }
    }
}
