using System.Numerics;
using System.Runtime.CompilerServices;
using Veldrid.Graphics;

namespace Veldrid.NeoDemo
{
    public class SceneContext
    {
        public ConstantBuffer ProjectionMatrixBuffer { get; private set; }
        public ConstantBuffer ViewMatrixBuffer { get; private set; }
        public ConstantBuffer LightInfoBuffer { get; private set; }
        public ConstantBuffer LightViewProjectionBuffer0 { get; internal set; }
        public ConstantBuffer LightViewProjectionBuffer1 { get; internal set; }
        public ConstantBuffer LightViewProjectionBuffer2 { get; internal set; }
        public ConstantBuffer CurrentLightViewProjectionBuffer { get; internal set; }
        public ConstantBuffer DepthLimitsBuffer { get; internal set; }
        public ConstantBuffer CameraInfoBuffer { get; private set; }
        public ConstantBuffer PointLightsBuffer { get; private set; }

        public DeviceTexture2D NearShadowMapTexture { get; private set; }
        public ShaderTextureBinding NearShadowMapBinding { get; private set; }
        public Framebuffer NearShadowMapFramebuffer { get; private set; }

        public DeviceTexture2D MidShadowMapTexture { get; private set; }
        public ShaderTextureBinding MidShadowMapBinding { get; private set; }
        public Framebuffer MidShadowMapFramebuffer { get; private set; }

        public DeviceTexture2D FarShadowMapTexture { get; private set; }
        public ShaderTextureBinding FarShadowMapBinding { get; private set; }
        public Framebuffer FarShadowMapFramebuffer { get; private set; }

        public Camera Camera { get; set; }
        public DirectionalLight DirectionalLight { get; } = new DirectionalLight();

        public virtual void CreateDeviceObjects(RenderContext rc)
        {
            ResourceFactory factory = rc.ResourceFactory;
            ProjectionMatrixBuffer = factory.CreateConstantBuffer(ShaderConstantType.Matrix4x4);
            ViewMatrixBuffer = factory.CreateConstantBuffer(ShaderConstantType.Matrix4x4);
            LightViewProjectionBuffer0 = factory.CreateConstantBuffer(ShaderConstantType.Matrix4x4);
            LightViewProjectionBuffer1 = factory.CreateConstantBuffer(ShaderConstantType.Matrix4x4);
            LightViewProjectionBuffer2 = factory.CreateConstantBuffer(ShaderConstantType.Matrix4x4);
            DepthLimitsBuffer = factory.CreateConstantBuffer(Unsafe.SizeOf<DepthCascadeLimits>());
            LightInfoBuffer = factory.CreateConstantBuffer(Unsafe.SizeOf<DirectionalLightInfo>());
            CameraInfoBuffer = factory.CreateConstantBuffer(Unsafe.SizeOf<CameraInfo>());
            if (Camera != null)
            {
                UpdateCameraBuffers();
            }

            PointLightsBuffer = factory.CreateConstantBuffer(Unsafe.SizeOf<PointLightsInfo.Blittable>());
            PointLightsBuffer.SetData(new PointLightsInfo.Blittable());

            NearShadowMapTexture = factory.CreateTexture(1, 2048, 2048, PixelFormat.R16_UInt, DeviceTextureCreateOptions.DepthStencil);
            NearShadowMapBinding = factory.CreateShaderTextureBinding(NearShadowMapTexture);
            NearShadowMapFramebuffer = factory.CreateFramebuffer();
            NearShadowMapFramebuffer.DepthTexture = NearShadowMapTexture;

            MidShadowMapTexture = factory.CreateTexture(1, 2048, 2048, PixelFormat.R16_UInt, DeviceTextureCreateOptions.DepthStencil);
            MidShadowMapBinding = factory.CreateShaderTextureBinding(MidShadowMapTexture);
            MidShadowMapFramebuffer = factory.CreateFramebuffer();
            MidShadowMapFramebuffer.DepthTexture = MidShadowMapTexture;

            FarShadowMapTexture = factory.CreateTexture(1, 4096, 4096, PixelFormat.R16_UInt, DeviceTextureCreateOptions.DepthStencil);
            FarShadowMapBinding = factory.CreateShaderTextureBinding(FarShadowMapTexture);
            FarShadowMapFramebuffer = factory.CreateFramebuffer();
            FarShadowMapFramebuffer.DepthTexture = FarShadowMapTexture;
        }

        public virtual void DestroyDeviceObjects()
        {
            ProjectionMatrixBuffer.Dispose();
            ViewMatrixBuffer.Dispose();
            LightInfoBuffer.Dispose();
            LightViewProjectionBuffer0.Dispose();
            LightViewProjectionBuffer1.Dispose();
            LightViewProjectionBuffer2.Dispose();
            NearShadowMapBinding.Dispose();
            NearShadowMapFramebuffer.Dispose();
            NearShadowMapTexture.Dispose();
            MidShadowMapBinding.Dispose();
            MidShadowMapFramebuffer.Dispose();
            MidShadowMapTexture.Dispose();
            FarShadowMapBinding.Dispose();
            FarShadowMapFramebuffer.Dispose();
            FarShadowMapTexture.Dispose();
            DepthLimitsBuffer.Dispose();
            CameraInfoBuffer.Dispose();
            PointLightsBuffer.Dispose();
        }

        public void SetCurrentScene(Scene scene)
        {
            Camera = scene.Camera;
            scene.Camera.ViewChanged += view => UpdateCameraBuffers();
            scene.Camera.ProjectionChanged += proj => UpdateCameraBuffers();

            UpdateCameraBuffers();
        }

        private void UpdateCameraBuffers()
        {
            ProjectionMatrixBuffer.SetData(Camera.ProjectionMatrix);
            ViewMatrixBuffer.SetData(Camera.ViewMatrix);
            CameraInfoBuffer.SetData(Camera.GetCameraInfo());
        }
    }
}
