#version 330 core

in vec2 vsout_texCoord;
out vec4 out_fragColor;

uniform sampler2D SurfaceTexture;

void main() 
{
    vec4 texColor = texture(SurfaceTexture, vsout_texCoord);
    out_fragColor = texColor;
}