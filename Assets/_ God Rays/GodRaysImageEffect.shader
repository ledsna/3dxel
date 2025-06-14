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
                float sceneZ = SampleSceneDepth(uv);
                float viewDepth = LinearEyeDepth(sceneZ, _ZBufferParams);

                // This can be simplified, because camers is orthographic
                // reconstruct view-space position
                float4 clip = float4(uv * 2 - 1, -1, 1);
                float4 viewPos = mul(unity_CameraInvProjection, clip);
                viewPos /= viewPos.w;

                // float3 rayDir = normalize(-GetViewForwardDir());
                float3 rayDir = normalize(mul(UNITY_MATRIX_I_V, float4(0, 0, 1, 0)));
                // return float4(rayDir, 1);
                float3 marchPos = viewPos.xyz + _WorldSpaceCameraPos;
                float stepLen = viewDepth / _SampleCount;
                float illumination = 0;

                [loop]
                for (int i = 0; i < _SampleCount; i++)
                {
                    half brightness = SAMPLE_TEXTURE2D_SHADOW(_MainLightShadowmapTexture,
                                  sampler_MainLightShadowmapTexture,
                                  TransformWorldToShadowCoord(marchPos)).r;
                    marchPos += rayDir * stepLen;
                    if (brightness < 0.9)
                        continue;

                    illumination += _Weight;
                }
                return illumination;
            }

            float4 frag(Varyings IN) : SV_Target
            {
                float2 uv = IN.texcoord;

                float illumination = GetIllumination(IN);
                illumination *= _Density;
                illumination = clamp(illumination, 0, 1);
                
                float4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);
                float3 god = illumination * _Exposure;
                return color + float4(god, 0);
            }
            ENDHLSL
        }
    }
}