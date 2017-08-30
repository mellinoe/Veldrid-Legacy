struct PSInput
{
    float4 position : SV_POSITION;
    float3 worldPosition : TEXCOORD0;
};

// Shader code from http://madebyevan.com/shaders/grid/

float4 PS(PSInput input) : SV_Target
{
    // Pick a coordinate to visualize in a grid
    float2 coord = input.worldPosition.xz;

    // Compute anti-aliased world-space grid lines
    float2 grid = abs(frac((coord / 100) - 0.5) - 0.5) / fwidth(coord);
    float l = min(grid.x, grid.y) * 100;

    // Just visualize the grid lines directly
    float comp = 1.0 - min(l, 1.0);
    return float4(float3(comp, comp, comp), 0.8);
}