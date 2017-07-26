#version 450

#extension GL_ARB_separate_shader_objects : enable
#extension GL_ARB_shading_language_420pack : enable

layout (binding = 0) uniform Projection
{
    mat4 projection_matrix;
};

layout (binding = 1) uniform ModelView
{
    mat4 modelview_matrix;
};


layout(location = 0) in vec3 in_position;
layout(location = 1) in vec4 in_color;

layout(location = 0) out vec4 color;

out gl_PerVertex 
{
    vec4 gl_Position;
};

void main()
{
    gl_Position = projection_matrix * modelview_matrix * vec4(in_position, 1);
    gl_Position.y = -gl_Position.y; // Correct for Vulkan clip coordinates
    color = in_color;
}
