﻿using ImGuiNET;
using System;
using System.Collections.Generic;
using System.Numerics;
using Veldrid;
using Veldrid.Graphics;
using Veldrid.Platform;
using System.Runtime.InteropServices;

using Key = Veldrid.Platform.Key;
using System.Reflection;
using System.IO;

namespace Veldrid
{
    /// <summary>
    /// A Veldrid RenderItem which can draw draw lists produced by ImGui.
    /// Also provides functions for updating ImGui input.
    /// </summary>
    public class ImGuiRenderer : IDisposable
    {
        private RenderContext _rc;
        private readonly Assembly _assembly;

        // Context objects
        private VertexBuffer _vertexBuffer;
        private IndexBuffer _indexBuffer;
        private BlendState _blendState;
        private DepthStencilState _depthDisabledState;
        private RasterizerState _rasterizerState;
        private ShaderTextureBinding _fontTextureBinding;

        // Material replacements
        private ShaderSet _shaderSet;
        private ShaderResourceBindingSlots _resourceBindings;
        private ConstantBuffer _projMatrixBuffer;

        private int _fontAtlasID = 1;
        private bool _controlDown;
        private bool _shiftDown;
        private bool _altDown;

        private int _windowWidth;
        private int _windowHeight;
        private Vector2 _scaleFactor = Vector2.One;

        /// <summary>
        /// Constructs a new ImGuiRenderer.
        /// </summary>
        public ImGuiRenderer(RenderContext rc, int width, int height)
        {
            _rc = rc;
            _assembly = typeof(ImGuiRenderer).GetTypeInfo().Assembly;
            _windowWidth = width;
            _windowHeight = height;

            ImGui.GetIO().FontAtlas.AddDefaultFont();

            CreateDeviceResources(rc);
            SetOpenTKKeyMappings();

            SetPerFrameImGuiData(1f / 60f);

            ImGui.NewFrame();
        }

        public void WindowResized(int width, int height)
        {
            _windowWidth = width;
            _windowHeight = height;
        }

        public void DestroyDeviceObjects()
        {
            Dispose();
        }

        public void CreateDeviceResources(RenderContext rc)
        {
            _rc = rc;
            ResourceFactory factory = rc.ResourceFactory;
            _vertexBuffer = factory.CreateVertexBuffer(1000, true);
            _indexBuffer = factory.CreateIndexBuffer(500, true);
            _blendState = factory.CreateCustomBlendState(
                true,
                Blend.InverseSourceAlpha, Blend.Zero, BlendFunction.Add,
                Blend.SourceAlpha, Blend.InverseSourceAlpha, BlendFunction.Add,
                RgbaFloat.Black);
            _depthDisabledState = factory.CreateDepthStencilState(false, DepthComparison.Always);
            _rasterizerState = factory.CreateRasterizerState(FaceCullingMode.None, TriangleFillMode.Solid, true, true);
            RecreateFontDeviceTexture(rc);

            var vertexShaderCode = LoadEmbeddedShaderCode(rc.ResourceFactory, "imgui-vertex", ShaderStages.Vertex);
            var fragmentShaderCode = LoadEmbeddedShaderCode(rc.ResourceFactory, "imgui-frag", ShaderStages.Fragment);
            Shader vertexShader = factory.CreateShader(ShaderStages.Vertex, vertexShaderCode);
            Shader fragmentShader = factory.CreateShader(ShaderStages.Fragment, fragmentShaderCode);

            VertexInputLayout inputLayout = factory.CreateInputLayout(
                new VertexInputDescription(20, new VertexInputElement[]
                {
                    new VertexInputElement("in_position", VertexSemanticType.Position, VertexElementFormat.Float2),
                    new VertexInputElement("in_texCoord", VertexSemanticType.TextureCoordinate, VertexElementFormat.Float2),
                    new VertexInputElement("in_color", VertexSemanticType.Color, VertexElementFormat.Byte4)
                }));

            _shaderSet = factory.CreateShaderSet(inputLayout, vertexShader, fragmentShader);

            _resourceBindings = factory.CreateShaderResourceBindingSlots(
                _shaderSet,
                new ShaderResourceDescription("ProjectionMatrixBuffer", ShaderConstantType.Matrix4x4),
                new ShaderResourceDescription("FontTexture", ShaderResourceType.Texture),
                new ShaderResourceDescription("FontSampler", ShaderResourceType.Sampler));
            _projMatrixBuffer = factory.CreateConstantBuffer(ShaderConstantType.Matrix4x4);
        }

