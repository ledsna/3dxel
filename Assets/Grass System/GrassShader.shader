Shader "Custom/GrassShader"
{
    Properties
    {
        _Scale("Scale", Float) = 0.3
        _MainTex ("Texture", 2D) = "white" {}
        [Toggle] _DEBUG_CULL_MASK ("Debug Cull Mask", Float) = 0
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
            "PreviewType" = "Plane"
        }

        Pass
        {
            Name "Universal Forward"
            Tags
            {
                "LightMode" = "UniversalForward"
            }

            Blend SrcAlpha OneMinusSrcAlpha
            // ZWrite On

            HLSLPROGRAM
            #pragma prefer_hlslcc gles
            #pragma exclude_renderers d3d11_9x
            #pragma multi_compile_instancing
            #pragma instancing_options procedural:Setup

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            // #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile_ _SHADOWS_SOFT

            // Mods
            #pragma shader_feature _DEBUG_CULL_MASK_ON
            
            #pragma vertex Vertex;
            #pragma fragment Frag;
            
            #include "GrassShader.hlsl"


            float4 Frag(VertexOutput input) : SV_Target
            {
                #if _DEBUG_CULL_MASK_ON
                {
                    return FragmentDebugCullMask(input);
                }
                #else
                {
                    return Fragment(input);
                }
                #endif
            }
            
            ENDHLSL
        }
    }
}