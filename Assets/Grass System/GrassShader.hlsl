#ifndef GRASS_SHADER
#define GRASS_SHADER
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Assets/Shaders/Toon/Quantized PBR.hlsl"

// Structs
// Struct Data From CPU
struct GrassData
{
    float3 position;
    float3 normal;
    float3 color;
};

struct VertexInput
{
    float3 positionOS: POSITION;
    float2 uv: TEXCOORD0;

    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct VertexOutput
{
    float4 positionCS : SV_POSITION;

    float2 uv : TEXCOORD0;
    float3 positionWS : TEXCOORD1;

    float3 rootNormalWS : TEXCOORD2;
    float3 rootPositionWS : TEXCOORD3;

};

// Properties
float _Scale;
TEXTURE2D(_MainTex);

// Samplers
SAMPLER(sampler_MainTex);
float4 _MainTex_ST;

// Root mesh's inherited properties
float _Metallic;
float _RadianceSteps;

float3 _Colour;
float _DiffuseSteps;

float _Smoothness;
float _SpecularSteps;

float _AmbientOcclusion;
float _RimSteps;

// float _DepthOutlineScale = 0;
// float _NormalsOutlineScale = 0;
// float _DepthThreshold = 0;
// float _NormalsThreshold = 0;
// float _HighlightPower = 0;
// float _ShadowPower = 0;

// Global Variables
StructuredBuffer<GrassData> _SourcePositionGrass;
float4x4 m_RS;
float4x4 m_VP;
float3 _RootPosition;
float3 _RootNormal;

// Is called for each instance before vertex stage
void Setup()
{
    #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        GrassData instanceData = _SourcePositionGrass[unity_InstanceID];

        unity_ObjectToWorld._m03_m13_m23_m33 = float4(instanceData.position + instanceData.normal * _Scale / 2 , 1.0);
        unity_ObjectToWorld = mul(unity_ObjectToWorld, m_RS);

        m_VP = mul(UNITY_MATRIX_VP, UNITY_MATRIX_M);

        _RootPosition = instanceData.position;
        _RootNormal = instanceData.normal;
    #endif
}

// Vertex And Fragment Stages
VertexOutput Vertex(VertexInput v)
{
    UNITY_SETUP_INSTANCE_ID(v);
    VertexOutput output;

    output.uv = v.uv;
    
    output.rootPositionWS = _RootPosition;
    output.rootNormalWS = _RootNormal;

    output.positionCS = mul(m_VP, float4(v.positionOS, 1));
    return output;
}


float4 Fragment(VertexOutput input) : SV_Target
{
    float3 colour = 1;
    float3 viewDir = GetWorldSpaceNormalizeViewDir(input.rootPositionWS);
    float2 lightmapUV = float2(0., 0.);

    CalculateCustomLighting_float(input.rootPositionWS, input.rootNormalWS, viewDir, 
                                  _Colour, _Smoothness, _AmbientOcclusion, lightmapUV,
                                  _DiffuseSteps, _SpecularSteps, _RimSteps, _RadianceSteps, 
                                  colour);

    float4 output = float4(colour, 1); 

    float3 texSample = _MainTex.Sample(sampler_MainTex, input.uv).rgb;

    // TODO: UPDATE CLIPPING BASED ON TEXTURE ALPHA
    clip(0.05 - texSample.r);
    return output;
}
#endif
