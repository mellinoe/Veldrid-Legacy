using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Veldrid.Graphics;
using Veldrid.Platform;

namespace Veldrid.NeoDemo.Objects
{
    public class ShadowmapDrawer : Renderable
    {
        private VertexBuffer _vb;
        private IndexBuffer _ib;
        private ShaderSet _shaderSet;
        private ShaderResourceBindingSlots _resourceSlots;
        private ConstantBuffer _orthographicBuffer;
        private DepthStencilState _dss;
        private ConstantBuffer _sizeInfoBuffer;

        private Vector2 _position;
        private Vector2 _size = new Vector2(100, 100);

        public Vector2 Position { get => _position; set { _position = value; UpdateSizeInfoBuffer(); } }

        public Vector2 Size { get => _size; set { _size = value; UpdateSizeInfoBuffer(); } }

        private void UpdateSizeInfoBuffer()
        {
            SizeInfo si = new SizeInfo { Size = _size, Position = _position };
            _sizeInfoBuffer.SetData(ref si);
        }

        public ShadowmapDrawer(Window window, Func<ShaderTextureBinding> bindingGetter)
        {
            window.Resized += OnWindowResized;
            _window = window;
            _bindingGetter = bindingGetter;
        }

        private void OnWindowResized()
        {
            _orthographicBuffer.SetData(Matrix4x4.CreateOrthographicOffCenter(0, _window.Width, _window.Height, 0, -1, 1));
        }

        public override void CreateDeviceObjects(RenderContext rc)
        {
            ResourceFactory factory = rc.ResourceFactory;
            _vb = factory.CreateVertexBuffer(s_quadVerts, new VertexDescriptor(16, 2), false);
            _ib = factory.CreateIndexBuffer(s_quadIndices, false);
            ShadowmapPreviewShaderSetInfo.CreateAll(
                factory,
                ShaderHelper.LoadBytecode(factory, "ShadowmapPreviewShader", ShaderStages.Vertex),
                ShaderHelper.LoadBytecode(factory, "ShadowmapPreviewShader", ShaderStages.Fragment),
                out _shaderSet,
                out _resourceSlots);

            _dss = factory.CreateDepthStencilState(false, DepthComparison.Always);

            _sizeInfoBuffer = factory.CreateConstantBuffer(Unsafe.SizeOf<SizeInfo>());
            UpdateSizeInfoBuffer();
            _orthographicBuffer = factory.CreateConstantBuffer(ShaderConstantType.Matrix4x4);
            OnWindowResized();
        }

        public override void DestroyDeviceObjects()
        {
            _vb.Dispose();
            _ib.Dispose();
            _shaderSet.Dispose();
            _sizeInfoBuffer.Dispose();
            _orthographicBuffer.Dispose();
        }

        public override RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition)
        {
            return RenderOrderKey.Create(_shaderSet.GetHashCode(), 0);
        }

        public override RenderPasses RenderPasses => RenderPasses.Overlay;

        public override void Render(RenderContext rc, SceneContext sc, RenderPasses renderPass)
        {
            rc.SetVertexBuffer(0, _vb);
            rc.IndexBuffer = _ib;
            rc.ShaderSet = _shaderSet;
            rc.ShaderResourceBindingSlots = _resourceSlots;
            rc.SetConstantBuffer(0, _orthographicBuffer);
            rc.SetConstantBuffer(1, _sizeInfoBuffer);
            rc.SetTexture(2, _bindingGetter());
            rc.SetSamplerState(3, rc.PointSampler);
            DepthStencilState dss = rc.DepthStencilState;
            rc.DepthStencilState = _dss;
            rc.DrawIndexedPrimitives(s_quadIndices.Length);
            rc.DepthStencilState = dss;
        }

        private static float[] s_quadVerts = new float[]
        {
            0, 0, 0, 0,
            1, 0, 1, 0,
            1, 1, 1, 1,
            0, 1, 0, 1
        };

        private static ushort[] s_quadIndices = new ushort[] { 0, 1, 2, 0, 2, 3 };
        private readonly Window _window;
        private readonly Func<ShaderTextureBinding> _bindingGetter;

        public struct SizeInfo
        {
            public Vector2 Position;
            public Vector2 Size;
        }
    }
}
