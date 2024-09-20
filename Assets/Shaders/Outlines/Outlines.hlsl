#ifndef OUTLINES_INCLUDED
#define OUTLINES_INCLUDED

#include <HLSLSupport.cginc>

SamplerState point_clamp_sampler;

float _DebugOn;
float _External;
float _Convex;
float _Concave;
float _ExternalScale;
float _InternalScale;
float _DepthThreshold;
float _NormalsThreshold;
float _HighlightPower;

Texture2D _CameraDepthTexture;
Texture2D _NormalsTexture;

float _Zoom;

float3 ViewNormalToWorld(float3 viewNormal) {
    return normalize(mul(UNITY_MATRIX_I_V, float4(viewNormal * 2 - 1, 0)));
}

// float Remap(float value, float2 from, float2 to) {
//     float t = (value - from[0]) / (from[1] - from[0]);
//     return lerp(to[0], to[1], t);
// }

float GetDepth(float2 uv)
{
    return _CameraDepthTexture.Sample(point_clamp_sampler, uv);
}

float3 GetNormal(float2 uv)
{
    return normalize(_NormalsTexture.Sample(point_clamp_sampler, uv));
}

void GetNeighbourUVs(float2 uv, float distance, out float2 neighbours[4])
{
    float2 pixel_size = 1. / _ScreenParams.xy;
    neighbours[0] = uv + float2(0, pixel_size.y) * distance;
    neighbours[1] = uv - float2(0, pixel_size.y) * distance;
    neighbours[2] = uv + float2(pixel_size.x, 0) * distance;
    neighbours[3] = uv - float2(pixel_size.x, 0) * distance;
}

void GetDepthDiffSum(float depth, float2 neighbours[4], out float depth_diff_sum) {
    depth_diff_sum = 0;
    [unroll]
    for (int i = 0; i < 4; ++i)
        depth_diff_sum += depth - GetDepth(neighbours[i]);
}

void GetNormalDiffSum(float3 normal, float2 neighbours[4], out float normal_diff_sum) {
    normal_diff_sum = 0;
    float3 normal_edge_bias = normalize(float3(1, 1, 1));

    [unroll]
    for (int j = 0; j < 4; ++j) {
        float3 neighbour_normal = GetNormal(neighbours[j]);
        float3 normal_diff = normal - neighbour_normal;
        float normal_diff_weight = smoothstep(-.01, .01, dot(normal_diff, normal_edge_bias));

        normal_diff_sum += dot(normal_diff, normal_diff) * normal_diff_weight;
    }
}

float Spike(float t) {
    return lerp(t * t, 1 - ((1 - t) * (1 - t)), t);
}

float3 OutlineColour(float2 uv, fixed3 base_colour, float illumination, float3 luminance)
{
    float depth_diff_sum = 0.;
    float normal_diff_sum = 0.;

    float2 neighbour_depths[4];
    float2 neighbour_normals[4];

    float3 external_outline_colour, internal_outline_colour;

    if (_DebugOn) {
        base_colour = 0;
        external_outline_colour = float3(0, 0, 1);
        internal_outline_colour = float3(1, 0, 0);
    }
    else {
        // external_outline_colour = lerp(base_colour / 2, luminance * illumination, Spike(_HighlightPower));
        // external_outline_colour = lerp(, , _HighlightPower);

        external_outline_colour = lerp(base_colour / 2, luminance, _HighlightPower);
        internal_outline_colour = external_outline_colour;
    }

    GetNeighbourUVs(uv, _ExternalScale, neighbour_depths);
    GetNeighbourUVs(uv, _InternalScale, neighbour_normals);

    GetDepthDiffSum(GetDepth(uv), neighbour_depths, depth_diff_sum);
    GetNormalDiffSum(GetNormal(uv), neighbour_normals, normal_diff_sum);

    float depth_edge = step(_DepthThreshold / 10000., depth_diff_sum);
    float normal_edge = step(_NormalsThreshold, sqrt(normal_diff_sum));

    if (depth_edge > 0 && _External)
        return lerp(base_colour, external_outline_colour, depth_edge);
    if (depth_diff_sum < 0 && _Concave || depth_diff_sum > 0 && _Convex)
        return lerp(base_colour, internal_outline_colour, normal_edge);
    return base_colour;
}

void GetOutline_float(float2 uv, float3 base_colour, float illumination, float3 luminance, out float3 colour) {
    if (!_External && !_Convex && !_Concave) {
        colour = base_colour;
        return;
    }
    colour = OutlineColour(uv, base_colour, illumination, luminance);
}
#endif