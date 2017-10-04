using System.Numerics;
using ShaderGen;
using static ShaderGen.ShaderBuiltins;
using Veldrid.NeoDemo;

[assembly: ShaderSet("ShadowMain", "Shaders.ShadowMain.VS", "Shaders.ShadowMain.FS")]

namespace Shaders
{
    public partial class ShadowMain
    {
        public Matrix4x4 Projection;
        public Matrix4x4 View;
        public Matrix4x4 World;
        public Matrix4x4 InverseTransposeWorld;
        public Matrix4x4 LightViewProjection1;
        public Matrix4x4 LightViewProjection2;
        public Matrix4x4 LightViewProjection3;
        public DirectionalLightInfo LightInfo;
        public CameraInfo CameraInfo;
        public PointLightsInfo PointLights;
        public MaterialProperties MaterialProperties;

        public Texture2DResource SurfaceTexture;
        public SamplerResource RegularSampler;
        public Texture2DResource AlphaMap;
        public SamplerResource AlphaMapSampler;
        public Texture2DResource ShadowMapNear;
        public Texture2DResource ShadowMapMid;
        public Texture2DResource ShadowMapFar;
        public SamplerResource ShadowMapSampler;

        public struct VertexInput
        {
            [PositionSemantic] public Vector3 Position;
            [NormalSemantic] public Vector3 Normal;
            [TextureCoordinateSemantic] public Vector2 TexCoord;
        }

        public struct PixelInput
        {
            [PositionSemantic] public Vector4 Position;
            [PositionSemantic] public Vector3 Position_WorldSpace;
            [TextureCoordinateSemantic] public Vector4 LightPosition1;
            [TextureCoordinateSemantic] public Vector4 LightPosition2;
            [TextureCoordinateSemantic] public Vector4 LightPosition3;
            [NormalSemantic] public Vector3 Normal;
            [TextureCoordinateSemantic] public Vector2 TexCoord;
        }

        [VertexShader]
        public PixelInput VS(VertexInput input)
        {
            PixelInput output;
            Vector4 worldPosition = Mul(World, new Vector4(input.Position, 1));
            Vector4 viewPosition = Mul(View, worldPosition);
            output.Position = Mul(Projection, viewPosition);

            output.Position_WorldSpace = new Vector3(worldPosition.X, worldPosition.Y, worldPosition.Z);

            Vector4 outNormal = Mul(InverseTransposeWorld, new Vector4(input.Normal, 1));
            output.Normal = Vector3.Normalize(new Vector3(outNormal.X, outNormal.Y, outNormal.Z));

            output.TexCoord = input.TexCoord;

            //store worldspace projected to light clip space with
            //a texcoord semantic to be interpolated across the surface
            output.LightPosition1 = Mul(World, new Vector4(input.Position, 1));
            output.LightPosition1 = Mul(LightViewProjection1, output.LightPosition1);

            output.LightPosition2 = Mul(World, new Vector4(input.Position, 1));
            output.LightPosition2 = Mul(LightViewProjection2, output.LightPosition2);

            output.LightPosition3 = Mul(World, new Vector4(input.Position, 1));
            output.LightPosition3 = Mul(LightViewProjection3, output.LightPosition3);

            return output;
        }

