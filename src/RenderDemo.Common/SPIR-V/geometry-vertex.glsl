#version 450

#extension GL_ARB_separate_shader_objects : enable
#extension GL_ARB_shading_language_420pack : enable

layout(location = 0) in vec3 in_position;
layout(location = 0) out vec4 out_color;

out gl_PerVertex
{
    vec4 gl_Position;
};

void main()
{
    // Pass-through position
    gl_Position = vec4(in_position, 0);
    out_color = vec4(1, 0, 0, 1); // Set by geometry shader.
}
