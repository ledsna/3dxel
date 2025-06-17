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
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _DRAW_GOD_RAYS
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            #include "blending.hlsl"

            int _SampleCount;
            float _A, _B, _C, _D;
            float3 _GodRayColor;

            float GetCorrectDepth(float2 uv)
            {
                #if UNITY_REVERSED_Z
                real depth = SampleSceneDepth(uv);
                #else
                    // Adjust z to match NDC for OpenGL
                    real depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(uv));
                #endif
                return depth;
            }

            float ConvertToRange(float val, float a, float b, float c = 0, float d = 1)
            {
                // linear map value from range [c, d] to [a, b]
                return (val - c) / (d - c) * (b - a) + a;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float depth = GetCorrectDepth(IN.texcoord);
                const float3 rayEnd = ComputeWorldSpacePosition(
                    IN.texcoord,
                    depth,
                    UNITY_MATRIX_I_VP
                );
                const float3 rayStart = ComputeWorldSpacePosition(
                    IN.texcoord,
                    UNITY_NEAR_CLIP_VALUE,
                    UNITY_MATRIX_I_VP
                );

                const float3 rayDir = normalize(rayEnd - rayStart);
                const float totalDistance = distance(rayStart, rayEnd);
                const float rayStep = totalDistance / _SampleCount;
                // offset on start
                float3 rayPos = rayStart;
                float accum = 0.0;

                for (int i = 0; i < _SampleCount; i++)
                {
                    float4 lightSpacePos = TransformWorldToShadowCoord(rayPos);
                    float attenuation = MainLightRealtimeShadow(lightSpacePos) * SampleMainLightCookie(rayPos).r;
                    // По идее чем дальше от камеры, тем слабее должен быть эффект
                    // accum += attenuation * (_SampleCount - i) / _SampleCount;
                    accum += attenuation;
                    rayPos += rayDir * rayStep;
                }

                accum /= _SampleCount;
                float godRays = accum * _A;
                godRays = pow(godRays, _C) * _B;
                #ifdef _DRAW_GOD_RAYS
                    return godRays;
                #else
                float4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, IN.texcoord);
                return SoftBlending(color, godRays, _GodRayColor);
                #endif
            }
            ENDHLSL
        }
    }
}