using ShaderGen;
using System.Numerics;
using System.Runtime.CompilerServices;
using Veldrid.Graphics;
using System;

namespace Veldrid.NeoDemo.Objects
{
    public class TexturedMesh : CullRenderable
    {
        private readonly MeshData _meshData;
        private readonly TextureData _textureData;
        private readonly Transform _transform = new Transform();

        private BoundingBox _centeredBounds;
        private VertexBuffer _vb;
        private IndexBuffer _ib;
        private int _indexCount;
        private ShaderSet _shaderSet;
        private ShaderResourceBindingSlots _resourceSlots;
        private DeviceTexture _texture;
        private ShaderTextureBinding _textureBinding;
        private DeviceTexture _alphamapTexture;
        private ShaderTextureBinding _alphamapBinding;

        private ShaderSet _mapSet;
        private ShaderResourceBindingSlots _mapSlots;

        private ConstantBuffer _worldBuffer;
        private ConstantBuffer _inverseTransposeWorldBuffer;
        private ConstantBuffer _materialPropertiesBuffer;

        public Transform Transform => _transform;

        public TexturedMesh(MeshData meshData, TextureData textureData)
        {
            _meshData = meshData;
            _centeredBounds = meshData.GetBoundingBox();
            _textureData = textureData;
        }

        public override BoundingBox BoundingBox => BoundingBox.Transform(_centeredBounds, _transform.GetTransformMatrix());

        public override void CreateDeviceObjects(RenderContext rc)
        {
            ResourceFactory factory = rc.ResourceFactory;
            _vb = _meshData.CreateVertexBuffer(factory);
            _ib = _meshData.CreateIndexBuffer(factory, out _indexCount);

            ShadowMainSetInfo.CreateAll(
                factory,
                ShaderHelper.LoadBytecode(factory, "ShadowMain", ShaderStages.Vertex),
                ShaderHelper.LoadBytecode(factory, "ShadowMain", ShaderStages.Fragment),
                out _shaderSet,
                out _resourceSlots);
            ShadowMainSetInfo.CreateAll(
                factory,
                ShaderHelper.LoadBytecode(factory, "ShadowDepth", ShaderStages.Vertex),
                ShaderHelper.LoadBytecode(factory, "ShadowDepth", ShaderStages.Fragment),
                out _mapSet,
                out _mapSlots);

            _worldBuffer = factory.CreateConstantBuffer(ShaderConstantType.Matrix4x4);
            _inverseTransposeWorldBuffer = factory.CreateConstantBuffer(ShaderConstantType.Matrix4x4);
            _materialPropertiesBuffer = factory.CreateConstantBuffer(Unsafe.SizeOf<MaterialProperties>());
            _texture = _textureData.CreateDeviceTexture(factory);
            _textureBinding = factory.CreateShaderTextureBinding(_texture);
            _alphamapTexture = factory.CreateTexture(new RgbaByte[] { RgbaByte.White }, 1, 1, PixelFormat.R8_G8_B8_A8_UInt);
            _alphamapBinding = factory.CreateShaderTextureBinding(_alphamapTexture);
            CreateRasterizerState(rc);
        }

        public override void DestroyDeviceObjects()
        {
            _vb.Dispose();
            _ib.Dispose();
            _shaderSet.Dispose();
            _worldBuffer.Dispose();
            _inverseTransposeWorldBuffer.Dispose();
            _texture.Dispose();
            _textureBinding.Dispose();
            _rasterizerState?.Dispose();
            _materialPropertiesBuffer.Dispose();
            _alphamapTexture.Dispose();
            _alphamapBinding.Dispose();
        }

        public override RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition)
        {
            return RenderOrderKey.Create(_shaderSet.GetHashCode(), Vector3.Distance(_transform.Position, cameraPosition));
        }

        public override RenderPasses RenderPasses => RenderPasses.ShadowMap | RenderPasses.Standard;

        private RasterizerState _rasterizerState;
        private bool _faceCullingChanged = false;
        private FaceCullingMode _faceCullingMode = FaceCullingMode.Back;

        public FaceCullingMode FaceCulling { get => _faceCullingMode; set { _faceCullingMode = value; _faceCullingChanged = true; } }

        public override void Render(RenderContext rc, SceneContext sc, RenderPasses renderPass)
        {
            if (_faceCullingChanged)
            {
                _faceCullingChanged = false;
                _rasterizerState?.Dispose();
                CreateRasterizerState(rc);
            }

            rc.SetRasterizerState(_rasterizerState ?? rc.DefaultRasterizerState);

            if (renderPass == RenderPasses.ShadowMap)
            {
                RenderShadowMap(rc, sc);
            }
            else if (renderPass == RenderPasses.Standard)
            {
                RenderStandard(rc, sc);
            }
        }

        private void CreateRasterizerState(RenderContext rc)
        {
            _rasterizerState = rc.ResourceFactory.CreateRasterizerState(
                _faceCullingMode,
                rc.DefaultRasterizerState.FillMode,
                rc.DefaultRasterizerState.IsDepthClipEnabled,
                rc.DefaultRasterizerState.IsScissorTestEnabled);
        }

        private void RenderShadowMap(RenderContext rc, SceneContext sc)
        {
            Matrix4x4 world = _transform.GetTransformMatrix();
            _worldBuffer.SetData(ref world);
            _inverseTransposeWorldBuffer.SetData(Utilities.CalculateInverseTranspose(ref world));

            rc.VertexBuffer = _vb;
            rc.IndexBuffer = _ib;
            rc.ShaderSet = _mapSet;
            rc.ShaderResourceBindingSlots = _mapSlots;
            rc.SetConstantBuffer(0, sc.LightProjectionBuffer);
            rc.SetConstantBuffer(1, sc.LightViewBuffer);
            rc.SetConstantBuffer(2, _worldBuffer);
            rc.DrawIndexedPrimitives(_indexCount);
        }

        private void RenderStandard(RenderContext rc, SceneContext sc)
        {
            Matrix4x4 world = _transform.GetTransformMatrix();
            _worldBuffer.SetData(ref world);
            _inverseTransposeWorldBuffer.SetData(Utilities.CalculateInverseTranspose(ref world));

            rc.VertexBuffer = _vb;
            rc.IndexBuffer = _ib;
            rc.ShaderSet = _shaderSet;
            rc.ShaderResourceBindingSlots = _resourceSlots;
            rc.SetConstantBuffer(0, sc.ProjectionMatrixBuffer);
            rc.SetConstantBuffer(1, sc.ViewMatrixBuffer);
            rc.SetConstantBuffer(2, _worldBuffer);
            rc.SetConstantBuffer(3, _inverseTransposeWorldBuffer);
            rc.SetConstantBuffer(4, sc.LightProjectionBuffer);
            rc.SetConstantBuffer(5, sc.LightViewBuffer);
            rc.SetConstantBuffer(6, sc.LightInfoBuffer);
            rc.SetConstantBuffer(7, sc.CameraInfoBuffer);
            rc.SetConstantBuffer(8, sc.PointLightsBuffer);
            rc.SetConstantBuffer(9, _materialPropertiesBuffer);
            rc.SetTexture(10, _textureBinding);
            rc.SetSamplerState(11, rc.Anisox4Sampler);
            rc.SetTexture(12, _alphamapBinding);
            rc.SetSamplerState(13, rc.LinearSampler);
            rc.SetTexture(14, sc.ShadowMapBinding);
            rc.SetSamplerState(15, rc.PointSampler);

            rc.DrawIndexedPrimitives(_indexCount);
        }
    }
}
