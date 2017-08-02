using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using Veldrid.Assets;
using Veldrid.Graphics;

namespace Veldrid.RenderDemo.ForwardRendering
{
    public class MtlShadowCaster : SwappableRenderItem, IDisposable
    {
        public string Name { get; set; } = "No Name";

        private readonly VertexPositionNormalTexture[] _vertices;
        private readonly ushort[] _indices;
        private readonly BoundingSphere _centeredBounds;

        private readonly DynamicDataProvider<Matrix4x4> _worldProvider;
        private readonly DependantDataProvider<Matrix4x4> _inverseTransposeWorldProvider;
        private readonly DynamicDataProvider<MtlMaterialProperties> _mtlPropertiesProvider;

        private readonly TextureData _overrideTextureData;
        private TextureData _alphaMapTextureData;

        private readonly string[] _standardStages = new string[] { "ShadowMap", "Standard" };
        private readonly string[] _alphaMapStages = new string[] { "ShadowMap", "AlphaBlend" };

        private VertexBuffer _vb;
        private IndexBuffer _ib;
        private ConstantBuffer _worldBuffer;
        private ConstantBuffer _inverseTransposeWorldBuffer;
        private ConstantBuffer _mtlPropertiesBuffer;
        private DeviceTexture2D _texture;
        private ShaderTextureBinding _textureBinding;
        private DeviceTexture2D _alphaMapTexture;
        private ShaderTextureBinding _alphaMapTextureBinding;
        private bool _alphaMapNeedsRecreation = true;
        private bool _hasAlphaMap = false;
        private Vector3 _scale = Vector3.One;

        // Per-Render-Context resources. Shared between all objects.
        private static ResourceFactory s_cachedResourceFactory;
        private static Material s_shadowPassMaterial;
        private static Material s_regularPassMaterial;
        private static SamplerState s_shadowMapSampler;
        private static DeviceTexture2D s_blankAlphaMapTexture;
        private static ShaderTextureBinding s_blankAlphaMapTextureBinding;
        private bool _usesSharedAlphaMap;

        public Vector3 Position { get; set; }
        public Quaternion Rotation { get; set; } = Quaternion.Identity;
        public Vector3 Scale
        {
            get => _scale;
            set { _scale = value; _worldProvider.Data = CreateWorldMatrix(); }
        }

        private Matrix4x4 CreateWorldMatrix()
        {
            return Matrix4x4.CreateScale(Scale)
                * Matrix4x4.CreateFromQuaternion(Rotation)
                * Matrix4x4.CreateTranslation(Position);
        }

        public MtlMaterialProperties MaterialProperties
        {
            get { return _mtlPropertiesProvider.Data; }
            set { _mtlPropertiesProvider.Data = value; }
        }

        public TextureData AlphaMap
        {
            get { return _alphaMapTextureData; }
            set
            {
                _alphaMapTextureData = value;
                _hasAlphaMap = true;
                _alphaMapNeedsRecreation = true;
            }
        }

        public MtlShadowCaster(
            RenderContext rc,
            VertexPositionNormalTexture[] vertices,
            ushort[] indices,
            TextureData overrideTexture)
        {
            _vertices = vertices;
            _indices = indices;
            _centeredBounds = BoundingSphere.CreateFromPoints(vertices);

            _overrideTextureData = overrideTexture;

            InitializeContextObjects(rc);

            _worldProvider = new DynamicDataProvider<Matrix4x4>();
            _inverseTransposeWorldProvider = new DependantDataProvider<Matrix4x4>(_worldProvider, Utilities.CalculateInverseTranspose);
            _mtlPropertiesProvider = new DynamicDataProvider<MtlMaterialProperties>();
            _worldProvider.DataChanged += () =>
            {
                _worldProvider.SetData(_worldBuffer); _inverseTransposeWorldProvider.SetData(_inverseTransposeWorldBuffer);
            };
            _worldProvider.Data = CreateWorldMatrix();
            _mtlPropertiesProvider.DataChanged += () => _mtlPropertiesProvider.SetData(_mtlPropertiesBuffer);
        }

        public void ChangeRenderContext(AssetDatabase ad, RenderContext context)
        {
            Dispose();
            InitializeContextObjects(context);
        }

