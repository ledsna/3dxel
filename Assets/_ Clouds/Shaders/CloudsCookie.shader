Shader "CustomRenderTexture/CloudsCookie"
{
    Properties
    {
        _SpeedX("X axis speed", Float) = 0
        _SpeedZ("Z axis speed", Float) = 0
        _Noise("Clouds Noise", 2D) = "white" {}
        _Details("Details Noise", 2D) = "white" {}
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

            float       _SpeedX;
            float       _SpeedZ;
            sampler2D   _Noise;
            float4      _Noise_ST;
            sampler2D   _Details;
            float4      _Details_ST;

            float4 frag(v2f_customrendertexture IN) : SV_Target
            {
                float steps = 3;
                // float2 uv_noise = IN.globalTexcoord.xy * _Noise_ST.xy + _Noise_ST.zw;
                // float2 uv_details = IN.globalTexcoord.xy * _Details_ST.xy + _Details_ST.zw;

                float color = tex2D(_Noise, IN.globalTexcoord.xy + float2(_Time.y / 320, _Time.y / 320)).r * 0.5
                            + tex2D(_Details, IN.globalTexcoord.xy  + float2(_Time.y / 192, _Time.y / 128)).r * 0.5;
                return smoothstep(0, 1, color) < 0.15 ? 0.1 : color < 0.27 ? 0.5 : 1;
            }
            ENDCG
        }
    }
}
