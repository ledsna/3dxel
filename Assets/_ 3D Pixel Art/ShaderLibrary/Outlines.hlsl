#ifndef OUTLINES_INCLUDED
#define OUTLINES_INCLUDED

// #ifndef real
// #include <HLSLSupport.cginc>
// #endif
//
// #ifndef SampleSceneDepth
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
// #endif

SamplerState point_clamp_sampler;

// Texture2D _CameraDepthTexture;
Texture2D _NormalsTexture;

// half3 ViewNormalToWorld(half3 viewNormal) {
//     return normalize(mul(UNITY_MATRIX_I_V, half4(viewNormal * 2 - 1, 0)).xyz);
// }

// float Remap(float value, float2 from, float2 to) {
//     float t = (value - from[0]) / (from[1] - from[0]);
//     return lerp(to[0], to[1], t);
// }

half GetDepth(half2 uv)
{
    half rawDepth = SampleSceneDepth(uv);
    half orthoLinearDepth = _ProjectionParams.x > 0 ? rawDepth : 1 - rawDepth;
    half orthoEyeDepth = lerp(_ProjectionParams.y, _ProjectionParams.z, orthoLinearDepth);

    return orthoEyeDepth;
}

half3 GetNormal(half2 uv)
{
    return normalize(_NormalsTexture.Sample(point_clamp_sampler, uv).xyz);
}

void GetNeighbourUVs(half2 uv, half distance, out half2 neighbours[4])
{
    half2 error  = half2(_ScreenParams.x / 641, _ScreenParams.y / 361);
    neighbours[0] = uv + half2(0, _ScreenParams.w - 1) * error * distance;
    neighbours[1] = uv - half2(0, _ScreenParams.w - 1) * error * distance;
    neighbours[2] = uv + half2(_ScreenParams.z - 1, 0) * error * distance;
    neighbours[3] = uv - half2(_ScreenParams.z - 1, 0) * error * distance;
}

void GetDepthDiffSum(half depth, half2 neighbours[4], out half depth_diff_sum) {
    depth_diff_sum = 0;
    [unroll]
    for (int i = 0; i < 4; ++i)
        depth_diff_sum += GetDepth(neighbours[i]) - depth;
}

void GetNormalDiffSum(half3 normal, half2 neighbours[4], out half normal_diff_sum) {
    normal_diff_sum = 0;
    half3 normal_edge_bias = normalize(float3(1, 1, 1));

    [unroll]
    for (int j = 0; j < 4; ++j) {
        half3 neighbour_normal = GetNormal(neighbours[j]);
        half3 normal_diff = normal - neighbour_normal;
        half normal_diff_weight = smoothstep(-.01, .01, dot(normal_diff, normal_edge_bias));

        normal_diff_sum += dot(normal_diff, normal_diff) * normal_diff_weight;
    }
}

// real Spike(half t) {
//     return lerp(t * t, 1 - ((1 - t) * (1 - t)), t);
// }

half3 OutlineColour(half2 uv, half3 albedo, half3 lit_colour)
{
    // return GetDepth(uv);
    // return GetNormal(uv);
    half2 neighbour_depths[4];
    half2 neighbour_normals[4];

    half3 external_outline_colour, internal_outline_colour;

    if (_DebugOn) {
        lit_colour = 0;
        external_outline_colour = half3(0, 0, 1);
        internal_outline_colour = half3(1, 0, 0);
    }
    else {
        // half multiplier = RGBtoHSV(lit_colour / float3(max(albedo.r, 0.0001), max(albedo.g, 0.0001), max(albedo.b, 0.0001))).b / RGBtoHSV(albedo).b;
        // hsv_lit.b *= lerp(0, multiplier, _OutlineStrength);

        // ABSOLUTE CINEMA
        // hsv_lit.b = lerp(hsv_lit.b, 1, _OutlineStrength);
        
        half3 complement = _OutlineColour.rgb / float3(max(albedo.r, 0.0001), max(albedo.g, 0.0001), max(albedo.b, 0.0001));

        half k;
        half A = RGBtoHSV(albedo).z;
        half3 hsv_lit = RGBtoHSV(lit_colour);
        
        half L = hsv_lit.z;
        if (L - A > 0)
            k = lerp(1, (1-A)/(L-A), _OutlineStrength);
        else if (L - A < 0)
            k = lerp(1, -A/(L-A), _OutlineStrength);
        else
            k = 0;
        
        hsv_lit.z = A * (1 - k) + L * k;
        
        external_outline_colour = HSVtoRGB(hsv_lit) * complement;
        internal_outline_colour = external_outline_colour;
    }
    #if UNITY_EDITOR
    #endif
    
    GetNeighbourUVs(uv, _ExternalScale, neighbour_depths);
    GetNeighbourUVs(uv, _InternalScale, neighbour_normals);

    half depth_diff_sum, normal_diff_sum;
    GetDepthDiffSum(GetDepth(uv), neighbour_depths, depth_diff_sum);
    GetNormalDiffSum(GetNormal(uv), neighbour_normals, normal_diff_sum);

    half depth_edge = step(_DepthThreshold / 10000., depth_diff_sum);
    half normal_edge = step(_NormalsThreshold, sqrt(normal_diff_sum));

    if (depth_edge > 0 && _External)
        return lerp(lit_colour, external_outline_colour, depth_edge);
    if (depth_diff_sum < 0 && _Concave || depth_diff_sum > 0 && _Convex)
        return lerp(lit_colour, internal_outline_colour, normal_edge);
    return lit_colour;
}

half3 GetOutline_float(half2 uv, half3 albedo, half3 lit_colour) {
    if (!_External && !_Convex && !_Concave) {
        return lit_colour;
    }

    return OutlineColour(uv, albedo, lit_colour);
}
#endif

