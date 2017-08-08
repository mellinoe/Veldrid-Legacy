using System;
using System.Runtime.CompilerServices;
using System.Runtime.InteropServices;
using Veldrid.Graphics;
using Veldrid.Graphics.Direct3D;
using Veldrid.Graphics.OpenGL;
using Veldrid.Graphics.OpenGLES;
using Veldrid.Graphics.Vulkan;
using Veldrid.Platform;
using Veldrid.Sdl2;

namespace Veldrid.StartupUtilities
{
    public static class VeldridStartup
    {
        public static void CreateWindowAndRenderContext(ref WindowCreateInfo windowCI, ref RenderContextCreateInfo contextCI, out Sdl2Window window, out RenderContext rc)
        {
            window = CreateWindow(ref windowCI);
            rc = CreateRenderContext(ref contextCI, window);
        }

        private static Sdl2Window CreateWindow(ref WindowCreateInfo windowCI)
        {
            Sdl2Window window = new Sdl2Window(
                windowCI.WindowTitle,
                50,
                50,
                windowCI.WindowWidth,
                windowCI.WindowHeight,
                SDL_WindowFlags.OpenGL | SDL_WindowFlags.Shown | SDL_WindowFlags.Resizable | GetWindowFlags(windowCI.WindowInitialState),
                RuntimeInformation.IsOSPlatform(OSPlatform.Windows));

            return window;
        }

        public static bool IsSupported(GraphicsBackend backend)
        {
            return true; // TODO
        }

        private static SDL_WindowFlags GetWindowFlags(WindowState state)
        {
            switch (state)
            {
                case WindowState.Normal:
                    return 0;
                case WindowState.FullScreen:
                    return SDL_WindowFlags.Fullscreen;
                case WindowState.Maximized:
                    return SDL_WindowFlags.Maximized;
                case WindowState.Minimized:
                    return SDL_WindowFlags.Minimized;
                case WindowState.BorderlessFullScreen:
                    return SDL_WindowFlags.FullScreenDesktop;
                default:
                    throw new VeldridException("Invalid WindowState: " + state);
            }
        }

        public static RenderContext CreateRenderContext(ref RenderContextCreateInfo contextCI, Sdl2Window window)
        {
            GraphicsBackend? backend = contextCI.Backend;
            if (!backend.HasValue)
            {
                backend = GetPlatformDefaultBackend();
            }
            switch (backend)
            {
                case GraphicsBackend.Direct3D11:
                    return CreateDefaultD3DRenderContext(ref contextCI, window);
                case GraphicsBackend.OpenGL:
                    return CreateDefaultOpenGLRenderContext(ref contextCI, window);
                case GraphicsBackend.OpenGLES:
                    return CreateDefaultOpenGLESRenderContext(ref contextCI, window);
                case GraphicsBackend.Vulkan:
                    return CreateVulkanRenderContext(ref contextCI, window);
                default:
                    throw new VeldridException("Invalid GraphicsBackend: " + contextCI.Backend);
            }
        }

        private static GraphicsBackend? GetPlatformDefaultBackend()
        {
            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                return GraphicsBackend.Direct3D11;
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.OSX))
            {
                return GraphicsBackend.OpenGL;
            }
            else
            {
                return IsSupported(GraphicsBackend.Vulkan) ? GraphicsBackend.Vulkan : GraphicsBackend.OpenGL;
            }
        }

        public static unsafe RenderContext CreateVulkanRenderContext(ref RenderContextCreateInfo contextCI, Sdl2Window window)
        {
            IntPtr sdlHandle = window.SdlWindowHandle;
            SDL_SysWMinfo sysWmInfo;
            Sdl2Native.SDL_GetVersion(&sysWmInfo.version);
            Sdl2Native.SDL_GetWMWindowInfo(sdlHandle, &sysWmInfo);
            VkSurfaceSource surfaceSource = GetSurfaceSource(sysWmInfo);
            VkRenderContext rc = new VkRenderContext(surfaceSource, window.Width, window.Height);
            if (contextCI.DebugContext)
            {
                rc.EnableDebugCallback();
            }

            return rc;
        }

        private static unsafe VkSurfaceSource GetSurfaceSource(SDL_SysWMinfo sysWmInfo)
        {
            switch (sysWmInfo.subsystem)
            {
                case SysWMType.Windows:
                    Win32WindowInfo w32Info = Unsafe.Read<Win32WindowInfo>(&sysWmInfo.info);
                    return VkSurfaceSource.CreateWin32(w32Info.hinstance, w32Info.window);
                case SysWMType.X11:
                    X11WindowInfo x11Info = Unsafe.Read<X11WindowInfo>(&sysWmInfo.info);
                    return VkSurfaceSource.CreateXlib(
                        (Vulkan.Xlib.Display*)x11Info.display,
                        new Vulkan.Xlib.Window() { Value = x11Info.window });
                default:
                    throw new PlatformNotSupportedException("Cannot create a Vulkan surface for " + sysWmInfo.subsystem + ".");
            }
        }

        public static OpenGLESRenderContext CreateDefaultOpenGLESRenderContext(ref RenderContextCreateInfo contextCI, Sdl2Window window)
        {
            if (contextCI.DebugContext)
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
                    throw new VeldridException("Unable to create GL Context: " + errorString);
                }
            }

            OpenGLPlatformContextInfo ci = new OpenGLPlatformContextInfo(
                contextHandle,
                Sdl2Native.SDL_GL_GetProcAddress,
                Sdl2Native.SDL_GL_GetCurrentContext,
                () => Sdl2Native.SDL_GL_SwapWindow(sdlHandle));
            OpenGLESRenderContext rc = new OpenGLESRenderContext(window, ci);
            if (contextCI.DebugContext)
            {
                rc.EnableDebugCallback(OpenTK.Graphics.ES30.DebugSeverity.DebugSeverityLow);
            }
            return rc;
        }

        public static OpenGLRenderContext CreateDefaultOpenGLRenderContext(ref RenderContextCreateInfo contextCI, Sdl2Window window)
        {
            IntPtr sdlHandle = window.SdlWindowHandle;
            if (contextCI.DebugContext)
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
                    throw new VeldridException("Unable to create GL Context: " + errorString);
                }
            }

            Sdl2Native.SDL_GL_MakeCurrent(sdlHandle, contextHandle);
            OpenGLPlatformContextInfo ci = new OpenGLPlatformContextInfo(
                contextHandle,
                Sdl2Native.SDL_GL_GetProcAddress,
                Sdl2Native.SDL_GL_GetCurrentContext,
                () => Sdl2Native.SDL_GL_SwapWindow(sdlHandle));
            OpenGLRenderContext rc = new OpenGLRenderContext(window, ci);
            if (contextCI.DebugContext)
            {
                // Slows things down significantly -- Only use when debugging something specific.
                rc.EnableDebugCallback(OpenTK.Graphics.OpenGL.DebugSeverity.DebugSeverityNotification);
            }
            return rc;
        }

        public static D3DRenderContext CreateDefaultD3DRenderContext(ref RenderContextCreateInfo contextCI, Sdl2Window window)
        {
            SharpDX.Direct3D11.DeviceCreationFlags flags = SharpDX.Direct3D11.DeviceCreationFlags.None;
            if (contextCI.DebugContext)
            {
                flags |= SharpDX.Direct3D11.DeviceCreationFlags.Debug;
            }

            return new D3DRenderContext(window, flags);
        }
    }
}
