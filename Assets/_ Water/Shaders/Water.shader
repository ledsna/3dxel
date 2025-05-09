Shader "Ledsna/Water"
{
    Properties
    {
        _Tint("Tint", Color) = (0.66,0.77,0.88,1)
        _Density("Density", Range(0, 1)) = 1
        _Smoothness("Surface Smoothness", Range(0.01, 1)) = 0.8 
        _FoamThreshold("Max Foam Depth (Units)", Range(0.001, 0.2)) = 0.03

        _WaveA1("Wave 1 Amplitude", Float) = 0.1
        _WaveL1("Wave 1 Wavelength", Float) = 10.0
        _WaveS1("Wave 1 Speed", Float) = 1.0
        _WaveD1("Wave 1 Direction (XZ)", Vector) = (1,0.2,0,0)
        _WaveQ1("Wave 1 Steepness", Range(0, 0.9)) = 0.5

        _WaveA2("Wave 2 Amplitude", Float) = 0.07
        _WaveL2("Wave 2 Wavelength", Float) = 7.0
        _WaveS2("Wave 2 Speed", Float) = 1.5
        _WaveD2("Wave 2 Direction (XZ)", Vector) = (0.3,1,0,0)
        _WaveQ2("Wave 2 Steepness", Range(0, 0.9)) = 0.4

        _RefractionStrength("Refraction Strength", Float) = 0.02

        _SpecularColor("Specular Tint/Intensity", Color) = (1,1,1,0.5) 
        _EnvironmentReflectionStrength("Env. Reflection Strength", Range(0,1)) = 0.3
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile_fog
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile _ _REFLECTION_PROBE_BLENDING 
            #pragma multi_compile _ _REFLECTION_PROBE_BOX_PROJECTION 

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl" 
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl" 
            // For PBR BRDF components, URP often has them in ShadingModels.hlsl or similar,
            // or we implement standard ones.
            // Let's implement minimal GGX D and Smith G for clarity.

            float4 _Tint;
            float _Smoothness;
            float _Density;
            float _FoamThreshold;
            sampler2D _NormalsTexture; 

            float _WaveA1, _WaveL1, _WaveS1, _WaveQ1;
            float4 _WaveD1;
            float _WaveA2, _WaveL2, _WaveS2, _WaveQ2;
            float4 _WaveD2;
            float _RefractionStrength;

            float4 _SpecularColor; 
            float _EnvironmentReflectionStrength;


            struct Attributes { float4 pOS:POSITION; float3 nOS:NORMAL; float4 tOS:TANGENT; };
            struct Varyings
            {
                float4 pCS:SV_POSITION;
                float3 pWS:TEXCOORD0;
                float3 nWS:TEXCOORD1;
                #if defined(_MAIN_LIGHT_SHADOWS) && !defined(_RECEIVE_SHADOWS_OFF)
                float4 shadowCoord : TEXCOORD2;
                #endif
                float3 viewDirWS : TEXCOORD3; 
            };

            struct GerstnerWaveOutput {
                float3 P_offset;
                float ddx_term, ddy_term, ddz_term;
                float pdx_term, pdy_term, pdz_term;
            };

            GerstnerWaveOutput CalculateGerstnerWave(float3 pOS, float A, float L, float S, float2 D, float Q) {
                GerstnerWaveOutput o;
                if (L < 0.001 || A < 0.00001) { 
                    o.P_offset=0; o.ddx_term=0; o.ddy_term=0; o.ddz_term=0; o.pdx_term=0; o.pdy_term=0; o.pdz_term=0; return o;
                }
                float k=2*PI/L, f=k*(dot(D,pOS.xz)-S*_Time.y), c=cos(f), s=sin(f);
                o.P_offset = float3(-Q*A*D.x*s, A*c, -Q*A*D.y*s);
                float kA=k*A, kQA=k*Q*A;
                o.ddx_term=kQA*D.x*D.x*c; o.ddy_term=kA*D.x*s; o.ddz_term=kQA*D.x*D.y*c;
                o.pdx_term=kQA*D.y*D.x*c; o.pdy_term=kA*D.y*s; o.pdz_term=kQA*D.y*D.y*c;
                return o;
            }

            Varyings vert(Attributes i) {
                Varyings o;
                float3 pOS=i.pOS.xyz, dOS=0;
                float s_ddx=0,s_ddy=0,s_ddz=0,s_pdx=0,s_pdy=0,s_pdz=0;

                float2 dir1=normalize(_WaveD1.xy); if(length(dir1)<0.001)dir1=float2(1,0);
                GerstnerWaveOutput w1=CalculateGerstnerWave(i.pOS.xyz,_WaveA1,_WaveL1,_WaveS1,dir1,_WaveQ1);
                dOS+=w1.P_offset; s_ddx+=w1.ddx_term;s_ddy+=w1.ddy_term;s_ddz+=w1.ddz_term; s_pdx+=w1.pdx_term;s_pdy+=w1.pdy_term;s_pdz+=w1.pdz_term;

                float2 dir2=normalize(_WaveD2.xy); if(length(dir2)<0.001)dir2=float2(0,1);
                GerstnerWaveOutput w2=CalculateGerstnerWave(i.pOS.xyz,_WaveA2,_WaveL2,_WaveS2,dir2,_WaveQ2);
                dOS+=w2.P_offset; s_ddx+=w2.ddx_term;s_ddy+=w2.ddy_term;s_ddz+=w2.ddz_term; s_pdx+=w2.pdx_term;s_pdy+=w2.pdy_term;s_pdz+=w2.pdz_term;
                
                pOS+=dOS;
                float3 T_os=SafeNormalize(float3(1.0f - s_ddx, s_ddy, -s_ddz));
                float3 B_os=SafeNormalize(float3(-s_pdx, s_pdy, 1.0f - s_pdz));
                float3 N_os=SafeNormalize(cross(T_os,B_os)); 

                o.pWS=TransformObjectToWorld(pOS);
                o.nWS=TransformObjectToWorldNormal(N_os); 
                o.nWS = SafeNormalize(o.nWS); 

                o.pCS=TransformWorldToHClip(o.pWS);
                o.viewDirWS = SafeNormalize(_WorldSpaceCameraPos.xyz - o.pWS);

                #if defined(_MAIN_LIGHT_SHADOWS) && !defined(_RECEIVE_SHADOWS_OFF)
                    o.shadowCoord = TransformWorldToShadowCoord(o.pWS);
                #endif
                
                return o;
            }

            float ComputeFogCoord(float HCSposZ, float3 posWS)
            {
                half fogFactor = 0;
                #if !defined(_FOG_FRAGMENT)
                    fogFactor = ComputeFogFactor(HCSposZ);
                #endif
                float fogCoord = InitializeInputDataFog(float4(posWS, 1.0), fogFactor);
                return fogCoord;
            }

            float3 ComputeFoam(float3 baseCol, float vDepth, float threshold) {
                return baseCol * saturate(1-(vDepth/threshold));
            }

            float3 AbsorbLight(float3 sceneCol, float density, float dist, float3 tintMinusOne) {
                return sceneCol * exp(density * max(0.0, dist) * tintMinusOne); 
            }

            // Standard Fresnel Schlick approximation
            float3 FresnelSchlick(float cosTheta, float3 F0) { // Changed to float3 F0
                return F0 + (1.0f - F0) * pow(saturate(1.0f - cosTheta), 5.0f);
            }

            // PBR BRDF Components (Minimal implementations)
            // D_GGX (Trowbridge-Reitz)
            float DistributionGGX(float NdotH, float roughness)
            {
                float alpha = roughness * roughness;
                float alphaSqr = alpha * alpha;
                float NdotHSqr = NdotH * NdotH;
                float denom = NdotHSqr * (alphaSqr - 1.0f) + 1.0f;
                return alphaSqr / (PI * denom * denom);
            }

            // G_Smith GGX Correlated (Height-Correlated Smith)
            // Also known as G_SmithSchlickGGX or G2
            float GeometrySmith(float NdotV, float NdotL, float roughness)
            {
                float alpha = roughness * roughness; // Or (roughness + 1)^2 / 8 for k_Direct
                float k = alpha / 2.0f; // k_Smith (alpha_ggx^2 / 2)
                
                float ggxV = NdotV / (NdotV * (1.0f - k) + k);
                float ggxL = NdotL / (NdotL * (1.0f - k) + k);
                return ggxV * ggxL;
            }


            half4 frag(Varyings i) : SV_Target {
                float2 uv = i.pCS.xy/_ScaledScreenParams.xy;

                float3 normalWS = -i.nWS; 
                float3 viewDirWS = i.viewDirWS; 

                float3 viewNormalVS = normalize(mul((float3x3)UNITY_MATRIX_V, normalWS)); 
                float2 refUV = saturate(uv + viewNormalVS.xy * _RefractionStrength);

                #if UNITY_REVERSED_Z
                    real depth = SampleSceneDepth(uv);
                    real refDepth = SampleSceneDepth(refUV);
                #else
                    real depth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0f, SampleSceneDepth(uv));
                    real refDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0f, SampleSceneDepth(refUV));
                #endif
                
                float clearDepthTex = tex2D(_NormalsTexture, uv).a;
                float clearDepth = (clearDepthTex > 0.001f && clearDepthTex < 0.999f) ? clearDepthTex : depth;

                float refClearDepthTex = tex2D(_NormalsTexture, refUV).a;
                float refClearDepth = (refClearDepthTex > 0.001f && refClearDepthTex < 0.999f) ? refClearDepthTex : refDepth;
                
                float3 refBackgroundPosWS = ComputeWorldSpacePosition(refUV, refClearDepth, UNITY_MATRIX_I_VP);
                float3 backgroundPosWS = ComputeWorldSpacePosition(uv, clearDepth, UNITY_MATRIX_I_VP);
                
                bool applyRefraction = distance(refBackgroundPosWS, _WorldSpaceCameraPos.xyz) > distance(i.pWS, _WorldSpaceCameraPos.xyz);
                
                float distanceForAbsorption = applyRefraction ?
                    distance(refBackgroundPosWS, i.pWS) : distance(backgroundPosWS, i.pWS);
                
                float3 sceneColor = SampleSceneColor(applyRefraction ? refUV : uv);
                float3 baseRefractedColor = AbsorbLight(sceneColor, _Density, distanceForAbsorption, _Tint.rgb - 1.0f);

                Light mainLight;
                #if defined(_MAIN_LIGHT_SHADOWS) && !defined(_RECEIVE_SHADOWS_OFF)
                    mainLight = GetMainLight(i.shadowCoord);
                #else
                    mainLight = GetMainLight();
                #endif
                float shadowAttenuation = mainLight.shadowAttenuation;
                
                // --- PBR Calculations ---
                float3 F0_water = float3(0.02f, 0.02f, 0.02f); // Base reflectivity for water
                float perceptualRoughness = 1.0f - _Smoothness; // Convert artist-friendly smoothness to PBR roughness
                perceptualRoughness = max(0.001f, perceptualRoughness); // Clamp roughness to avoid issues with alpha=0 in D_GGX

                // Reflections

                // Environment Reflection
                float3 reflectV = reflect(-viewDirWS, normalWS);
                float3 envReflection = GlossyEnvironmentReflection(reflectV, perceptualRoughness, 1.0f); 
                float3 totalReflection = envReflection;


                // PBR Sun Specular Highlight

                    float3 lightDirWS = mainLight.direction; 
                    float3 halfVec = SafeNormalize(lightDirWS + viewDirWS);

                    float NdotL = saturate(dot(normalWS, lightDirWS));
                    float NdotV = saturate(dot(normalWS, viewDirWS)); // Already have viewDirWS
                    float NdotH = saturate(dot(normalWS, halfVec));
                    // float LdotH = saturate(dot(lightDirWS, halfVec)); // Not directly needed for this BRDF form

                    // BRDF Components
                    float D_ggx = DistributionGGX(NdotH, perceptualRoughness);
                    float G_smith = GeometrySmith(NdotV, NdotL, perceptualRoughness);
                    float3 F_schlick = FresnelSchlick(saturate(dot(viewDirWS, halfVec)), F0_water); // Fresnel on LdotH or VdotH for Cook-Torrance
                                                                                                // For dielectrics, F0 is used with NdotV for final blend.
                                                                                                // For specular term, F is often on VdotH.

                    // Cook-Torrance Specular BRDF: D * G * F / (4 * NdotL * NdotV)
                    // The (NdotL) part is the lambertian cosine term, which is multiplied by light color.
                    // The specular term itself is often written as (D*G*F) / (4*NdotV) (NdotL is outside)
                    // Or, more simply, the specular lobe is D*G, and F scales it.
                    // URP's Lit often simplifies this for direct lighting.
                    // Let's use a common formulation: Specular = LightColor * (D * G * F) / (4 * NdotL * NdotV) * NdotL
                    // which simplifies to LightColor * (D * G * F) / (4 * NdotV)
                    // However, a more direct approach for just the specular highlight intensity:
                    // Specular Lobe = D * G
                    // Reflected Light = LightColor * SpecularLobe * F_schlick_on_VdotH * NdotL
                    // The NdotL is important for energy arriving at the surface.

                    // Denominator for energy conservation. Ensure it's not zero.
                    float denominator = 4.0f * NdotL * NdotV + 0.001f; // Add epsilon to prevent division by zero
                    float3 specularCookTorrance = (D_ggx * G_smith * F_schlick) / denominator;
                    
                    float3 sunSpecular = mainLight.color * specularCookTorrance * PI; // Multiply by PI as it's often in BRDF definitions
                                                                                    // This PI factor can be contentious depending on BRDF source.
                                                                                    // URP Lit might bake it in differently.
                                                                                    // Let's try without PI first, then with.
                    // sunSpecular = mainLight.color * specularCookTorrance;


                    // Alternative: URP's internal BRDF might be simpler to call if accessible
                    // For now, this is a standard Cook-Torrance.
                    // The _SpecularColor.a can act as an intensity multiplier.
                    totalReflection += sunSpecular * _SpecularColor.rgb * _SpecularColor.a * shadowAttenuation;
                
                // Final Fresnel blend using NdotV for the base material property
                float finalFresnel = FresnelSchlick(NdotV, F0_water.x); // Use NdotV for the view-dependent fresnel
                
                float3 colorBeforeFoam = lerp(baseRefractedColor, totalReflection, finalFresnel * _EnvironmentReflectionStrength);

                float3 opaquePosWS = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
                float verticalDepth = max(0.0f, i.pWS.y - opaquePosWS.y); 
                float3 foamColor = ComputeFoam(float3(1,1,1), verticalDepth, _FoamThreshold);
                float3 finalColor = saturate(colorBeforeFoam * (1.0f + foamColor * foamColor) + foamColor * foamColor * 0.25f);

                finalColor = MixFog(finalColor, ComputeFogCoord(i.pCS.z, i.pWS));
                return half4(finalColor, 1);
            }
            ENDHLSL
        }
    }
    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}