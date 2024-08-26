#ifndef GRASS_SHADER
#define GRASS_SHADER
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Assets/HLSL Scripts/PBR.hlsl"

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
    float3 positionLS: POSITION;
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
float _LightSourceSteps;

float3 _Colour;
float _DiffuseSteps;

float _Smoothness;
float _SpecularSteps;

float _AmbientOcclusion;
float _RimSteps;

// Global Variables
StructuredBuffer<GrassData> _SourcePositionGrass;
float4x4 m_RS;
float3 _RootPosition;
float3 _RootNormal;


// Is called for each instance before vertex stage
void Setup()
{
    #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        GrassData instanceData = _SourcePositionGrass[unity_InstanceID];
        // TODO
        unity_ObjectToWorld._m03_m13_m23_m33 = float4(instanceData.position + instanceData.normal * _Scale / 2 , 1.0);
        unity_ObjectToWorld = mul(unity_ObjectToWorld, m_RS);
        _RootPosition = instanceData.position;
        _RootNormal = instanceData.normal;
    #endif
}

// Usual Lit Shader
// float3 getLight(float3 color, float3 positionWS, float3 normalWS)
// {
//     Light light = GetMainLight();

//     float3 ambient = 0.1;

//     float diff = max(0, dot(light.direction, normalWS));
//     float3 diffuse = diff * 0.8;

//     float3 viewDir = normalize(GetCameraPositionWS() - positionWS);
//     float3 reflectDir = reflect(-light.direction, normalWS);
//     float3 specular = pow(max(dot(viewDir, reflectDir), 0), 32);

//     return color * (ambient + diffuse + specular);
// }

// Vertex And Fragment Stages
VertexOutput Vertex(VertexInput v)
{
    UNITY_SETUP_INSTANCE_ID(v);
    VertexOutput output;
    // TODO: OPTIMIZATION
    output.rootPositionWS = _RootPosition;
    output.rootNormalWS = _RootNormal;

    output.positionWS = mul(UNITY_MATRIX_M, float4(v.positionLS, 1)).xyz;
    output.positionCS = mul(UNITY_MATRIX_VP, float4(output.positionWS, 1));
    output.uv = v.uv;
    return output;
}


float4 Fragment(VertexOutput input) : SV_Target
{
    float3 colour = 1;
    float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - input.rootPositionWS);

    CalculateCustomLighting_float(input.rootPositionWS, input.rootNormalWS, viewDir, 
                                  _Colour, _Smoothness, _AmbientOcclusion, _Metallic,
                                  _DiffuseSteps, _SpecularSteps, _RimSteps, _LightSourceSteps, 
                                  colour);

    float4 output = float4(colour, 1); 

    float3 texSample = _MainTex.Sample(sampler_MainTex, input.uv).rgb;

    // TODO: UPDATE CLIPPING BASED ON TEXTURE ALPHA
    clip(0.05 - texSample.g);
    return output;
}
#endif
