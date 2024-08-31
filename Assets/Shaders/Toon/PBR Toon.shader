Shader "Ledsna/PBR Toon"
{

    Properties
    {
        [ToggleUI]_Debug("Debug", Float) = 0
        [ToggleUI]_External("External", Float) = 0
        [ToggleUI]_Convex("Convex", Float) = 0
        [ToggleUI]_Concave("Concave", Float) = 0
        _HighlightPower("HighlightPower", Range(0, 1)) = 0.5

        _Colour("Colour", Color) = (1, 1, 1, 1)

        _Metallic("Metallic", Range(0, 1)) = 0
        _AttenuationSteps("Attenuation Steps", Float) = 10
        _DiffuseSteps("Diffuse Steps", Float) = -1
        _Smoothness("Smoothness", Range(0, 1)) = 0.5
        _SpecularSteps("Specular Steps", Float) = -1
        _AmbientOcclusion("Ambient Occlusion", Range(0, 1)) = 0.5
        _RimSteps("Rim Steps", Int) = -1
        _DepthThreshold("Depth Threshold", Float) = 52
        _NormalsThreshold("Normals Threshold", Float) = 0.17
        _ExternalScale("External Scale", Float) = 1
        _InternalScale("Internal Scale", Float) = 1
    }

    SubShader
    {
        Tags 
        { 
            "RenderType" = "Opaque"
            "RenderPipeline" = "UniversalPipeline"
        }
        
        // LOD 100

        Pass
        {
            Name "DepthOnly"
            Tags
            {
                "LightMode" = "DepthOnly"
            }
        
            Cull Back
                ZTest LEqual
                ZWrite On
                ColorMask R
        }

        Pass
        {
            Name "ShadowCaster"
            Tags
            {
                "LightMode" = "ShadowCaster"
            }
        
            Cull Back
                ZTest LEqual
                ZWrite On
                ColorMask 0
        }

        Pass
        {
            Name "Universal Forward"
            Tags
            {
                // LightMode: <None>
            }

            Cull Back
                Blend One Zero
                ZTest LEqual
                ZWrite On
            
            HLSLPROGRAM

            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS // _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _ADDITIONAL_LIGHTS _ADDITIONAL_LIGHT_SHADOWS
            #pragma multi_compile _ _CASTING_PUNCTUAL_LIGHT_SHADOW
            #pragma multi_compile_fragment _ _SHADOWS_SOFT

            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Assets/Shaders/Toon/ToonLighting.hlsl"
            #include "Assets/Shaders/Outlines/Outlines.hlsl"

            // sampler2D _MainTex;
            // float4 _MainTex_ST;

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;

                float2 uv : TEXCOORD0;

                float2 lightmapUV : TEXCOORD1;
            };

            struct Varyings
            {
                float4 position : SV_POSITION;
                float4 screenPosition : TEXCOORD0;

                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;

                float2 lightmapUV : TEXCOORD4;
            };

            Varyings vert (Attributes IN)
            {
                Varyings OUT;
                OUT.positionWS = mul(unity_ObjectToWorld, IN.positionOS).xyz;
                OUT.normalWS = normalize(mul((float3x3)unity_ObjectToWorld, IN.normalOS));

                OUT.position = TransformObjectToHClip(IN.positionOS.xyz);
                OUT.screenPosition = ComputeScreenPos(OUT.position);

                OUT.lightmapUV = IN.lightmapUV;

                return OUT;
            }

            half4 frag (Varyings i) : SV_Target
            {
                float2 screenUV = i.screenPosition.xy / i.screenPosition.w;

                float3 baseColour;
                float totalAttenuation;
                float3 totalLuminance;

                float3 reflectionWS = reflect(-GetWorldSpaceNormalizeViewDir(i.positionWS), i.normalWS);

                half4 reflectionColour = UNITY_SAMPLE_TEXCUBE(unity_SpecCube0, reflectionWS);

                baseColour = lerp(_Colour, reflectionColour, _Metallic);

                CalculateCustomLighting_float(i.positionWS, i.normalWS, GetWorldSpaceNormalizeViewDir(i.positionWS), i.lightmapUV, baseColour,
                    baseColour, totalAttenuation, totalLuminance);
                

                GetOutline_float(screenUV, baseColour, totalAttenuation, totalLuminance, baseColour);

                return float4(baseColour, 1);
            }
            
            ENDHLSL
        }
    }
}
