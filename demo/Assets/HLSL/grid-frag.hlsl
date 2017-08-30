struct PSInput
{
    float4 position : SV_POSITION;
    float3 worldPosition : TEXCOORD0;
};


float4 PS(PSInput input) : SV_Target
{
    if (frac(input.worldPosition.x / 10.0) < 0.05
        || frac(input.worldPosition.z / 10.0) < 0.05)
    {
        return float4(1, 1, 1, 1);
    }
    else
    {
        return float4(0, 0, 0, 1);
    }
}
