#version 450

#extension GL_ARB_separate_shader_objects : enable
#extension GL_ARB_shading_language_420pack : enable

layout (binding = 0) uniform ProjectionMatrixBuffer
{
    mat4 projection_matrix;
};

layout (binding = 1) uniform ViewMatrixBuffer
{
    mat4 view_matrix;
};

layout (binding = 3) uniform WorldMatrixBuffer
{
    mat4 world_matrix;
};

layout (binding = 4) uniform InverseTransposeWorldMatrixBuffer
{
    mat4 inverseTransposeWorldMatrix;
};

layout (location = 0) in vec3 in_position;
layout (location = 1) in vec3 in_normal;
layout (location = 2) in vec2 in_texCoord;

layout (location = 0) out vec3 normal;
layout (location = 1) out vec2 texCoord;

out gl_PerVertex 
{
    vec4 gl_Position;
};

void main()
{
    vec4 worldPos = world_matrix * vec4(in_position, 1);
    vec4 viewPos = view_matrix * worldPos;
    vec4 screenPos = projection_matrix * viewPos;
    gl_Position = screenPos;
    gl_Position.y = -gl_Position.y; // Correct for Vulkan clip coordinates

    texCoord = in_texCoord; // Pass along unchanged.

    normal = normalize(mat3(inverseTransposeWorldMatrix) * in_normal);
}
