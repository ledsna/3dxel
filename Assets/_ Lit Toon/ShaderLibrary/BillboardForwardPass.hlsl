#ifndef GRASS_SHADER_INCLUDED
#define GRASS_SHADER_INCLUDED

#include "BillboardGpuInstance.hlsl"

Texture2D _CloudsCookie;
SamplerState sampler_CloudsCookie;
half _XOffsetScale;
half _ZOffsetScale;

float3 RGBtoHSV(float3 In)
{
    float4 K = float4(0.0, -1.0 / 3.0, 2.0 / 3.0, -1.0);
    float4 P = lerp(float4(In.bg, K.wz), float4(In.gb, K.xy), step(In.b, In.g));
    float4 Q = lerp(float4(P.xyw, In.r), float4(In.r, P.yzx), step(P.x, In.r));
    float D = Q.x - min(Q.w, Q.y);
    float  E = 1e-10;
    return float3(abs(Q.z + (Q.w - Q.y)/(6.0 * D + E)), D / (Q.x + E), Q.x);
}

float3 HSVtoRGB(float3 In)
{
    float4 K = float4(1.0, 2.0 / 3.0, 1.0 / 3.0, 3.0);
    float3 P = abs(frac(In.xxx + K.xyz) * 6.0 - K.www);
    return In.z * lerp(K.xxx, saturate(P - K.xxx), In.y);
}

Texture2D _ClipTex;
SamplerState clip_point_clamp_sampler;

// Texture2D unity_ShadowMask;
SamplerState mask_point_clamp_sampler;

#ifndef UNIVERSAL_FORWARD_LIT_PASS_INCLUDED
#define UNIVERSAL_FORWARD_LIT_PASS_INCLUDED

#include "../ShaderLibrary/Lighting.hlsl"
// #include "Assets/Shaders/Outlines/Outlines.hlsl"
#if defined(LOD_FADE_CROSSFADE)
#include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/LODCrossFade.hlsl"
#endif

// GLES2 has limited amount of interpolators
#if defined(_PARALLAXMAP) && !defined(SHADER_API_GLES)
#define REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR
#endif

#if (defined(_NORMALMAP) || (defined(_PARALLAXMAP) && !defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR))) || defined(_DETAIL)
#define REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR
#endif

struct Attributes
{
    float4 positionOS   : POSITION;
    float3 normalOS     : NORMAL;
    float4 tangentOS    : TANGENT;
    float2 texcoord     : TEXCOORD0;
    float2 staticLightmapUV   : TEXCOORD1;
    float2 dynamicLightmapUV  : TEXCOORD2;
    UNITY_VERTEX_INPUT_INSTANCE_ID
};

struct Varyings
{
    float2 uv                       : TEXCOORD0;

    #if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
    float3 positionWS               : TEXCOORD1;
    #endif

    float3 normalWS                 : TEXCOORD2;
    #if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
    half4 tangentWS                : TEXCOORD3;    // xyz: tangent, w: sign
    #endif

    #ifdef _ADDITIONAL_LIGHTS_VERTEX
    half4 fogFactorAndVertexLight   : TEXCOORD5; // x: fogFactor, yzw: vertex light
    #else
    half  fogFactor                 : TEXCOORD5;
    #endif

    #if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    float4 shadowCoord              : TEXCOORD6;
    #endif

    #if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    half3 viewDirTS                : TEXCOORD7;
    #endif

    DECLARE_LIGHTMAP_OR_SH(staticLightmapUV, vertexSH, 8);
    #ifdef DYNAMICLIGHTMAP_ON
    float2  dynamicLightmapUV : TEXCOORD9; // Dynamic lightmap UVs
    #endif

    float4 positionCS               : SV_POSITION;
    float2 screenUV                 : TEXCOORD10;
    float4 positionHCS              : TEXCOORD11;
    UNITY_VERTEX_INPUT_INSTANCE_ID
    UNITY_VERTEX_OUTPUT_STEREO
};

