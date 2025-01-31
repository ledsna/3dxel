#ifndef UNIVERSAL_DEPTH_ONLY_PASS_INCLUDED
#define UNIVERSAL_DEPTH_ONLY_PASS_INCLUDED

// Although Rider displays as unused, it is needed for GPU Instance
#include "BillboardGpuInstance.hlsl"

Texture2D _ClipTex;
SamplerState clip_point_clamp_sampler;


#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#if defined(LOD_FADE_CROSSFADE)
    #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

struct Attributes
{
    float4 position     : POSITION;
    float2 texcoord     : TEXCOORD0;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    #if defined(_ALPHATEST_ON)
        float2 uv       : TEXCOORD0;
    #endif
    float4 positionCS   : SV_POSITION;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

Varyings DepthOnlyVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    #if defined(_ALPHATEST_ON)
        output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);
    #endif

    // half3 flatviewdir = GetViewForwardDir();
    // flatviewdir.y = 0;
    // half3 wspos = mul(unity_ObjectToWorld, input.position.xyz);
    // wspos += normalize(flatviewdir) * wspos.y / tan(90);
    // wspos.y = 0;
    // half3 pos = wspos + positionWS + half3(0., 0.1, 0.);
    // output.positionCS = TransformWorldToHClip(positionWS + half3(0., 0.1, 0.) + mul(unity_ObjectToWorld, input.position.xyz));

    // output.positionCS = mul(m_MVP, input.position.xyz);
    output.positionCS = TransformObjectToHClip(input.position.xyz);
    
    // output.positionCS = TransformObjectToHClip(input.position.xyz) + TransformWorldToHClip(half3(0., 0.1, 0.));
    return output;
}

half DepthOnlyFragment(Varyings input) : SV_TARGET
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

    #if defined(_ALPHATEST_ON)
        Alpha(SampleAlbedoAlpha(input.uv, TEXTURE2D_ARGS(_BaseMap, sampler_BaseMap)).a, _BaseColor, _Cutoff);
    // без альфатеста работать не будет ЛОЛ 
        half4 clipSample = _ClipTex.Sample(clip_point_clamp_sampler, input.uv);
        clip(clipSample.a > 0.5 ? 1 : -1);
    #endif
    

    #if defined(LOD_FADE_CROSSFADE)
        LODFadeCrossFade(input.positionCS);
    #endif
    
    return input.positionCS.z;
    // smoothstep(0, 1, TransformObjectToHClip(mul(WtO, half4(positionWS.xyz, 1.0)).xyz).z);
}
#endif
