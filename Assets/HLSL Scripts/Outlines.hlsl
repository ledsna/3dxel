#include <HLSLSupport.cginc>

SamplerState point_clamp_sampler;
Texture2D _CameraDepthTexture;

float _Zoom;
float4 _LightColour;
half4 unity_LightColor;

float3 ViewNormalToWorld(float3 viewNormal) {
    return normalize(mul(UNITY_MATRIX_I_V, float4(viewNormal * 2 - 1, 0)));

}

float DiffuseComponent(float3 worldNormal, float3 lightDirection) {
    return dot(normalize(worldNormal), normalize(lightDirection));
}

float DiffuseForView(float3 viewNormal, float3 lightDirection)
{
    return DiffuseComponent(ViewNormalToWorld(viewNormal), lightDirection) *
        (_HighlightPower - _ShadowPower) + _ShadowPower;
}

float GetDepth(float2 uv)
{
    return _CameraDepthTexture.Sample(point_clamp_sampler, uv);
}

float3 GetNormal(float2 uv)
{
    return _NormalsTexture.Sample(point_clamp_sampler, uv);
}

void GetNeighbourUVs(float2 uv, float distance, out float2 neighbours[4])
{
    float2 pixel_size = 1. / _ScreenParams.xy;
    neighbours[0] = uv + float2(0, pixel_size.y) * distance;
    neighbours[1] = uv - float2(0, pixel_size.y) * distance;
    neighbours[2] = uv + float2(pixel_size.x, 0) * distance;
    neighbours[3] = uv - float2(pixel_size.x, 0) * distance;
}

float3 OutlineColour(float2 uv, fixed3 base_colour, float3 lightDirection)
{
    float depth = GetDepth(uv);
    float3 normal = GetNormal(uv);
    return normal;
    float3 normal_edge_bias = normalize(float3(1, 1, 1));

    float2 neighbour_depths[4];
    float2 neighbour_normals[4];
    
    GetNeighbourUVs(uv, _DepthOutlineScale, neighbour_depths);
    GetNeighbourUVs(uv, _NormalsOutlineScale, neighbour_normals);
    
    float depth_diff_sum = 0.;

    [unroll]
    for (int d = 0; d < 4; d++)
        depth_diff_sum += depth - GetDepth(neighbour_depths[d]);

    float dotSum = 0.0;
    [unroll]
    for (int n = 0; n < 4; n++)
    {
        float3 neighbour_normal = GetNormal(neighbour_normals[n]);
        float3 normal_diff = normal - neighbour_normal;
        float normal_diff_weight = smoothstep(-.01, .01, dot(normal_diff, normal_edge_bias));

        dotSum += dot(normal_diff, normal_diff) * normal_diff_weight;
    } 
    
    float normal_edge = step(_NormalsThreshold, sqrt(dotSum));
    float depth_edge = step(_DepthThreshold / 10000., depth_diff_sum);

    fixed3 external_outline_colour = base_colour * (_ShadowPower - 1);
    fixed3 internal_outline_colour = base_colour * DiffuseForView(normal, lightDirection);
    // external_outline_colour = unity_LightColor;

    // external_outline_colour = lerp(external_outline_colour, lerp(base_colour, _LightColour, _HighlightPower - 2), 
    // saturate(DiffuseComponent(ViewNormalToWorld(normal), lightDirection)));

    // Debug
    // base_colour = fixed3(1, 1, 1); 
    // external_outline_colour = fixed3(0, 0, 1);
    // internal_outline_colour = fixed3(1, 0, 0);

    if (depth_diff_sum < 0.0)
        return base_colour;
    if (depth_edge > 0.0)
        return lerp(base_colour, external_outline_colour, depth_edge);
    return lerp(base_colour, internal_outline_colour, normal_edge);
}

#ifndef OUTLINES_INCLUDED
#define OUTLINES_INCLUDED

void GetOutline_float(float2 uv, float3 base_colour, float3 lightDirection, out float3 colour) {
    
    colour = OutlineColour(uv, base_colour, -lightDirection);
}
#endif