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
        private DeviceTexture _gridTexture;
        private ShaderTextureBinding _textureBinding;

        public override void CreateDeviceObjects(RenderContext rc)
        {
            ResourceFactory factory = rc.ResourceFactory;
            _vb = factory.CreateVertexBuffer(
                new VertexPosition[]
                {
                    new VertexPosition(new System.Numerics.Vector3(-1000, 0, -1000)),
                    new VertexPosition(new System.Numerics.Vector3(+1000, 0, -1000)),
                    new VertexPosition(new System.Numerics.Vector3(+1000, 0, +1000)),
                    new VertexPosition(new System.Numerics.Vector3(-1000, 0, +1000)),
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
                new ShaderResourceDescription("ViewMatrixBuffer", ShaderConstantType.Matrix4x4),
                new ShaderResourceDescription("GridTexture", ShaderResourceType.Texture),
                new ShaderResourceDescription("GridSampler", ShaderResourceType.Sampler));

            const int gridSize = 64;
            RgbaByte[] pixels = CreateGridTexturePixels(gridSize, 2, RgbaByte.White, new RgbaByte());
            _gridTexture = factory.CreateTexture(pixels, gridSize, gridSize, PixelFormat.R8_G8_B8_A8_UInt);
            _textureBinding = factory.CreateShaderTextureBinding(_gridTexture);
        }

        public override void DestroyDeviceObjects()
        {
            _shaderSet.Dispose();
            _vb.Dispose();
            _ib.Dispose();
            _gridTexture.Dispose();
            _textureBinding.Dispose();
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
            rc.SetTexture(2, _textureBinding);
            rc.SetSamplerState(3, rc.LinearSampler);
            rc.VertexBuffer = _vb;
            rc.IndexBuffer = _ib;
            BlendState previousBlend = rc.BlendState;
            rc.BlendState = rc.AlphaBlend;
            rc.DrawIndexedPrimitives(6);
            rc.BlendState = previousBlend;
        }

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
