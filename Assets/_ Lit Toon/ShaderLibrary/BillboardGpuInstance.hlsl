// Struct Data From CPU
struct GrassData
{
    float3 position;
    float3 normal;
    float2 lightmapUV;
};

StructuredBuffer<GrassData> _SourcePositionGrass;

float _Scale;
// Inputs
float4x4 m_RS;
// Globals
float3 normalWS;
float3 positionWS;
float2 lightmapUV;

// Is called for each instance before vertex stage
void Setup()
{
    #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
    InitIndirectDrawArgs(0);
    uint instanceID = GetIndirectInstanceID_Base(unity_InstanceID);
    GrassData instanceData = _SourcePositionGrass[instanceID];
        
    normalWS = instanceData.normal;
    positionWS = instanceData.position;
    lightmapUV = instanceData.lightmapUV;
    
    unity_ObjectToWorld._m03_m13_m23_m33 = float4(positionWS + instanceData.normal * _Scale / 2 , 1.0);
    unity_ObjectToWorld = mul(unity_ObjectToWorld, m_RS);
    #endif
}