void InitializeInputData(Varyings input, half3 normalTS, out InputData inputData)
{
    inputData = (InputData)0;
    inputData.positionCS = input.positionCS;

#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
    inputData.positionWS = input.positionWS;
#endif

    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
#if defined(_NORMALMAP) || defined(_DETAIL)
    float sgn = input.tangentWS.w;      // should be either +1 or -1
    float3 bitangent = sgn * cross(input.normalWS.xyz, input.tangentWS.xyz);
    half3x3 tangentToWorld = half3x3(input.tangentWS.xyz, bitangent.xyz, input.normalWS.xyz);

    #if defined(_NORMALMAP)
    inputData.tangentToWorld = tangentToWorld;
    #endif
    inputData.normalWS = TransformTangentToWorld(normalTS, tangentToWorld);
#else
    inputData.normalWS = input.normalWS;
#endif

    inputData.normalWS = NormalizeNormalPerPixel(inputData.normalWS);
    inputData.viewDirectionWS = viewDirWS;

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    inputData.shadowCoord = input.shadowCoord;
#elif defined(MAIN_LIGHT_CALCULATE_SHADOWS)
    inputData.shadowCoord = TransformWorldToShadowCoord(inputData.positionWS);
#else
    inputData.shadowCoord = float4(0, 0, 0, 0);
#endif
    //
    //
    //
    // neat trick to avoid messing with real shadows (above)
    inputData.positionWS = positionWS;
#ifdef _ADDITIONAL_LIGHTS_VERTEX
    inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactorAndVertexLight.x);
    inputData.vertexLighting = input.fogFactorAndVertexLight.yzw;
#else
    inputData.fogCoord = InitializeInputDataFog(float4(input.positionWS, 1.0), input.fogFactor);
#endif

#if defined(DYNAMICLIGHTMAP_ON)
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.dynamiclightmapUV, input.vertexSH, inputData.normalWS);
#else
    inputData.bakedGI = SAMPLE_GI(input.staticLightmapUV, input.vertexSH, inputData.normalWS);
#endif
    inputData.normalizedScreenSpaceUV = GetNormalizedScreenSpaceUV(input.positionCS);
    inputData.shadowMask = SAMPLE_SHADOWMASK(input.staticLightmapUV);
    #if defined(DEBUG_DISPLAY)
    #if defined(DYNAMICLIGHTMAP_ON)
    inputData.dynamicLightmapUV = input.dynamiclightmapUV;
    #endif
    #if defined(LIGHTMAP_ON)
    inputData.staticLightmapUV = input.staticLightmapUV;
    #else
    inputData.vertexSH = input.vertexSH;
    #endif
    #endif
}

///////////////////////////////////////////////////////////////////////////////
//                  Vertex and Fragment functions                            //
///////////////////////////////////////////////////////////////////////////////

// Used in Standard (Physically Based) shader
Varyings LitPassVertex(Attributes input)
{
    Varyings output = (Varyings)0;
    
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_TRANSFER_INSTANCE_ID(input, output);
    UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

    VertexPositionInputs vertexInput = GetVertexPositionInputs(input.positionOS.xyz);
    /// СУПЕР ХАРДКОД ПОЖАЛУЙСТА ИЗБАВЬСЯ ОТ ЭТОГО УЖАСА 
    // vertexInput.positionWS = positionWS + half3(0, 0.1, 0);
    
    // VertexNormalInputs normalInput = GetVertexNormalInputs(input.normalOS, input.tangentOS);
    half3 normalOS = mul(m_WtO, half4(normalWS, 1)).xyz;
    VertexNormalInputs normalInput = GetVertexNormalInputs(normalOS, input.tangentOS);
    half3 vertexLight = VertexLighting(vertexInput.positionWS, normalInput.normalWS);

    half fogFactor = 0;
    #if !defined(_FOG_FRAGMENT)
        fogFactor = ComputeFogFactor(vertexInput.positionCS.z);
    #endif

    output.uv = TRANSFORM_TEX(input.texcoord, _BaseMap);

    // already normalized from normal transform to WS.
    output.normalWS = normalInput.normalWS;
#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR) || defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    real sign = input.tangentOS.w * GetOddNegativeScale();
    half4 tangentWS = half4(normalInput.tangentWS.xyz, sign);
#endif
#if defined(REQUIRES_WORLD_SPACE_TANGENT_INTERPOLATOR)
    output.tangentWS = tangentWS;
#endif

#if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(vertexInput.positionWS);
    half3 viewDirTS = GetViewDirectionTangentSpace(tangentWS, output.normalWS, viewDirWS);
    output.viewDirTS = viewDirTS;
#endif

    // OUTPUT_LIGHTMAP_UV(input.staticLightmapUV, unity_LightmapST, output.staticLightmapUV);
    OUTPUT_LIGHTMAP_UV(lightmapUV, unity_LightmapST, output.staticLightmapUV);
#ifdef DYNAMICLIGHTMAP_ON
    output.dynamicLightmapUV = input.dynamicLightmapUV.xy * unity_DynamicLightmapST.xy + unity_DynamicLightmapST.zw;
#endif
    OUTPUT_SH(output.normalWS.xyz, output.vertexSH);
#ifdef _ADDITIONAL_LIGHTS_VERTEX
    output.fogFactorAndVertexLight = half4(fogFactor, vertexLight);
#else
    output.fogFactor = fogFactor;
#endif

#if defined(REQUIRES_WORLD_SPACE_POS_INTERPOLATOR)
    output.positionWS = vertexInput.positionWS;
#endif

#if defined(REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR)
    output.shadowCoord = GetShadowCoord(vertexInput);
#endif

    // output.positionCS = mul(m_MVP, float4(input.positionOS.xyz, 1));
    output.positionCS = vertexInput.positionCS;
    float4 screenPosition = ComputeScreenPos(output.positionCS);
    output.screenUV = screenPosition.xy / screenPosition.w;

    return output;
}

