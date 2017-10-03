using System;
using System.Numerics;
using System.Runtime.CompilerServices;
using Veldrid.Graphics;
using Veldrid.Platform;

namespace Veldrid.NeoDemo.Objects
{
    public class Simple2DObject : Renderable
    {
        private readonly TextureData _texData;

        private VertexBuffer _vb;
        private IndexBuffer _ib;
        private ShaderSet _shaderSet;
        private ShaderResourceBindingSlots _resourceSlots;
        private DeviceTexture2D _tex;
        private ShaderTextureBinding _texBinding;

        private ConstantBuffer _orthographicBuffer;
        private ConstantBuffer _sizeInfoBuffer;

        private Vector2 _position;
        private Vector2 _size = new Vector2(200, 200);

        public Vector2 Position { get => _position; set { _position = value; UpdateSizeInfoBuffer(); } }

        public Vector2 Size { get => _size; set { _size = value; UpdateSizeInfoBuffer(); } }

        private void UpdateSizeInfoBuffer()
        {
            SizeInfo si = new SizeInfo { Size = _size, Position = _position };
            _sizeInfoBuffer.SetData(ref si);
        }

        public Simple2DObject(TextureData texData, Window window)
        {
            _texData = texData;
            window.Resized += OnWindowResized;
            _window = window;
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
            Simple2DSetInfo.CreateAll(
                factory,
                ShaderHelper.LoadBytecode(factory, "Simple2D", ShaderStages.Vertex),
                ShaderHelper.LoadBytecode(factory, "Simple2D", ShaderStages.Fragment),
                out _shaderSet,
                out _resourceSlots);

            _tex = _texData.CreateDeviceTexture(factory);
            _texBinding = factory.CreateShaderTextureBinding(_tex);
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
            _texBinding.Dispose();
            _tex.Dispose();
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
            rc.SetTexture(2, sc.ShadowMapBinding);
            rc.SetSamplerState(3, rc.PointSampler);
            rc.DrawIndexedPrimitives(s_quadIndices.Length);
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

        public struct SizeInfo
        {
            public Vector2 Position;
            public Vector2 Size;
        }
    }
}
