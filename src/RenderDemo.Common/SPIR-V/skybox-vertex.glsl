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

layout(location = 0) in vec3 position;
layout(location = 0) out vec3 TexCoords;

void main()
{
    gl_Position = (projection * view * vec4(position, 1.0)).xyww;  
    gl_Position.y = -gl_Position.y; // Correct for Vulkan clip coordinates
    TexCoords = position;
}  