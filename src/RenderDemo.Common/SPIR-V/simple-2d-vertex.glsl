#version 450

#extension GL_ARB_separate_shader_objects : enable
#extension GL_ARB_shading_language_420pack : enable

layout(binding = 0) uniform WorldMatrixBuffer
{
    mat4 world;
};

layout(binding = 1) uniform ProjectionMatrixBuffer
{
    mat4 projection;
};

layout(location = 0) in vec3 in_position;
layout(location = 1) in vec2 in_texCoord;

layout(location = 0) out vec2 out_texCoord;

void main()
{
    vec4 out_position = vec4(in_position, 1);
    out_position = world * out_position;
    out_position = projection * out_position;
    gl_Position = out_position;
    gl_Position.y = -gl_Position.y; // Correct for Vulkan clip coordinates
    out_texCoord = in_texCoord;
}
