#version 330 core

uniform WorldViewProjectionBuffer
{
    mat4 worldViewProjection;
};

in vec3 vsin_position;
in vec2 vsin_texCoord;

out vec2 vsout_texCoord;

void main() 
{
    gl_Position = worldViewProjection * vec4(vsin_position, 1.0);
    vsout_texCoord = vsin_texCoord;
}
