// Adapted from https://valeriomarty.medium.com/raymarched-volumetric-lighting-in-unity-urp-e7bc84d31604
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

            // Instead of making shader variant we can just use #define, as shader will not work when lighting disabled
            // #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS
            // https://discussions.unity.com/t/can-i-use-shader_feature-instead-of-multi_compile-on-built-in-unity-keywords/901694/2
            #define _MAIN_LIGHT_SHADOWS
            #pragma shader_feature_fragment _ ITERATIONS_8 ITERATIONS_16 ITERATIONS_32 ITERATIONS_64 ITERATIONS_86 ITERATIONS_128

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float _Scattering;
            float _MaxDistance;
            float _JitterVolumetric;

            #if defined(ITERATIONS_8)
                #define LOOP_COUNT 8
            #elif defined(ITERATIONS_16)
                #define LOOP_COUNT 16
            #elif defined(ITERATIONS_32)
                #define LOOP_COUNT 32
            #elif defined(ITERATIONS_64)
                #define LOOP_COUNT 64
            #elif defined(ITERATIONS_86)
                #define LOOP_COUNT 86
            #elif defined(ITERATIONS_128)
                #define LOOP_COUNT 128
            #else
            #define LOOP_COUNT 64 // Default
            #endif

            float random01(float2 p)
            {
                return frac(sin(dot(p, float2(41, 289))) * 45758.5453);
            }

            // Mie scaterring approximated with Henyey-Greenstein phase function.
            real ComputeScattering(real lightDotView)
            {
                real result = 1.0 - _Scattering * _Scattering;
                result /= 4.0 * PI * pow(1.0 + _Scattering * _Scattering - (2.0 * _Scattering) * lightDotView, 1.5);
                return result;
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

            float frag(Varyings input) : SV_Target
            {
                float depth = GetCorrectDepth(input.texcoord);
                float3 rayEnd = ComputeWorldSpacePosition(
                    input.texcoord,
                    depth,
                    UNITY_MATRIX_I_VP
                );
                float3 rayStart = ComputeWorldSpacePosition(
                    input.texcoord,
                    UNITY_NEAR_CLIP_VALUE,
                    UNITY_MATRIX_I_VP
                );

                float sampleCount = LOOP_COUNT;
                float3 rayDir = normalize(rayEnd - rayStart);
                float totalDistance = min(distance(rayStart, rayEnd), _MaxDistance);
                float rayStep = totalDistance / sampleCount;
                float3 rayPos = rayStart;

                // for eliminating badding make different offset using random
                float rayStartOffset = random01(input.texcoord) * rayStep * _JitterVolumetric / 100;
                rayPos += rayDir * rayStartOffset;

                float accum = 0.0;

                [unroll(LOOP_COUNT)]
                for (int i = 0; i < sampleCount; i++)
                {
                    float4 lightSpacePos = TransformWorldToShadowCoord(rayPos);
                    float attenuation = MainLightRealtimeShadow(lightSpacePos) * SampleMainLightCookie(rayPos).r;
                    // По идее чем дальше от камеры, тем слабее должен быть эффект
                    // accum += attenuation * (_SampleCount - i) / _SampleCount;
                    // TODO: Not Sure about _MainLightPosition. I need correct direction of light source
                    accum += attenuation; // * ComputeScattering(dot(rayDir, _MainLightPosition.xyz));
                    rayPos += rayDir * rayStep;
                }

                return accum / sampleCount;
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

            float _Intensity;
            float3 _GodRayColor;

            #ifdef FBO_OPTIMIZATION_APPLIED
            FRAMEBUFFER_INPUT_X_FLOAT(0); // Color Texture
            FRAMEBUFFER_INPUT_X_FLOAT(1); // God Rays Texture
            #else
            sampler2D _GodRaysTexture;
            #endif


            float4 frag(Varyings input) :SV_Target
            {
                #ifdef FBO_OPTIMIZATION_APPLIED
                float4 color = LOAD_FRAMEBUFFER_INPUT(0, input.positionCS.xy);
                float godRays = LOAD_FRAMEBUFFER_INPUT(1, input.positionCS.xy).x;
                #else
                float godRays = tex2D(_GodRaysTexture, input.texcoord).x;
                float4 color = FragBlit(input, sampler_LinearClamp);
                #endif

                return SaturateAdditionalBlending(color, godRays, _Intensity, _GodRayColor);
            }
            ENDHLSL
        }
    }
}