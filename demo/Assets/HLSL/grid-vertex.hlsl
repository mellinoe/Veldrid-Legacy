cbuffer ProjectionMatrixBuffer : register(b0)
{
    float4x4 projection;
}

cbuffer ViewMatrixBuffer : register(b1)
{
    float4x4 view;
}

struct VSInput
{
    float3 position : POSITION;
};

struct PSInput
{
    float4 position : SV_POSITION;
    float3 worldPosition : TEXCOORD0;
};

PSInput VS(VSInput input)
{
    PSInput output;
    float4 worldPosition = float4(input.position, 1);
    float4 viewPosition = mul(view, worldPosition);
    output.position = mul(projection, viewPosition);
    output.worldPosition = worldPosition.xyz;

    return output;
}
