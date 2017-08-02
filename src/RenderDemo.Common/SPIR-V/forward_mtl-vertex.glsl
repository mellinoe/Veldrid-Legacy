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

layout(binding = 2) uniform LightProjectionMatrixBuffer
{
    mat4 lightProjection;
};

layout(binding = 3) uniform LightViewMatrixBuffer
{
    mat4 lightView;
};

layout(binding = 7) uniform WorldMatrixBuffer
{
    mat4 world;
};

layout(binding = 8) uniform InverseTransposeWorldMatrixBuffer
{
    mat4 inverseTransposeWorld;
};

layout(location = 0) in vec3 in_position;
layout(location = 1) in vec3 in_normal;
layout(location = 2) in vec2 in_texCoord;

layout(location = 0) out vec3 out_position_worldSpace;
layout(location = 1) out vec4 out_lightPosition; //vertex with regard to light view
layout(location = 2) out vec3 out_normal;
layout(location = 3) out vec2 out_texCoord;

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

    out_position_worldSpace = worldPosition.xyz;

    out_normal = mat3(inverseTransposeWorld) * in_normal;
    out_normal = normalize(out_normal);

    out_texCoord = in_texCoord;

    //store worldspace projected to light clip space with
    //a texcoord semantic to be interpolated across the surface
    out_lightPosition = world * vec4(in_position, 1);
    out_lightPosition = lightView * out_lightPosition;
    out_lightPosition = lightProjection * out_lightPosition;
    out_lightPosition.y = -out_lightPosition.y; // Correct for Vulkan clip coordinates
}