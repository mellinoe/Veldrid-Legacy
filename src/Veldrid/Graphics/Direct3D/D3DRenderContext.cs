﻿using SharpDX.Direct3D11;
using SharpDX.DXGI;
using SharpDX.Mathematics.Interop;
using System;

namespace Veldrid.Graphics.Direct3D
{
    public class D3DRenderContext : RenderContext
    {
        private SharpDX.Direct3D11.Device _device;
        private SwapChain _swapChain;
        private DeviceContext _deviceContext;

        private D3DFramebuffer _defaultFramebuffer;
        private DepthStencilState _depthState;
        private RasterizerState _rasterizerState;
        private DeviceCreationFlags _deviceFlags;

        public D3DRenderContext(Window window) : this(window, DeviceCreationFlags.None) { }

        public D3DRenderContext(Window window, DeviceCreationFlags flags)
            : base(window)
        {
            _deviceFlags = flags;
            CreateAndInitializeDevice();
            ResourceFactory = new D3DResourceFactory(_device);
        }

        public override ResourceFactory ResourceFactory { get; }

        protected unsafe override void PlatformClearBuffer()
        {
            RgbaFloat clearColor = ClearColor;
            _deviceContext.ClearRenderTargetView(CurrentFramebuffer.RenderTargetView, *(RawColor4*)&clearColor);
            _deviceContext.ClearDepthStencilView(CurrentFramebuffer.DepthStencilView, DepthStencilClearFlags.Depth, 1.0f, 0);
        }

        public override void DrawIndexedPrimitives(int startingVertex, int indexCount)
        {
            _device.ImmediateContext.DrawIndexed(indexCount, startingVertex, 0);
        }

        protected override void PlatformSwapBuffers()
        {
            _swapChain.Present(0, PresentFlags.None);
        }

        private void CreateAndInitializeDevice()
        {
            var swapChainDescription = new SwapChainDescription()
            {
                BufferCount = 1,
                IsWindowed = true,
                ModeDescription = new ModeDescription(Window.Width, Window.Height, new Rational(60, 1), Format.R8G8B8A8_UNorm),
                OutputHandle = Window.Handle,
                SampleDescription = new SampleDescription(1, 0),
                SwapEffect = SwapEffect.Discard,
                Usage = Usage.RenderTargetOutput
            };

            SharpDX.Direct3D11.Device.CreateWithSwapChain(SharpDX.Direct3D.DriverType.Hardware, _deviceFlags, swapChainDescription, out _device, out _swapChain);
            _deviceContext = _device.ImmediateContext;
            var factory = _swapChain.GetParent<Factory>();
            factory.MakeWindowAssociation(Window.Handle, WindowAssociationFlags.IgnoreAll);

            CreateRasterizerState();
            CreateDepthBufferState();
            OnWindowResized();
            SetFramebuffer(_defaultFramebuffer);

            _deviceContext.InputAssembler.PrimitiveTopology = SharpDX.Direct3D.PrimitiveTopology.TriangleList;
        }

        private void SetRegularTargets()
        {
            // Setup targets and viewport for rendering
            _deviceContext.Rasterizer.SetViewport(0, 0, Window.Width, Window.Height);
            CurrentFramebuffer.Apply();
        }

        private void CreateDepthBufferState()
        {
            DepthStencilStateDescription description = DepthStencilStateDescription.Default();
            description.DepthComparison = Comparison.LessEqual;
            description.IsDepthEnabled = true;

            _depthState = new DepthStencilState(_device, description);
        }

        private void CreateRasterizerState()
        {
            var desc = RasterizerStateDescription.Default();
            desc.IsMultisampleEnabled = true;
            desc.CullMode = CullMode.Back;
            _rasterizerState = new RasterizerState(_device, desc);
        }

        protected override void PlatformResize()
        {
            RecreateDefaultFramebuffer();

            // TODO: This seems wrong.
            if (CurrentFramebuffer == null)
            {
                SetFramebuffer(_defaultFramebuffer);
            }

            SetRegularTargets();
        }

        private void RecreateDefaultFramebuffer()
        {
            if (_defaultFramebuffer != null)
            {
                _defaultFramebuffer.Dispose();
            }

            _swapChain.ResizeBuffers(1, Window.Width, Window.Height, Format.R8G8B8A8_UNorm, SwapChainFlags.AllowModeSwitch);

            // Get the backbuffer from the swapchain
            using (var backBufferTexture = _swapChain.GetBackBuffer<Texture2D>(0))
            using (var depthBufferTexture = new Texture2D(_device, new Texture2DDescription()
            {
                Format = Format.D16_UNorm,
                ArraySize = 1,
                MipLevels = 1,
                Width = Math.Max(1, Window.Width),
                Height = Math.Max(1, Window.Height),
                SampleDescription = new SampleDescription(1, 0),
                Usage = ResourceUsage.Default,
                BindFlags = BindFlags.DepthStencil,
                CpuAccessFlags = CpuAccessFlags.None,
                OptionFlags = ResourceOptionFlags.None
            }))
            {
                bool currentlyBound = CurrentFramebuffer == _defaultFramebuffer;
                // Create the depth buffer view
                _defaultFramebuffer = new D3DFramebuffer(_device, new D3DTexture(_device, backBufferTexture), new D3DTexture(_device, depthBufferTexture));
                if (currentlyBound)
                {
                    SetFramebuffer(_defaultFramebuffer);
                }
            }
        }

        protected override void PlatformSetDefaultFramebuffer()
        {
            SetFramebuffer(_defaultFramebuffer);
            SetRegularTargets();
        }

        private new D3DFramebuffer CurrentFramebuffer => (D3DFramebuffer)base.CurrentFramebuffer;
    }
}