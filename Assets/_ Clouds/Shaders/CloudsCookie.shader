Shader "CustomRenderTexture/CloudsCookie"
{
    Properties
    {
//        [HideInInspector]
//        _Noise("Clouds Noise", 2D) = "white" {}
//        [HideInInspector]
//        _Details("Details Noise", 2D) = "white" {}
//        [HideInInspector]
//        _CookieSteps("Cookie Steps", Float) = -1
//        [HideInInspector]
//        _NoiseSpeed("Noise speed", Float) = 0
//        [HideInInspector]
//        _DetailsSpeed("Details speed", Float) = 0
     }

     SubShader
     {
        Blend One Zero

        Pass
        {
            Name "CloudsCookie"

            CGPROGRAM
            #include "UnityCustomRenderTexture.cginc"
            #pragma vertex CustomRenderTextureVertexShader
            #pragma fragment frag
            #pragma target 3.0

            sampler2D   _Noise;
            sampler2D   _Details;

            half       _CookieSteps;

            half2       _NoiseSpeed;
            half2       _DetailsSpeed;
            
            #ifndef QUANTIZE_INCLUDED
            #define QUANTIZE_INCLUDED
            half Quantize(half steps, half shade)
            {
                if (steps == -1) return shade;
                if (steps == 0) return 0;
                if (steps == 1) return 1;

                return floor(shade * (steps - 1) + 0.5) / (steps - 1);
            }
            #endif

            float4 frag(v2f_customrendertexture IN) : SV_Target
            {
                half2 noiseOffset = _Time.yy * _NoiseSpeed / 60;
                half2 detailsOffset = _Time.yy * _DetailsSpeed / 60;

                half CookieSample = tex2D(_Noise, IN.globalTexcoord.xy + noiseOffset).r * 0.5
                                  + tex2D(_Details, IN.globalTexcoord.xy + detailsOffset).r * 0.5;
                half color = smoothstep(0, 1, CookieSample);
                return color < 0.1 ? 0.1 : color < 0.175 ? 0.2 : color < 0.18 ? 0.35 : 1;
            }
            ENDCG
        }
    }
}
