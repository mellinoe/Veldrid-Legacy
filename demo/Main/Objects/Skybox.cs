using SixLabors.ImageSharp;
using SixLabors.ImageSharp.Advanced;
using System;
using System.Numerics;
using Veldrid.Graphics;

namespace Veldrid.NeoDemo.Objects
{
    public class Skybox : Renderable
    {
        private readonly ImageSharpTexture _front;
        private readonly ImageSharpTexture _back;
        private readonly ImageSharpTexture _left;
        private readonly ImageSharpTexture _right;
        private readonly ImageSharpTexture _top;
        private readonly ImageSharpTexture _bottom;

        // Context objects
        private VertexBuffer _vb;
        private IndexBuffer _ib;
        private ConstantBuffer _viewMatrixBuffer;
        private ShaderTextureBinding _cubemapBinding;
        private RasterizerState _rasterizerState;
        private ShaderResourceBindingSlots _resourceSlots;
        private ShaderSet _shaderSet;
        private CubemapTexture _cubemapTexture;

        public Skybox(
            ImageSharpTexture front, ImageSharpTexture back, ImageSharpTexture left,
            ImageSharpTexture right, ImageSharpTexture top, ImageSharpTexture bottom)
        {
            _front = front;
            _back = back;
            _left = left;
            _right = right;
            _top = top;
            _bottom = bottom;
        }

        public unsafe override void CreateDeviceObjects(RenderContext rc)
        {
            ResourceFactory factory = rc.ResourceFactory;

            _vb = factory.CreateVertexBuffer(s_vertices.Length * VertexPosition.SizeInBytes, false);
            _vb.SetVertexData(s_vertices, new VertexDescriptor(VertexPosition.SizeInBytes, 1, IntPtr.Zero));

            _ib = factory.CreateIndexBuffer(s_indices.Length * sizeof(int), false);
            _ib.SetIndices(s_indices);

            Shader vs = ShaderHelper.LoadShader(factory, "Skybox", ShaderStages.Vertex);
            Shader fs = ShaderHelper.LoadShader(factory, "Skybox", ShaderStages.Fragment);
            VertexInputLayout inputLayout = factory.CreateInputLayout(
                new VertexInputDescription(
                    12,
                    new VertexInputElement("Position", VertexSemanticType.Position, VertexElementFormat.Float3)));
            _shaderSet = factory.CreateShaderSet(inputLayout, vs, fs);
            _resourceSlots = factory.CreateShaderResourceBindingSlots(
                _shaderSet,
                new ShaderResourceDescription("ProjectionBuffer", ShaderConstantType.Matrix4x4),
                new ShaderResourceDescription("ViewBuffer", ShaderConstantType.Matrix4x4),
                new ShaderResourceDescription("CubeTexture", ShaderResourceType.Texture),
                new ShaderResourceDescription("CubeSampler", ShaderResourceType.Sampler));

            _viewMatrixBuffer = factory.CreateConstantBuffer(ShaderConstantType.Matrix4x4);

            fixed (Rgba32* frontPin = &_front.ISImage.DangerousGetPinnableReferenceToPixelBuffer())
            fixed (Rgba32* backPin = &_back.ISImage.DangerousGetPinnableReferenceToPixelBuffer())
            fixed (Rgba32* leftPin = &_left.ISImage.DangerousGetPinnableReferenceToPixelBuffer())
            fixed (Rgba32* rightPin = &_right.ISImage.DangerousGetPinnableReferenceToPixelBuffer())
            fixed (Rgba32* topPin = &_top.ISImage.DangerousGetPinnableReferenceToPixelBuffer())
            fixed (Rgba32* bottomPin = &_bottom.ISImage.DangerousGetPinnableReferenceToPixelBuffer())
            {
                _cubemapTexture = factory.CreateCubemapTexture(
                    (IntPtr)frontPin,
                    (IntPtr)backPin,
                    (IntPtr)leftPin,
                    (IntPtr)rightPin,
                    (IntPtr)topPin,
                    (IntPtr)bottomPin,
                    _front.Width,
                    _front.Height,
                    _front.PixelSizeInBytes,
                    _front.Format);
                _cubemapBinding = factory.CreateShaderTextureBinding(_cubemapTexture);
            }

            _rasterizerState = factory.CreateRasterizerState(FaceCullingMode.None, TriangleFillMode.Solid, false, false);
        }

