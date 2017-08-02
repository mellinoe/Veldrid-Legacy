using ImGuiNET;
using System;
using System.Collections.Generic;
using System.IO;
using System.Numerics;
using System.Runtime.CompilerServices;
using Veldrid.Assets;
using Veldrid.Graphics;
using Veldrid.Graphics.Pipeline;
using Veldrid.RenderDemo.ForwardRendering;
using Veldrid.RenderDemo.Models;

namespace Veldrid.RenderDemo.Drawers
{
    public class ModelDrawer : Drawer<ConstructedMeshInfo>
    {
        private static ConditionalWeakTable<ConstructedMeshInfo, PreviewScene> _previewScenes = new ConditionalWeakTable<ConstructedMeshInfo, PreviewScene>();
        private static RenderContext s_validContext;

        public override bool Draw(string label, ref ConstructedMeshInfo obj, RenderContext rc)
        {
            Vector2 region = ImGui.GetContentRegionAvailable();
            float minDimension = Math.Min(900, Math.Min(region.X, region.Y)) - 50;
            Vector2 imageDimensions = new Vector2(minDimension, minDimension / (1.33f));

            PreviewScene scene;
            scene = GetOrCreateScene(obj, rc);
            scene.Size = new Point((int)imageDimensions.X, (int)imageDimensions.Y);
            scene.RenderFrame();
            IntPtr id = ImGuiImageHelper.GetOrCreateImGuiBinding(rc, scene.RenderedScene);
            ImGui.Image(id, new Vector2(scene.Width, scene.Height), rc.TopLeftUv, rc.BottomRightUv, Vector4.One, Vector4.One);

            return false;
        }

        private static PreviewScene GetOrCreateScene(ConstructedMeshInfo obj, RenderContext rc)
        {
            if (s_validContext != rc)
            {
                s_validContext = rc;
                _previewScenes = new ConditionalWeakTable<ConstructedMeshInfo, PreviewScene>();
            }

            PreviewScene scene;
            if (!_previewScenes.TryGetValue(obj, out scene))
            {
                scene = new PreviewScene(rc, obj);
                _previewScenes.Add(obj, scene);
            }

            return scene;
        }

        private class PreviewScene
        {
            private Point _size = new Point(500, 360);
            public Point Size { get { return _size; } set { if (_size != value) { _size = value; OnSizeChanged(); } } }

            public int Width => _size.X;
            public int Height => _size.Y;

            public float Fov { get; set; } = 1.05f;

            public bool AutoRotateCamera { get; set; } = true;

            RenderContext _rc;
            private Framebuffer _fb;

            private readonly PreviewModel _previewItem;
            private readonly PreviewModel _floor;

            private readonly ConstantBuffer _projection;
            private readonly ConstantBuffer _view;

            private readonly ConstantBuffer _lightProjection;
            private readonly ConstantBuffer _lightView;
            private readonly ConstantBuffer _lightInfo;

            private readonly Dictionary<string, ConstantBuffer> SceneBuffers = new Dictionary<string, ConstantBuffer>();

            private readonly PipelineStage[] _stages;
            private readonly FlatListVisibilityManager _visiblityManager;
            private Vector3 _lightDirection = Vector3.Normalize(new Vector3(-1f, -.6f, -.3f));
            private Vector3 _cameraPosition;
            private double _circleWidth = 10.0f;
            private readonly StandardPipelineStage _standardPipelineStage;
            private readonly ShadowMapStage _shadowMapStage;

