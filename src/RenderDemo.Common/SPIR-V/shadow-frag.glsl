#version 450

#extension GL_ARB_separate_shader_objects : enable
#extension GL_ARB_shading_language_420pack : enable

layout(binding = 4) uniform LightInfoBuffer
{
    vec3 lightDir;
    float _padding;
};

layout(location = 0) in vec3 out_position_worldSpace;
layout(location = 1) in vec4 out_lightPosition; //vertex with regard to light view
layout(location = 2) in vec3 out_normal;
layout(location = 3) in vec2 out_texCoord;

layout(binding = 7) uniform texture2D SurfaceTexture;
layout(binding = 8) uniform sampler SurfaceSampler;

layout(binding = 9) uniform texture2D ShadowMap;
layout(binding = 10) uniform sampler ShadowSampler;

layout(location = 0) out vec4 outputColor;

void main()
{
    vec4 surfaceColor = texture(sampler2D(SurfaceTexture, SurfaceSampler), out_texCoord);
    vec4 ambient = vec4(.4, .4, .4, 1);

    // perform perspective divide
    vec3 projCoords = out_lightPosition.xyz / out_lightPosition.w;

    // if out_position is not visible to the light - dont illuminate it
    // results in hard light frustum
    if (projCoords.x < -1.0f || projCoords.x > 1.0f ||
        projCoords.y < -1.0f || projCoords.y > 1.0f ||
        projCoords.z < 0.0f || projCoords.z > 1.0f)
    {
        outputColor = ambient * surfaceColor;
        return;
    }

    // Transform to [0,1] range.
    // NOTE: Vulkan z-range is already [0,1].
    projCoords.x = projCoords.x * 0.5 + 0.5;
    projCoords.y = projCoords.y * 0.5 + 0.5;

    vec3 L = -1 * normalize(lightDir);
    float ndotl = dot(normalize(out_normal), L);

    float cosTheta = clamp(ndotl, 0, 1);
    float bias = 0.0005 * tan(acos(cosTheta));
    bias = clamp(bias, 0, 0.01);

    projCoords.z -= bias;

    // sample shadow map - point sampler
    float shadowMapDepth = texture(sampler2D(ShadowMap, ShadowSampler), projCoords.xy).r;

    // if clip space z value greater than shadow map value then pixel is in shadow
    if (shadowMapDepth < projCoords.z)
    {
        outputColor = ambient * surfaceColor;
        return;
    }

    // otherwise calculate ilumination at fragment
    ndotl = clamp(ndotl, 0, 1);
    outputColor = ambient * surfaceColor + surfaceColor * ndotl;
    return;
}
