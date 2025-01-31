// #ifndef GRASS_SHADER
// #define GRASS_SHADER
// #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
//
// // Structs
// // Struct Data From CPU
// struct GrassData
// {
//     float3 position;
//     float3 normal;
//     float2 lightmapUV;
// };
//
// struct VertexInput
// {
//     float3 positionOS: POSITION;
//     float2 uv: TEXCOORD0;
//     UNITY_VERTEX_INPUT_INSTANCE_ID
// };
//
// struct VertexOutput
// {
//     float4 positionCS : SV_POSITION;
//     nointerpolation float3 color : TEXCOORD0;
//     float2 uv : TEXCOORD1;
// };
//
// // Properties
// float _Scale;
// TEXTURE2D(_MainTex);
//
// // Samplers
// SAMPLER(sampler_MainTex);
// float4 _MainTex_ST;
//
// // Root mesh's inherited properties
// float _Metallic;
// float _RadianceSteps;
//
// float3 _Colour;
// float _DiffuseSteps;
//
// float _Smoothness;
// float _SpecularSteps;
//
// float _AmbientOcclusion;
// float _RimSteps;
//
// // Global Variables
// StructuredBuffer<GrassData> _SourcePositionGrass;
// float4x4 m_RS;
// float4x4 m_MVP;
// float3 color = 1;
//
// // Is called for each instance before vertex stage
// void Setup()
// {
//     #if defined(UNITY_PROCEDURAL_INSTANCING_ENABLED)
//         GrassData instanceData = _SourcePositionGrass[unity_InstanceID];
//    
//         unity_ObjectToWorld._m03_m13_m23_m33 = float4(instanceData.position + instanceData.normal * _Scale / 2 , 1.0);
//         unity_ObjectToWorld = mul(unity_ObjectToWorld, m_RS);
//
//         m_MVP = mul(UNITY_MATRIX_VP, unity_ObjectToWorld);
//     
//         float3 viewDir = GetWorldSpaceNormalizeViewDir(instanceData.position);
//     
//     #endif
// }
//
// // Vertex And Fragment Stages
// VertexOutput Vertex(VertexInput v)
// {
//     UNITY_SETUP_INSTANCE_ID(v);
//     VertexOutput output;
//     output.uv = v.uv;
//     output.color = color;
//     output.positionCS = mul(m_MVP, float4(v.positionOS, 1));
//     return output;
// }
//
//
// float4 Fragment(VertexOutput input) : SV_Target
// {
//     float4 output = float4(input.color, 1); 
//
//     float3 texSample = _MainTex.Sample(sampler_MainTex, input.uv).rgb;
//
//     //TODO: UPDATE CLIPPING BASED ON TEXTURE ALPHA
//     clip(0.05 - texSample.r);
//     return output;
// }
//
// float4 FragmentDebugCullMask(VertexOutput input) : SV_Target
// {
//     if(1 - input.color.r < 0.1)
//     {
//         return float4(1,0,0,1);
//     }
//     return Fragment(input);
// }
//
// // --------------------------
// #endif