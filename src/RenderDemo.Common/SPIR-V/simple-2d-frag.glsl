#version 450

#extension GL_ARB_separate_shader_objects : enable
#extension GL_ARB_shading_language_420pack : enable

layout(binding = 2) uniform texture2D SurfaceTexture;
layout(binding = 3) uniform sampler SurfaceSampler;

layout(location = 0) in vec2 out_texCoord;

layout(location = 0) out vec4 outputColor;

void main()
{
    bool flipTexCoords = false;
    vec2 texCoord_mod = out_texCoord;
    if (flipTexCoords)
    {
        texCoord_mod.y = 1 - texCoord_mod.y;
    }
    float r = texture(sampler2D(SurfaceTexture, SurfaceSampler), texCoord_mod).r;
    outputColor = vec4(r, r, r, 1);
}
