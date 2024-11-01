#ifndef UNITY_BSDF_INCLUDED
#define UNITY_BSDF_INCLUDED

#if SHADER_API_MOBILE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_SWITCH
#pragma warning (disable : 3205) // conversion of larger type to smaller
#endif

#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

struct CBSDF
{
    float3 diffR; // Diffuse  reflection   (T -> MS -> T, same sides)
    float3 specR; // Specular reflection   (R, RR, TRT, etc)
    float3 diffT; // Diffuse  transmission (rough T or TT, opposite sides)
    float3 specT; // Specular transmission (T, TT, TRRT, etc)
};

real F_Schlick(real f0, real f90, real u)
{
    real x = 1.0 - u;
    real x2 = x * x;
    real x5 = x * x2 * x2;
    return (f90 - f0) * x5 + f0;                // sub mul mul mul sub mad
}

real F_Transm_Schlick(real f0, real f90, real u)
{
    real x = 1.0 - u;
    real x2 = x * x;
    real x5 = x * x2 * x2;
    return (1.0 - f90 * x5) - f0 * (1.0 - x5);  // sub mul mul mul mad sub mad
}

TEMPLATE_2_REAL(IorToFresnel0, transmittedIor, incidentIor, return Sq((transmittedIor - incidentIor) / (transmittedIor + incidentIor)) )
// ior is a value between 1.0 and 3.0. 1.0 is air interface

TEMPLATE_1_REAL(ConvertF0ForAirInterfaceToF0ForClearCoat15Fast, fresnel0, return saturate(fresnel0 * (fresnel0 * 0.526868 + 0.529324) - 0.0482256))

real D_GGX(real NdotH, real roughness)
{
    real a2 = Sq(roughness);
    real s = (NdotH * a2 - NdotH) * NdotH + 1.0;

    return INV_PI * SafeDiv(a2, s * s);
}

real GetSmithJointGGXPartLambdaV(real NdotV, real roughness)
{
    real a2 = Sq(roughness);
    return sqrt((-NdotV * a2 + NdotV) * NdotV + a2);
}

real V_SmithJointGGX(real NdotL, real NdotV, real roughness, real partLambdaV)
{
    real a2 = Sq(roughness);

    real lambdaV = NdotL * partLambdaV;
    real lambdaL = NdotV * sqrt((-NdotL * a2 + NdotL) * NdotL + a2);

    return 0.5 / max(lambdaV + lambdaL, REAL_MIN);
}

real V_SmithJointGGX(real NdotL, real NdotV, real roughness)
{
    real partLambdaV = GetSmithJointGGXPartLambdaV(NdotV, roughness);
    return V_SmithJointGGX(NdotL, NdotV, roughness, partLambdaV);
}

real GetSmithJointGGXAnisoPartLambdaV(real TdotV, real BdotV, real NdotV, real roughnessT, real roughnessB)
{
    return length(real3(roughnessT * TdotV, roughnessB * BdotV, NdotV));
}

real V_SmithJointGGXAniso(real TdotV, real BdotV, real NdotV, real TdotL, real BdotL, real NdotL, real roughnessT, real roughnessB, real partLambdaV)
{
    real lambdaV = NdotL * partLambdaV;
    real lambdaL = NdotV * length(real3(roughnessT * TdotL, roughnessB * BdotL, NdotL));

    return 0.5 / (lambdaV + lambdaL);
}

real V_SmithJointGGXAniso(real TdotV, real BdotV, real NdotV, real TdotL, real BdotL, real NdotL, real roughnessT, real roughnessB)
{
    real partLambdaV = GetSmithJointGGXAnisoPartLambdaV(TdotV, BdotV, NdotV, roughnessT, roughnessB);
    return V_SmithJointGGXAniso(TdotV, BdotV, NdotV, TdotL, BdotL, NdotL, roughnessT, roughnessB, partLambdaV);
}

real DisneyDiffuseNoPI(real NdotV, real NdotL, real LdotV, real perceptualRoughness)
{
    real fd90 = 0.5 + (perceptualRoughness + perceptualRoughness * LdotV);
    // Two schlick fresnel term
    real lightScatter = F_Schlick(1.0, fd90, NdotL);
    real viewScatter = F_Schlick(1.0, fd90, NdotV);
    return rcp(1.03571) * (lightScatter * viewScatter);
}

real D_CharlieNoPI(real NdotH, real roughness)
{
    float invR = rcp(roughness);
    float cos2h = NdotH * NdotH;
    float sin2h = 1.0 - cos2h;
    return (2.0 + invR) * PositivePow(sin2h, invR * 0.5) / 2.0;
}

real D_Charlie(real NdotH, real roughness)
{
    return INV_PI * D_CharlieNoPI(NdotH, roughness);
}

#if SHADER_API_MOBILE || SHADER_API_GLES || SHADER_API_GLES3 || SHADER_API_SWITCH
#pragma warning (enable : 3205) // conversion of larger type to smaller
#endif

#endif // UNITY_BSDF_INCLUDED
