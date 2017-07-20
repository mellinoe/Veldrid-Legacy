#version 450

#extension GL_ARB_separate_shader_objects : enable
#extension GL_ARB_shading_language_420pack : enable

layout (location = 0) in vec3 vsin_position;
layout (location = 1) in vec2 vsin_texCoord;

layout (binding = 0) uniform World
{
    mat4 world;
} WorldBuffer;

layout (binding = 1) uniform View
{
    mat4 view;
} ViewBuffer;


layout (binding = 2) uniform Projection
{
    mat4 projection;
} ProjectionBuffer;


layout (location = 0) out vec2 vsout_texCoord;

out gl_PerVertex 
{
    vec4 gl_Position;
};

void main() 
{
	mat4 correctedProjection = ProjectionBuffer.projection;
	correctedProjection[1][1] *= -1;
    gl_Position = correctedProjection * ViewBuffer.view * WorldBuffer.world * vec4(vsin_position, 1.0);
    vsout_texCoord = vsin_texCoord;
}
