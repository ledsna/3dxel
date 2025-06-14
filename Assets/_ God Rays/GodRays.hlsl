#ifndef GOD_RAYS_SHADER_INCLUDED
#define GOD_RAYS_SHADER_INCLUDED

#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

float _Brightness;
float _AlphaFade;
half4 _Color;

struct Attributes
{
    float4 positionOS : POSITION;
    float2 uv : TEXCOORD0;
};

struct Varyings
{
    float4 positionHCS : SV_POSITION;
    float4 shadowCoord : TEXCOORD0;
    float2 uv : TEXCOORD1;
};

Varyings vert(Attributes IN)
{
    Varyings OUT;
    float3 positionWS = TransformObjectToWorld(IN.positionOS.xyz);
    OUT.positionHCS = TransformObjectToHClip(IN.positionOS.xyz);
    OUT.shadowCoord = TransformWorldToShadowCoord(positionWS);
    // OUT.uv = TRANSFORM_TEX(IN.uv, _BaseMap);
    OUT.uv = IN.uv;
    return OUT;
}

// The fragment shader definition.            
half4 frag(Varyings IN) : SV_Target
{
    half brightness = SAMPLE_TEXTURE2D_SHADOW(_MainLightShadowmapTexture, sampler_MainLightShadowmapTexture,
                                              IN.shadowCoord).r;
    // if (brightness < 0.9)
    //     return half4(0, 0, 0, 0);
    
    half alpha = brightness * IN.uv.y * _AlphaFade;
    
    return half4(_Color.xyz, alpha);
}

#endif
