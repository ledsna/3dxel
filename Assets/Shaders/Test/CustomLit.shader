Shader "Ledsna/CustomLit"
{
    Properties 
    {
        [Header(Surface options)]
        [MainTexture] _ColorMap("Color", 2D) = "white" {}
        [MainColor] _ColorTint("Tint", Color) = (1, 1, 1, 1)
        _Smoothness("Smoothness", Float) = 0
    }
    
    SubShader
    {
        Tags{"RenderPipeline" = "UniversalPipeline"}
        
        Pass
        {
            Name "ForwardLit"
            Tags {"LightMode" = "UniversalForward"}
            
            HLSLPROGRAM

            #define _SPECULAR_COLOR

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "Assets/Shader Functions/CustomLitForwardPass.hlsl"
            
            ENDHLSL
        }

        Pass
        {
            Name "ShadowCaster"
            Tags {"LightMode" = "ShadowCaster"}
            
            ColorMask 0
            
            HLSLPROGRAM
            #pragma vertex Vertex
            #pragma fragment Fragment

            #include "Assets/Shader Functions/CustomLitShadowCasterPass.hlsl"
            ENDHLSL
        }
        
    }
}