            public PreviewScene(RenderContext rc, ConstructedMeshInfo previewItem)
            {
                _rc = rc;
                ResourceFactory factory = rc.ResourceFactory;
                _fb = factory.CreateFramebuffer(Width, Height);

                _projection = factory.CreateConstantBuffer(ShaderConstantType.Matrix4x4);
                UpdateProjectionData();
                _view = factory.CreateConstantBuffer(ShaderConstantType.Matrix4x4);
                _view.SetData(Matrix4x4.CreateLookAt(Vector3.UnitZ * 7f + Vector3.UnitY * 1.5f, Vector3.Zero, Vector3.UnitY));

                _lightProjection = factory.CreateConstantBuffer(ShaderConstantType.Matrix4x4);
                _lightProjection.SetData(Matrix4x4.CreateOrthographicOffCenter(-18, 18, -18, 18, -10, 60f));

                _lightView = factory.CreateConstantBuffer(ShaderConstantType.Matrix4x4);
                _lightView.SetData(Matrix4x4.CreateLookAt(-_lightDirection * 20f, Vector3.Zero, Vector3.UnitY));

                _lightInfo = factory.CreateConstantBuffer(ShaderConstantType.Float4);
                _lightInfo.SetData(new Vector4(_lightDirection, 1));

                _standardPipelineStage = new StandardPipelineStage(rc, "Standard", _fb);
                _shadowMapStage = new ShadowMapStage(rc, "ShadowMap_Preview");
                _stages = new PipelineStage[]
                {
                    _shadowMapStage,
                    _standardPipelineStage,
                };

                SceneBuffers.Add("ProjectionMatrix", _projection);
                SceneBuffers.Add("ViewMatrix", _view);
                SceneBuffers.Add("LightProjMatrix", _lightProjection);
                SceneBuffers.Add("LightViewMatrix", _lightView);
                SceneBuffers.Add("LightInfo", _lightInfo);

                _floor = CreatePreviewModel(PlaneModel.Vertices, PlaneModel.Indices);
                _floor.WorldMatrix.Data = Matrix4x4.CreateScale(10f, 1f, 10f);

                _previewItem = CreatePreviewModel(previewItem.Vertices, previewItem.Indices);
                _previewItem.WorldMatrix.Data = Matrix4x4.CreateTranslation(0, 1.5f, 0);

                _visiblityManager = new FlatListVisibilityManager();
                _visiblityManager.AddRenderItem(_floor);
                _visiblityManager.AddRenderItem(_previewItem);
            }

            public DeviceTexture RenderedScene => _fb.ColorTexture;

            private PreviewModel CreatePreviewModel(VertexPositionNormalTexture[] vertices, ushort[] indices)
            {
                AssetDatabase lfd = new LooseFileDatabase(Path.Combine(AppContext.BaseDirectory, "Assets"));
                VertexBuffer vb = _rc.ResourceFactory.CreateVertexBuffer(vertices.Length * VertexPositionNormalTexture.SizeInBytes, false);
                vb.SetVertexData(
                    vertices,
                    new VertexDescriptor(VertexPositionNormalTexture.SizeInBytes, VertexPositionNormalTexture.ElementCount, 0, IntPtr.Zero));

                IndexBuffer ib = _rc.ResourceFactory.CreateIndexBuffer(indices.Length * sizeof(ushort), false);
                ib.SetIndices(indices);

                Material shadowmapMaterial = ShadowCaster.CreateShadowPassMaterial(_rc.ResourceFactory);
                Material regularMaterial = ShadowCaster.CreateRegularPassMaterial(_rc.ResourceFactory);

                var deviceTexture = lfd.LoadAsset<ImageSharpMipmapChain>("Textures/CubeTexture.png").CreateDeviceTexture(_rc.ResourceFactory);
                var textureBinding = _rc.ResourceFactory.CreateShaderTextureBinding(deviceTexture);

                return new PreviewModel(
                    vb,
                    ib,
                    indices.Length,
                    regularMaterial,
                    shadowmapMaterial,
                    _rc.ResourceFactory.CreateConstantBuffer(ShaderConstantType.Matrix4x4),
                    new DynamicDataProvider<Matrix4x4>(Matrix4x4.Identity),
                    _rc.ResourceFactory.CreateConstantBuffer(ShaderConstantType.Matrix4x4),
                    SceneBuffers,
                    textureBinding);
            }

            public void RenderFrame()
            {
                UpdateCamera();

                _rc.SetFramebuffer(_fb);
                _rc.ClearBuffer(RgbaFloat.Clear);
                foreach (var stage in _stages)
                {
                    stage.ExecuteStage(_visiblityManager, _cameraPosition);
                }
            }

            private void UpdateCamera()
            {
                float timeFactor = (float)DateTime.Now.TimeOfDay.TotalMilliseconds / 1000;
                if (AutoRotateCamera)
                {
                    _cameraPosition = new Vector3(
                        (float)(Math.Cos(timeFactor) * _circleWidth),
                        6 + (float)Math.Sin(timeFactor) * 2,
                        (float)(Math.Sin(timeFactor) * _circleWidth));
                    _view.SetData(Matrix4x4.CreateLookAt(_cameraPosition, -_cameraPosition, Vector3.UnitY));
                }
            }

