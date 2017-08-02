struct PixelInput
{
    float4 position : SV_POSITION;
    float2 texCoord : TEXCOORD0;
};

Texture2D SurfaceTexture : register(t0);
SamplerState Sampler : register(s0);

float4 PS(PixelInput input) : SV_Target
{
    float4 color = SurfaceTexture.Sample(Sampler, input.texCoord);
    return color;
}
