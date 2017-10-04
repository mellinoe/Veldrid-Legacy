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
        public ConstantBuffer DepthLimitsBuffer { get; internal set; }
        public ConstantBuffer CameraInfoBuffer { get; private set; }
        public ConstantBuffer PointLightsBuffer { get; private set; }

        public DeviceTexture2D ShadowMapTexture { get; private set; }
        public ShaderTextureBinding ShadowMapBinding { get; private set; }
        public Framebuffer ShadowMapFramebuffer { get; private set; }

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

            ShadowMapTexture = factory.CreateTexture(1, 2048, 2048, PixelFormat.R16_UInt, DeviceTextureCreateOptions.DepthStencil);
            ShadowMapBinding = factory.CreateShaderTextureBinding(ShadowMapTexture);
            ShadowMapFramebuffer = factory.CreateFramebuffer();
            ShadowMapFramebuffer.DepthTexture = ShadowMapTexture;
        }

        public virtual void DestroyDeviceObjects()
        {
            ProjectionMatrixBuffer.Dispose();
            ViewMatrixBuffer.Dispose();
            LightInfoBuffer.Dispose();
            LightViewProjectionBuffer0.Dispose();
            LightViewProjectionBuffer1.Dispose();
            LightViewProjectionBuffer2.Dispose();
            ShadowMapBinding.Dispose();
            ShadowMapFramebuffer.Dispose();
            ShadowMapTexture.Dispose();
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
