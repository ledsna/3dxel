Shader "Ledsna/BilaterialBlur"
{
    HLSLINCLUDE
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
    #include "Packages/com.unity.render-pipelines.core/Runtime/Utilities/Blit.hlsl"
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
    
    int _GaussSamples; // TODO: Remove that shit. Use Shader Feature with constant values instead
    float _GaussAmount;

    static const float gauss_filter_weights[] = {
        0.14446445, 0.13543542,
        0.11153505, 0.08055309,
        0.05087564, 0.02798160,
        0.01332457, 0.00545096
    };
    #define BLUR_DEPTH_FALLOFF 100.0

    float GetCorrectDepth(float2 uv)
    {
        #if UNITY_REVERSED_Z
        real depth = SampleSceneDepth(uv);
        #else
        // Adjust z to match NDC for OpenGL
        real depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(uv));
        #endif
        return depth;
    }

    // blurAxis must take follow values:
    // (1, 0) — blur by X axis
    // (0, 1) — blur by Y axis
    float BilaterialBlur(float2 texcoord, float2 blurAxis)
    {
        // reference
        // https://valeriomarty.medium.com/raymarched-volumetric-lighting-in-unity-urp-e7bc84d31604
        float accumResult = 0;
        float accumWeights = 0;
        float depthCenter = GetCorrectDepth(texcoord);

        for (int index = -_GaussSamples; index <= _GaussSamples; index++)
        {
            //we offset our uvs by a tiny amount 
            float2 uv = texcoord + index * _GaussAmount / 1000 * blurAxis;
            //sample the color at that location
            float2 kernelSample = SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);
            //depth at the sampled pixel
            float depthKernel = GetCorrectDepth(uv);
            //weight calculation depending on distance and depth difference
            float depthDiff = abs(depthKernel - depthCenter);
            float r2 = depthDiff * BLUR_DEPTH_FALLOFF;
            float g = exp(-r2 * r2);
            float weight = g * 1;// gauss_filter_weights[abs(index)];
            //sum for every iteration of the color and weight of this sample 
            accumResult += weight * kernelSample;
            accumWeights += weight;
        }
        //final color
        return accumResult / accumWeights;
    }
    
    ENDHLSL

    SubShader
    {
        Tags
        {
            "RenderType"="Opaque"
            "DisableBatching"="True"
            "RenderPipeline" = "UniversalPipeline"
        }
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            Name "Bilaterial Blur X"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment fragX

            float fragX(Varyings IN) : SV_Target
            {
                return BilaterialBlur(IN.texcoord, float2(1, 0));
            }
            ENDHLSL
        }

        Pass
        {
            Name "Bilaterial Blur Y"

            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment fragY

            float fragY(Varyings IN) : SV_Target
            {
                return BilaterialBlur(IN.texcoord, float2(0, 1));
            }
            ENDHLSL
        }
    }
}