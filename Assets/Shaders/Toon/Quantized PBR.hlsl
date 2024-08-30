#ifndef CUSTOM_LIGHTING_INCLUDED
#define CUSTOM_LIGHTING_INCLUDED

// This is a neat trick to work around a bug in the shader graph when
// enabling shadow keywords. Created by @cyanilux
// https://github.com/Cyanilux/URP_ShaderGraphCustomLighting
// Licensed under the MIT License, Copyright (c) 2020 Cyanilux
#ifndef SHADERGRAPH_PREVIEW
    #include "Packages/com.unity.render-pipelines.universal/Editor/ShaderGraph/Includes/ShaderPass.hlsl"
    #if (SHADERPASS != SHADERPASS_FORWARD)
        #undef REQUIRES_VERTEX_SHADOW_COORD_INTERPOLATOR
    #endif
#endif

struct CustomLightingData {
    // Position and orientation
    float3 positionWS;
    float3 normalWS;
    float3 viewDirectionWS;
    float4 shadowCoord;

    // Surface attributes
    float3 albedo;
    float smoothness;
    float ambientOcclusion;
    float diffuseSteps;
    float specularSteps;
    float rimSteps;
    float radianceSteps;

    // Baked lighting
    float3 bakedGI;
    float4 shadowMask;
    float fogFactor;
};

float quantize(float steps, float shade)
{
    if (steps == -1) return shade;
    if (steps == 0) return 0;
    if (steps == 1) return 1;
    return floor(shade * (steps - 1) + 0.5) / (steps - 1);
}

// Translate a [0, 1] smoothness value to an exponent 
float GetSmoothnessPower(float rawSmoothness) {
    return exp2(10 * rawSmoothness + 1);
}

#ifndef SHADERGRAPH_PREVIEW
float3 CustomGlobalIllumination(CustomLightingData d) {
    float3 indirectDiffuse = d.albedo * d.bakedGI * d.ambientOcclusion;

    float3 reflectVector = reflect(-d.viewDirectionWS, d.normalWS);
    // This is a rim light term, making reflections stronger along
    // the edges of view
    // float fresnel = Pow4(1 - saturate(dot(d.viewDirectionWS, d.normalWS)));
    float fresnel = quantize(d.rimSteps, Pow4(1 - saturate(dot(d.viewDirectionWS, d.normalWS))));
    // This function samples the baked reflections cubemap
    // It is located in URP/ShaderLibrary/Lighting.hlsl
    float3 indirectSpecular = GlossyEnvironmentReflection(reflectVector,
        RoughnessToPerceptualRoughness(1 - d.smoothness),
        d.ambientOcclusion) * fresnel;

    return indirectDiffuse + indirectSpecular;
}

float4 CustomLightHandling(CustomLightingData d, Light light) {

    float attenuation = quantize(d.radianceSteps, light.distanceAttenuation * light.shadowAttenuation);
    // float attenuation = light.distanceAttenuation * light.shadowAttenuation;

    float3 radiance = light.color * attenuation;

    // ledsna edit
    // float diffuse = saturate(dot(d.normalWS, light.direction));
    float diffuse = saturate(dot(d.normalWS, light.direction));
    float specularDot = saturate(dot(d.normalWS, normalize(light.direction + d.viewDirectionWS)));
    float specular = pow(specularDot, GetSmoothnessPower(d.smoothness)) * diffuse;

    float3 color = d.albedo * radiance * (quantize(d.diffuseSteps, diffuse) +
                                          quantize(d.specularSteps, specular));

    return float4(color, attenuation);
}
#endif

