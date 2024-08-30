#ifndef OUTLINES_INCLUDED
#define OUTLINES_INCLUDED

#include <HLSLSupport.cginc>

SamplerState point_clamp_sampler;
Texture2D _CameraDepthTexture;

float _Zoom;

float3 ViewNormalToWorld(float3 viewNormal) {
    return normalize(mul(UNITY_MATRIX_I_V, float4(viewNormal * 2 - 1, 0)));
}

float Remap(float value, float2 from, float2 to) {
    float t = (value - from[0]) / (from[1] - from[0]);
    return lerp(to[0], to[1], t);
}

float CalculateLighting(float3 viewNormal, float3 lightDirection)
{
    float diffuse = dot(ViewNormalToWorld(viewNormal), lightDirection);
    return Remap(diffuse, float2(0, 1), float2(_ShadowPower, _HighlightPower));
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

void MainLight_float (out float3 Direction, out float3 Color, out float DistanceAtten){
	#ifdef SHADERGRAPH_PREVIEW
		Direction = normalize(float3(1,1,-0.4));
		Color = float4(1,1,1,1);
		DistanceAtten = 1;
	#else
		Light mainLight = GetMainLight();
		Direction = mainLight.direction;
		Color = mainLight.color;
		DistanceAtten = mainLight.distanceAttenuation;
	#endif
}

void MainLightShadows_float (float3 WorldPos, half4 Shadowmask, out float ShadowAtten){
	#ifdef SHADERGRAPH_PREVIEW
		ShadowAtten = 1;
	#else
		#if defined(_MAIN_LIGHT_SHADOWS_SCREEN) && !defined(_SURFACE_TYPE_TRANSPARENT)
		float4 shadowCoord = ComputeScreenPos(TransformWorldToHClip(WorldPos));
		#else
		float4 shadowCoord = TransformWorldToShadowCoord(WorldPos);
		#endif
		ShadowAtten = MainLightShadow(shadowCoord, WorldPos, Shadowmask, _MainLightOcclusionProbes);
	#endif
}

float3 OutlineColour(float2 uv, fixed3 base_colour, float attenuation, float3 luminance)
{
    float3 external_outline_colour, internal_outline_colour;

    if (_Debug) {
        base_colour = 0;
        external_outline_colour = float3(0, 0, 1);
        internal_outline_colour = float3(1, 0, 0);
    }

    float depth = GetDepth(uv);
    float3 normal = GetNormal(uv);

    float2 neighbour_depths[4];
    float2 neighbour_normals[4];

    GetNeighbourUVs(uv, _DepthOutlineScale, neighbour_depths);
    GetNeighbourUVs(uv, _NormalsOutlineScale, neighbour_normals);
    
    float depth_diff_sum = 0.;

    float dot_sum = 0.0;
    float3 normal_edge_bias = normalize(float3(1, 1, 1));

    if (_DepthOutlineScale != 0) {
        [unroll]
        for (int i = 0; i < 4; ++i)
            depth_diff_sum += depth - GetDepth(neighbour_depths[i]);
    }

    if (depth_diff_sum < 0.0)
        return base_colour;

    if (_NormalsOutlineScale != 0) {
        [unroll]
        for (int j = 0; j < 4; ++j) {
            float3 neighbour_normal = GetNormal(neighbour_normals[j]);
            float3 normal_diff = normal - neighbour_normal;
            float normal_diff_weight = smoothstep(-.01, .01, dot(normal_diff, normal_edge_bias));

            dot_sum += dot(normal_diff, normal_diff) * normal_diff_weight;
        }
    }
    
    float normal_edge = step(_NormalsThreshold, sqrt(dot_sum));
    float depth_edge = step(_DepthThreshold / 10000., depth_diff_sum);

    external_outline_colour = lerp(base_colour / _ShadowPower, luminance, pow(attenuation, 1));
    internal_outline_colour = external_outline_colour;
    
    if (depth_edge > 0.0)
        return lerp(base_colour, external_outline_colour, depth_edge);
    return lerp(base_colour, internal_outline_colour, normal_edge);
}

void GetOutline_float(float2 uv, float3 base_colour, float attenuation, float3 luminance, out float3 colour) {
    #if SHADERGRAPH_PREVIEW
        colour = base_colour;
    #else
        if (!_Outlined) {
            colour = base_colour;
            return;
        }
        colour = OutlineColour(uv, base_colour, attenuation, luminance);
    #endif
}
#endif