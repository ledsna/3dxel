Shader "Ledsna/Water"
{
    Properties
    {
        _ReflectionStrength("Reflection Strength", Float) = 0
        _Tint("Tint", Color) = (0,0,0)
        _Density("Density", Range(0, 1)) = 0.5

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
            sampler2D _NormalsTexture;
            
            struct Attributes
            {
                float4 positionOS   : POSITION;
            };

            struct Varyings
            {
                float4 positionHCS  : SV_POSITION;
                float3 positionWS   : TEXCOORD0;
            };
            
            Varyings vert(Attributes IN)
            {
                
                Varyings OUT;
                // OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.positionWS = TransformObjectToWorld(IN.positionOS.xyz);
                OUT.positionHCS = TransformWorldToHClip(OUT.positionWS);
                
                return OUT;
            }
            
            half3 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.positionHCS.xy / _ScaledScreenParams.xy;

                real depth = tex2D(_NormalsTexture, uv).a;

                if (depth == 0 || depth == 1) {
                    #if UNITY_REVERSED_Z
                        depth = SampleSceneDepth(uv);
                    #else
                        depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(UV));
                    #endif
                }

                half fogFactor = 0;
                #if !defined(_FOG_FRAGMENT)
                    half fogFactor = ComputeFogFactor(IN.positionHCS.z);
                #endif
                float fogCoord = InitializeInputDataFog(float4(IN.positionWS, 1.0), fogFactor);

                half3 color;
                
                float3 reconstructedPositionWS = ComputeWorldSpacePosition(uv, depth, Inverse(GetWorldToHClipMatrix(GetAlpha(IN.positionWS))));
                float diff = distance(reconstructedPositionWS, IN.positionWS);

                if (distance(_WorldSpaceCameraPos, reconstructedPositionWS) <= distance(_WorldSpaceCameraPos, IN.positionWS))
                    color = SampleSceneColor(uv);
                else
                    color = SampleSceneColor(uv) * exp(_Density * diff * (_Tint - 1));

                // return color;

                return MixFog(color.rgb, fogCoord);
            }
            ENDHLSL
        }
    }
}