            private void UpdateProjectionData()
            {
                _projection.SetData(Matrix4x4.CreatePerspectiveFieldOfView(Fov, (float)Width / Height, 0.1f, 100f));
            }

            private void OnSizeChanged()
            {
                _fb.Dispose();
                _fb = _rc.ResourceFactory.CreateFramebuffer(Width, Height);
                _standardPipelineStage.OverrideFramebuffer = _fb;
                UpdateProjectionData();
            }
        }

        private class PreviewModel : RenderItem
        {
            private readonly VertexBuffer _vb;
            private readonly IndexBuffer _ib;
            private readonly int _elementCount;
            private readonly Material _shadowmapMaterial;
            private readonly Material _regularMaterial;
            private readonly ConstantBuffer _worldBuffer;
            private readonly DynamicDataProvider<Matrix4x4> _worldProvider;
            private readonly ConstantBuffer _inverseTransposeWorldBuffer;
            private readonly ConstantBufferDataProvider _inverseWorldProvider;
            private readonly ShaderTextureBinding _textureBinding;
            private readonly Dictionary<string, ConstantBuffer> _buffersDict = new Dictionary<string, ConstantBuffer>();

            private static readonly string[] s_stages = new string[] { "ShadowMap", "Standard" };

            public DynamicDataProvider<Matrix4x4> WorldMatrix => _worldProvider;

            public PreviewModel(
                VertexBuffer vb,
                IndexBuffer ib,
                int elementCount,
                Material regularMaterial,
                Material shadowmapMaterial,
                ConstantBuffer worldBuffer,
                DynamicDataProvider<Matrix4x4> worldProvider,
                ConstantBuffer inverseTransposeWorldBuffer,
                Dictionary<string, ConstantBuffer> buffersDict,
                ShaderTextureBinding surfaceTextureBinding
                )
            {
                _vb = vb;
                _ib = ib;
                _elementCount = elementCount;
                _regularMaterial = regularMaterial;
                _shadowmapMaterial = shadowmapMaterial;
                _worldProvider = worldProvider;
                _inverseWorldProvider = new DependantDataProvider<Matrix4x4>(worldProvider, Utilities.CalculateInverseTranspose);
                _textureBinding = surfaceTextureBinding;

                _worldBuffer = worldBuffer;
                _inverseTransposeWorldBuffer = inverseTransposeWorldBuffer;
                _buffersDict = buffersDict;
            }

            public void Render(RenderContext rc, string stage)
            {
                rc.VertexBuffer = _vb;
                rc.IndexBuffer = _ib;
                if (stage == "ShadowMap")
                {
                    _shadowmapMaterial.Apply(rc);
                    rc.SetConstantBuffer(0, _buffersDict["LightProjMatrix"]);
                    rc.SetConstantBuffer(1, _buffersDict["LightViewMatrix"]);
                    _worldProvider.SetData(_worldBuffer);
                    rc.SetConstantBuffer(2, _worldBuffer);
                }
                else
                {
                    _regularMaterial.Apply(rc);

                    rc.SetConstantBuffer(0, _buffersDict["ProjectionMatrix"]);
                    rc.SetConstantBuffer(1, _buffersDict["ViewMatrix"]);
                    rc.SetConstantBuffer(2, _buffersDict["LightProjMatrix"]);
                    rc.SetConstantBuffer(3, _buffersDict["LightViewMatrix"]);
                    rc.SetConstantBuffer(4, _buffersDict["LightInfo"]);
                    _worldProvider.SetData(_worldBuffer);
                    rc.SetConstantBuffer(5, _worldBuffer);
                    _inverseWorldProvider.SetData(_inverseTransposeWorldBuffer);
                    rc.SetConstantBuffer(6, _inverseTransposeWorldBuffer);

                    rc.SetTexture(7, _textureBinding);
                    rc.SetSamplerState(8, rc.Anisox4Sampler);
                    rc.SetTexture(9, SharedTextures.GetTextureBinding("ShadowMap_Preview"));
                    rc.SetSamplerState(10, rc.PointSampler);
                }

                rc.DrawIndexedPrimitives(_elementCount, 0);
            }

            public IList<string> GetStagesParticipated()
            {
                return s_stages;
            }

            public RenderOrderKey GetRenderOrderKey(Vector3 viewPosition)
            {
                return new RenderOrderKey();
            }

            public bool Cull(ref BoundingFrustum visibleFrustum)
            {
                return false;
            }
        }
    }
}
