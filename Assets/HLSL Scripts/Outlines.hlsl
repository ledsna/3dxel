#include <HLSLSupport.cginc>
SamplerState point_clamp_sampler;
// Texture2D _FilteredNormalsTexture;
float4x4 _CameraViewToWorld;
float _Zoom;
// float _DepthOutlineScale;
// float _NormalsOutlineScale;
// float _DepthThreshold;
// float _NormalsThreshold;
// float _HighlightPower;
// float _ShadowPower;

float3 ViewNormalToWorld(float3 viewNormal) {
    return normalize(mul((float3x3)_CameraViewToWorld, float4(normalize(viewNormal * 2 - 1), 0)));
}

float DiffuseComponent(float3 worldNormal, float3 lightDirection) {
    return dot(normalize(worldNormal), normalize(lightDirection));
}

float DiffuseForView(float3 viewNormal, float3 lightDirection)
{
    return DiffuseComponent(ViewNormalToWorld(viewNormal), lightDirection) *
        (_HighlightPower - _ShadowPower) + _ShadowPower;
}

float getDepth(float2 uv)
{
    return _CameraDepthTexture.Sample(point_clamp_sampler, uv);
}

float3 getNormal(float2 uv)
{
    return _FilteredNormalsTexture.Sample(point_clamp_sampler, uv);
}

void get_neighbour_uvs(float2 uv, float distance, out float2 neighbours[4])
{
    float2 pixel_size = 1. / _ScreenParams.xy;
    neighbours[0] = uv + float2(0, pixel_size.y) * distance;
    neighbours[1] = uv - float2(0, pixel_size.y) * distance;
    neighbours[2] = uv + float2(pixel_size.x, 0) * distance;
    neighbours[3] = uv - float2(pixel_size.x, 0) * distance;
}

float3 outline_color(float2 uv, fixed3 base_color, float3 lightDirection)
{
    float depth = getDepth(uv);
    float3 normal = getNormal(uv);
    float3 normal_edge_bias = normalize(float3(1, 1, 1));

    float2 neighbour_depths[4];
    float2 neighbour_normals[4];
    
    get_neighbour_uvs(uv, _DepthOutlineScale, neighbour_depths);
    get_neighbour_uvs(uv, _NormalsOutlineScale, neighbour_normals);
    
    float depth_diff_sum = 0.;

    [unroll]
    for (int d = 0; d < 4; d++)
        depth_diff_sum += depth - getDepth(neighbour_depths[d]);

    float dotSum = 0.0;
    [unroll]
    for (int n = 0; n < 4; n++)
    {
        float3 neighbour_normal = getNormal(neighbour_normals[n]);
        float3 normal_diff = normal - neighbour_normal;
        float normal_diff_weight = smoothstep(-.01, .01, dot(normal_diff, normal_edge_bias));

        dotSum += dot(normal_diff, normal_diff) * normal_diff_weight;
    } 
    
    float normal_edge = step(_NormalsThreshold, sqrt(dotSum));
    float depth_edge = step(_DepthThreshold / 10000., depth_diff_sum);

    fixed3 external_outline_color = base_color * (_ShadowPower - 1);
    fixed3 internal_outline_color = base_color * DiffuseForView(normal, lightDirection);

    // Debug
    // base_color = fixed3(1, 1, 1); 
    // external_outline_color = fixed3(0, 0, 1);
    // internal_outline_color = fixed3(1, 0, 0);
    
    if (depth_diff_sum < 0.0)
        return base_color;
    if (depth_edge > 0.0)
        return lerp(base_color, external_outline_color, depth_edge);
    return lerp(base_color, internal_outline_color, normal_edge);
}


#ifndef OUTLINE_GRAPH_INCLUDED
#define OUTLINE_GRAPH_INCLUDED
void GetOutline_float(float2 uv, float3 base_color, float3 lightDirection, out float3 color) {
    
    color = outline_color(uv, base_color, -lightDirection);
}
#endif