// Used in Standard (Physically Based) shader
void LitPassFragment(
    Varyings input, out half4 outColor : SV_Target0
#ifdef _WRITE_RENDERING_LAYERS
    , out float4 outRenderingLayers : SV_Target1
#endif
)
{
    UNITY_SETUP_INSTANCE_ID(input);
    UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(input);

#if defined(_PARALLAXMAP)
#if defined(REQUIRES_TANGENT_SPACE_VIEW_DIR_INTERPOLATOR)
    half3 viewDirTS = input.viewDirTS;
#else
    half3 viewDirWS = GetWorldSpaceNormalizeViewDir(input.positionWS);
    half3 viewDirTS = GetViewDirectionTangentSpace(input.tangentWS, input.normalWS, viewDirWS);
#endif
    ApplyPerPixelDisplacement(viewDirTS, input.uv);
#endif

    SurfaceData surfaceData;
    InitializeStandardLitSurfaceData(input.uv, surfaceData);

#ifdef LOD_FADE_CROSSFADE
    LODFadeCrossFade(input.positionCS);
#endif

    InputData inputData;
    InitializeInputData(input, surfaceData.normalTS, inputData);
    SETUP_DEBUG_TEXTURE_DATA(inputData, input.uv);
    // inputData.bakedGI.x *= pow(inputData.bakedGI.x, 2);
    // inputData.bakedGI.y *= pow(inputData.bakedGI.y, 2);
    // inputData.bakedGI.z *= pow(inputData.bakedGI.z, 2);
    // outColor = half4(inputData.bakedGI, 1);
    // return;

#ifdef _DBUFFER
    ApplyDecalToSurfaceData(input.positionCS, surfaceData, inputData);
#endif
    half4 color = UniversalFragmentPBR(inputData, surfaceData);
    color.rgb = MixFog(color.rgb, inputData.fogCoord);
    color.a = OutputAlpha(color.a, IsSurfaceTypeTransparent(_Surface));

    half3 colour = color.rgb;
    // GetOutline_float(input.screenUV, colour, totalIllumination, totalLuminance, colour);

    if (_ValueSaturationCelShader)
    {   
        colour = RGBtoHSV(colour / float3(max(_BaseColor.r, 0.0001), max(_BaseColor.g, 0.0001), max(_BaseColor.b, 0.0001)));
        colour.g = Quantize(_SaturationSteps, colour.g);
        colour.b = pow(10, Quantize(_ValueSteps, log10(colour.b)));
        colour = HSVtoRGB(colour) * _BaseColor.rgb;
    }
    outColor = half4(colour, 1);

    half4 clipSample = _ClipTex.Sample(clip_point_clamp_sampler, input.uv);
    clip(clipSample.a > 0.5 ? 1 : -1);

#ifdef _WRITE_RENDERING_LAYERS
    uint renderingLayers = GetMeshRenderingLayer();
    outRenderingLayers = float4(EncodeMeshRenderingLayer(renderingLayers), 0, 0, 0);
#endif
}

#endif

// --------------------------
#endif
