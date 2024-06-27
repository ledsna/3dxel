#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

TEXTURE2D(_ColorMap);
SAMPLER(sampler_ColorMap);
float4 _ColorMap_ST;
float4 _ColorTint;

float _Smoothness;


struct Attributes
{
    float3 positionOS : POSITION;
    float3 normalOS : NORMAL;
    float2 uv : TEXCOORD0;
};

struct Interpolators
{
    float4 positionCS : SV_POSITION;
    float2 uv : TEXCOORD0;
    float3 positionWS : TECOORD1;
    float3 normalWS : TEXCOORD2;
};

Interpolators Vertex(Attributes input)
{
    Interpolators output;
    
    VertexPositionInputs position_inputs = GetVertexPositionInputs(input.positionOS);
    VertexNormalInputs normal_inputs = GetVertexNormalInputs(input.normalOS);
    
    output.positionCS = position_inputs.positionCS;
    output.positionWS = position_inputs.positionWS;
    output.normalWS = normal_inputs.normalWS;
    output.uv = TRANSFORM_TEX(input.uv, _ColorMap);
    
    return output;
}

float4 Fragment(Interpolators input) : SV_TARGET {
    float4 color_sample = SAMPLE_TEXTURE2D(_ColorMap, sampler_ColorMap, input.uv);
    
    InputData lighting_input = (InputData)0;
    lighting_input.normalWS = normalize(input.normalWS);
    lighting_input.viewDirectionWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    lighting_input.shadowCoord = TransformWorldToShadowCoord(input.positionWS);
        
    SurfaceData surface_input = (SurfaceData)0;
    surface_input.albedo = color_sample.rgb * _ColorTint;
    surface_input.alpha = color_sample.a * _ColorTint;
    surface_input.specular = 1;
    surface_input.smoothness = _Smoothness;
    
    return UniversalFragmentBlinnPhong(lighting_input, surface_input);
    
}