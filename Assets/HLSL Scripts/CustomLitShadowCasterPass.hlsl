#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

float3 _LightDirection;

struct Attributes
{
    float3 positionOS : POSITION;
    float3 normalOS : NORMAL;
};

struct Interpolators
{
    float4 positionCS : SV_POSITION;
};

float4 GetShadowCasterPositionCS(float3 positionWS, float3 normalWS)
{
    float3 lightDirectionWS = _LightDirection;
    float4 positionCS = TransformWorldToHClip(ApplyShadowBias(positionWS, normalWS, lightDirectionWS));
    #if UNITY_REVERSED_Z
        positionCS.z = min(positionCS.z, UNITY_NEAR_CLIP_VALUE);
    #else
        positionCS.z = max(positionCS.z, UNITY_NEAR_CLIP_VALUE);
    #endif
    return positionCS;
}

Interpolators Vertex(Attributes input)
{
    Interpolators output;
    
    VertexPositionInputs position_inputs = GetVertexPositionInputs(input.positionOS);
    VertexNormalInputs normal_inputs = GetVertexNormalInputs(input.normalOS); 
    
    output.positionCS = position_inputs.positionCS;
    return output;
}

float4 Fragment(Interpolators input) : SV_TARGET {
    return 0;
}