using System;
using System.Numerics;
using System.Runtime.InteropServices;
using Vulkan;
using static Vulkan.VulkanNative;

namespace Veldrid.Graphics.Vulkan
{
    public unsafe class VkRenderContext : RenderContext
    {
        private static readonly FixedUtf8String s_name = "VkRenderContext";

        public VkRenderContext()
        {
            VkInstanceCreateInfo instanceCI = VkInstanceCreateInfo.New();
            VkApplicationInfo applicationInfo = new VkApplicationInfo();
            applicationInfo.apiVersion = new VkVersion(1, 0, 0);
            applicationInfo.applicationVersion = new VkVersion(1, 0, 0);
            applicationInfo.engineVersion = new VkVersion(1, 0, 0);
            applicationInfo.pApplicationName = s_name;
            applicationInfo.pEngineName = s_name;

            instanceCI.pApplicationInfo = &applicationInfo;

            StackList<IntPtr, Size64Bytes> instanceExtensions = new StackList<IntPtr, Size64Bytes>();
            StackList<IntPtr, Size64Bytes> instanceLayers = new StackList<IntPtr, Size64Bytes>();

            instanceExtensions.Add(CommonStrings.VK_KHR_SURFACE_EXTENSION_NAME);

            if (RuntimeInformation.IsOSPlatform(OSPlatform.Windows))
            {
                instanceExtensions.Add(CommonStrings.VK_KHR_WIN32_SURFACE_EXTENSION_NAME);
            }
            else if (RuntimeInformation.IsOSPlatform(OSPlatform.Linux))
            {
                instanceExtensions.Add(CommonStrings.VK_KHR_XCB_SURFACE_EXTENSION_NAME);
            }
            else
            {
                throw new NotSupportedException("This platform does not support Vulkan.");
            }

            bool debug = false;
#if DEBUG
            debug = true;
#endif
            if (debug)
            {
                instanceExtensions.Add(CommonStrings.VK_EXT_DEBUG_REPORT_EXTENSION_NAME);
                instanceLayers.Add(CommonStrings.StandardValidationLayerName);
            }

            instanceCI.enabledExtensionCount = instanceExtensions.Count;
            instanceCI.ppEnabledExtensionNames = (byte**)instanceExtensions.Data;

            instanceCI.enabledLayerCount = instanceLayers.Count;
            instanceCI.ppEnabledLayerNames = (byte**)instanceLayers.Data;

            VkResult result = vkCreateInstance(ref instanceCI, null, out VkInstance instance);
            if (result != VkResult.Success)
            {
                throw new VeldridException("Error creating a Vulkan instance: " + result);
            }
            else
            {
                Console.WriteLine("Successfully created a VkInstance!");
            }
        }

        public override ResourceFactory ResourceFactory => throw new NotImplementedException();

        public override RenderCapabilities RenderCapabilities => throw new NotImplementedException();

        public override void DrawIndexedPrimitives(int count, int startingIndex)
        {
            throw new NotImplementedException();
        }

        public override void DrawIndexedPrimitives(int count, int startingIndex, int startingVertex)
        {
            throw new NotImplementedException();
        }

        public override void DrawInstancedPrimitives(int indexCount, int instanceCount, int startingIndex)
        {
            throw new NotImplementedException();
        }

        public override void DrawInstancedPrimitives(int indexCount, int instanceCount, int startingIndex, int startingVertex)
        {
            throw new NotImplementedException();
        }

        protected override Vector2 GetBottomRightUvCoordinate()
        {
            return Vector2.One;
        }

        protected override Vector2 GetTopLeftUvCoordinate()
        {
            return Vector2.Zero;
        }

        protected override void PlatformClearBuffer()
        {
            throw new NotImplementedException();
        }

        protected override void PlatformClearMaterialResourceBindings()
        {
            throw new NotImplementedException();
        }

        protected override void PlatformDispose()
        {
            throw new NotImplementedException();
        }

        protected override GraphicsBackend PlatformGetGraphicsBackend()
        {
            throw new NotImplementedException();
        }

        protected override void PlatformResize(int width, int height)
        {
            throw new NotImplementedException();
        }

        protected override void PlatformSetBlendstate(BlendState blendState)
        {
            throw new NotImplementedException();
        }

        protected override void PlatformSetConstantBuffer(int slot, ConstantBuffer cb)
        {
            throw new NotImplementedException();
        }

        protected override void PlatformSetDefaultFramebuffer()
        {
            throw new NotImplementedException();
        }

        protected override void PlatformSetDepthStencilState(DepthStencilState depthStencilState)
        {
            throw new NotImplementedException();
        }

        protected override void PlatformSetFramebuffer(Framebuffer framebuffer)
        {
            throw new NotImplementedException();
        }

        protected override void PlatformSetIndexBuffer(IndexBuffer ib)
        {
            throw new NotImplementedException();
        }

        protected override void PlatformSetPrimitiveTopology(PrimitiveTopology primitiveTopology)
        {
            throw new NotImplementedException();
        }

        protected override void PlatformSetRasterizerState(RasterizerState rasterizerState)
        {
            throw new NotImplementedException();
        }

        protected override void PlatformSetSamplerState(int slot, SamplerState samplerState, bool mipmapped)
        {
            throw new NotImplementedException();
        }

        protected override void PlatformSetScissorRectangle(Rectangle rectangle)
        {
            throw new NotImplementedException();
        }

        protected override void PlatformSetShaderConstantBindings(ShaderConstantBindingSlots shaderConstantBindings)
        {
            throw new NotImplementedException();
        }

        protected override void PlatformSetShaderSet(ShaderSet shaderSet)
        {
            throw new NotImplementedException();
        }

        protected override void PlatformSetShaderTextureBindingSlots(ShaderTextureBindingSlots bindingSlots)
        {
            throw new NotImplementedException();
        }

        protected override void PlatformSetTexture(int slot, ShaderTextureBinding textureBinding)
        {
            throw new NotImplementedException();
        }

        protected override void PlatformSetVertexBuffer(int slot, VertexBuffer vb)
        {
            throw new NotImplementedException();
        }

        protected override void PlatformSetViewport(int x, int y, int width, int height)
        {
            throw new NotImplementedException();
        }

        protected override void PlatformSwapBuffers()
        {
            throw new NotImplementedException();
        }
    }
}
