﻿using OpenTK.Graphics.OpenGL;
using System.Linq;

namespace Veldrid.Graphics.OpenGL
{
    public class OpenGLVertexInputLayout : VertexInputLayout
    {
        public MaterialVertexInput[] InputDescription { get; }
        public OpenGLMaterialVertexInput[] VBLayoutsBySlot { get; }

        public OpenGLVertexInputLayout(MaterialVertexInput[] vertexInputs)
        {
            InputDescription = vertexInputs;
            VBLayoutsBySlot = vertexInputs.Select(mvi => new OpenGLMaterialVertexInput(mvi)).ToArray();
        }
    }

    public class OpenGLMaterialVertexInput
    {
        public int VertexSizeInBytes { get; }
        public OpenGLMaterialVertexInputElement[] Elements { get; }

        public OpenGLMaterialVertexInput(int vertexSizeInBytes, OpenGLMaterialVertexInputElement[] elements)
        {
            VertexSizeInBytes = vertexSizeInBytes;
            Elements = elements;
        }

        public OpenGLMaterialVertexInput(MaterialVertexInput genericInput)
        {
            VertexSizeInBytes = genericInput.VertexSizeInBytes;
            Elements = new OpenGLMaterialVertexInputElement[genericInput.Elements.Length];
            int offset = 0;
            for (int i = 0; i < Elements.Length; i++)
            {
                var genericElement = genericInput.Elements[i];
                Elements[i] = new OpenGLMaterialVertexInputElement(genericElement, offset);
                offset += genericElement.SizeInBytes;
            }
        }
    }

    public struct OpenGLMaterialVertexInputElement
    {
        public byte SizeInBytes { get; }
        public byte ElementCount { get; }
        public VertexAttribPointerType Type { get; }
        public int Offset { get; }
        public bool Normalized { get; }
        public int InstanceStepRate { get; set; }

        public OpenGLMaterialVertexInputElement(byte sizeInBytes, byte elementCount, VertexAttribPointerType type, int offset, bool normalized)
        {
            SizeInBytes = sizeInBytes;
            ElementCount = elementCount;
            Type = type;
            Offset = offset;
            Normalized = normalized;
            InstanceStepRate = 0;
        }

        public OpenGLMaterialVertexInputElement(
            byte sizeInBytes,
            byte elementCount,
            VertexAttribPointerType type,
            int offset,
            bool normalized,
            int instanceStepRate)
        {
            SizeInBytes = sizeInBytes;
            ElementCount = elementCount;
            Type = type;
            Offset = offset;
            Normalized = normalized;
            InstanceStepRate = instanceStepRate;
        }

        public OpenGLMaterialVertexInputElement(MaterialVertexInputElement genericElement, int offset)
        {
            SizeInBytes = genericElement.SizeInBytes;
            ElementCount = VertexFormatHelpers.GetElementCount(genericElement.ElementFormat);
            Type = GetGenericFormatType(genericElement.ElementFormat);
            Offset = offset;
            Normalized = genericElement.SemanticType == VertexSemanticType.Color && genericElement.ElementFormat == VertexElementFormat.Byte4;
            InstanceStepRate = genericElement.InstanceStepRate;
        }

        private static VertexAttribPointerType GetGenericFormatType(VertexElementFormat format)
        {
            switch (format)
            {
                case VertexElementFormat.Float1:
                case VertexElementFormat.Float2:
                case VertexElementFormat.Float3:
                case VertexElementFormat.Float4:
                    return VertexAttribPointerType.Float;
                case VertexElementFormat.Byte1:
                case VertexElementFormat.Byte4:
                    return VertexAttribPointerType.UnsignedByte;
                default:
                    throw Illegal.Value<VertexElementFormat>();
            }
        }
    }
}
