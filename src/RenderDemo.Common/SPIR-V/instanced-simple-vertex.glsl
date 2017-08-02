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

// Per-Vertex
layout(location = 0) in vec3 in_position;
// Per-Instance
layout(location = 1) in vec3 in_offset;
layout(location = 2) in vec4 in_color;

layout(location = 0) out vec4 out_color;

out gl_PerVertex
{
    vec4 gl_Position;
};

void main()
{
    vec4 worldPos = world * vec4(in_position + in_offset, 1);
    vec4 viewPos = view * worldPos;
    vec4 projPos = projection * viewPos;
    gl_Position = projPos;
    gl_Position.y *= -1;

    out_color = in_color;
}
