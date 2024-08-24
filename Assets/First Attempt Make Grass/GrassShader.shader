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

            #pragma vertex Vertex;
            #pragma fragment Fragment;
            
            #include "GrassShader.hlsl"
            
            ENDHLSL
        }
    }
}