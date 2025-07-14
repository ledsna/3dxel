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
        
        _HighlightWidthUnits("Highlight Width (Units)", Float) = 0.1
        _BaseWidthVariationPerTarget("Center Width Variation (Factor)", Float) = 0.1
        _BaseWidthVariationSeed("Center Width Variation Seed", Float) = 0.19

        _AvgOscillationAmplitude("Oscillation Amplitude (Units)", Float) = 0.1
        _AmplitudeVariationPerTarget("Amplitude Variation (Factor)", Float) = 0.25
        _AmplitudeVariationSeed("Amplitude Variation Seed", Float) = 0.33
        
        _AvgOscillationSpeed("Avg. Oscillation Speed", Float) = 5.0
        _SpeedVariationPerTarget("Speed Variation (Factor)", Float) = 0.25
        _SpeedVariationSeed("Speed Variation Seed", Float) = 0.77
        
        _TargetPhaseSeed("Target Oscillation Phase Seed", Float) = 0.4
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" }
        Pass
        {
            Name "ForwardLit"
            Tags
            {
                "LightMode" = "UniversalForward"
            }
            
            HLSLPROGRAM
            #pragma target 4.5
            
            #pragma vertex vert
            #pragma fragment frag
            
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _LIGHT_COOKIES
            #pragma multi_compile _ _FORWARD_PLUS
            #pragma multi_compile _ _SHADOWS_SOFT
            #pragma multi_compile_fragment _ _ADDITIONAL_LIGHT_SHADOWS

            // #pragma multi_compile _ _REFLECTION_PROBE_BLENDING 
            // #pragma multi_compile _ _REFLECTION_PROBE_BOX_PROJECTION
            // #pragma multi_compile _ _ADDITIONAL_LIGHTS_VERTEX _ADDITIONAL_LIGHTS
            #pragma multi_compile _ _ADDITIONAL_LIGHTS

            #pragma multi_compile_fog
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl" 
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

#define NUM_TARGETS 12 // 100
            
static const float2 PREDEFINED_TARGET_XY_OFFSETS[NUM_TARGETS] = {
    float2(0.723f, 0.189f), float2(0.099f, 0.874f), float2(0.452f, 0.547f), float2(0.891f, 0.332f),
    float2(0.276f, 0.961f), float2(0.638f, 0.075f), float2(0.112f, 0.423f), float2(0.805f, 0.788f),
    float2(0.587f, 0.219f), float2(0.034f, 0.692f), float2(0.976f, 0.501f), float2(0.314f, 0.157f),
    // float2(0.699f, 0.912f), float2(0.187f, 0.045f), float2(0.402f, 0.733f), float2(0.941f, 0.258f),
    // float2(0.078f, 0.603f), float2(0.765f, 0.399f), float2(0.223f, 0.814f), float2(0.509f, 0.117f),
    // float2(0.853f, 0.667f), float2(0.146f, 0.372f), float2(0.601f, 0.983f), float2(0.927f, 0.028f),
    // float2(0.388f, 0.481f), float2(0.715f, 0.715f), float2(0.059f, 0.294f), float2(0.991f, 0.839f),
    // float2(0.253f, 0.572f), float2(0.668f, 0.131f), float2(0.108f, 0.925f), float2(0.821f, 0.466f),
    // float2(0.489f, 0.088f), float2(0.753f, 0.621f), float2(0.201f, 0.317f), float2(0.537f, 0.888f),
    // float2(0.909f, 0.064f), float2(0.021f, 0.745f), float2(0.683f, 0.411f), float2(0.355f, 0.997f),
    // float2(0.817f, 0.233f), float2(0.169f, 0.596f), float2(0.952f, 0.824f), float2(0.298f, 0.015f),
    // float2(0.612f, 0.678f), float2(0.047f, 0.346f), float2(0.739f, 0.901f), float2(0.431f, 0.182f),
    // float2(0.877f, 0.513f), float2(0.125f, 0.769f), float2(0.574f, 0.037f), float2(0.965f, 0.634f),
    // float2(0.210f, 0.445f), float2(0.656f, 0.857f), float2(0.089f, 0.099f), float2(0.781f, 0.702f),
    // float2(0.336f, 0.286f), float2(0.918f, 0.949f), float2(0.007f, 0.524f), float2(0.550f, 0.168f),
    // float2(0.832f, 0.801f), float2(0.194f, 0.071f), float2(0.625f, 0.563f), float2(0.370f, 0.936f),
    // float2(0.983f, 0.305f), float2(0.068f, 0.657f), float2(0.707f, 0.248f), float2(0.261f, 0.892f),
    // float2(0.475f, 0.011f), float2(0.844f, 0.759f), float2(0.133f, 0.532f), float2(0.592f, 0.199f),
    // float2(0.901f, 0.974f), float2(0.019f, 0.207f), float2(0.676f, 0.612f), float2(0.348f, 0.053f),
    // float2(0.790f, 0.868f), float2(0.237f, 0.493f), float2(0.971f, 0.381f), float2(0.082f, 0.142f),
    // float2(0.524f, 0.726f), float2(0.866f, 0.004f), float2(0.158f, 0.953f), float2(0.649f, 0.270f),
    // float2(0.309f, 0.685f), float2(0.935f, 0.580f), float2(0.051f, 0.847f), float2(0.774f, 0.105f),
    // float2(0.418f, 0.794f), float2(0.882f, 0.363f), float2(0.177f, 0.641f), float2(0.609f, 0.021f),
    // float2(0.959f, 0.916f), float2(0.287f, 0.430f), float2(0.729f, 0.124f), float2(0.012f, 0.809f),
    // float2(0.561f, 0.539f), float2(0.810f, 0.092f), float2(0.245f, 0.782f), float2(0.694f, 0.261f)
};

            CBUFFER_START(UnityPerMaterial)
                float _HighlightWidthUnits;
                float _BaseWidthVariationPerTarget;
                float _BaseWidthVariationSeed;
                float _AvgOscillationAmplitude;
                float _AmplitudeVariationPerTarget;
                float _AmplitudeVariationSeed;
                float _AvgOscillationSpeed;
                float _SpeedVariationPerTarget;
                float _SpeedVariationSeed;
                float _TargetPhaseSeed;
            CBUFFER_END

            float4 _Tint;
            float _Smoothness;
            float _Density;
            float _FoamThreshold;
            sampler2D _NormalsTexture;
            sampler2D _PositionsTexture;

            float _BaseAmplitude;
            float _BaseFrequency;
            int _NumOctaves; // This is your "amount of waves"
            float _AmplitudeFalloff;
            float _FrequencyGrowth;
            float _WaveSpeed;
            float _RandomSeed; // Seed for random directions

            float _RefractionStrength;
            float4 _SpecularColor; 

            sampler2D _Reflection1;


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
                float3 pOS      : TEXCOORD4;
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
                o.pOS = pOS;
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

            float HitFoamParticle(float3 pos_os)
            {

                float3 scale;
                scale.x = length(float3(unity_ObjectToWorld[0].x, unity_ObjectToWorld[1].x, unity_ObjectToWorld[2].x));
                scale.y = length(float3(unity_ObjectToWorld[0].y, unity_ObjectToWorld[1].y, unity_ObjectToWorld[2].y));
                scale.z = length(float3(unity_ObjectToWorld[0].z, unity_ObjectToWorld[1].z, unity_ObjectToWorld[2].z));

                const float2 TILE_DIMENSIONS = float2(30, 14) / scale.xz; 
                float2 tileGridCoordinate = floor(pos_os.xz / TILE_DIMENSIONS);
                float2 tileCornerOS_xz = tileGridCoordinate * TILE_DIMENSIONS;
                
                float target_count = min(NUM_TARGETS, 25);
                [loop]
                for (int k = 0; k < target_count; ++k) {
                    float k_float = (float)k;
                    float2 localTargetNormalizedOffset = PREDEFINED_TARGET_XY_OFFSETS[k];
                    float2 actualOffsetFromTileCorner = localTargetNormalizedOffset * TILE_DIMENSIONS;
                    float3 target_pos_os = float3(
                        tileCornerOS_xz.x + actualOffsetFromTileCorner.x, 
                        pos_os.y, 
                        tileCornerOS_xz.y + actualOffsetFromTileCorner.y
                    );

                    // float2 viewPos = mul((float2x2)UNITY_MATRIX_I_P, screenUV);
                    // float2 targetViewPos = mul((float2x2)UNITY_MATRIX_I_P, targe)

                    float2 screen_uv;
                    float2 target_screen_uv;

                    if (unity_OrthoParams.w)
                    {
                        screen_uv = TransformObjectToHClipDir(pos_os).xy * 0.5 + 0.5;
                        target_screen_uv = TransformObjectToHClipDir(target_pos_os).xy * 0.5 + 0.5;
                    }
                    else
                    {
                        float4 pos_hcs = TransformObjectToHClip(pos_os);
                        float4 target_pos_hcs = TransformObjectToHClip(target_pos_os);
                        screen_uv = pos_hcs.xy / pos_hcs.w * 0.5 + 0.5;
                        target_screen_uv = target_pos_hcs.xy / target_pos_hcs.w * 0.5 + 0.5;
                    }
                    
                    float2 pos_pixel = screen_uv * _ScaledScreenParams.xy;
                    float2 target_pos_pixel = target_screen_uv * _ScaledScreenParams.xy;
                    
                    float2 pix_distance = abs(target_pos_pixel - pos_pixel);

                    float2 pos_vs = TransformWorldToView(TransformObjectToWorld(pos_os));
                    float2 target_pos_vs = TransformWorldToView(TransformObjectToWorld(target_pos_os));
                    float2 ws_units_distance = abs(pos_vs - target_pos_vs);

                    float width_factor = 1.0 + sin(k_float * _BaseWidthVariationSeed) * _BaseWidthVariationPerTarget;
                    float width = _HighlightWidthUnits * width_factor;
                    width = max(0.1, width);

                    float speed_factor = 1.0 + sin(k_float * _SpeedVariationSeed) * _SpeedVariationPerTarget;
                    float speed = _AvgOscillationSpeed * speed_factor;
                    speed = max(0.01, speed);
                    
                    float amplitude_factor = 1.0 + sin(k_float * _AmplitudeVariationSeed) * _AmplitudeVariationPerTarget;
                    float amplitude = _AvgOscillationAmplitude * amplitude_factor;
                    amplitude = max(0.0, amplitude);
                    
                    float phase = k_float * _TargetPhaseSeed;
                    float oscillation = sin(_Time.y * speed + phase);
                    float current_width = width + oscillation * amplitude;

                    // current_width *= _ScreenParams.x / 640;
                    // float width_threshold = current_width / 2;
                    
                    // if (!unity_OrthoParams.w)
                        // width_threshold /= TransformObjectToHClip(pos_os).w / (2 * width);
                
                    if (ws_units_distance.x < current_width / 2 && 
                        pix_distance.y < .5) {
                        return 1;
                    }
                }
                return 0;
            }

            float3 ComputeFoam(float3 baseCol, float vDepth, float threshold, float3 pOS) {
                return baseCol / max(1, TransformObjectToHClip(pOS).w / 10) *
                    max(saturate(1-(vDepth/threshold)), HitFoamParticle(pOS));
            }

            float3 AbsorbLight(float3 sceneCol, float density, float dist, float3 absorption) {
                return sceneCol * exp(-density * max(0.0, dist) * absorption);
            }
            
            float3 GetSceneColourWithoutFog(float3 colour, float3 positionWS, float z)
            {
                float fogIntensity = ComputeFogIntensity(ComputeFogCoord(z, positionWS));
                return (colour - unity_FogColor.rgb * (1 - fogIntensity)) / fogIntensity;
            }

            half4 frag(Varyings i) : SV_Target {
                float2 uv = GetNormalizedScreenSpaceUV(i.pCS);

                float3 normalWS = i.nWS;
                float3 viewDirWS = i.viewDirWS;

                float3 viewNormalVS = normalize(mul((float3x3)UNITY_MATRIX_V, normalWS));
                float2 refUV = saturate(uv + viewNormalVS.xy * _RefractionStrength);

                float clearDepth = tex2D(_NormalsTexture, uv).a;
                float refClearDepth = tex2D(_NormalsTexture, refUV).a;

                #if UNITY_REVERSED_Z
                    real depth = SampleSceneDepth(uv);
                    real refDepth = SampleSceneDepth(refUV);
                #else
                    real depth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0f, SampleSceneDepth(uv));
                    real refDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1.0f, SampleSceneDepth(refUV));
                #endif

                float3 bbPos = tex2D(_PositionsTexture, uv).xyz;
                float3 refBbPos = tex2D(_PositionsTexture, refUV).xyz;

                clearDepth = (clearDepth > 0.001f && clearDepth < 0.999f) ? clearDepth : depth;
                refClearDepth = (refClearDepth > 0.001f && refClearDepth < 0.999f) ? refClearDepth : refDepth;
                
                float3 refBackgroundPosWS = ComputeWorldSpacePosition(refUV, refClearDepth, UNITY_MATRIX_I_VP);
                float3 backgroundPosWS = ComputeWorldSpacePosition(uv, clearDepth, UNITY_MATRIX_I_VP);

                bool applyRefraction = distance(_WorldSpaceCameraPos.xyz, refBackgroundPosWS) > distance(_WorldSpaceCameraPos.xyz, i.pWS);
                
