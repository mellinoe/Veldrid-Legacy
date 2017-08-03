#version 450

#extension GL_ARB_separate_shader_objects : enable
#extension GL_ARB_shading_language_420pack : enable

layout(binding = 4) uniform LightInfoBuffer
{
    vec3 lightDir;
    float _padding;
};

layout(binding = 5) uniform CameraInfoBuffer
{
    vec3 cameraPosition_worldSpace;
    float _padding1;
    vec3 cameraLookDirection;
    float _padding3;
};

#define MAX_POINT_LIGHTS 4

struct PointLightInfo
{
    vec3 position;
    float range;
    vec3 color;
    float _padding;
};

layout(binding = 6) uniform PointLightsBuffer
{
    int numActiveLights;
    PointLightInfo pointLights[MAX_POINT_LIGHTS];
};

layout(binding = 9) uniform MaterialPropertiesBuffer
{
    vec3 specularIntensity;
    float specularPower;
};

layout(binding = 10) uniform texture2D SurfaceTexture;
layout(binding = 11) uniform sampler SurfaceSampler;
layout(binding = 12) uniform texture2D AlphaMap;
layout(binding = 13) uniform sampler AlphaSampler;
layout(binding = 14) uniform texture2D ShadowMap;
layout(binding = 15) uniform sampler ShadowSampler;

layout(location = 0) in vec3 out_position_worldSpace;
layout(location = 1) in vec4 out_lightPosition; //vertex with regard to light view
layout(location = 2) in vec3 out_normal;
layout(location = 3) in vec2 out_texCoord;

layout(location = 0) out vec4 outputColor;

vec4 WithAlpha(vec4 baseColor, float alpha)
{
    return vec4(baseColor.rgb, alpha);
}

void main()
{
    float alphaMapSample = texture(sampler2D(AlphaMap, AlphaSampler), out_texCoord).r;
    if (alphaMapSample == 0)
    {
        discard;
    }

    vec4 surfaceColor = texture(sampler2D(SurfaceTexture, SurfaceSampler), out_texCoord);
    vec4 ambientLight = vec4(.4, .4, .4, 1);

    // Point Diffuse

    vec4 pointDiffuse = vec4(0, 0, 0, 1);
    vec4 pointSpec = vec4(0, 0, 0, 1);
    for (int i = 0; i < numActiveLights; i++)
    {
        PointLightInfo pli = pointLights[i];
        vec3 lightDir = normalize(pli.position - out_position_worldSpace);
        float intensity = clamp(dot(out_normal, lightDir), 0, 1);
        float lightDistance = distance(pli.position, out_position_worldSpace);
        intensity = clamp(intensity * (1 - (lightDistance / pli.range)), 0, 1);

        pointDiffuse += intensity * vec4(pli.color, 1) * surfaceColor;

        // Specular
        vec3 vertexToEye = normalize(cameraPosition_worldSpace - out_position_worldSpace);
        vec3 lightReflect = normalize(reflect(lightDir, out_normal));

        float specularFactor = dot(vertexToEye, lightReflect);
        if (specularFactor > 0)
        {
            specularFactor = pow(abs(specularFactor), specularPower);
            pointSpec += (1 - (lightDistance / pli.range)) * (vec4(pli.color * specularIntensity * specularFactor, 1.0f));
        }
    }

    pointDiffuse = clamp(pointDiffuse, 0, 1);
    pointSpec = clamp(pointSpec, 0, 1);

    // Directional light calculations

    // perform perspective divide
    vec3 projCoords = out_lightPosition.xyz / out_lightPosition.w;

    // if out_position is not visible to the light - dont illuminate it
    // results in hard light frustum
    if (projCoords.x < -1.0f || projCoords.x > 1.0f ||
        projCoords.y < -1.0f || projCoords.y > 1.0f ||
        projCoords.z < 0.0f || projCoords.z > 1.0f)
    {
        outputColor = ambientLight * surfaceColor + pointDiffuse + pointSpec;
        outputColor = WithAlpha(outputColor, surfaceColor.a);
        return;
    }

    // Transform to [0,1] range.
    // NOTE: Vulkan z-range is already [0,1].
    projCoords.x = projCoords.x * 0.5 + 0.5;
    projCoords.y = projCoords.y * 0.5 + 0.5;

    vec3 L = -1 * normalize(lightDir);
    float diffuseFactor = dot(normalize(out_normal), L);

    float cosTheta = clamp(diffuseFactor, 0, 1);
    float bias = 0.0015 * tan(acos(cosTheta));
    bias = clamp(bias, 0, 0.01);

    projCoords.z -= bias;

    //sample shadow map - point sampler
    float shadowMapDepth = texture(sampler2D(ShadowMap, ShadowSampler), projCoords.xy).r;

    //if clip space z value greater than shadow map value then pixel is in shadow
    if (shadowMapDepth < projCoords.z)
    {
        outputColor = ambientLight * surfaceColor + pointDiffuse + pointSpec;
        outputColor = WithAlpha(outputColor, surfaceColor.a);
        return;
    }

    //otherwise calculate ilumination at fragment
    diffuseFactor = clamp(diffuseFactor, 0, 1);

    vec4 specularColor = vec4(0, 0, 0, 0);

    vec3 vertexToEye = normalize(cameraPosition_worldSpace - out_position_worldSpace);
    vec3 lightReflect = normalize(reflect(lightDir, out_normal));
    vec3 lightColor = vec3(1, 1, 1);

    float specularFactor = dot(vertexToEye, lightReflect);
    if (specularFactor > 0)
    {
        specularFactor = pow(specularFactor, specularPower);
        specularColor = vec4(lightColor * specularIntensity * specularFactor, 1.0f);
    }

    outputColor = specularColor + (ambientLight * surfaceColor) + (diffuseFactor * surfaceColor) + pointDiffuse + pointSpec;
    outputColor = WithAlpha(outputColor, surfaceColor.a);
}
