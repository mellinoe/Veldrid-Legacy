using System;
using System.Numerics;
using Veldrid.Graphics;

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

        public Simple2DObject(TextureData texData)
        {
            _texData = texData;
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
        }

        public override void DestroyDeviceObjects()
        {
            _vb.Dispose();
            _ib.Dispose();
            _shaderSet.Dispose();
            _texBinding.Dispose();
            _tex.Dispose();
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
            rc.SetTexture(0, _texBinding);
            rc.SetSamplerState(1, rc.PointSampler);
            rc.DrawIndexedPrimitives(s_quadIndices.Length);
        }

        private static float[] s_quadVerts = new float[]
        {
            -1, 1, 0, 0,
            1, 1, 1, 0,
            1, -1, 1, 1,
            -1, -1, 0, 1
        };

        private static ushort[] s_quadIndices = new ushort[] { 0, 1, 2, 0, 2, 3 };
    }
}
