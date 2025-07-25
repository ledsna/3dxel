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
            
            ColorMask R
            
            HLSLPROGRAM
            
            #pragma vertex Vert
            #pragma fragment frag

            // Instead of making shader variant we can just use #define, as shader will not work when lighting disabled
            // #pragma multi_compile_fragment _ _MAIN_LIGHT_SHADOWS
            // https://discussions.unity.com/t/can-i-use-shader_feature-instead-of-multi_compile-on-built-in-unity-keywords/901694/2
            #define _MAIN_LIGHT_SHADOWS
            #pragma shader_feature_local_fragment ITERATIONS_8 ITERATIONS_16 ITERATIONS_32 ITERATIONS_64 ITERATIONS_86 ITERATIONS_128
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/RealtimeLights.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

            float _Scattering;
            float _MaxDistance;
            float _JitterVolumetric;

            #pragma region Define LOOP_COUNT
            
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
            #define LOOP_COUNT 64 // Default value
            #endif

            #pragma endregion 

            #pragma region Help functions
            
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

            #pragma endregion
            
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
                
                float totalDistance = min(distance(rayStart, rayEnd), _MaxDistance);
                float dist = 0.0;
                float rayStep = totalDistance / LOOP_COUNT;

                float3 rayDir = normalize(rayEnd - rayStart) * rayStep;
                float3 rayPos = rayStart;

                // for eliminating badding make different offset using random
                float rayStartOffset = random01(input.texcoord) * rayStep * _JitterVolumetric * 0.01;
                rayPos += normalize(rayDir) * rayStartOffset;
                dist += rayStartOffset;
                
                // Calculating ray pos also in light space for less matrix computations in loop
                float4 rayStartLS = TransformWorldToShadowCoord(rayPos);
                float4 rayEndLS = TransformWorldToShadowCoord(rayPos + rayDir * LOOP_COUNT);
                float distanceInShadowCoords = distance(rayStartLS, rayEndLS);
                float4 rayDirLS = normalize(rayEndLS - rayStartLS) * distanceInShadowCoords / LOOP_COUNT;
                float4 rayPosLS = rayStartLS;

                float accum = 0.0;

                [unroll(LOOP_COUNT)]
                for (int i = 0; i < LOOP_COUNT; i++)
                {
                    float cookie = step(0.9, SampleMainLightCookie(rayPos).r);
                    float shadowTerm = MainLightRealtimeShadow(rayPosLS) * cookie;
                    accum += shadowTerm * dist;
                    rayPos += rayDir;
                    rayPosLS += rayDirLS;
                    dist += rayStep;
                }
                float radiance = accum / (rayStep  * (1 + LOOP_COUNT) * LOOP_COUNT / 2); 
                return radiance;
            }
            ENDHLSL
        }

        Pass
        {
            Name "Compositing"
            
            HLSLPROGRAM
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
            #include "../ShaderLibrary/blending.hlsl"
            
            #pragma vertex Vert
            #pragma fragment frag
            
            float _Intensity;
            float3 _GodRayColor;

            FRAMEBUFFER_INPUT_X_FLOAT(0); // Color Texture
            FRAMEBUFFER_INPUT_X_FLOAT(1); // God Rays Texture

            float4 frag(Varyings input) :SV_Target
            {
                float4 color = LOAD_FRAMEBUFFER_INPUT(0, input.positionCS.xy);
                float godRays = LOAD_FRAMEBUFFER_INPUT(1, input.positionCS.xy).x;


                // L: Мне кажется, модулировать годреи косинусом угла между вектором взора и вектором света 
                //      охуенная идея. Раскомментируй и посмотри сам:
                _Intensity *= (dot(_MainLightPosition.xyz, GetViewForwardDir()) + 1) / 2;

                
                // return godRays * _Intensity;
                // return godRays;
                return AlphaBlending(color, godRays, _Intensity, _GodRayColor);
            }
            ENDHLSL
        }
    }
}