Shader "Ledsna/BillboardPositions"
{
    Properties
    {
        _Scale("Scale", Float) = 0.3
        _ClipTex("Clipping Texture", 2D) = "white" {}
        
        _Cutoff("Alpha Cutoff", Range(0.0, 1.0)) = 0.5
        [MainTexture] _BaseMap("Albedo", 2D) = "white" {}

    }
    SubShader
    {
        Tags
        {
            "Queue" = "AlphaTest"
            "PreviewType" = "Plane"
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "UniversalMaterialType" = "Lit"
            "IgnoreProjector" = "True"
            "TerrainCompatible" = "True"
        }
        LOD 300

        Pass
        {
            // Lightmode matches the ShaderPassName set in UniversalRenderPipeline.cs. SRPDefaultUnlit and passes with
            // no LightMode tag are also rendered by Universal Render Pipeline
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            // -------------------------------------
            // Render State Commands

            HLSLPROGRAM
            #pragma target 3.5

            // -------------------------------------
            // Shader Stages
            #pragma vertex LitPassVertex
            #pragma fragment LitPassFragment

            // -------------------------------------
            // Material Keywords
            // #pragma shader_feature_local _NORMALMAP
            // #pragma shader_feature_local _PARALLAXMAP
            // #pragma shader_feature_local _RECEIVE_SHADOWS_OFF
            // #pragma shader_feature_local _ _DETAIL_MULX2 _DETAIL_SCALED
            // #pragma shader_feature_local_fragment _SURFACE_TYPE_TRANSPARENT
            #pragma shader_feature_local_fragment _ALPHATEST_ON
            // #pragma shader_feature_local_fragment _ _ALPHAPREMULTIPLY_ON _ALPHAMODULATE_ON
            #pragma shader_feature_local_fragment _EMISSION
            // #pragma shader_feature_local_fragment _METALLICSPECGLOSSMAP
            // #pragma shader_feature_local_fragment _SMOOTHNESS_TEXTURE_ALBEDO_CHANNEL_A
            // #pragma shader_feature_local_fragment _OCCLUSIONMAP
            // #pragma shader_feature_local_fragment _SPECULARHIGHLIGHTS_OFF
            // #pragma shader_feature_local_fragment _ENVIRONMENTREFLECTIONS_OFF
            #pragma shader_feature_local_fragment _SPECULAR_SETUP

            // -------------------------------------
            // Universal Pipeline keywords
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE// _MAIN_LIGHT_SHADOWS_SCREEN
            // #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS

            // #pragma multi_compile _ EVALUATE_SH_MIXED EVALUATE_SH_VERTEX
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS
            // #pragma multi_compile_fragment _ _REFLECTION_PROBE_BLENDING
            // #pragma multi_compile_fragment _ _REFLECTION_PROBE_BOX_PROJECTION
            #pragma multi_compile_fragment _ _SHADOWS_SOFT// _SHADOWS_SOFT_LOW _SHADOWS_SOFT_MEDIUM _SHADOWS_SOFT_HIGH
            // #pragma multi_compile_fragment _ _SCREEN_SPACE_OCCLUSION
            // #pragma multi_compile_fragment _ _DBUFFER_MRT1 _DBUFFER_MRT2 _DBUFFER_MRT3
            #pragma multi_compile_fragment _ _LIGHT_COOKIES
            // #pragma multi_compile _ _LIGHT_LAYERS
            #pragma multi_compile _ _FORWARD_PLUS
            // #include_with_pragmas "Packages/com.unity.render-pipelines.core/ShaderLibrary/FoveatedRenderingKeywords.hlsl"
            // #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RenderingLayers.hlsl"


            // -------------------------------------
            // Unity defined keywords
            #pragma multi_compile _ LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ SHADOWS_SHADOWMASK
            #pragma multi_compile _ DIRLIGHTMAP_COMBINED
            #pragma multi_compile _ LIGHTMAP_ON
            // #pragma multi_compile _ DYNAMICLIGHTMAP_ON
            // #pragma multi_compile_fragment _ LOD_FADE_CROSSFADE
            #pragma multi_compile_fog
            // #pragma multi_compile_fragment _ DEBUG_DISPLAY
            

            //--------------------------------------
            // GPU Instancing
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:Setup
            // #pragma instancing_options renderinglayer
            #include_with_pragmas "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DOTS.hlsl"

            #include "../ShaderLibrary/LitInput.hlsl"

            // Struct Data From CPU
            struct GrassData
            {
                float3 position;
                float3 normal;
                float2 lightmapUV;
            };

            StructuredBuffer<GrassData> _SourcePositionGrass;
            StructuredBuffer<int> _MapIdToData;

            float _Scale;
            // Inputs
    float4x4 m_RS;

            // Globals
            float3 normalWS; 
            float3 positionWS;
            float2 lightmapUV;

            Texture2D _ClipTex;
            SamplerState clip_point_clamp_sampler;

            // Texture2D unity_ShadowMask;
            SamplerState mask_point_clamp_sampler;

            // Is called for each instance before vertex stage
            void Setup()
            {
                #ifdef UNITY_PROCEDURAL_INSTANCING_ENABLED
                    GrassData instanceData = _SourcePositionGrass[_MapIdToData[unity_InstanceID]];
                    normalWS = instanceData.normal;
                    positionWS = instanceData.position + half3(0, 0.1, 0);;
                    lightmapUV = instanceData.lightmapUV;
                
                    unity_ObjectToWorld._m03_m13_m23_m33 = float4(instanceData.position + instanceData.normal * _Scale / 2 , 1.0);
                    unity_ObjectToWorld = mul(unity_ObjectToWorld, m_RS);
                
                #endif
            }

            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float2 texcoord     : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv                       : TEXCOORD0;
                float4 positionCS               : SV_POSITION;
                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            ///////////////////////////////////////////////////////////////////////////////
            //                  Vertex and Fragment functions                            //
            ///////////////////////////////////////////////////////////////////////////////

            // Used in Standard (Physically Based) shader
            Varyings LitPassVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_TRANSFER_INSTANCE_ID(input, output);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                
                VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
                
                /// СУПЕР ХАРДКОД ПОЖАЛУЙСТА ИЗБАВЬСЯ ОТ ЭТОГО УЖАСА 
                // vertexInput.positionWS = positionWS + half3(0, 0.1, 0);

                output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
                output.positionCS = vertexInput.positionCS;
                return output;
            }

            // Used in Standard (Physically Based) shader
            void LitPassFragment(
                Varyings input, out half4 outColor : SV_Target0
            #ifdef _WRITE_RENDERING_LAYERS
                , out float4 outRenderingLayers : SV_Target1
            #endif
            )
            {
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

                // inputData.positionCS = input.positionHCS;
                // inputData.bakedGI.x *= pow(inputData.bakedGI.x, 2);
                // inputData.bakedGI.y *= pow(inputData.bakedGI.y, 2);
                // inputData.bakedGI.z *= pow(inputData.bakedGI.z, 2);
                // outColor = half4(inputData.bakedGI, 1);
                // return;
                outColor = half4(positionWS, dot(normalWS, GetViewForwardDir()));

                half4 clipSample = _ClipTex.Sample(clip_point_clamp_sampler, input.uv);
                clip(clipSample.a > 0.5 ? 1 : -1);
                // outColor = clipSample.rrrr;

            #ifdef _WRITE_RENDERING_LAYERS
                 uint renderingLayers = GetMeshRenderingLayer();
                outRenderingLayers = float4(EncodeMeshRenderingLayer(renderingLayers), 0, 0, 0);
            #endif
            }

            ENDHLSL
        }
    }

//    Dependency "BaseMapShader" = "Hidden/Universal Render Pipeline/Terrain/Lit (Base Pass)"

    // CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.CustomLitShader"
//    CustomEditor "UnityEditor.Rendering.Universal.ShaderGUI.CustomShaderGUI"
    FallBack "Hidden/Universal Render Pipeline/FallbackError"
}