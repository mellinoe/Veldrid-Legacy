cbuffer WorldMatrixBuffer : register(b0)
{
    float4x4 world;
}

cbuffer ViewMatrixBuffer : register(b1)
{
    float4x4 view;
}

cbuffer ProjectionMatrixBuffer : register(b2)
{
    float4x4 projection;
}

struct VertexInput
{
    float3 position : POSITION;
    float2 texCoord : TEXCOORD0;
};

struct PixelInput
{
    float4 position : SV_POSITION;
    float2 texCoord : TEXCOORD0;
};

PixelInput VS(VertexInput input)
{
    PixelInput output;

    float4 worldPosition = mul(world, float4(input.position, 1));
    float4 viewPosition = mul(view, worldPosition);
    output.position = mul(projection, viewPosition);

    output.texCoord = input.texCoord;

    return output;
}
