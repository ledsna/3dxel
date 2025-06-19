Shader "Ledsna/GodRays"
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
            Name "God Rays"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment frag
            #pragma target 4.0
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _DRAW_GOD_RAYS // Better to use shader_feature instead
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            int _SampleCount; // TODO: Remove that shit. Use Shader Feature with constant values instead
            float _A, _B, _C, _D;
            float _MaxDistance;
            float _JitterVolumetric;

            real random(real2 p)
            {
                return frac(sin(dot(p, real2(41, 289))) * 45758.5453) - 0.5;
            }

            real random01(real2 p)
            {
                return frac(sin(dot(p, real2(41, 289))) * 45758.5453);
            }

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

            float frag(Varyings IN) : SV_Target
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
                const float totalDistance = min(distance(rayStart, rayEnd), _MaxDistance);
                const float rayStep = totalDistance / _SampleCount;
                float3 rayPos = rayStart;

                // for eliminating badding make different offset using random
                float rayStartOffset = random01(IN.texcoord) * rayStep * _JitterVolumetric / 100;
                rayPos += rayDir * rayStartOffset;

                float accum = 0.0;

                // [unrool]
                [loop]
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
                return accum * _A;
            }
            ENDHLSL
        }

        Pass
        {
            Name "Compositing"

            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #pragma vertex Vert
            #pragma fragment frag
            #pragma shader_feature FBO_OPTIMIZATION_APPLIED
            #include "blending.hlsl"

            
            #ifdef FBO_OPTIMIZATION_APPLIED
            FRAMEBUFFER_INPUT_X_FLOAT(0); // Color Texture
            FRAMEBUFFER_INPUT_X_FLOAT(1); // God Rays Texture
            #else
            sampler2D _GodRaysTexture;
            #endif
            
            
            float3 _GodRayColor;

            float4 frag(Varyings IN) :SV_Target
            {
                #ifdef FBO_OPTIMIZATION_APPLIED
                    float4 color = LOAD_FRAMEBUFFER_INPUT(0, IN.positionCS.xy);
                    float godRays = LOAD_FRAMEBUFFER_INPUT(1, IN.positionCS.xy);
                #else
                    float godRays = tex2D(_GodRaysTexture, IN.texcoord).x;
                    float4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, IN.texcoord);
                #endif
            
                
                return SoftBlending(color, godRays, _GodRayColor);
            }
            ENDHLSL
        }
    }
}