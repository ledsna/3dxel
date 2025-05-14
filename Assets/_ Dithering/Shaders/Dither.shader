Shader "Ledsna/Dither"
{
    Properties
    {
        _DitherIn("Strength", Float) = 1
    }
    SubShader
    {
        Tags {
            "Queue" = "AlphaTest"
            "RenderType"="Opaque"
        }
        LOD 100

        Pass
        {
            HLSLPROGRAM
            #pragma target 2.0
            
            #pragma vertex vert
            #pragma fragment frag

            // #pragma multi_compile_fog

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            float _DitherIn;

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
                clip(Dither(_DitherIn, ComputeDitherUVs(input.positionWS)));
                return 1;
            }
            ENDHLSL
        }
    }
}