        private CompiledShaderCode LoadEmbeddedShaderCode(ResourceFactory factory, string name, ShaderStages stage)
        {
            switch (factory.BackendType)
            {
                case GraphicsBackend.Direct3D11:
                    {
                        string resourceName = name + ".hlsl";
                        return factory.ProcessShaderCode(stage, GetEmbeddedResourceText(resourceName));
                    }
                case GraphicsBackend.OpenGL:
                case GraphicsBackend.OpenGLES:
                    {
                        string resourceName = name + ".glsl";
                        return factory.ProcessShaderCode(stage, GetEmbeddedResourceText(resourceName));
                    }
                case GraphicsBackend.Vulkan:
                    {
                        string resourceName = name + ".spv";
                        return factory.LoadProcessedShader(GetEmbeddedResourceBytes(resourceName));
                    }
                default:
                    throw new NotImplementedException();
            }
        }

        private string GetEmbeddedResourceText(string resourceName)
        {
            using (StreamReader sr = new StreamReader(_assembly.GetManifestResourceStream(resourceName)))
            {
                return sr.ReadToEnd();
            }
        }

        private byte[] GetEmbeddedResourceBytes(string resourceName)
        {
            using (Stream s = _assembly.GetManifestResourceStream(resourceName))
            {
                byte[] ret = new byte[s.Length];
                s.Read(ret, 0, (int)s.Length);
                return ret;
            }
        }

        /// <summary>
        /// Recreates the device texture used to render text.
        /// </summary>
        public unsafe void RecreateFontDeviceTexture(RenderContext rc)
        {
            var io = ImGui.GetIO();
            // Build
            var textureData = io.FontAtlas.GetTexDataAsRGBA32();

            // Store our identifier
            io.FontAtlas.SetTexID(_fontAtlasID);

            var deviceTexture = rc.ResourceFactory.CreateTexture(1, textureData.Width, textureData.Height, PixelFormat.R8_G8_B8_A8_UInt);
            deviceTexture.SetTextureData(
                0,
                0, 0,
                textureData.Width,
                textureData.Height,
                (IntPtr)textureData.Pixels,
                textureData.BytesPerPixel * textureData.Width * textureData.Height);
            _fontTextureBinding = rc.ResourceFactory.CreateShaderTextureBinding(deviceTexture);

            io.FontAtlas.ClearTexData();
        }

        private string[] _stages = { "Standard" };

        public void SetRenderStages(string[] stages) { _stages = stages; }

        /// <summary>
        /// Gets a list of stages participated by this RenderItem.
        /// </summary>
        public IList<string> GetStagesParticipated()
        {
            return _stages;
        }

        /// <summary>
        /// Renders the ImGui draw list data.
        /// </summary>
        public unsafe void Render(RenderContext rc)
        {
            ImGui.Render();
            RenderImDrawData(ImGui.GetDrawData(), rc);
        }

        /// <summary>
        /// Updates ImGui input and IO configuration state.
        /// </summary>
        public void Update(float deltaSeconds)
        {
            SetPerFrameImGuiData(deltaSeconds);
        }

        /// <summary>
        /// Sets per-frame data based on the RenderContext and window.
        /// This is called by Update(float).
        /// </summary>
        private unsafe void SetPerFrameImGuiData(float deltaSeconds)
        {
            IO io = ImGui.GetIO();
            io.DisplaySize = new Vector2(
                _windowWidth / _scaleFactor.X,
                _windowHeight / _scaleFactor.Y);
            io.DisplayFramebufferScale = _scaleFactor;
            io.DeltaTime = deltaSeconds; // DeltaTime is in seconds.
        }

