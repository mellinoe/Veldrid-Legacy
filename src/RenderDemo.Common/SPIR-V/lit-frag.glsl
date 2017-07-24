#version 450

#extension GL_ARB_separate_shader_objects : enable
#extension GL_ARB_shading_language_420pack : enable

layout (binding = 2) uniform LightBuffer
{
    vec4 diffuseColor;
    vec3 lightDirection;
};

layout(binding = 5) uniform texture2D SurfaceTexture;
layout(binding = 6) uniform sampler Sampler;

layout(location = 0) in vec3 normal;
layout(location = 1) in vec2 texCoord;

layout(location = 0) out vec4 outputColor;

void main()
{
    vec4 ambientColor = vec4(.4, .4, .4, 1);

    vec4 color = texture(sampler2D(SurfaceTexture, Sampler), texCoord);
    vec3 lightDir = -normalize(lightDirection);
    float effectiveness = dot(normal, lightDir);
    float lightEffectiveness = clamp(effectiveness, 0, 1);
    vec4 lightColor = clamp(diffuseColor * lightEffectiveness, 0, 1);
    outputColor = clamp((lightColor * color) + (ambientColor * color), 0, 1);
}