        private void InitializeContextObjects(RenderContext rc)
        {
            ResourceFactory factory = rc.ResourceFactory;

            CreateSharedResources(factory);

            _vb = factory.CreateVertexBuffer(_vertices.Length * VertexPositionNormalTexture.SizeInBytes, false);
            _vb.SetVertexData(
                _vertices,
                new VertexDescriptor(
                    VertexPositionNormalTexture.SizeInBytes,
                    VertexPositionNormalTexture.ElementCount,
                    0,
                    IntPtr.Zero));
            _ib = factory.CreateIndexBuffer(sizeof(int) * _indices.Length, false);
            _ib.SetIndices(_indices);

            _worldBuffer = factory.CreateConstantBuffer(ShaderConstantType.Matrix4x4);
            _inverseTransposeWorldBuffer = factory.CreateConstantBuffer(ShaderConstantType.Matrix4x4);
            _mtlPropertiesBuffer = factory.CreateConstantBuffer(ShaderConstantType.Float4);

            _texture = TextureCache.GetCachedTexture(rc, _overrideTextureData);
            _textureBinding = factory.CreateShaderTextureBinding(_texture);

            if (_alphaMapNeedsRecreation)
            {
                _alphaMapNeedsRecreation = false;
                RecreateAlphaMapTextureResources(rc);
            }
        }

        private static void CreateSharedResources(ResourceFactory factory)
        {
            if (s_cachedResourceFactory != factory)
            {
                s_cachedResourceFactory = factory;
                CreateShadowPassMaterial(factory);
                CreateRegularPassMaterial(factory);

                s_shadowMapSampler = factory.CreateSamplerState(
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

                s_blankAlphaMapTexture = factory.CreateTexture(new RgbaByte[] { RgbaByte.White }, 1, 1, PixelFormat.R8_G8_B8_A8_UInt);
                s_blankAlphaMapTextureBinding = factory.CreateShaderTextureBinding(s_blankAlphaMapTexture);
            }
        }

        private static void CreateShadowPassMaterial(ResourceFactory factory)
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

            s_shadowPassMaterial = new Material(shaderSet, constantSlots);
        }

        private static void CreateRegularPassMaterial(ResourceFactory factory)
        {
            Shader vs = factory.CreateShader(ShaderStages.Vertex, ShaderHelper.LoadShaderCode("forward_mtl-vertex", ShaderStages.Vertex, factory));
            Shader fs = factory.CreateShader(ShaderStages.Fragment, ShaderHelper.LoadShaderCode("forward_mtl-frag", ShaderStages.Fragment, factory));
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
                new ShaderResourceDescription("CameraInfoBuffer", Unsafe.SizeOf<Camera.Info>()),
                new ShaderResourceDescription("PointLightsBuffer", Unsafe.SizeOf<PointLightsBuffer>()),
                new ShaderResourceDescription("WorldMatrixBuffer", ShaderConstantType.Matrix4x4),
                new ShaderResourceDescription("InverseTransposeWorldMatrixBuffer", ShaderConstantType.Matrix4x4),
                new ShaderResourceDescription("MaterialPropertiesBuffer", ShaderConstantType.Float4),
                new ShaderResourceDescription("SurfaceTexture", ShaderResourceType.Texture, ShaderStages.Fragment),
                new ShaderResourceDescription("SurfaceTexture", ShaderResourceType.Sampler, ShaderStages.Fragment),
                new ShaderResourceDescription("AlphaMap", ShaderResourceType.Texture, ShaderStages.Fragment),
                new ShaderResourceDescription("AlphaMap", ShaderResourceType.Sampler, ShaderStages.Fragment),
                new ShaderResourceDescription("ShadowMap", ShaderResourceType.Texture, ShaderStages.Fragment),
                new ShaderResourceDescription("ShadowMap", ShaderResourceType.Sampler, ShaderStages.Fragment));

            s_regularPassMaterial = new Material(shaderSet, constantSlots);
        }

        private void RecreateAlphaMapTextureResources(RenderContext rc)
        {
            if (!_usesSharedAlphaMap)
            {
                _alphaMapTexture?.Dispose();
                _alphaMapTextureBinding?.Dispose();
            }

            if (_alphaMapTextureData != null)
            {
                _usesSharedAlphaMap = false;
                _alphaMapTexture = _alphaMapTextureData.CreateDeviceTexture(rc.ResourceFactory);
                _alphaMapTextureBinding = rc.ResourceFactory.CreateShaderTextureBinding(_alphaMapTexture);
            }
            else
            {
                _usesSharedAlphaMap = true;
                _alphaMapTexture = s_blankAlphaMapTexture;
                _alphaMapTextureBinding = s_blankAlphaMapTextureBinding;
            }
        }

