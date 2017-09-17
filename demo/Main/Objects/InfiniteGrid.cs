using System;
using System.Numerics;
using Veldrid.Graphics;

namespace Veldrid.NeoDemo.Objects
{
    public class InfiniteGrid : CullRenderable
    {
        private ShaderSet _shaderSet;
        private ShaderResourceBindingSlots _resourceBindings;
        private VertexBuffer _vb;
        private IndexBuffer _ib;
        private DeviceTexture _gridTexture;
        private ShaderTextureBinding _textureBinding;
        private RasterizerState _rasterizerState;
        private readonly BoundingBox _boundingBox
            = new BoundingBox(new Vector3(-1000, -1, -1000), new Vector3(1000, 1, 1000));

        public override void CreateDeviceObjects(RenderContext rc)
        {
            ResourceFactory factory = rc.ResourceFactory;
            _vb = factory.CreateVertexBuffer(
                new VertexPosition[]
                {
                    new VertexPosition(new Vector3(-1000, 0, -1000)),
                    new VertexPosition(new Vector3(+1000, 0, -1000)),
                    new VertexPosition(new Vector3(+1000, 0, +1000)),
                    new VertexPosition(new Vector3(-1000, 0, +1000)),
                },
                new VertexDescriptor(VertexPosition.SizeInBytes, VertexPosition.ElementCount),
                false);
            _ib = factory.CreateIndexBuffer(new ushort[] { 0, 1, 2, 0, 2, 3 }, false);

            Shader vs = ShaderHelper.LoadShader(factory, "Grid-vertex", ShaderStages.Vertex);
            Shader fs = ShaderHelper.LoadShader(factory, "Grid-fragment", ShaderStages.Fragment);
            VertexInputLayout inputLayout = factory.CreateInputLayout(
                new VertexInputDescription(VertexPosition.SizeInBytes, new VertexInputElement("Position", VertexSemanticType.Position, VertexElementFormat.Float3)));
            _shaderSet = factory.CreateShaderSet(inputLayout, vs, fs);
            _resourceBindings = factory.CreateShaderResourceBindingSlots(
                _shaderSet,
                new ShaderResourceDescription("ProjectionBuffer", ShaderConstantType.Matrix4x4),
                new ShaderResourceDescription("ViewBuffer", ShaderConstantType.Matrix4x4),
                new ShaderResourceDescription("GridTexture", ShaderResourceType.Texture),
                new ShaderResourceDescription("GridSampler", ShaderResourceType.Sampler));

            const int gridSize = 64;
            RgbaByte[] pixels = CreateGridTexturePixels(gridSize, 1, RgbaByte.White, new RgbaByte());
            _gridTexture = factory.CreateTexture(pixels, gridSize, gridSize, PixelFormat.R8_G8_B8_A8_UInt);
            _textureBinding = factory.CreateShaderTextureBinding(_gridTexture);

            _rasterizerState = factory.CreateRasterizerState(FaceCullingMode.None, TriangleFillMode.Solid, true, true);
        }

        public override void DestroyDeviceObjects()
        {
            _shaderSet.Dispose();
            _vb.Dispose();
            _ib.Dispose();
            _gridTexture.Dispose();
            _textureBinding.Dispose();
            _rasterizerState.Dispose();
        }

        public override bool Cull(ref BoundingFrustum visibleFrustum)
        {
            return visibleFrustum.Contains(_boundingBox) == ContainmentType.Disjoint;
        }

        public override void Render(RenderContext rc, SceneContext sc, RenderPasses renderPass)
        {
            rc.ShaderSet = _shaderSet;
            rc.ShaderResourceBindingSlots = _resourceBindings;
            rc.SetConstantBuffer(0, sc.ProjectionMatrixBuffer);
            rc.SetConstantBuffer(1, sc.ViewMatrixBuffer);
            rc.SetTexture(2, _textureBinding);
            rc.SetSamplerState(3, rc.PointSampler);
            rc.VertexBuffer = _vb;
            rc.IndexBuffer = _ib;
            BlendState previousBlend = rc.BlendState;
            rc.BlendState = rc.AlphaBlend;
            RasterizerState previousRS = rc.RasterizerState;
            rc.RasterizerState = _rasterizerState;
            rc.DrawIndexedPrimitives(6);
            rc.BlendState = previousBlend;
            rc.RasterizerState = previousRS;
        }

        public override RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition)
        {
            return RenderOrderKey.Create(_shaderSet.GetHashCode(), cameraPosition.Length());
        }

        public override RenderPasses RenderPasses => RenderPasses.AlphaBlend;

        public override BoundingBox BoundingBox => _boundingBox;

        private RgbaByte[] CreateGridTexturePixels(int dimensions, int borderPixels, RgbaByte borderColor, RgbaByte backgroundColor)
        {
            RgbaByte[] ret = new RgbaByte[dimensions * dimensions];

            for (int y = 0; y < dimensions; y++)
            {
                for (int x = 0; x < dimensions; x++)
                {
                    if ((y < borderPixels) || (dimensions - 1 - y < borderPixels)
                        || (x < borderPixels) || (dimensions - 1 - x < borderPixels))
                    {
                        ret[x + (y * dimensions)] = borderColor;
                    }
                    else
                    {
                        ret[x + (y * dimensions)] = backgroundColor;
                    }
                }
            }

            return ret;
        }
    }
}
