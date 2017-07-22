using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using Veldrid.Assets;
using Veldrid.Graphics;

namespace Veldrid.RenderDemo.ForwardRendering
{
    public class ShadowCaster : SwappableRenderItem, IDisposable, RayCastable
    {
        public string Name { get; set; } = "No Name";

        private readonly BoundingSphere _centeredBounds;

        private readonly TextureData _textureData;

        private readonly string[] _stages = new string[] { "ShadowMap", "Standard" };

        private VertexBuffer _vb;
        private IndexBuffer _ib;
        private Material _shadowPassMaterial;
        private Material _regularPassMaterial;
        private ConstantBuffer _worldBuffer;
        private ConstantBuffer _inverseTransposeWorldBuffer;
        private DeviceTexture2D _surfaceTexture;
        private ShaderTextureBinding _surfaceTextureBinding;
        private readonly SimpleMeshDataProvider _meshData;
        private SamplerState _shadowMapSampler;

        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; } = Quaternion.Identity;
        public Vector3 Scale { get; set; } = Vector3.One;

        public ShadowCaster(
            RenderContext rc,
            AssetDatabase ad,
            VertexPositionNormalTexture[] vertices,
            ushort[] indices,
            TextureData texture)
        {
            _meshData = new SimpleMeshDataProvider(vertices, indices);
            _textureData = texture;
            _centeredBounds = BoundingSphere.CreateFromPoints(vertices);

            InitializeContextObjects(ad, rc);
        }

        public void ChangeRenderContext(AssetDatabase ad, RenderContext context)
        {
            Dispose();
            InitializeContextObjects(ad, context);
        }

        private void InitializeContextObjects(AssetDatabase ad, RenderContext rc)
        {
            ResourceFactory factory = rc.ResourceFactory;
            _vb = factory.CreateVertexBuffer(_meshData.Vertices.Length * VertexPositionNormalTexture.SizeInBytes, false);
            _vb.SetVertexData(
                _meshData.Vertices,
                new VertexDescriptor(
                    VertexPositionNormalTexture.SizeInBytes,
                    VertexPositionNormalTexture.ElementCount,
                    0,
                    IntPtr.Zero));
            _ib = factory.CreateIndexBuffer(sizeof(int) * _meshData.Indices.Length, false);
            _ib.SetIndices(_meshData.Indices);

            _shadowPassMaterial = CreateShadowPassMaterial(rc.ResourceFactory);
            _regularPassMaterial = CreateRegularPassMaterial(rc.ResourceFactory);

            _worldBuffer = factory.CreateConstantBuffer(ShaderConstantType.Matrix4x4);
            _inverseTransposeWorldBuffer = factory.CreateConstantBuffer(ShaderConstantType.Matrix4x4);

            _surfaceTexture = _textureData.CreateDeviceTexture(factory);
            _surfaceTextureBinding = factory.CreateShaderTextureBinding(_surfaceTexture);

            _shadowMapSampler = rc.ResourceFactory.CreateSamplerState(
                SamplerAddressMode.Border,
                SamplerAddressMode.Border,
                SamplerAddressMode.Border,
                SamplerFilter.MinMagMipPoint,
                1,
                RgbaFloat.White,
                DepthComparison.Always,
                0,
                int.MaxValue,
                0);
        }

        public RenderOrderKey GetRenderOrderKey(Vector3 viewPosition)
        {
            float distance = Vector3.Distance(Position, viewPosition);
            return RenderOrderKey.Create(distance, _regularPassMaterial.GetHashCode());
        }

        public IList<string> GetStagesParticipated() => _stages;

        public void Render(RenderContext rc, string pipelineStage)
        {
            rc.VertexBuffer = _vb;
            rc.IndexBuffer = _ib;

            Matrix4x4 worldData =
                Matrix4x4.CreateScale(Scale)
                * Matrix4x4.CreateFromQuaternion(Rotation)
                * Matrix4x4.CreateTranslation(Position);
            _worldBuffer.SetData(ref worldData, 64);

            Matrix4x4 inverseTransposeData = Utilities.CalculateInverseTranspose(worldData);
            _inverseTransposeWorldBuffer.SetData(ref inverseTransposeData, 64);


            if (pipelineStage == "ShadowMap")
            {
                _shadowPassMaterial.Apply(rc);
                rc.SetConstantBuffer(0, SharedDataProviders.LightProjMatrixBuffer);
                rc.SetConstantBuffer(1, SharedDataProviders.LightViewMatrixBuffer);
                rc.SetConstantBuffer(2, _worldBuffer);
            }
            else
            {
                Debug.Assert(pipelineStage == "Standard");

                _regularPassMaterial.Apply(rc);
                rc.SetConstantBuffer(0, SharedDataProviders.ProjectionMatrixBuffer);
                rc.SetConstantBuffer(1, SharedDataProviders.ViewMatrixBuffer);
                rc.SetConstantBuffer(2, SharedDataProviders.LightProjMatrixBuffer);
                rc.SetConstantBuffer(3, SharedDataProviders.LightViewMatrixBuffer);
                rc.SetConstantBuffer(4, SharedDataProviders.LightInfoBuffer);
                rc.SetConstantBuffer(5, _worldBuffer);
                rc.SetConstantBuffer(6, _inverseTransposeWorldBuffer);

                rc.SetTexture(7, _surfaceTextureBinding);
                rc.SetSamplerState(8, rc.Anisox4Sampler);
                rc.SetTexture(9, SharedTextures.GetTextureBinding("ShadowMap"));
                rc.SetSamplerState(10, _shadowMapSampler);
            }

            rc.DrawIndexedPrimitives(_meshData.Indices.Length, 0);
        }