        public static Skybox LoadDefaultSkybox()
        {
            return new Skybox(
                new ImageSharpTexture(AssetHelper.GetPath("Textures/cloudtop/cloudtop_ft.png")),
                new ImageSharpTexture(AssetHelper.GetPath("Textures/cloudtop/cloudtop_bk.png")),
                new ImageSharpTexture(AssetHelper.GetPath("Textures/cloudtop/cloudtop_lf.png")),
                new ImageSharpTexture(AssetHelper.GetPath("Textures/cloudtop/cloudtop_rt.png")),
                new ImageSharpTexture(AssetHelper.GetPath("Textures/cloudtop/cloudtop_up.png")),
                new ImageSharpTexture(AssetHelper.GetPath("Textures/cloudtop/cloudtop_dn.png")));
        }

        public override void DestroyDeviceObjects()
        {
            _shaderSet.Dispose();
            _cubemapTexture.Dispose();
            _cubemapBinding.Dispose();
            _vb.Dispose();
            _ib.Dispose();
            _viewMatrixBuffer.Dispose();
            _rasterizerState.Dispose();
        }

        public override void Render(RenderContext rc, SceneContext sc, RenderPasses renderPass)
        {
            rc.VertexBuffer = _vb;
            rc.IndexBuffer = _ib;
            rc.ShaderSet = _shaderSet;
            rc.ShaderResourceBindingSlots = _resourceSlots;
            rc.SetConstantBuffer(0, sc.ProjectionMatrixBuffer);
            Matrix4x4 viewMat = Utilities.ConvertToMatrix3x3(sc.Camera.ViewMatrix);
            _viewMatrixBuffer.SetData(ref viewMat);
            rc.SetConstantBuffer(1, _viewMatrixBuffer);
            RasterizerState previousRasterState = rc.RasterizerState;
            rc.SetRasterizerState(_rasterizerState);
            rc.SetTexture(2, _cubemapBinding);
            rc.DrawIndexedPrimitives(s_indices.Length, 0);
            rc.SetRasterizerState(previousRasterState);
        }

        public override RenderOrderKey GetRenderOrderKey(Vector3 cameraPosition)
        {
            return new RenderOrderKey(ulong.MaxValue);
        }

        private static readonly VertexPosition[] s_vertices = new VertexPosition[]
        {
            // Top
            new VertexPosition(new Vector3(-20.0f,20.0f,-20.0f)),
            new VertexPosition(new Vector3(20.0f,20.0f,-20.0f)),
            new VertexPosition(new Vector3(20.0f,20.0f,20.0f)),
            new VertexPosition(new Vector3(-20.0f,20.0f,20.0f)),
            // Bottom
            new VertexPosition(new Vector3(-20.0f,-20.0f,20.0f)),
            new VertexPosition(new Vector3(20.0f,-20.0f,20.0f)),
            new VertexPosition(new Vector3(20.0f,-20.0f,-20.0f)),
            new VertexPosition(new Vector3(-20.0f,-20.0f,-20.0f)),
            // Left
            new VertexPosition(new Vector3(-20.0f,20.0f,-20.0f)),
            new VertexPosition(new Vector3(-20.0f,20.0f,20.0f)),
            new VertexPosition(new Vector3(-20.0f,-20.0f,20.0f)),
            new VertexPosition(new Vector3(-20.0f,-20.0f,-20.0f)),
            // Right
            new VertexPosition(new Vector3(20.0f,20.0f,20.0f)),
            new VertexPosition(new Vector3(20.0f,20.0f,-20.0f)),
            new VertexPosition(new Vector3(20.0f,-20.0f,-20.0f)),
            new VertexPosition(new Vector3(20.0f,-20.0f,20.0f)),
            // Back
            new VertexPosition(new Vector3(20.0f,20.0f,-20.0f)),
            new VertexPosition(new Vector3(-20.0f,20.0f,-20.0f)),
            new VertexPosition(new Vector3(-20.0f,-20.0f,-20.0f)),
            new VertexPosition(new Vector3(20.0f,-20.0f,-20.0f)),
            // Front
            new VertexPosition(new Vector3(-20.0f,20.0f,20.0f)),
            new VertexPosition(new Vector3(20.0f,20.0f,20.0f)),
            new VertexPosition(new Vector3(20.0f,-20.0f,20.0f)),
            new VertexPosition(new Vector3(-20.0f,-20.0f,20.0f)),
        };

        private static readonly ushort[] s_indices = new ushort[]
        {
            0,1,2, 0,2,3,
            4,5,6, 4,6,7,
            8,9,10, 8,10,11,
            12,13,14, 12,14,15,
            16,17,18, 16,18,19,
            20,21,22, 20,22,23,
        };
    }
}