        public RenderOrderKey GetRenderOrderKey(Vector3 viewPosition)
        {
            float distance = Vector3.Distance(Position, viewPosition);
            uint materialHashCode = (uint)s_regularPassMaterial.GetHashCode();
            materialHashCode = (materialHashCode & 0xFFFF0000) | ((uint)_texture.GetHashCode() & 0x0000FFFF);
            return RenderOrderKey.Create(distance, materialHashCode);
        }

        public IList<string> GetStagesParticipated() => _hasAlphaMap ? _alphaMapStages : _standardStages;

        public void Render(RenderContext rc, string pipelineStage)
        {
            if (_alphaMapNeedsRecreation)
            {
                _alphaMapNeedsRecreation = false;
                RecreateAlphaMapTextureResources(rc);
            }

            rc.VertexBuffer = _vb;
            rc.IndexBuffer = _ib;

            if (pipelineStage == "ShadowMap")
            {
                s_shadowPassMaterial.Apply(rc);
                rc.SetConstantBuffer(0, SharedDataProviders.LightProjMatrixBuffer);
                rc.SetConstantBuffer(1, SharedDataProviders.LightViewMatrixBuffer);
                rc.SetConstantBuffer(2, _worldBuffer);
            }
            else
            {
                Debug.Assert(pipelineStage == (!_hasAlphaMap ? "Standard" : "AlphaBlend"));
                s_regularPassMaterial.Apply(rc);
                rc.SetConstantBuffer(0, SharedDataProviders.ProjectionMatrixBuffer);
                rc.SetConstantBuffer(1, SharedDataProviders.ViewMatrixBuffer);
                rc.SetConstantBuffer(2, SharedDataProviders.LightProjMatrixBuffer);
                rc.SetConstantBuffer(3, SharedDataProviders.LightViewMatrixBuffer);
                rc.SetConstantBuffer(4, SharedDataProviders.LightInfoBuffer);
                rc.SetConstantBuffer(5, SharedDataProviders.CameraInfoBuffer);
                rc.SetConstantBuffer(6, SharedDataProviders.PointLightsBuffer);
                rc.SetConstantBuffer(7, _worldBuffer);
                rc.SetConstantBuffer(8, _inverseTransposeWorldBuffer);
                rc.SetConstantBuffer(9, _mtlPropertiesBuffer);

                rc.SetTexture(10, _textureBinding);
                rc.SetSamplerState(11, rc.Anisox4Sampler); // Surface texture
                rc.SetTexture(12, _alphaMapTextureBinding);
                rc.SetSamplerState(13, rc.PointSampler); // Alpha map
                rc.SetTexture(14, SharedTextures.GetTextureBinding("ShadowMap"));
                rc.SetSamplerState(15, s_shadowMapSampler); // Shadow map

                if (_hasAlphaMap)
                {
                    rc.SetBlendState(rc.AlphaBlend);
                }
            }

            rc.DrawIndexedPrimitives(_indices.Length, 0);

            if (_hasAlphaMap)
            {
                rc.SetBlendState(rc.OverrideBlend);
            }
        }

        private void Serialize<T>(ref T value)
        {
            JsonSerializer js = new JsonSerializer();
            js.TypeNameHandling = TypeNameHandling.All;
            var fileName = typeof(T).Name + ".json";

            using (var fs = File.CreateText(fileName))
            {
                js.Serialize(fs, value);
            }

            using (var fs = File.OpenText(fileName))
            {
                value = js.Deserialize<T>(new JsonTextReader(fs));
            }
        }

        public void Dispose()
        {
            // _texture is a shared object -- don't dispose it.
            _textureBinding?.Dispose();

            if (!_usesSharedAlphaMap)
            {
                _alphaMapTexture.Dispose();
                _alphaMapTexture = null;
                _alphaMapTextureBinding.Dispose();
                _alphaMapTextureBinding = null;
            }

            _alphaMapNeedsRecreation = true;
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
                return BoundingBox.CreateFromVertices(_vertices, Rotation, Position, Scale);
            }
        }

        public override string ToString()
        {
            return string.Format("{0}, {1}", Name, BoundingBox.GetCenter());
        }
    }

    public struct MtlMaterialProperties : IEquatable<MtlMaterialProperties>
    {
        public Vector3 SpecularIntensity;
        public float SpecularPower;

        public MtlMaterialProperties(Vector3 specularIntensity, float specularPower)
        {
            SpecularIntensity = specularIntensity;
            SpecularPower = specularPower;
        }

        public bool Equals(MtlMaterialProperties other)
        {
            return SpecularIntensity.Equals(other.SpecularIntensity) && SpecularPower.Equals(other.SpecularPower);
        }
    }

}