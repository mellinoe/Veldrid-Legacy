using System;
using Veldrid.Graphics;

namespace Veldrid.NeoDemo.Objects
{
    public class InfiniteGrid : CullRenderable
    {
        private ShaderSet _shaderSet;
        private ShaderResourceBindingSlots _resourceBindings;
        private VertexBuffer _vb;
        private IndexBuffer _ib;

        public override void CreateDeviceObjects(RenderContext rc)
        {
            ResourceFactory factory = rc.ResourceFactory;
            _vb = factory.CreateVertexBuffer(
                new VertexPosition[]
                {
                    new VertexPosition(new System.Numerics.Vector3(-9999999, 0, -9999999)),
                    new VertexPosition(new System.Numerics.Vector3(+9999999, 0, -9999999)),
                    new VertexPosition(new System.Numerics.Vector3(+9999999, 0, +9999999)),
                    new VertexPosition(new System.Numerics.Vector3(-9999999, 0, +9999999)),
                },
                new VertexDescriptor(VertexPosition.SizeInBytes, VertexPosition.ElementCount),
                false);
            _ib = factory.CreateIndexBuffer(new ushort[] { 0, 1, 2, 0, 2, 3 }, false);

            Shader vs = ShaderHelper.LoadShader(factory, "grid-vertex", ShaderStages.Vertex);
            Shader fs = ShaderHelper.LoadShader(factory, "grid-frag", ShaderStages.Fragment);
            VertexInputLayout inputLayout = factory.CreateInputLayout(
                new VertexInputDescription(VertexPosition.SizeInBytes, new VertexInputElement("vsin_position", VertexSemanticType.Position, VertexElementFormat.Float3)));
            _shaderSet = factory.CreateShaderSet(inputLayout, vs, fs);
            _resourceBindings = factory.CreateShaderResourceBindingSlots(
                _shaderSet,
                new ShaderResourceDescription("ProjectionMatrixBuffer", ShaderConstantType.Matrix4x4),
                new ShaderResourceDescription("ViewMatrixBuffer", ShaderConstantType.Matrix4x4));
        }

        public override void DestroyDeviceObjects()
        {
            _shaderSet.Dispose();
            _vb.Dispose();
            _ib.Dispose();
        }

        public override bool Cull(ref BoundingFrustum visibleFrustum)
        {
            return false;
        }

        public override void Render(RenderContext rc, SceneContext sc)
        {
            rc.ShaderSet = _shaderSet;
            rc.ShaderResourceBindingSlots = _resourceBindings;
            rc.SetConstantBuffer(0, sc.ProjectionMatrixBuffer);
            rc.SetConstantBuffer(1, sc.ViewMatrixBuffer);
            rc.VertexBuffer = _vb;
            rc.IndexBuffer = _ib;
            rc.DrawIndexedPrimitives(6);
        }
    }
}
