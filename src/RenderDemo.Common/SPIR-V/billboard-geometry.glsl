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

layout(binding = 2) uniform CameraInfoBuffer
{
    vec3 cameraWorldPosition;
    float _unused1;
    vec3 cameraLookDirection;
    float _unused2;
};

layout(binding = 3) uniform WorldMatrixBuffer
{
    mat4 world;
};

layout (points) in;
layout (triangle_strip, max_vertices = 4) out;

layout(location = 0) in vec4 in_color[];

layout(location = 0) out vec4 out_color;

void main()
{
    vec4 inPos = vec4(gl_in[0].gl_Position.xyz, 1);
    vec3 worldCenter = (world * inPos).xyz;
    vec3 globalUp = vec3(0, 1, 0);
    vec3 right = normalize(cross(cameraLookDirection, globalUp));
    vec3 up = normalize(cross(right.xyz, cameraLookDirection));
    vec3 worldPositions[4] = vec3[4]
    (
        worldCenter - right * .5 + up * .5,
        worldCenter + right * .5 + up * .5,
        worldCenter - right * .5 - up * .5,
        worldCenter + right * .5 - up * .5
    );
    
    for (int i = 0; i < 4; i++)
    {
        gl_Position = projection * view * vec4(worldPositions[i], 1);
        gl_Position.y *= -1;
        out_color = vec4(1, 0, 0, 1);
        EmitVertex();
    }
}