        [FragmentShader]
        public Vector4 FS(PixelInput input)
        {
            Vector3 depthBounds = new Vector3(20, 40, 60);
            Vector3 lightDir = -LightInfo.Direction;
            Vector4 color = new Vector4(0.3f, 0.3f, 0.3f, 1.0f); //you can use whatever color you want for shadows
            float shadowBias = 0.0005f;
            float lightIntensity;

            float inputPositionInv = 1.0f / input.Position.W;
            float lightPositionInv1 = 1.0f / input.LightPosition1.W;
            float lightPositionInv2 = 1.0f / input.LightPosition2.W;
            float lightPositionInv3 = 1.0f / input.LightPosition3.W;

            float depthTest = input.Position.Z * inputPositionInv;

            Vector2 shadowCoords_0 = new Vector2(input.LightPosition1.X * lightPositionInv1 * 0.5f + 0.5f, -input.LightPosition1.Y * lightPositionInv1 * 0.5f + 0.5f);
            Vector2 shadowCoords_1 = new Vector2(input.LightPosition2.X * lightPositionInv2 * 0.5f + 0.5f, -input.LightPosition2.Y * lightPositionInv2 * 0.5f + 0.5f);
            Vector2 shadowCoords_2 = new Vector2(input.LightPosition3.X * lightPositionInv3 * 0.5f + 0.5f, -input.LightPosition3.Y * lightPositionInv3 * 0.5f + 0.5f);

            float lightDepthValues_0 = input.LightPosition1.Z * lightPositionInv1;
            float lightDepthValues_1 = input.LightPosition2.Z * lightPositionInv2;
            float lightDepthValues_2 = input.LightPosition3.Z * lightPositionInv3;

            int shadowIndex = 3;

            Vector2 shadowCoords = new Vector2(0, 0);
            float lightDepthValue = 0;
            if ((Saturate(shadowCoords_0.X) == shadowCoords_0.X) && (Saturate(shadowCoords_0.Y) == shadowCoords_0.Y) && (depthTest > (1.0f - (depthBounds.X * inputPositionInv))))
            {
                shadowIndex = 0;
                shadowCoords = shadowCoords_0;
                lightDepthValue = lightDepthValues_0;
            }
            else if ((Saturate(shadowCoords_1.X) == shadowCoords_1.X) && (Saturate(shadowCoords_1.Y) == shadowCoords_1.Y) && (depthTest > (1.0f - (depthBounds.Y * inputPositionInv))))
            {
                shadowIndex = 1;
                shadowCoords = shadowCoords_1;
                lightDepthValue = lightDepthValues_1;
            }
            else if ((Saturate(shadowCoords_2.X) == shadowCoords_2.X) && (Saturate(shadowCoords_2.Y) == shadowCoords_2.Y) && (depthTest > (1.0f - (depthBounds.Z * inputPositionInv))))
            {
                shadowIndex = 2;
                shadowCoords = shadowCoords_2;
                lightDepthValue = lightDepthValues_2;
            }

            if (shadowIndex != 3) //we are in a shadow map
            {
                float depthVal = SampleDepthMap(shadowIndex, shadowCoords);

                if (shadowIndex == 0) color = new Vector4(1f, 0f, 0f, 1f);
                if (shadowIndex == 1) color = new Vector4(0f, 1f, 0f, 1f);
                if (shadowIndex == 2) color = new Vector4(0f, 0f, 1f, 1f);

                //if ((lightDepthValue - shadowBias) < depthVal)
                //{
                //    lightIntensity = Saturate(Vector3.Dot(input.Normal, lightDir));
                //    if (lightIntensity > 0.0f)
                //    {
                //        color = new Vector4(1.0f, 0, 0, 1.0f);
                //    }
                //}
            }
            else
            {
                lightIntensity = Saturate(Vector3.Dot(input.Normal, lightDir));
                if (lightIntensity > 0.0f)
                {
                    color = new Vector4(1.0f, 1f, 1f, 1.0f); //not in any shadow map, but is facing the light, so we light it.
                }
            }

            return color;
        }

        float SampleDepthMap(int index, Vector2 coord)
        {
            if (index == 0)
            {
                return Sample(ShadowMapNear, ShadowMapSampler, coord).X;
            }
            else if (index == 1)
            {
                return Sample(ShadowMapMid, ShadowMapSampler, coord).X;
            }
            else
            {
                return Sample(ShadowMapFar, ShadowMapSampler, coord).X;
            }
        }

