Shader "Custom/TiledUltraUniqueOscillatingTargetURP"
{
    Properties
    {
        _AvgHighlightWidthPixels("Avg. Center Highlight Width (Pixels)", Float) = 15.0
        _BaseWidthVariationPerTarget("Center Width Variation (Factor)", Float) = 0.1
        _BaseWidthVariationSeed("Center Width Variation Seed", Float) = 0.19

        _AvgOscillationAmplitude("Avg. Oscillation Amplitude (Pixels)", Float) = 15.0
        _AmplitudeVariationPerTarget("Amplitude Variation (Factor)", Float) = 0.25
        _AmplitudeVariationSeed("Amplitude Variation Seed", Float) = 0.33
        
        _AvgOscillationSpeed("Avg. Oscillation Speed", Float) = 5.0
        _SpeedVariationPerTarget("Speed Variation (Factor)", Float) = 0.25
        _SpeedVariationSeed("Speed Variation Seed", Float) = 0.77
        
        _TargetPhaseSeed("Target Oscillation Phase Seed", Float) = 0.4
        _HighlightHeightPixels("Highlight Height (Pixels)", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderPipeline"="UniversalPipeline" "Queue"="Transparent" "RenderType"="Transparent" }

        Pass
        {
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #define NUM_TARGETS 20
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            static const float2 PREDEFINED_TARGET_XY_OFFSETS[NUM_TARGETS] = {
                float2(0.10, 0.20), float2(0.50, 0.80), float2(0.30, 0.40),
                float2(0.70, 0.10), float2(0.90, 0.90), float2(0.20, 0.60),
                float2(0.80, 0.30), float2(0.40, 0.70), float2(0.60, 0.50),
                float2(0.05, 0.95), float2(0.95, 0.05), float2(0.50, 0.05),
                float2(0.05, 0.50), float2(0.75, 0.75), float2(0.25, 0.25),
                float2(0.15, 0.85), float2(0.85, 0.15), float2(0.50, 0.35),
                float2(0.35, 0.50), float2(0.65, 0.65)
            };
            
            CBUFFER_START(UnityPerMaterial)
                float _AvgHighlightWidthPixels;
                float _BaseWidthVariationPerTarget;
                float _BaseWidthVariationSeed;
                float _AvgOscillationAmplitude;
                float _AmplitudeVariationPerTarget;
                float _AmplitudeVariationSeed;
                float _AvgOscillationSpeed;
                float _SpeedVariationPerTarget;
                float _SpeedVariationSeed;
                float _TargetPhaseSeed;
                float _HighlightHeightPixels;
            CBUFFER_END

            struct Attributes { float4 positionOS : POSITION; };
            struct Varyings { float4 positionCS : SV_POSITION; float3 positionOS : TEXCOORD0; };
            
            Varyings vert(Attributes i) {
                Varyings OUT;
                OUT.positionOS = i.positionOS.xyz;
                OUT.positionCS = TransformObjectToHClip(i.positionOS);
                return OUT;
            }

            half4 frag(Varyings i) : SV_Target {
                float3 finalColor = 0; 

                const float2 TILE_DIMENSIONS = float2(1.0, 2.0); 
                float2 tileGridCoordinate = floor(i.positionOS.xz / TILE_DIMENSIONS);
                float2 tileCornerOS_xz = tileGridCoordinate * TILE_DIMENSIONS;

                [loop]
                for (int k = 0; k < NUM_TARGETS; ++k) {
                    float k_float = (float)k;
                    float2 localTargetNormalizedOffset = PREDEFINED_TARGET_XY_OFFSETS[k];
                    float2 actualOffsetFromTileCorner = localTargetNormalizedOffset * TILE_DIMENSIONS;
                    float3 targetPosOS = float3(
                        tileCornerOS_xz.x + actualOffsetFromTileCorner.x, 
                        i.positionOS.y, 
                        tileCornerOS_xz.y + actualOffsetFromTileCorner.y
                    );

                    float3 deltaOS = i.positionOS - targetPosOS;
                    float3 deltaCS_unnormalized = mul((float3x3)UNITY_MATRIX_MVP, deltaOS); 
                    float2 diffPixels = deltaCS_unnormalized.xy * _ScaledScreenParams.xy;

                    float targetUniqueBaseWidthFactor = 1.0 + sin(k_float * _BaseWidthVariationSeed) * _BaseWidthVariationPerTarget;
                    float actualBaseWidth = _AvgHighlightWidthPixels * targetUniqueBaseWidthFactor;
                    actualBaseWidth = max(0.1, actualBaseWidth);

                    float targetUniqueSpeedFactor = 1.0 + sin(k_float * _SpeedVariationSeed) * _SpeedVariationPerTarget;
                    float actualSpeed = _AvgOscillationSpeed * targetUniqueSpeedFactor;
                    actualSpeed = max(0.01, actualSpeed);
                    
                    float targetUniqueAmplitudeFactor = 1.0 + sin(k_float * _AmplitudeVariationSeed) * _AmplitudeVariationPerTarget;
                    float actualAmplitude = _AvgOscillationAmplitude * targetUniqueAmplitudeFactor;
                    actualAmplitude = max(0.0, actualAmplitude);
                    
                    float phase = k_float * _TargetPhaseSeed;
                    float oscillation = sin(_Time.y * actualSpeed + phase);
                    float currentDynamicWidth = actualBaseWidth + oscillation * actualAmplitude;

                    if (abs(diffPixels.x) < currentDynamicWidth && 
                        abs(diffPixels.y) < _HighlightHeightPixels) {
                        finalColor = 1; 
                        break; 
                    }
                }
                
                return half4(finalColor, 1.0); 
            }
            ENDHLSL
        }
    }
    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}