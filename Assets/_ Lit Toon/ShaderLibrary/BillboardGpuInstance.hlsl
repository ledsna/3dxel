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
float4x4 m_MVP;
float4x4 m_WtO;
float3 normalWS; 
float3 positionWS;
float2 lightmapUV;

// Is called for each instance before vertex stage
void Setup()
{
    #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
    GrassData instanceData = _SourcePositionGrass[unity_InstanceID];
    normalWS = instanceData.normal;
    positionWS = instanceData.position;
    // positionWS.y += unity_InstanceID*0.01;
    lightmapUV = instanceData.lightmapUV;

    unity_ObjectToWorld._m03_m13_m23_m33 = float4(instanceData.position + instanceData.normal * _Scale / 2 , 1.0);

    unity_ObjectToWorld = mul(unity_ObjectToWorld, m_RS);
    m_WtO = unity_WorldToObject;
    m_MVP = mul(UNITY_MATRIX_VP, unity_ObjectToWorld);
    
    #endif
}
