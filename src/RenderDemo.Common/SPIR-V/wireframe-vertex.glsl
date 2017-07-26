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
layout(location = 1) in vec4 in_color;

layout(location = 0) out vec4 fsin_color;

out gl_PerVertex 
{
    vec4 gl_Position;
};

void main()
{

    vec4 worldPosition = world * vec4(in_position, 1);
    vec4 viewPosition = view * worldPosition;
    gl_Position = projection * viewPosition;
    gl_Position.y = -gl_Position.y; // Correct for Vulkan clip coordinates

    fsin_color = in_color;
}
