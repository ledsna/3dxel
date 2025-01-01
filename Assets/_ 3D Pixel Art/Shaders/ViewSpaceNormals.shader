Shader "Ledsna/ViewSpaceNormals"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 300

        Pass
        {
            Name "ViewSpaceNormals"
            
            ZWrite On
            ZTest LEqual
            ColorMask RGBA
            
            HLSLPROGRAM

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            #pragma vertex NormalsVertex
            #pragma fragment NormalsFragment
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float3 viewNormal : TEXCOORD0;
            };

            Varyings NormalsVertex(Attributes input)
            {
                Varyings output = (Varyings)0;
                
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.viewNormal = TransformWorldToViewNormal(TransformObjectToWorldNormal(input.normalOS), true);
                return output;
            }

            float4 NormalsFragment(Varyings input) : SV_Target
            {

                return float4(input.viewNormal * 0.5 + 0.5, input.positionCS.z);
            }
            
            ENDHLSL
        }
    }
}
