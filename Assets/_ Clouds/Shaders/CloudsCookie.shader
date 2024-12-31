Shader "Ledsna/CloudShadows"
{
    Properties
    {
        [HideInInspector]
        _Noise("Clouds Noise", 2D) = "white" {}
        [HideInInspector]
        _Details("Details Noise", 2D) = "white" {}
        [HideInInspector]
        _CookieSteps("Cookie Steps", Float) = -1
        [HideInInspector]
        _NoiseSpeed("Noise speed", Vector) = (0., 0., 0., 0.)
        [HideInInspector]
        _DetailsSpeed("Details speed", Vector) = (0., 0., 0., 0.)
     }

     SubShader
     {
        Blend One Zero

        Pass
        {
            Name "Cookie"

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

            float4 frag(v2f_customrendertexture IN) : SV_Target
            {
                half2 noiseOffset = _Time.yy * _NoiseSpeed / 60;
                half2 detailsOffset = _Time.yy * _DetailsSpeed / 60;

                half CookieSample = tex2D(_Noise, IN.globalTexcoord.xy + noiseOffset).r * 0.5
                                  + tex2D(_Details, IN.globalTexcoord.xy + detailsOffset).r * 0.5;
                half color = smoothstep(0, 1, CookieSample);
                // return 1;
                return color < 0.1 ? 0.15 : color < 0.135 ? 0.35 : color < 0.15 ? 0.45 :  1;
            }
            ENDCG
        }
    }
}
