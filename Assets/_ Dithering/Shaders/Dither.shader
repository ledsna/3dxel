Shader "Ledsna/Dither"
{
    Properties
    {
        _Colour("Colour", Color) = (1,1,1,1)
        _Density("Density", Float) = 1
    }
    SubShader
    {
        Tags {
            "Queue" = "AlphaTest"
            "RenderType"="Opaque"
        }
        LOD 100
        
        Blend SrcAlpha OneMinusDstColor

        Pass
        {
            HLSLPROGRAM
            #pragma target 2.0
            
            #pragma vertex vert
            #pragma fragment frag

            // #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            // #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"

float3 RGBtoHSV(float3 In)
{
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 P = lerp(float4(In.bg, K.wz), float4(In.gb, K.xy), step(In.b, In.g));
    float4 Q = lerp(float4(P.xyw, In.r), float4(In.r, P.yzx), step(P.x, In.r));
    float D = Q.x - min(Q.w, Q.y);
    float E = 1e-10;
    return float3(abs(Q.z + (Q.w - Q.y)/(6.0 * D + E)), D / (Q.x + E), Q.x);
}

float3 HSVtoRGB(float3 In)
{
    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 P = abs(frac(In.xxx + K.xyz) * 6.0 - K.www);
    return In.z * lerp(K.xxx, saturate(P - K.xxx), In.y);
}

            float _Density;
            float3 _Colour;
            // sampler2D _NormalsTexture;

            float2 ComputeDitherUVs(float3 positionWS)
            {
                if (unity_OrthoParams.w)
                    return TransformWorldToHClipDir(positionWS).xy * 0.5 + 0.5;
                
                float4 hclipPosition = TransformWorldToHClip(positionWS);
                return hclipPosition.xy / hclipPosition.w * 0.5 + 0.5;
            }

            float Dither(float In, float2 ScreenPosition)
            {
                float2 pixelPos = ScreenPosition * float2(641, 361);
                
                uint    x       = (pixelPos.x % 4 + 4) % 4;
                uint    y       = (pixelPos.y % 4 + 4) % 4;
                uint    index     = x * 4 + y;

                float DITHER_THRESHOLDS[16] =
                {
                    1.0 / 17.0,  9.0 / 17.0,  3.0 / 17.0, 11.0 / 17.0,
                    13.0 / 17.0,  5.0 / 17.0, 15.0 / 17.0,  7.0 / 17.0,
                    4.0 / 17.0, 12.0 / 17.0,  2.0 / 17.0, 10.0 / 17.0,
                    16.0 / 17.0,  8.0 / 17.0, 14.0 / 17.0,  6.0 / 17.0
                };
                return In - DITHER_THRESHOLDS[index];
            }

            struct appdata
            {
                float4 positionOS : POSITION;
            };

            struct v2f
            {
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD0;
            };

            v2f vert (appdata input)
            {
                v2f output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                return output;
            }

            half4 frag (v2f input) : SV_Target
            {
                float2 uv = input.positionCS.xy / _ScaledScreenParams.xy;
                float3 colour = SampleSceneColor(uv);

                // float clear_depth = tex2D(_NormalsTexture, uv).z;
                // float depth = SampleSceneDepth(uv);
                // clip(depth < clear_depth ? -1 : 1);
                float2 screenUV = ComputeDitherUVs(input.positionWS);
                float dither = Dither(_Density, screenUV);
                // // clip(dither);
                // if (dither < 0)
                // {
                //     float n1 = Dither(_Density, screenUV + float2(1. / 641, 0));
                //     float n2 = Dither(_Density, screenUV + float2(-1. / 641, 0));
                //     float n3 = Dither(_Density, screenUV + float2(0, 1. / 361));
                //     float n4 = Dither(_Density, screenUV + float2(0, -1. / 361));
                //
                //     if (n1 >= 0 || n2 >= 0 || n3 >= 0 || n4 >= 0)
                //         return half4(0,0,0,1);
                //     clip(-1);
                // }
                clip(dither);
                return 1;
                // return half4(HSVtoRGB(float3(RGBtoHSV(1-colour).x, 1, 1)) + 0.5, 1);
            }
            ENDHLSL
        }
    }
}
