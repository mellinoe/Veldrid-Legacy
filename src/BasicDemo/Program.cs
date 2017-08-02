using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Veldrid;
using Veldrid.Graphics;
using Veldrid.Graphics.Direct3D;
using Veldrid.Graphics.OpenGL;
using Veldrid.Graphics.OpenGLES;
using Veldrid.Graphics.Vulkan;
using Veldrid.Platform;
using Veldrid.Sdl2;

namespace BasicDemo
{
    public static class Program
    {
        private static bool s_allowDebugContexts = false;
        public static void Main(string[] args)
        {
            GraphicsBackend backend = GraphicsBackend.OpenGL;

            bool onWindows = RuntimeInformation.IsOSPlatform(OSPlatform.Windows);
            Sdl2Window window = new Sdl2Window("Veldrid Render Demo", 100, 100, 960, 540, SDL_WindowFlags.Resizable | SDL_WindowFlags.OpenGL, RuntimeInformation.IsOSPlatform(OSPlatform.Windows));
            RenderContext rc;
            if (backend == GraphicsBackend.Vulkan)
            {
                rc = CreateVulkanRenderContext(window);
            }
            else if (backend == GraphicsBackend.Direct3D11)
            {
                rc = CreateDefaultD3dRenderContext(window);
            }
            else if (backend == GraphicsBackend.OpenGL)
            {
                rc = CreateDefaultOpenGLRenderContext(window);
            }
            else
            {
                rc = CreateDefaultOpenGLESRenderContext(window);
            }

            BasicDemoApp app = new BasicDemoApp(window, rc);
            app.Run();
        }

        private static unsafe RenderContext CreateVulkanRenderContext(Sdl2Window window)
        {
            IntPtr sdlHandle = window.SdlWindowHandle;
            SDL_SysWMinfo sysWmInfo;
            Sdl2Native.SDL_GetVersion(&sysWmInfo.version);
            Sdl2Native.SDL_GetWMWindowInfo(sdlHandle, &sysWmInfo);
            Win32WindowInfo w32Info = Unsafe.Read<Win32WindowInfo>(&sysWmInfo.info);
            return new VkRenderContext(VkSurfaceSource.CreateWin32(w32Info.hinstance, w32Info.window), window.Width, window.Height);
        }

        private static OpenGLESRenderContext CreateDefaultOpenGLESRenderContext(Sdl2Window window)
        {
            bool debugContext = false;
            debugContext |= s_allowDebugContexts;

            if (debugContext)
            {
                Sdl2Native.SDL_GL_SetAttribute(SDL_GLAttribute.ContextFlags, (int)SDL_GLContextFlag.Debug);
            }

            IntPtr sdlHandle = window.SdlWindowHandle;
            Sdl2Native.SDL_GL_SetAttribute(SDL_GLAttribute.ContextProfileMask, (int)SDL_GLProfile.ES);
            Sdl2Native.SDL_GL_SetAttribute(SDL_GLAttribute.ContextMajorVersion, 3);
            Sdl2Native.SDL_GL_SetAttribute(SDL_GLAttribute.ContextMinorVersion, 0);
            IntPtr contextHandle = Sdl2Native.SDL_GL_CreateContext(sdlHandle);
            Sdl2Native.SDL_GL_MakeCurrent(sdlHandle, contextHandle);

            if (contextHandle == IntPtr.Zero)
            {
                unsafe
                {
                    byte* error = Sdl2Native.SDL_GetError();
                    string errorString = Utilities.GetString(error);
                    throw new InvalidOperationException("Unable to create GL Context: " + errorString);
                }
            }

            Sdl2Native.SDL_GL_MakeCurrent(sdlHandle, contextHandle);
            OpenGLPlatformContextInfo ci = new OpenGLPlatformContextInfo(
                contextHandle,
                Sdl2Native.SDL_GL_GetProcAddress,
                Sdl2Native.SDL_GL_GetCurrentContext,
                () => Sdl2Native.SDL_GL_SwapWindow(sdlHandle));
            var rc = new OpenGLESRenderContext(window, ci);
            if (debugContext)
            {
                rc.EnableDebugCallback(OpenTK.Graphics.ES30.DebugSeverity.DebugSeverityNotification);
            }
            return rc;
        }

        private static OpenGLRenderContext CreateDefaultOpenGLRenderContext(Sdl2Window window)
        {
            bool debugContext = false;
            debugContext |= s_allowDebugContexts;

            IntPtr sdlHandle = window.SdlWindowHandle;
            if (debugContext)
            {
                Sdl2Native.SDL_GL_SetAttribute(SDL_GLAttribute.ContextFlags, (int)SDL_GLContextFlag.Debug);
            }

            Sdl2Native.SDL_GL_SetAttribute(SDL_GLAttribute.ContextProfileMask, (int)SDL_GLProfile.Core);
            Sdl2Native.SDL_GL_SetAttribute(SDL_GLAttribute.ContextMajorVersion, 4);
            Sdl2Native.SDL_GL_SetAttribute(SDL_GLAttribute.ContextMinorVersion, 0);

            IntPtr contextHandle = Sdl2Native.SDL_GL_CreateContext(sdlHandle);
            if (contextHandle == IntPtr.Zero)
            {
                unsafe
                {
                    byte* error = Sdl2Native.SDL_GetError();
                    string errorString = Utilities.GetString(error);
                    throw new InvalidOperationException("Unable to create GL Context: " + errorString);
                }
            }

            Sdl2Native.SDL_GL_MakeCurrent(sdlHandle, contextHandle);
            OpenGLPlatformContextInfo ci = new OpenGLPlatformContextInfo(
                contextHandle,
                Sdl2Native.SDL_GL_GetProcAddress,
                Sdl2Native.SDL_GL_GetCurrentContext,
                () => Sdl2Native.SDL_GL_SwapWindow(sdlHandle));
            var rc = new OpenGLRenderContext(window, ci);
            if (debugContext)
            {
                // Slows things down significantly -- Only use when debugging something specific.
                rc.EnableDebugCallback(OpenTK.Graphics.OpenGL.DebugSeverity.DebugSeverityNotification);
            }
            return rc;
        }

        private static D3DRenderContext CreateDefaultD3dRenderContext(Window window)
        {
            SharpDX.Direct3D11.DeviceCreationFlags flags = SharpDX.Direct3D11.DeviceCreationFlags.None;
            if (s_allowDebugContexts)
            {
                flags |= SharpDX.Direct3D11.DeviceCreationFlags.Debug;
            }
            return new D3DRenderContext(window, flags);
        }
    }
}