        /// <summary>
        /// Updates the current input state tracked by ImGui.
        /// This calls ImGui.NewFrame().
        /// </summary>
        public void OnInputUpdated(InputSnapshot snapshot)
        {
            UpdateImGuiInput(snapshot);
            ImGui.NewFrame();
        }

        private unsafe void UpdateImGuiInput(InputSnapshot snapshot)
        {
            IO io = ImGui.GetIO();

            Vector2 mousePosition = snapshot.MousePosition;

            io.MousePosition = mousePosition;
            io.MouseDown[0] = snapshot.IsMouseDown(MouseButton.Left);
            io.MouseDown[1] = snapshot.IsMouseDown(MouseButton.Right);
            io.MouseDown[2] = snapshot.IsMouseDown(MouseButton.Middle);

            float delta = snapshot.WheelDelta;
            io.MouseWheel = delta;

            ImGui.GetIO().MouseWheel = delta;

            IReadOnlyList<char> keyCharPresses = snapshot.KeyCharPresses;
            for (int i = 0; i < keyCharPresses.Count; i++)
            {
                char c = keyCharPresses[i];
                ImGui.AddInputCharacter(c);
            }

            IReadOnlyList<KeyEvent> keyEvents = snapshot.KeyEvents;
            for (int i = 0; i < keyEvents.Count; i++)
            {
                KeyEvent keyEvent = keyEvents[i];
                io.KeysDown[(int)keyEvent.Key] = keyEvent.Down;
                if (keyEvent.Key == Key.ControlLeft)
                {
                    _controlDown = keyEvent.Down;
                }
                if (keyEvent.Key == Key.ShiftLeft)
                {
                    _shiftDown = keyEvent.Down;
                }
                if (keyEvent.Key == Key.AltLeft)
                {
                    _altDown = keyEvent.Down;
                }
            }

            io.CtrlPressed = _controlDown;
            io.AltPressed = _altDown;
            io.ShiftPressed = _shiftDown;
        }

        private static unsafe void SetOpenTKKeyMappings()
        {
            IO io = ImGui.GetIO();
            io.KeyMap[GuiKey.Tab] = (int)Key.Tab;
            io.KeyMap[GuiKey.LeftArrow] = (int)Key.Left;
            io.KeyMap[GuiKey.RightArrow] = (int)Key.Right;
            io.KeyMap[GuiKey.UpArrow] = (int)Key.Up;
            io.KeyMap[GuiKey.DownArrow] = (int)Key.Down;
            io.KeyMap[GuiKey.PageUp] = (int)Key.PageUp;
            io.KeyMap[GuiKey.PageDown] = (int)Key.PageDown;
            io.KeyMap[GuiKey.Home] = (int)Key.Home;
            io.KeyMap[GuiKey.End] = (int)Key.End;
            io.KeyMap[GuiKey.Delete] = (int)Key.Delete;
            io.KeyMap[GuiKey.Backspace] = (int)Key.BackSpace;
            io.KeyMap[GuiKey.Enter] = (int)Key.Enter;
            io.KeyMap[GuiKey.Escape] = (int)Key.Escape;
            io.KeyMap[GuiKey.A] = (int)Key.A;
            io.KeyMap[GuiKey.C] = (int)Key.C;
            io.KeyMap[GuiKey.V] = (int)Key.V;
            io.KeyMap[GuiKey.X] = (int)Key.X;
            io.KeyMap[GuiKey.Y] = (int)Key.Y;
            io.KeyMap[GuiKey.Z] = (int)Key.Z;
        }

