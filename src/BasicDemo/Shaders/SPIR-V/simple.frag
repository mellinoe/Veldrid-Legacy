#version 450

#extension GL_ARB_separate_shader_objects : enable
#extension GL_ARB_shading_language_420pack : enable

layout (location = 0) in vec2 in_texCoord;
layout (location = 0) out vec4 out_fragColor;

layout(binding = 3) uniform texture2D SurfaceTexture;
layout(binding = 4) uniform sampler Sampler;

void main() 
{
    vec4 texColor = texture(sampler2D(SurfaceTexture, Sampler), in_texCoord);
    out_fragColor = texColor;
}