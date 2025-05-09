Shader "Ledsna/Water"
{
    Properties
    {
        _ReflectionStrength("Reflection Strength", Float) = 0
        _Tint("Tint", Color) = (0,0,0)
        _Density("Density", Range(0, 1)) = 0.5
        _FoamThreshold("Max Foam Depth (Units)", Range(0, 0.1)) = 0.03
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "RenderPipeline" = "UniversalPipeline" "Queue" = "Transparent"}
//        ZWrite Off
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            float _ReflectionStrength;
            float3 _Tint;
            float _Density;
            float _FoamThreshold;
            sampler2D _NormalsTexture;
            
            struct Attributes
            {
                float4 positionOS   : POSITION;
                float3 normalOS     : NORMAL;
                float4 tangentOS    : TANGENT;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
                float3 normalWS     : TEXCOORD1;
                float3 normalOS     : TEXCOORD2;
                float3 normalTS     : TEXCOORD3;
            };
            
            Varyings vert(Attributes IN)
            {
                
                Varyings OUT;
                // OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.normalWS = TransformObjectToWorldNormal(IN.positionOS);
                OUT.normalOS = IN.normalOS;
                OUT.positionHCS = TransformWorldToHClip(OUT.positionWS);

                float3 worldTangent = TransformObjectToWorldDir(IN.tangentOS.xyz);
                float3 worldBitangent = cross(OUT.normalWS, worldTangent) * IN.tangentOS.w;

                float3x3 TBN = float3x3(worldTangent, worldBitangent, OUT.normalWS);
                OUT.normalTS = mul(TBN, OUT.normalWS);
                
                return OUT;
            }

            float ComputeFogCoord(float HCSposZ, float3 posWS)
            {
                half fogFactor = 0;
                #if !defined(_FOG_FRAGMENT)
                    fogFactor = ComputeFogFactor(HCSposZ);
                #endif
                float fogCoord = InitializeInputDataFog(float4(posWS, 1.0), fogFactor);
                return fogCoord;
            }

            float3 ComputeAbsorbedLight(float3 initial_colour, float density, float depth, float3 absorbed_colour)
            {
                float3 final_colour = initial_colour * exp(density * depth * absorbed_colour);
                return final_colour;
            }

            float3 ComputeFoamColour(float3 foam_tint, float depth, float threshold)
            {
                float3 foam_colour = foam_tint * step(depth, threshold);
                return foam_colour;
            }

            float3 GetSceneColour(float2 uv, float3 normalTS)
            {
                float2 uv_offset = normalTS.xy;
                uv_offset /= unity_OrthoParams.xy * 2;
                float3 scene_colour = SampleSceneColor(uv + uv_offset);
                return scene_colour;
            }
            
            half3 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.positionHCS.xy / _ScaledScreenParams.xy;

                half3 color;
                float3 tint, foam;

                // Depth without billboarded grass blades and shit:
                real clear_depth = tex2D(_NormalsTexture, uv).a;
                
                #if UNITY_REVERSED_Z
                    real depth = SampleSceneDepth(uv);
                #else
                    real depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(UV));
                #endif

                if (clear_depth == 0 || clear_depth == 1) {
                    clear_depth = depth;
                }

                float3 posWSFromDepth = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
                float3 posWSFromClearDepth = ComputeWorldSpacePosition(uv, clear_depth, UNITY_MATRIX_I_VP);

                float depth_dist = distance(posWSFromClearDepth, IN.positionWS);

                float vertical_depth = (IN.positionWS - posWSFromDepth).y;
                
                // if (distance(_WorldSpaceCameraPos, posWSFromDepthTex) <= distance(_WorldSpaceCameraPos, IN.positionWS))
                //     tint = SampleSceneColor(uv);
                // else
                    tint = ComputeAbsorbedLight(GetSceneColour(uv, IN.normalTS), _Density, depth_dist, _Tint - 1);

                foam = ComputeFoamColour(float3(1,1,1), vertical_depth, _FoamThreshold);
                
                color = tint + foam;
                return MixFog(color.rgb, ComputeFogCoord(IN.positionHCS.z, IN.positionWS));
            }
            ENDHLSL
        } 
    }
}