        private unsafe void RenderImDrawData(DrawData* draw_data, RenderContext rc)
        {
            VertexDescriptor descriptor = new VertexDescriptor((byte)sizeof(DrawVert), 3, IntPtr.Zero);

            int vertexOffsetInVertices = 0;
            int indexOffsetInElements = 0;

            if (draw_data->CmdListsCount == 0)
            {
                return;
            }

            for (int i = 0; i < draw_data->CmdListsCount; i++)
            {
                NativeDrawList* cmd_list = draw_data->CmdLists[i];

                _vertexBuffer.SetVertexData(new IntPtr(cmd_list->VtxBuffer.Data), descriptor, cmd_list->VtxBuffer.Size, vertexOffsetInVertices);
                _indexBuffer.SetIndices(new IntPtr(cmd_list->IdxBuffer.Data), IndexFormat.UInt16, cmd_list->IdxBuffer.Size, indexOffsetInElements);

                vertexOffsetInVertices += cmd_list->VtxBuffer.Size;
                indexOffsetInElements += cmd_list->IdxBuffer.Size;
            }

            // Setup orthographic projection matrix into our constant buffer
            {
                var io = ImGui.GetIO();

                Matrix4x4 mvp = Matrix4x4.CreateOrthographicOffCenter(
                    0f,
                    io.DisplaySize.X,
                    io.DisplaySize.Y,
                    0.0f,
                    -1.0f,
                    1.0f);

                _projMatrixBuffer.SetData(ref mvp, sizeof(Matrix4x4));
            }

            BlendState previousBlendState = rc.BlendState;
            rc.SetBlendState(_blendState);
            rc.SetDepthStencilState(_depthDisabledState);
            RasterizerState previousRasterizerState = rc.RasterizerState;
            rc.SetRasterizerState(_rasterizerState);
            rc.VertexBuffer = _vertexBuffer;
            rc.IndexBuffer = _indexBuffer;

            rc.ShaderSet = _shaderSet;
            rc.ShaderResourceBindingSlots = _resourceBindings;
            rc.SetConstantBuffer(0, _projMatrixBuffer);
            rc.SetSamplerState(2, _rc.PointSampler);

            ImGui.ScaleClipRects(draw_data, ImGui.GetIO().DisplayFramebufferScale);

            // Render command lists
            int vtx_offset = 0;
            int idx_offset = 0;
            for (int n = 0; n < draw_data->CmdListsCount; n++)
            {
                NativeDrawList* cmd_list = draw_data->CmdLists[n];
                for (int cmd_i = 0; cmd_i < cmd_list->CmdBuffer.Size; cmd_i++)
                {
                    DrawCmd* pcmd = &(((DrawCmd*)cmd_list->CmdBuffer.Data)[cmd_i]);
                    if (pcmd->UserCallback != IntPtr.Zero)
                    {
                        throw new NotImplementedException();
                    }
                    else
                    {
                        if (pcmd->TextureId != IntPtr.Zero)
                        {
                            if (pcmd->TextureId == new IntPtr(_fontAtlasID))
                            {
                                _rc.SetTexture(1, _fontTextureBinding);
                            }
                            else
                            {
                                ShaderTextureBinding binding = ImGuiImageHelper.GetShaderTextureBinding(pcmd->TextureId);
                                _rc.SetTexture(1, binding);
                            }
                        }

                        // TODO: This doesn't take into account viewport coordinates.
                        rc.SetScissorRectangle(
                            (int)pcmd->ClipRect.X,
                            (int)pcmd->ClipRect.Y,
                            (int)pcmd->ClipRect.Z,
                            (int)pcmd->ClipRect.W);

                        rc.DrawIndexedPrimitives((int)pcmd->ElemCount, idx_offset, vtx_offset);
                    }

                    idx_offset += (int)pcmd->ElemCount;
                }
                vtx_offset += cmd_list->VtxBuffer.Size;
            }

            rc.ClearScissorRectangle();
            rc.SetBlendState(previousBlendState);
            rc.SetDepthStencilState(rc.DefaultDepthStencilState);
            rc.SetRasterizerState(previousRasterizerState);
        }

        /// <summary>
        /// Frees all graphics resources used by the renderer.
        /// </summary>
        public void Dispose()
        {
            _vertexBuffer.Dispose();
            _indexBuffer.Dispose();
            _depthDisabledState.Dispose();
            _blendState.Dispose();
            _fontTextureBinding.Dispose();
            _shaderSet.Dispose();
        }

        /// <summary>
        /// Returns a value indicating whether this RenderItem is culled based on the given visible frustum.
        /// </summary>
        public bool Cull(ref BoundingFrustum visibleFrustum)
        {
            return false;
        }
    }
}
