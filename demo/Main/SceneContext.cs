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
        public ConstantBuffer LightProjectionBuffer { get; private set; }
        public ConstantBuffer LightViewBuffer { get; private set; }

        private DeviceTexture2D _shadowMapTexture;
        public ShaderTextureBinding ShadowMapBinding { get; private set; }
        public Framebuffer ShadowMapFramebuffer { get; private set; }

        public Camera Camera { get; set; }
        public DirectionalLight DirectionalLight { get; } = new DirectionalLight();

        public SceneContext()
        {
        }

        public virtual void CreateDeviceObjects(RenderContext rc)
        {
            ResourceFactory factory = rc.ResourceFactory;
            ProjectionMatrixBuffer = factory.CreateConstantBuffer(ShaderConstantType.Matrix4x4);
            ViewMatrixBuffer = factory.CreateConstantBuffer(ShaderConstantType.Matrix4x4);
            LightInfoBuffer = factory.CreateConstantBuffer(Unsafe.SizeOf<DirectionalLightInfo>());
            LightProjectionBuffer = factory.CreateConstantBuffer(ShaderConstantType.Matrix4x4);
            LightViewBuffer = factory.CreateConstantBuffer(ShaderConstantType.Matrix4x4);
            if (Camera != null)
            {
                ProjectionMatrixBuffer.SetData(Camera.ProjectionMatrix);
                ViewMatrixBuffer.SetData(Camera.ViewMatrix);
            }

            Matrix4x4 lightProj = Matrix4x4.CreateOrthographic(100, 100, 5, 1000);
            Matrix4x4 lightView = Matrix4x4.CreateLookAt(-DirectionalLight.Direction * 50, Vector3.Zero, Vector3.UnitY);
            LightProjectionBuffer.SetData(ref lightProj);
            LightViewBuffer.SetData(ref lightView);

            _shadowMapTexture = factory.CreateTexture(1, 2048, 2048, PixelFormat.R16_UInt, DeviceTextureCreateOptions.DepthStencil);
            ShadowMapBinding = factory.CreateShaderTextureBinding(_shadowMapTexture);
            ShadowMapFramebuffer = factory.CreateFramebuffer();
            ShadowMapFramebuffer.DepthTexture = _shadowMapTexture;
        }

        public virtual void DestroyDeviceObjects()
        {
            ProjectionMatrixBuffer.Dispose();
            ViewMatrixBuffer.Dispose();
            LightInfoBuffer.Dispose();
            LightProjectionBuffer.Dispose();
            LightViewBuffer.Dispose();
            ShadowMapBinding.Dispose();
            ShadowMapFramebuffer.Dispose();
            _shadowMapTexture.Dispose();
        }

        public void SetCurrentScene(Scene scene)
        {
            Camera = scene.Camera;
            scene.Camera.ViewChanged += view => ViewMatrixBuffer.SetData(ref view);
            ViewMatrixBuffer.SetData(scene.Camera.ViewMatrix);
            scene.Camera.ProjectionChanged += proj => ProjectionMatrixBuffer.SetData(ref proj);
            ProjectionMatrixBuffer.SetData(scene.Camera.ProjectionMatrix);
        }
    }
}
