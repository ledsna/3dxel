Shader "Ledsna/GodRaysImageEffect"
{
    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "DisableBatching"="True"
            "RenderPipeline" = "UniversalPipeline"
        }
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "GodRaysPass"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            #pragma target 4.0
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"


            int _SampleCount;
            float _Density, _Weight, _Decay, _Exposure;

            float GetIllumination(Varyings IN)
            {
                float2 uv = IN.texcoord;
                #if UNITY_REVERSED_Z
                    real depth = SampleSceneDepth(uv);
                #else
                    // Adjust z to match NDC for OpenGL
                    real depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(UV));
                #endif
                
                float viewDepth = LinearEyeDepth(depth, _ZBufferParams);

                float3 collisionWorldPos = ComputeWorldSpacePosition(
                    uv, 
                    depth, 
                    UNITY_MATRIX_I_VP
                );

                float3 rayDir = normalize(collisionWorldPos - _WorldSpaceCameraPos);
                // float3 rayDir = normalize(mul(UNITY_MATRIX_I_V, float4(0, 0, 1, 0)));
                // return float4(rayDir, 1);
                float3 marchPos = _WorldSpaceCameraPos;
                float stepLen = _Decay;
                float illumination = 0;
                
                [loop]
                for (int i = 0; i < _SampleCount; i++)
                {
                    if (i * stepLen > viewDepth)
                        break;
                    half brightness = GetMainLightShadowFade(marchPos);
                    
                    marchPos += rayDir * stepLen;
                    illumination += brightness * _Weight; // * _Weight * ((float)i / _SampleCount);
                }
                return illumination / _SampleCount;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float2 UV = IN.positionCS.xy / _ScaledScreenParams.xy;
                
                #if UNITY_REVERSED_Z
                    real depth = SampleSceneDepth(UV);
                #else
                    // Adjust z to match NDC for OpenGL
                    real depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(UV));
                #endif

                #if UNITY_REVERSED_Z
                    if(depth < 0.0001)
                        return half4(0,0,0,1);
                #else
                    if(depth > 0.9999)
                        return half4(0,0,0,1);
                #endif
                
                float3 worldPos = ComputeWorldSpacePosition(UV, depth, UNITY_MATRIX_I_VP);

                uint scale = 1;
                uint3 worldIntPos = uint3(abs(worldPos.xyz * scale));
                bool white = (worldIntPos.x & 1) ^ (worldIntPos.y & 1) ^ (worldIntPos.z & 1);
                half4 color = white ? half4(1,1,1,1) : half4(0,0,0,1);
                return color;
                
                // // return float4(1 - GetMainLightShadowFade(fragmentWorldPos).xxx, 1);
                // float illumination = GetIllumination(IN);
                // return float4(illumination.xxx, 1);
                // // illumination = clamp(illumination, 0, 1);
                //
                // float4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);
                // return color + float4(illumination.xxx, 0);
            }
            ENDHLSL
        }
    }
}