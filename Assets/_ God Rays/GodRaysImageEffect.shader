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
                float depth = SampleSceneDepth(uv);
                float viewDepth = LinearEyeDepth(depth, _ZBufferParams);

                float3 fragmentWorldPos = ComputeWorldSpacePosition(
                    uv, 
                    depth, 
                    unity_MatrixInvVP
                );

                // float3 rayDir = normalize(-GetViewForwardDir());
                float3 rayDir = normalize(mul(UNITY_MATRIX_I_V, float4(0, 0, 1, 0)));
                // return float4(rayDir, 1);
                float3 marchPos = fragmentWorldPos;
                float stepLen = viewDepth / _SampleCount;
                float illumination = 0;
                float currentWeight = _Weight;
                float3 currentPos = fragmentWorldPos;

                [loop]
                for (int i = 0; i < _SampleCount; i++)
                {
                    half brightness = SAMPLE_TEXTURE2D_SHADOW(_MainLightShadowmapTexture,
                                  sampler_MainLightShadowmapTexture,
                                  TransformWorldToShadowCoord(marchPos)).r;
                    marchPos += rayDir * stepLen;
                    // Accumulate only lit samples
                    illumination += brightness * currentWeight;
                    
                    // Apply exponential decay
                    currentWeight *= _Decay;
                }
                return illumination * _Density * _Exposure;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.texcoord;

                float illumination = GetIllumination(IN);
                // illumination = clamp(illumination, 0, 1);
                
                float4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);
                return color + float4(illumination.xxx, 0);
            }
            ENDHLSL
        }
    }
}