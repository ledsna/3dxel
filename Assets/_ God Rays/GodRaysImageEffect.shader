Shader "Ledsna/GodRaysImageEffect"
{
//    Properties
//    {
//        _MainTex ("Source", 2D) = "white" {}
//        _CameraDepthTexture ("Depth", 2D) = "white" {}
//        _SampleCount ("Samples", Int) = 64
//        _Density ("Density", Float) = 0.8
//        _Weight ("Weight", Float) = 0.5
//        _Decay ("Decay", Float) = 1.0
//        _Exposure ("Exposure", Float) = 1.0
//    }
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"

            int _SampleCount;
            float _Density, _Weight, _Decay, _Exposure;
            float3 _LightDir; // view-space light dir

            float LinearEyeDepth(float d)
            {
                #if UNITY_REVERSED_Z
                    return _ProjectionParams.x / (d - _ProjectionParams.y);
                #else
                    return _ProjectionParams.x / ( _ProjectionParams.y - d );
                #endif
            }

            float4 frag(Varyings  IN) : SV_Target
            {
                // read depth
                float2 uv = IN.texcoord;

                
                float sceneZ = 1; // SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, uv);


                float viewDepth = LinearEyeDepth(sceneZ);
                
                // reconstruct view-space position
                float4 clip = float4(uv * 2 - 1, sceneZ, 1);
                float4 viewPos4 = mul(unity_CameraInvProjection, clip);
                viewPos4.xyz /= viewPos4.w;
                float3 viewPos = viewPos4.xyz;

                // Ray march towards light
                float3 rayDir = normalize(_LightDir) * -1; 
                float3 marchPos = viewPos;
                float stepLen = viewDepth / _SampleCount;
                float illumination = 0;

                for (int i = 0; i < _SampleCount; i++)
                {
                    marchPos += rayDir * stepLen;
                    // project back to screen
                    float4 proj = mul(unity_CameraProjection, float4(marchPos,1));
                    proj.xy /= proj.w; proj.xy = proj.xy * 0.5 + 0.5;
                    // sample depth map to see if occluded

                    
                    float d = 1;//SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, proj.xy);


                    float z = LinearEyeDepth(d);
                    if (marchPos.z > z) 
                        break;
                    // accumulate
                    illumination += _Weight * exp(-_Decay * (float)i) ;
                }

                illumination *= _Density;
                illumination = clamp(illumination, 0, 1);

                // final composite
                float4 color = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);
                float3 god = illumination * _Exposure * _LightDir;
                return color + float4(god, 0);
            }
            
            ENDHLSL
        }
    }
}