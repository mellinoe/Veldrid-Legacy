struct PSInput
{
    float4 position : SV_POSITION;
    float3 worldPosition : POSITION;
};

Texture2D GridTexture : register(t0);
SamplerState GridSampler : register(s0);

float4 PS(PSInput input) : SV_Target
{
    // Pick a coordinate to visualize in a grid
    float2 coord = input.worldPosition.xz / 10.0;
    return GridTexture.Sample(GridSampler, coord);
}