        /*
        [FragmentShader]
        public Vector4 FS_Old(PixelInput input)
        {
            float alphaMapSample = Sample(AlphaMap, AlphaMapSampler, input.TexCoord).X;
            if (alphaMapSample == 0)
            {
                Discard();
            }

            Vector4 surfaceColor = Sample(SurfaceTexture, RegularSampler, input.TexCoord);
            Vector4 ambientLight = new Vector4(.4f, .4f, .4f, 1f);

            // Point Diffuse

            Vector4 pointDiffuse = new Vector4(0, 0, 0, 1);
            Vector4 pointSpec = new Vector4(0, 0, 0, 1);
            for (int i = 0; i < PointLights.NumActiveLights; i++)
            {
                PointLightInfo pli = PointLights.PointLights[i];
                Vector3 lightDir = Vector3.Normalize(pli.Position - input.Position_WorldSpace);
                float intensity = Saturate(Vector3.Dot(input.Normal, lightDir));
                float lightDistance = Vector3.Distance(pli.Position, input.Position_WorldSpace);
                intensity = Saturate(intensity * (1 - (lightDistance / pli.Range)));

                pointDiffuse += intensity * new Vector4(pli.Color, 1) * surfaceColor;

                // Specular
                Vector3 vertexToEye0 = Vector3.Normalize(CameraInfo.CameraPosition_WorldSpace - input.Position_WorldSpace);
                Vector3 lightReflect0 = Vector3.Normalize(Vector3.Reflect(lightDir, input.Normal));

                float specularFactor0 = Vector3.Dot(vertexToEye0, lightReflect0);
                if (specularFactor0 > 0)
                {
                    specularFactor0 = Pow(Abs(specularFactor0), MaterialProperties.SpecularPower);
                    pointSpec += (1 - (lightDistance / pli.Range)) * (new Vector4(pli.Color * MaterialProperties.SpecularIntensity * specularFactor0, 1.0f));
                }
            }

            pointDiffuse = Saturate(pointDiffuse);
            pointSpec = Saturate(pointSpec);

            // Directional light calculations

            //re-homogenize position after interpolation
            input.LightPosition /= input.LightPosition.W;
            input.LightPosition.W = 1;

            // if position is not visible to the light - dont illuminate it
            // results in hard light frustum
            if (input.LightPosition.X < -1.0f || input.LightPosition.X > 1.0f ||
                input.LightPosition.Y < -1.0f || input.LightPosition.Y > 1.0f ||
                input.LightPosition.Z < 0.0f || input.LightPosition.Z > 1.0f)
            {
                return WithAlpha((ambientLight * surfaceColor) + pointDiffuse + pointSpec, surfaceColor.X);
            }

            // transform clip space coords to texture space coords (-1:1 to 0:1)
            input.LightPosition.X = input.LightPosition.X / 2 + 0.5f;
            input.LightPosition.Y = input.LightPosition.Y / -2 + 0.5f;

            Vector3 L = -1 * Vector3.Normalize(LightInfo.Direction);
            float diffuseFactor = Vector3.Dot(Vector3.Normalize(input.Normal), L);

            float cosTheta = Clamp(diffuseFactor, 0, 1);
            float bias = 0.0005f * Tan(Acos(cosTheta));
            bias = Clamp(bias, 0, 0.01f);

            input.LightPosition.Z -= bias;

            //sample shadow map - point sampler
            float ShadowMapDepth = Sample(ShadowMapNear, ShadowMapSampler, new Vector2(input.LightPosition.X, input.LightPosition.Y)).X;

            //if clip space z value greater than shadow map value then pixel is in shadow
            if (ShadowMapDepth < input.LightPosition.Z)
            {
                return WithAlpha((ambientLight * surfaceColor) + pointDiffuse + pointSpec, surfaceColor.X);
            }

            //otherwise calculate ilumination at fragment
            diffuseFactor = Clamp(diffuseFactor, 0, 1);

            Vector4 specularColor = new Vector4(0, 0, 0, 0);

            Vector3 vertexToEye = Vector3.Normalize(CameraInfo.CameraPosition_WorldSpace - input.Position_WorldSpace);
            Vector3 lightReflect = Vector3.Normalize(Vector3.Reflect(LightInfo.Direction, input.Normal));
            Vector3 lightColor = new Vector3(1, 1, 1);

            float specularFactor = Vector3.Dot(vertexToEye, lightReflect);
            if (specularFactor > 0)
            {
                specularFactor = Pow(Abs(specularFactor), MaterialProperties.SpecularPower);
                specularColor = new Vector4(lightColor * MaterialProperties.SpecularIntensity * specularFactor, 1.0f);
            }

            return WithAlpha(specularColor + (ambientLight * surfaceColor)
                + (diffuseFactor * surfaceColor) + pointDiffuse + pointSpec, surfaceColor.X);
        }
    */

        Vector4 WithAlpha(Vector4 baseColor, float alpha)
        {
            return new Vector4(baseColor.XYZ(), alpha);
        }
    }
}