        public static Material CreateShadowPassMaterial(ResourceFactory factory)
        {
            Shader vs = factory.CreateShader(ShaderStages.Vertex, ShaderHelper.LoadShaderCode("shadowmap-vertex", ShaderStages.Vertex, factory));
            Shader fs = factory.CreateShader(ShaderStages.Fragment, ShaderHelper.LoadShaderCode("shadowmap-frag", ShaderStages.Fragment, factory));
            VertexInputLayout inputLayout = factory.CreateInputLayout(
                new VertexInputDescription(
                    32,
                    new VertexInputElement("in_position", VertexSemanticType.Position, VertexElementFormat.Float3),
                    new VertexInputElement("in_normal", VertexSemanticType.Normal, VertexElementFormat.Float3),
                    new VertexInputElement("in_texCoord", VertexSemanticType.TextureCoordinate, VertexElementFormat.Float2)));
            ShaderSet shaderSet = factory.CreateShaderSet(inputLayout, vs, fs);
            ShaderResourceBindingSlots constantSlots = factory.CreateShaderResourceBindingSlots(
                shaderSet,
                new ShaderResourceDescription("ProjectionMatrixBuffer", ShaderConstantType.Matrix4x4), // Light Projection
                new ShaderResourceDescription("ViewMatrixBuffer", ShaderConstantType.Matrix4x4), // Light View
                new ShaderResourceDescription("WorldMatrixBuffer", ShaderConstantType.Matrix4x4));

            return new Material(shaderSet, constantSlots);
        }

        public static Material CreateRegularPassMaterial(ResourceFactory factory)
        {
            Shader vs = factory.CreateShader(ShaderStages.Vertex, ShaderHelper.LoadShaderCode("shadow-vertex", ShaderStages.Vertex, factory));
            Shader fs = factory.CreateShader(ShaderStages.Fragment, ShaderHelper.LoadShaderCode("shadow-frag", ShaderStages.Fragment, factory));
            VertexInputLayout inputLayout = factory.CreateInputLayout(
                new VertexInputDescription(
                    32,
                    new VertexInputElement("in_position", VertexSemanticType.Position, VertexElementFormat.Float3),
                    new VertexInputElement("in_normal", VertexSemanticType.Normal, VertexElementFormat.Float3),
                    new VertexInputElement("in_texCoord", VertexSemanticType.TextureCoordinate, VertexElementFormat.Float2)));
            ShaderSet shaderSet = factory.CreateShaderSet(inputLayout, vs, fs);
            ShaderResourceBindingSlots constantSlots = factory.CreateShaderResourceBindingSlots(
                shaderSet,
                new ShaderResourceDescription("ProjectionMatrixBuffer", ShaderConstantType.Matrix4x4),
                new ShaderResourceDescription("ViewMatrixBuffer", ShaderConstantType.Matrix4x4),
                new ShaderResourceDescription("LightProjectionMatrixBuffer", ShaderConstantType.Matrix4x4),
                new ShaderResourceDescription("LightViewMatrixBuffer", ShaderConstantType.Matrix4x4),
                new ShaderResourceDescription("LightInfoBuffer", ShaderConstantType.Float4),
                new ShaderResourceDescription("WorldMatrixBuffer", ShaderConstantType.Matrix4x4),
                new ShaderResourceDescription("InverseTransposeWorldMatrixBuffer", ShaderConstantType.Matrix4x4),
                new ShaderResourceDescription("SurfaceTexture", ShaderResourceType.Texture),
                new ShaderResourceDescription("SurfaceTexture", ShaderResourceType.Sampler),
                new ShaderResourceDescription("ShadowMap", ShaderResourceType.Texture),
                new ShaderResourceDescription("ShadowMap", ShaderResourceType.Sampler));

            return new Material(shaderSet, constantSlots);
        }

        public void Dispose()
        {
            _regularPassMaterial.Dispose();
            _shadowPassMaterial.Dispose();
            _surfaceTexture?.Dispose();
            _surfaceTextureBinding?.Dispose();
            _vb.Dispose();
            _ib.Dispose();
        }

        public bool Cull(ref BoundingFrustum visibleFrustum)
        {
            var boundingSphere = new BoundingSphere((_centeredBounds.Center * (Scale.X)) + Position, _centeredBounds.Radius * Scale.X);
            return visibleFrustum.Contains(boundingSphere) == ContainmentType.Disjoint;
        }

        public BoundingBox BoundingBox
        {
            get
            {
                return BoundingBox.CreateFromVertices(_meshData.Vertices, Rotation, Position, Scale);
            }
        }

        public override string ToString()
        {
            return string.Format("{0}, {1}", Name, BoundingBox.GetCenter());
        }

        public int RayCast(Ray ray, List<float> distances)
        {
            return _meshData.RayCast(ray, distances);
        }
    }
}