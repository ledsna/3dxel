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
            "RenderType"="Opaque"
            "RenderPipeline"= "UniversalPipline"
        }

        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:setup

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #pragma multi_compile _SHADOWS_SHADOWMASK
            #pragma multi_compile _LIGHTMAP_SHADOW_MIXING
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS

            #pragma vertex Vertex;
            #pragma fragment Fragment;
            
            #include "GrassShader.hlsl"
            
            ENDHLSL
        }
    }
}