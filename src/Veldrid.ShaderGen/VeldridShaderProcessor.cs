using System;
using ShaderGen;
using System.IO;
using System.Linq;
using Veldrid.Graphics;
using System.Collections.Generic;

namespace Veldrid.ShaderGen
{
    public class VeldridShaderProcessor : IShaderSetProcessor
    {
        public string UserArgs { get; set; }

        public void ProcessShaderSet(ShaderSetProcessorInput input)
        {
            if (input.VertexFunction == null || input.FragmentFunction == null)
            {
                throw new InvalidOperationException("Veldrid.ShaderGen failed -- incomplete shader set.");
            }

            string outputPath = Path.Combine(UserArgs, input.SetName + ".SetInfo.cs");
            using (StreamWriter fs = File.CreateText(outputPath))
            {
                CsCodeWriter ccw = new CsCodeWriter(fs);

                ccw.WriteLine("// This file is generated.");
                ccw.Using("Veldrid");
                ccw.Using("Veldrid.Graphics");

                using (ccw.PushBlock($"public static class {input.SetName}SetInfo"))
                {
                    EmitVertexInputsGetter(input, ccw);
                    EmitResourceDescsGetter(input, ccw);
                    EmitCreateAll(input, ccw);
                }
            }
        }

        private void EmitVertexInputsGetter(ShaderSetProcessorInput input, CsCodeWriter ccw)
        {
            using (ccw.PushBlock($"public static VertexInputDescription[] GetVertexInputs()"))
            {
                ccw.WriteLine($"return new VertexInputDescription[]");
                using (ccw.PushBlock(null, ";"))
                {
                    ccw.WriteLine("new VertexInputDescription(");
                    ccw.IncreaseIndentation();
                    ParameterDefinition[] vsParams = input.VertexFunction.Parameters;
                    foreach (ParameterDefinition param in vsParams)
                    {
                        StructureDefinition sd = input.Model.GetStructureDefinition(param.Type);
                        foreach (FieldDefinition fd in sd.Fields)
                        {
                            string name = fd.Name;
                            VertexSemanticType semantic = GetSemantic(fd.SemanticType);
                            VertexElementFormat format = GetFormat(fd.Type);
                            ccw.Write($"new VertexInputElement(\"{name}\", VertexSemanticType.{semantic}, VertexElementFormat.{format})");
                            if (fd == sd.Fields.Last())
                            {
                                ccw.WriteLine(")");
                            }
                            else
                            {
                                ccw.WriteLine(",");
                            }
                        }
                    }
                    ccw.DecreaseIndentation();
                }
            }
        }

        private void EmitResourceDescsGetter(ShaderSetProcessorInput input, CsCodeWriter ccw)
        {
            using (ccw.PushBlock("public static ShaderResourceDescription[] GetResources()"))
            {
                ccw.WriteLine("return new ShaderResourceDescription[]");
                using (ccw.PushBlock(null, ";"))
                {
                    foreach (ResourceDefinition rd in input.Model.Resources)
                    {
                        string name = rd.Name;
                        string secondParam =
                            rd.ResourceKind == ShaderResourceKind.Texture2D || rd.ResourceKind == ShaderResourceKind.TextureCube
                            ? "ShaderResourceType.Texture"
                            : rd.ResourceKind == ShaderResourceKind.Sampler
                                ? "ShaderResourceType.Sampler"
                                : GetConstantSecondParam(rd.ValueType, input.Model);

                        ccw.WriteLine($"new ShaderResourceDescription(\"{name}\", {secondParam}),");
                    }
                }
            }
        }

        private void EmitCreateAll(ShaderSetProcessorInput input, CsCodeWriter ccw)
        {
            ccw.WriteLine(
@"public static void CreateAll(
        ResourceFactory factory,
        CompiledShaderCode vsCode,
        CompiledShaderCode fsCode,
        out ShaderSet shaderSet,
        out ShaderResourceBindingSlots resourceSlots)
    {
        Shader vs = factory.CreateShader(ShaderStages.Vertex, vsCode);
        Shader fs = factory.CreateShader(ShaderStages.Fragment, fsCode);
        VertexInputLayout layout = factory.CreateInputLayout(GetVertexInputs());
        shaderSet = factory.CreateShaderSet(layout, vs, fs);
        resourceSlots = factory.CreateShaderResourceBindingSlots(shaderSet, GetResources());
    }");
        }

        private VertexSemanticType GetSemantic(SemanticType semanticType)
        {
            switch (semanticType)
            {
                case SemanticType.Position: return VertexSemanticType.Position;
                case SemanticType.Normal: return VertexSemanticType.Normal;
                case SemanticType.TextureCoordinate: return VertexSemanticType.TextureCoordinate;
                case SemanticType.Color: return VertexSemanticType.Color;
                case SemanticType.Tangent: return VertexSemanticType.Color;

                case SemanticType.None:
                default:
                    throw new InvalidOperationException("Unexpected semantic type: " + semanticType);
            }
        }

        private VertexElementFormat GetFormat(TypeReference typeRef)
        {
            if (!s_knownFormats.TryGetValue(typeRef.Name, out VertexElementFormat ret))
            {
                throw new InvalidOperationException("Unsupported vertex element type: " + typeRef.Name);
            }

            return ret;
        }

        private string GetConstantSecondParam(TypeReference typeRef, ShaderModel model)
        {
            if (s_knownConstantTypes.TryGetValue(typeRef.Name, out ShaderConstantType knownType))
            {
                return "ShaderConstantType." + knownType;
            }
            else
            {
                int totalSize = model.GetTypeSize(typeRef);
                return totalSize.ToString();
            }
        }

        private static readonly Dictionary<string, VertexElementFormat> s_knownFormats = new Dictionary<string, VertexElementFormat>()
        {
            { "System.Single", VertexElementFormat.Float1 },
            { "System.Numerics.Vector2", VertexElementFormat.Float2 },
            { "System.Numerics.Vector3", VertexElementFormat.Float3 },
            { "System.Numerics.Vector4", VertexElementFormat.Float4 },
        };

        private static readonly Dictionary<string, ShaderConstantType> s_knownConstantTypes = new Dictionary<string, ShaderConstantType>()
        {
            { "System.Numerics.Vector4", ShaderConstantType.Float4 },
            { "System.Numerics.Matrix4x4", ShaderConstantType.Matrix4x4 },
        };
    }
}
