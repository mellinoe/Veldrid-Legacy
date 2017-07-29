#version 450

#extension GL_ARB_separate_shader_objects : enable
#extension GL_ARB_shading_language_420pack : enable

layout(location = 0) in vec3 TexCoords;
layout(location = 0) out vec4 color;

layout(binding = 2) uniform textureCube Skybox;
layout(binding = 3) uniform sampler SkyboxSampler;

void main()
{
    color = texture(samplerCube(Skybox, SkyboxSampler), TexCoords);
}