#ifndef GRASS_SHADER
#define GRASS_SHADER
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
#include "Assets/HLSL Scripts/PBR.hlsl"

// Structs
// -------
// Struct Data From CPU
struct GrassData
{
    float3 position;
    float3 normal;
    float3 color;
    float2 lightmapUV;
};

struct VertexOutput
{
    float4 positionCS : SV_POSITION;
    float3 planePositionWS : TEXCOORD4;
    float2 uv : TEXCOORD0;
    float3 normalWS : TEXCOORD1;
    float3 positionWS : TEXCOORD2;
    float2 lightmapUV : TEXCOORD3;  // New field for lightmap UVs
};

struct VertexInput
{
    float3 positionLS: POSITION;
    float2 uv: TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};
// -------

// Properties
// ----------
float4 _Color;
float _Size;
TEXTURE2D(_MainTex);
SAMPLER(sampler_MainTex);
float4 _MainTex_ST;
float _DiffuseQuantizationSteps;
float _MaxQuantizationStepsPerLight;
// ----------

// Global Variables
// ----------------
StructuredBuffer<GrassData> _SourcePositionGrass;
float4x4 m_RS;
float3 normal;
float2 lightmapUV;
float3 planePositionWS;
// ----------------


// This Function call for each instance before vertex stage
void setup()
{
    #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
        GrassData instanceData = _SourcePositionGrass[unity_InstanceID];
        // TODO
        unity_ObjectToWorld._m03_m13_m23_m33 = float4(instanceData.position + instanceData.normal * _Size/2 , 1.0);
        unity_ObjectToWorld = mul(unity_ObjectToWorld, m_RS);
        planePositionWS = instanceData.position;
        normal = instanceData.normal;
        lightmapUV = instanceData.lightmapUV;
    #endif
}

// Usual Lit Shader
float3 getLight(float3 color, float3 positionWS, float3 normalWS)
{
    Light light = GetMainLight();

    float3 ambient = 0.1;

    float diff = max(0, dot(light.direction, normalWS));
    float3 diffuse = diff * 0.8;

    float3 viewDir = normalize(GetCameraPositionWS() - positionWS);
    float3 reflectDir = reflect(-light.direction, normalWS);
    float3 specular = pow(max(dot(viewDir, reflectDir), 0), 32);

    return color * (ambient + diffuse + specular);
}

// Vertex And Fragment Stages
// --------------------------
VertexOutput Vertex(VertexInput v)
{
    UNITY_SETUP_INSTANCE_ID(v);
    VertexOutput output;
    // TODO: OPTIMIZATION
    output.planePositionWS = planePositionWS;
    output.positionWS = mul(UNITY_MATRIX_M, float4(v.positionLS, 1)).xyz;
    output.positionCS = mul(UNITY_MATRIX_VP, float4(output.positionWS, 1));
    output.normalWS = normal;
    output.uv = v.uv;
    output.lightmapUV = lightmapUV;
    return output;
}


float4 Fragment(VertexOutput input) : SV_Target
{
    float3 colour = 1;
    float3 viewDir = normalize(_WorldSpaceCameraPos.xyz - input.planePositionWS);
    
    float smoothness = 0;
    float rimsteps = 0;
    float specsteps = 0;
    float ao = 0;
    
    CalculateCustomLighting_float(input.planePositionWS, input.normalWS, viewDir, _Color.rgb, 
    smoothness, ao, input.lightmapUV, _DiffuseQuantizationSteps, specsteps, rimsteps, _MaxQuantizationStepsPerLight, colour);

    return float4(colour, 1);
}
// --------------------------
#endif
