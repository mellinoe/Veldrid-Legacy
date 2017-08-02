#version 450

#extension GL_ARB_separate_shader_objects : enable
#extension GL_ARB_shading_language_420pack : enable

layout(binding = 0) uniform ProjectionMatrixBuffer
{
    mat4 projection;
};

layout(binding = 1) uniform ViewMatrixBuffer
{
    mat4 view;
};

layout(binding = 2) uniform WorldMatrixBuffer
{
    mat4 world;
};

layout(location = 0) in vec3 in_position;
layout(location = 1) in vec3 in_normal;
layout(location = 2) in vec2 in_texCoord;

out gl_PerVertex 
{
    vec4 gl_Position;
};

void main()
{
    vec4 worldPos = world * vec4(in_position, 1);
    vec4 viewPos = view * worldPos;
    vec4 screenPos = projection * viewPos;
    gl_Position = screenPos;
    gl_Position.y = -gl_Position.y; // Correct for Vulkan clip coordinates
}