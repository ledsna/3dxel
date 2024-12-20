Shader "Hidden/ViewSpaceNormals"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300

        Pass
        {
            Name "ViewSpaceNormals"
            
            ZWrite On
            ColorMask RGBA
            
            HLSLPROGRAM

            #include "UnityCG.cginc"
            #pragma vertex NormalsVertex
            #pragma fragment NormalsFragment
            
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:Setup
            #pragma instancing_options renderinglayer

            #ifndef VIEWSPACE_NORMALS_PASS_INCLUDED
            #define VIEWSPACE_NORMALS_PASS_INCLUDED

            // Struct Data From CPU
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

            Texture2D _ClipTex;
            SamplerState clip_point_clamp_sampler;

            void Setup()
            {
                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                GrassData instanceData = _SourcePositionGrass[_MapIdToData[unity_InstanceID]];
                normalWS = instanceData.normal;
                positionWS = instanceData.position;
                m_MVP = mul(UNITY_MATRIX_VP, unity_ObjectToWorld);
                
                #endif
            }
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 viewNormal : TEXCOORD0; // Changed to viewNormal
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings NormalsVertex(Attributes input)
            {
                Varyings output = (Varyings)0;

                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                    input.positionOS = mul(unity_WorldToObject, positionWS);
                    input.normalOS = mul(unity_WorldToObject, normalWS);
                #endif
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                // VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

                output.positionCS = UnityObjectToClipPos(input.positionOS);

                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                    float3 worldNormal = normalWS;
                    output.viewNormal = mul(m_MVP, normalWS);
                #else
                    float3 worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, input.normalOS)); // Normal in world space
                    output.viewNormal = mul((float3x3)UNITY_MATRIX_V, worldNormal); // Transform normal to view space
                #endif
                return output;
            }

            float3 NormalsFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                    return 1;
                #endif
                return input.viewNormal * 0.5 + 0.5; // Remapping from [-1,1] to [0,1] for visualization
            }
            
            #endif
            ENDHLSL
        }
    }
}
