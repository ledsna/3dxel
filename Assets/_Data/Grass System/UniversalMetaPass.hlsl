#ifndef UNIVERSAL_META_PASS_INCLUDED
#define UNIVERSAL_META_PASS_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/MetaInput.hlsl"

struct GrassData
{
    float3 position;
    float3 normal;
    float2 lightmapUV;
};

StructuredBuffer<GrassData> _SourcePositionGrass;
StructuredBuffer<int> _MapIdToData;

// Inputs
float4x4 m_RS;
// Globals
float4x4 m_MVP;
float3 normalWS; 
float3 positionWS;
float2 lightmapUV;

// Is called for each instance before vertex stage
void Setup()
{
    #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
    GrassData instanceData = _SourcePositionGrass[_MapIdToData[unity_InstanceID]];
    normalWS = instanceData.normal;
    positionWS = instanceData.position;
    // lightmapUV = (positionWS.xz * _LightmapST.xy) + _LightmapST.zw;
    lightmapUV = instanceData.lightmapUV;

    unity_ObjectToWorld._m03_m13_m23_m33 = float4(instanceData.position + instanceData.normal * _Scale / 2 , 1.0);

    unity_ObjectToWorld = mul(unity_ObjectToWorld, m_RS);
    m_MVP = mul(UNITY_MATRIX_VP, unity_ObjectToWorld);
    
    #endif
}


struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float2 uv0          : TEXCOORD0;
    float2 uv1          : TEXCOORD1;
    float2 uv2          : TEXCOORD2;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float4 positionCS   : SV_POSITION;
    float2 uv           : TEXCOORD0;
#ifdef EDITOR_VISUALIZATION
    float2 VizUV        : TEXCOORD1;
    float4 LightCoord   : TEXCOORD2;
#endif
};

Varyings UniversalVertexMeta(Attributes input)
{
    Varyings output = (Varyings)0;
    // input.positionOS = mul(positionWS, unity_WorldToObject);
    // input.normalOS = mul(unity_WorldToObject, normalWS);
    // output.positionCS = UnityMetaVertexPosition(input.positionOS.xyz, input.uv1, input.uv2);
    output.positionCS = UnityMetaVertexPosition(mul(m_MVP, float4(input.positionOS.xyz, 1)).xyz, input.uv1, input.uv2);
    // output.positionCS = 
    output.uv = TRANSFORM_TEX(input.uv0, _BaseMap);
    
#ifdef EDITOR_VISUALIZATION
    UnityEditorVizData(input.positionOS.xyz, input.uv0, input.uv1, input.uv2, output.VizUV, output.LightCoord);
#endif
    return output;
}

half4 UniversalFragmentMeta(Varyings fragIn, MetaInput metaInput)
{
#ifdef EDITOR_VISUALIZATION
    metaInput.VizUV = fragIn.VizUV;
    metaInput.LightCoord = fragIn.LightCoord;
#endif

    return UnityMetaFragment(metaInput);
}
#endif