float4 CalculateCustomLighting(CustomLightingData d) {
#ifdef SHADERGRAPH_PREVIEW
    // In preview, estimate diffuse + specular
    float3 lightDir = float3(0.5, 0.5, 0);
    float intensity = saturate(dot(d.normalWS, lightDir)) +
        pow(saturate(dot(d.normalWS, normalize(d.viewDirectionWS + lightDir))), GetSmoothnessPower(d.smoothness));
    return float4(d.albedo * intensity, 1);
#else
    // Get the main light. Located in URP/ShaderLibrary/Lighting.hlsl
    Light mainLight = GetMainLight(d.shadowCoord, d.positionWS, d.shadowMask);
    // In mixed subtractive baked lights, the main light must be subtracted
    // from the bakedGI value. This function in URP/ShaderLibrary/Lighting.hlsl takes care of that.
    MixRealtimeAndBakedGI(mainLight, d.normalWS, d.bakedGI);
    float3 color = CustomGlobalIllumination(d);
    float totalAttenuation = 0;
    // Shade the main light
    float4 litColour = CustomLightHandling(d, mainLight);
    color += litColour.xyz;
    totalAttenuation += litColour.w;

    #ifdef _ADDITIONAL_LIGHTS
        // Shade additional cone and point lights. Functions in URP/ShaderLibrary/Lighting.hlsl
        uint numAdditionalLights = GetAdditionalLightsCount();
        for (uint lightI = 0; lightI < numAdditionalLights; lightI++) {
            Light light = GetAdditionalLight(lightI, d.positionWS, d.shadowMask);
            litColour = CustomLightHandling(d, light);
            color += litColour.xyz;
            totalAttenuation += litColour.w;
        }
    #endif

    color = MixFog(color, d.fogFactor);

    return float4(color, totalAttenuation);
#endif
}

void CalculateCustomLighting_float(float3 Position, float3 Normal, float3 ViewDirection,
    float3 Albedo, float Smoothness, float AmbientOcclusion,
    float2 LightmapUV, float DiffuseSteps, float SpecularSteps, float RimSteps,
    float RadianceSteps,
    out float3 Color, out float TotalAttenuation) {

    CustomLightingData d;
    d.positionWS = Position;
    d.normalWS = Normal;
    d.viewDirectionWS = ViewDirection;
    d.albedo = Albedo;
    d.smoothness = Smoothness;
    d.ambientOcclusion = AmbientOcclusion;
    d.diffuseSteps = DiffuseSteps;
    d.specularSteps = SpecularSteps;
    d.rimSteps = RimSteps;
    d.radianceSteps = RadianceSteps;

#ifdef SHADERGRAPH_PREVIEW
    // In preview, there's no shadows or bakedGI
    d.shadowCoord = 0;
    d.bakedGI = 0;
    d.shadowMask = 0;
    d.fogFactor = 0;
#else
    // Calculate the main light shadow coord
    // There are two types depending on if cascades are enabled
    float4 positionCS = TransformWorldToHClip(Position);
    #if SHADOWS_SCREEN
        d.shadowCoord = ComputeScreenPos(positionCS);
    #else
        d.shadowCoord = TransformWorldToShadowCoord(Position);
    #endif

    // The following URP functions and macros are all located in
    // URP/ShaderLibrary/Lighting.hlsl
    // Technically, OUTPUT_LIGHTMAP_UV, OUTPUT_SH and ComputeFogFactor
    // should be called in the vertex function of the shader. However, as of
    // 2021.1, we do not have access to custom interpolators in the shader graph.

    // The lightmap UV is usually in TEXCOORD1
    // If lightmaps are disabled, OUTPUT_LIGHTMAP_UV does nothing
    float2 lightmapUV;
    OUTPUT_LIGHTMAP_UV(LightmapUV, unity_LightmapST, lightmapUV);
    // Samples spherical harmonics, which encode light probe data
    float3 vertexSH;
    OUTPUT_SH(Normal, vertexSH);
    // This function calculates the final baked lighting from light maps or probes
    d.bakedGI = SAMPLE_GI(lightmapUV, vertexSH, Normal);
    // This function calculates the shadow mask if baked shadows are enabled
    d.shadowMask = SAMPLE_SHADOWMASK(lightmapUV);
    // This returns 0 if fog is turned off
    // It is not the same as the fog node in the shader graph
    d.fogFactor = ComputeFogFactor(positionCS.z);
#endif
    float4 customLighting = CalculateCustomLighting(d);
    Color = customLighting.xyz;
    TotalAttenuation = customLighting.w;
}

#endif