Shader "Ledsna/Water"
{
    Properties
    {
        _Tint("Tint", Color) = (0.66,0.77,0.88,1)
        _Density("Density", Range(0, 1)) = 1
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
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareDepthTexture.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/DeclareOpaqueTexture.hlsl"

            float4 _Tint;
            float _Density;
            float _FoamThreshold;
            sampler2D _NormalsTexture;

            float _WaveA1, _WaveL1, _WaveS1, _WaveQ1;
            float4 _WaveD1;
            float _WaveA2, _WaveL2, _WaveS2, _WaveQ2;
            float4 _WaveD2;
            float _RefractionStrength;

            struct Attributes { float4 pOS:POSITION; float3 nOS:NORMAL; float4 tOS:TANGENT; };
            struct Varyings { float4 pCS:SV_POSITION; float3 pWS:TEXCOORD0; float3 nWS:TEXCOORD1; };

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
                float3 T_os=SafeNormalize(float3(1-s_ddx,s_ddy,-s_ddz));
                float3 B_os=SafeNormalize(float3(-s_pdx,s_pdy,1-s_pdz));
                float3 N_os=SafeNormalize(cross(T_os,B_os));

                o.pWS=TransformObjectToWorld(pOS);
                o.nWS=SafeNormalize(TransformObjectToWorldNormal(N_os));
                o.pCS=TransformWorldToHClip(o.pWS);
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

            float3 AbsorbLight(float3 sceneCol, float density, float dist, float3 tint) {
                return sceneCol * exp(density * dist * tint);
            }

            half4 frag(Varyings i) : SV_Target {
                float2 uv = i.pCS.xy/_ScaledScreenParams.xy;

                float3 viewNormal = normalize(mul((float3x3)UNITY_MATRIX_V, i.nWS));
                float2 refUV = saturate(uv + viewNormal.xy * _RefractionStrength);

                #if UNITY_REVERSED_Z
                    real depth = SampleSceneDepth(uv);
                    real refDepth = SampleSceneDepth(refUV);
                #else
                    real depth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(uv));
                    real refDepth = lerp(UNITY_NEAR_CLIP_VALUE, 1, SampleSceneDepth(refUV));
                #endif
                
                float clearDepthTex = tex2D(_NormalsTexture, uv).a;
                float clearDepth = (clearDepthTex != 0 && clearDepthTex != 1) ? clearDepthTex : depth;

                float refClearDepthTex = tex2D(_NormalsTexture, refUV).a;
                float refClearDepth = (refClearDepthTex != 0 && refClearDepthTex != 1) ? refClearDepthTex : refDepth;
                
                // #if UNITY_REVERSED_Z
                //     bool isRefractedSky = refClearDepth == 0;
                // #else
                //     bool isRefractedSky = refClearDepth == 1;
                // #endif
                //
                float3 refBackgroundPosWS = ComputeWorldSpacePosition(refUV, refClearDepth, UNITY_MATRIX_I_VP);
                float3 backgroundPosWS = ComputeWorldSpacePosition(uv, clearDepth, UNITY_MATRIX_I_VP);
                bool applyRefraction = //!isRefractedSky &&
                    distance(refBackgroundPosWS, _WorldSpaceCameraPos.xyz) > distance(i.pWS, _WorldSpaceCameraPos);
                
                float distanceToLight = applyRefraction ?
                    distance(refBackgroundPosWS, i.pWS) : distance(backgroundPosWS, i.pWS);
                
                float3 sceneColor = SampleSceneColor(applyRefraction ? refUV : uv);
                float3 tintedSceneColor = AbsorbLight(sceneColor, _Density, distanceToLight, _Tint.rgb - 1);

                float3 opaquePosWS = ComputeWorldSpacePosition(uv, depth, UNITY_MATRIX_I_VP);
                float3 downWS = TransformObjectToWorldDir(float3(0, 1, 0), true);
                float verticalDepth = dot(i.pWS - opaquePosWS, downWS);
                float3 foam = ComputeFoam(float3(1,1,1), verticalDepth, _FoamThreshold);

                float3 finalColor = saturate(tintedSceneColor * (1 + 1 * foam * foam) + foam * foam / 4);
                
                finalColor = MixFog(finalColor, ComputeFogCoord(i.pCS.z, i.pWS));
                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}