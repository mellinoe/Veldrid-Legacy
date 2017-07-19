#version 450

#extension GL_ARB_separate_shader_objects : enable
#extension GL_ARB_shading_language_420pack : enable

layout (location = 0) in vec3 vsin_position;
layout (location = 1) in vec4 vsin_color;

layout (binding = 0) uniform World
{
    mat4 world;
} WorldBuffer;

layout (binding = 0) uniform View
{
    mat4 view;
} ViewBuffer;


layout (binding = 0) uniform Projection
{
    mat4 projection;
} ProjectionBuffer;


layout (location = 0) out vec4 vsout_color;

out gl_PerVertex 
{
    vec4 gl_Position;
};

void main() 
{
    gl_Position = ProjectionBuffer.projection * ViewBuffer.view * WorldBuffer.world * vec4(vsin_position, 1.0);
    vsout_color = vsin_color;
}
