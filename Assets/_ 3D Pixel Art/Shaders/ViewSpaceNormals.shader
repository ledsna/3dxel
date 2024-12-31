Shader "Ledsna/ViewSpaceNormals"
{
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "Queue" = "AlphaTest"
        }
        LOD 300

        Pass
        {
            Name "ViewSpaceNormals"
            
            ZWrite On
            ZTest LEqual
            ColorMask RGBA
            
            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            #pragma vertex NormalsVertex
            #pragma fragment NormalsFragment
            
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:Setup
            #pragma instancing_options renderinglayer
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

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
            Texture2D _BaseMap;
            float4 _BaseMap_ST;
            float _Scale;
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
                unity_ObjectToWorld._m03_m13_m23_m33 = float4(instanceData.position + instanceData.normal * _Scale / 2 , 1.0);
                unity_ObjectToWorld = mul(unity_ObjectToWorld, m_RS);
                m_MVP = mul(UNITY_MATRIX_VP, unity_ObjectToWorld);
                
                #endif
            }
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 texcoord     : TEXCOORD0;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 viewNormal : TEXCOORD0;
                float2 uv       : TEXCOORD1;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            Varyings NormalsVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
                // VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);

                // output.positionCS = TransformObjectToHClip(input.positionOS.xyz);

                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                    output.positionCS = TransformWorldToHClip(TransformObjectToWorld(input.positionOS));

                    output.viewNormal = TransformWorldToViewNormal(normalWS, true);
                #else
                    output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                    output.viewNormal = TransformWorldToViewNormal(TransformObjectToWorldNormal(input.normalOS), true);
                #endif
                return output;
            }

            float4 NormalsFragment(Varyings input) : SV_Target
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                    #if defined(_ALPHATEST_ON)
                        half4 clipSample = _ClipTex.Sample(clip_point_clamp_sampler, input.uv);
                        clip(clipSample.a > 0.5 ? 1 : -1);
                        return input.positionCS.z;
                    #endif
                return input.positionCS.z;
                #endif

                return float4(input.viewNormal * 0.5 + 0.5, input.positionCS.z);; // Remapping from [-1,1] to [0,1] for visualization
            }
            
            #endif
            ENDHLSL
        }
    }
}
