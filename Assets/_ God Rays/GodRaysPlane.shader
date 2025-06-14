Shader "Ledsna/GodRays"
{
    Properties
    {
        _Color ("Color", Color) = (1, 0.8, 0.7, 1)
        _Brightness ("Brightness", Range(0, 0.5)) = 0.01
        _AlphaFade ("Alpha Fade", Range(0, 1)) = 0.5
    }
    SubShader
    {
        Tags {
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "PreviewType" = "Plane"
        }

        Pass
        {
            Name "GodRayPass"
            Tags { "LightMode" = "UniversalForward" }
            
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off
            
            HLSLPROGRAM

            #include "GodRays.hlsl"
            
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE _MAIN_LIGHT_SHADOWS_SCREEN
            #pragma multi_compile _ _SHADOWS_SOFT
            ENDHLSL
        }
    }
}
