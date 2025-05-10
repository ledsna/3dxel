Shader "Ledsna/Water"
{
    Properties
    {
        _Tint("Tint", Color) = (0.66,0.77,0.88,1)
        _Density("Density", Range(0, 1)) = 1
        _Smoothness("Surface Smoothness", Range(0.01, 1)) = 0.8 
        _FoamThreshold("Max Foam Depth (Units)", Range(0.001, 0.2)) = 0.03

        // Sum of Sines Wave Properties
        _BaseAmplitude("Base Amplitude", Float) = 0.15
        _BaseFrequency("Base Frequency", Float) = 1.0
        _NumOctaves("Number of Octaves", Range(1, 10)) = 6 // This is your "amount of waves"
        _AmplitudeFalloff("Amplitude Falloff (k)", Range(0.0, 0.9)) = 0.25 
        _FrequencyGrowth("Frequency Growth (s)", Range(0.0, 1.5)) = 0.8  
        _WaveSpeed("Wave Speed", Float) = 0.7
        _RandomSeed("Wave Random Seed", Float) = 123.456 // Seed for random directions

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

            float4 _Tint;
            float _Smoothness;
            float _Density;
            float _FoamThreshold;
            sampler2D _NormalsTexture; 

            float _BaseAmplitude;
            float _BaseFrequency;
            int _NumOctaves; // This is your "amount of waves"
            float _AmplitudeFalloff;
            float _FrequencyGrowth;
            float _WaveSpeed;
            float _RandomSeed; // Seed for random directions

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

            struct SummedSineWaveOutput {
                float y_offset;  
                float dp_dx;     
                float dp_dz;     
            };

            // Simple hash function to generate pseudo-random numbers from a float
            float random(float seed) {
                return frac(sin(seed * 12.9898 + _RandomSeed) * 43758.5453);
            }
            // Generate a random 2D direction vector
            float2 randomDirection(float seed) {
                float angle = random(seed + 10.0) * 2.0 * PI; // Add an offset to seed for angle
                return float2(cos(angle), sin(angle));
            }


            Varyings vert(Attributes input) {
                Varyings o;
                float3 pOS = input.pOS.xyz; 
                float time = _Time.y * _WaveSpeed;

                SummedSineWaveOutput waveData;
                waveData.y_offset = 0.0f;
                waveData.dp_dx = 0.0f;
                waveData.dp_dz = 0.0f;

                float currentAmplitude = _BaseAmplitude;
                float currentFrequency = _BaseFrequency;
                
                for (int k = 0; k < _NumOctaves; k++) // _NumOctaves is your "amount of waves"
                {
                    if (currentAmplitude < 0.0001f) break; 

                    // Generate a pseudo-random direction for each octave based on its index + global seed
                    float2 dir = randomDirection(float(k) * 0.731 + 23.45f); // Unique seed per octave

                    float waveNumber = currentFrequency; 
                    float phaseConstant = time * currentFrequency * 0.5f; 

                    float argument = waveNumber * dot(dir, pOS.xz) + phaseConstant;
                    
                    waveData.y_offset += currentAmplitude * sin(argument);
                    
                    float cos_arg = cos(argument);
                    waveData.dp_dx += currentAmplitude * cos_arg * waveNumber * dir.x;
                    waveData.dp_dz += currentAmplitude * cos_arg * waveNumber * dir.y;

                    currentAmplitude *= (1.0f - _AmplitudeFalloff);
                    currentFrequency *= (1.0f + _FrequencyGrowth);
                }
                
                float3 displacedPosOS = float3(pOS.x, pOS.y + waveData.y_offset, pOS.z);
                float3 N_os = SafeNormalize(float3(-waveData.dp_dx, 1.0f, -waveData.dp_dz)); 

                o.pWS = TransformObjectToWorld(displacedPosOS); 
                o.nWS = TransformObjectToWorldNormal(N_os); 
                o.nWS = SafeNormalize(o.nWS); 

                o.pCS = TransformWorldToHClip(o.pWS);
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

            float3 FresnelSchlick(float cosTheta, float3 F0) { 
                return F0 + (1.0f - F0) * pow(saturate(1.0f - cosTheta), 5.0f);
            }

            float DistributionGGX(float NdotH, float roughness)
            {
                float alpha = roughness * roughness;
                float alphaSqr = alpha * alpha;
                float NdotHSqr = NdotH * NdotH;
                float denom = NdotHSqr * (alphaSqr - 1.0f) + 1.0f;
                return alphaSqr / (PI * denom * denom);
            }

            float GeometrySmith(float NdotV, float NdotL, float roughness)
            {
                float alpha = roughness * roughness; 
                float k = alpha / 2.0f; 
                
                float ggxV = NdotV / (NdotV * (1.0f - k) + k);
                float ggxL = NdotL / (NdotL * (1.0f - k) + k);
                return ggxV * ggxL;
            }

            // FRAGMENT SHADER IS NOW IDENTICAL TO YOUR LAST PROVIDED VERSION
            // THAT YOU SAID HAD CORRECT LIGHTING.
            // The only change is that i.nWS now comes from the summed-sine analytical derivatives.
            half4 frag(Varyings i) : SV_Target {
                float2 uv = i.pCS.xy/_ScaledScreenParams.xy;

                // With N_os = float3(-dpdx, 1.0f, -dpdz) from analytical derivatives,
                // i.nWS should be correctly outward. Test this.
                float3 normalWS = i.nWS; 
                // float3 normalWS = -i.nWS; // Fallback if specular is still inverted

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
                
                float3 F0_water = float3(0.02f, 0.02f, 0.02f); 
                float perceptualRoughness = 1.0f - _Smoothness; 
                perceptualRoughness = max(0.001f, perceptualRoughness); 

                float3 reflectV = reflect(-viewDirWS, normalWS);
                float3 envReflection = GlossyEnvironmentReflection(reflectV, perceptualRoughness, 1.0f); 
                float3 totalReflection = envReflection; // Start with environment reflection

                // PBR Sun Specular Highlight
                float3 lightDirWS = mainLight.direction; 
                float3 halfVec = SafeNormalize(lightDirWS + viewDirWS);

                float NdotL = saturate(dot(normalWS, lightDirWS));
                float NdotV = saturate(dot(normalWS, viewDirWS)); 
                float NdotH = saturate(dot(normalWS, halfVec));

                float D_ggx = DistributionGGX(NdotH, perceptualRoughness);
                float G_smith = GeometrySmith(NdotV, NdotL, perceptualRoughness);
                float3 F_schlick_spec = FresnelSchlick(saturate(dot(viewDirWS, halfVec)), F0_water); 

                float denominator = 4.0f * NdotL * NdotV + 0.001f; 
                float3 specularCookTorrance = (D_ggx * G_smith * F_schlick_spec) / denominator;
                
                float3 sunSpecular = mainLight.color * specularCookTorrance;
                totalReflection += sunSpecular * _SpecularColor.rgb * _SpecularColor.a; // Add sun specular

                // Final Fresnel blend for the surface appearance (view-dependent)
                float finalFresnel = FresnelSchlick(NdotV, F0_water); 
                
                float3 colorBeforeFoam = lerp(baseRefractedColor, totalReflection,
                    finalFresnel * shadowAttenuation * _EnvironmentReflectionStrength);

                float3 opaquePosWS = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
                float verticalDepth = max(0.0f, i.pWS.y - opaquePosWS.y); 
                float3 foamColor = ComputeFoam(float3(1,1,1), verticalDepth, _FoamThreshold);
                float3 finalColor = saturate(colorBeforeFoam * (1.0f + foamColor * foamColor) + foamColor * foamColor * 0.25f);

                finalColor = MixFog(finalColor, ComputeFogCoord(i.pCS.z, i.pWS));

                return half4(finalColor, _Tint.a);
            }
            ENDHLSL
        }
    }
    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}