// float3 rayOrigin = applyRefraction ? refBackgroundPosWS : backgroundPosWS;
// float3 rayDir = normalize(_WorldSpaceCameraPos - rayOrigin);
//
// float3 planeOrigin = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
// float3 planeNormal = normalize(mul((float3x3)unity_ObjectToWorld, float3(0, 1, 0)));
//
// float denom = dot(planeNormal, rayDir);
// float t = dot(planeNormal, planeOrigin - rayOrigin) / denom;
//
// float3 intersec = rayOrigin + t * rayDir;
// check if refracted fragment is below water
// distance from water fragment to actual fragment
// if (tex2D(_PositionsTexture, uv).a > 0)
//     backgroundPosWS = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
// if (tex2D(_PositionsTexture, refUV).a > 0)
//     refBackgroundPosWS = ComputeWorldSpacePosition(uv, refDepth, UNITY_MATRIX_I_VP);
                
                float distanceForAbsorption = distance(i.pWS, applyRefraction ? refBackgroundPosWS : backgroundPosWS);

                float3 sceneColour = SampleSceneColor(applyRefraction ? refUV : uv);
                sceneColour = GetSceneColourWithoutFog(sceneColour, applyRefraction ? refBackgroundPosWS : backgroundPosWS,
                    i.pCS.z);

                float3 baseRefractedColor = AbsorbLight(sceneColour, _Density, distanceForAbsorption, 1 - _Tint.rgb);
                
                Light mainLight;
                #if defined(_MAIN_LIGHT_SHADOWS) && !defined(_RECEIVE_SHADOWS_OFF)
                    mainLight = GetMainLight(i.shadowCoord, i.pWS, half4(1,1,1,1), _Smoothness);
                #else
                    mainLight = GetMainLight();
                #endif
                float shadowAttenuation = mainLight.shadowAttenuation;

                float3 reflectV = reflect(-viewDirWS, normalWS);
                float2 diff = uv - refUV;
                // float3 envReflection = GlossyEnvironmentReflection(reflectV, perceptualRoughness, 1.0f);
                float2 uv_reflected = float2(1 - uv.x, uv.y) + diff;
                
                float4 reflectionSample = tex2D(_Reflection1, uv_reflected);

                float fogCoord = ComputeFogCoord(i.pCS.z, i.pWS);
                
                float viewZ = -(mul((float3x3)UNITY_MATRIX_V, i.pWS).z);
                float z_diff = reflectionSample.a - viewZ;
                float nearToFarZDiff = max(z_diff - _ProjectionParams.y, 0);

                float3 envReflection = MixFog(reflectionSample.rgb, ComputeFogFactorZ0ToFar(nearToFarZDiff));
                
                // PBR Sun Specular Highlight
                float NdotV = saturate(dot(normalWS, viewDirWS));

                half alpha = 1;
                BRDFData brdf_data;

                half reflectivity = ReflectivitySpecular(_SpecularColor);
                half oneMinusReflectivity = half(1.0) - reflectivity;
                half3 brdfDiffuse = float3(0, 0, 0);
                half3 brdfSpecular = _SpecularColor;
                InitializeBRDFDataDirect(_Tint, brdfDiffuse, brdfSpecular, reflectivity, oneMinusReflectivity, _Smoothness, alpha, brdf_data);

                // Compute direct lights specular
                half3 directSpecular = 0;

                // Main light
                directSpecular += LightingPhysicallyBased(
                    brdf_data, 
                    mainLight, 
                    normalWS, 
                    viewDirWS
                );

                float3 totalReflection = envReflection;
                
                #if defined(_ADDITIONAL_LIGHTS)
                    uint pixelLightCount = GetAdditionalLightsCount();
                    #if USE_FORWARD_PLUS
                    [loop] for (uint lightIndex = 0; lightIndex < min(URP_FP_DIRECTIONAL_LIGHTS_COUNT, MAX_VISIBLE_LIGHTS); lightIndex++)
                    {
                        FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK
                        Light light = GetAdditionalLight(lightIndex, i.pWS, half4(1,1,1,1));
                        directSpecular += LightingPhysicallyBased(
                            brdf_data, 
                            light, 
                            normalWS, 
                            viewDirWS
                        );
                    }
                    {
                        uint lightIndex;
                        ClusterIterator _urp_internal_clusterIterator = ClusterInit(GetNormalizedScreenSpaceUV(i.pCS), i.pWS, 0);
                        [loop] while (ClusterNext(_urp_internal_clusterIterator, lightIndex)) {
                            lightIndex += URP_FP_DIRECTIONAL_LIGHTS_COUNT;
                            FORWARD_PLUS_SUBTRACTIVE_LIGHT_CHECK
                            Light light = GetAdditionalLight(lightIndex, i.pWS, half4(1,1,1,1));
                            directSpecular += LightingPhysicallyBased(
                                brdf_data, 
                                light, 
                                normalWS, 
                                viewDirWS
                            );
                        }
                    }
                    #else
                        for (uint lightIndex = 0u; lightIndex < lightCount; ++lightIndex) {
                            Light light = GetAdditionalLight(lightIndex, i.pWS, half4(1,1,1,1));
                            directSpecular += LightingPhysicallyBased(
                                brdf_data, 
                                light, 
                                normalWS, 
                                viewDirWS
                            );
                        }
                    #endif
                #endif

                float F0_water = 0.05;
                float finalFresnel = F0_water + (1.0f - F0_water) * pow(saturate(1.0f - NdotV), 5.0f);

                totalReflection += directSpecular;
                float3 colorBeforeFoam = lerp(baseRefractedColor, totalReflection, finalFresnel);

                float3 opaquePosWS = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
                float verticalDepth = max(0.0f, i.pWS.y - opaquePosWS.y); 
                float3 foamColor = ComputeFoam(lerp(envReflection, totalReflection, shadowAttenuation),
                    verticalDepth, _FoamThreshold, i.pOS);// * max(1, directSpecular);
                // foamColor = MixFog(foamColor, foamColor == 0 ? 0 : ComputeFogCoord(i.pCS.z, i.pWS));
                float3 finalColor = saturate(colorBeforeFoam + foamColor);

                #if defined(_FOG_FRAGMENT)
                    finalColor = MixFog(finalColor, fogCoord);
                #endif
                
                return half4(finalColor, 1);
            }
            ENDHLSL
        }
    }
    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}