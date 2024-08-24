Shader "Custom/GrassShader"
{
    Properties
    {
        _Color("Far color", Color) = (.2, .2, .2, 1)
        _Size("Size", Float) = 0.3
        _MainTex ("Texture", 2D) = "white" {}

        _DiffuseQuantizationSteps("Diffuse Quantization Steps", Float) = 3.
        _MaxQuantizationStepsPerLight("Max Quantization Steps Per Light", Float) = 10.
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite Off

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup
            
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _SHADOWS_SOFT
            
            #pragma vertex Vertex;
            #pragma fragment Fragment;
            
            #include "GrassShader.hlsl"
            
            ENDHLSL
        }
    }
}