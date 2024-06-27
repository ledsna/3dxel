Shader "Custom/VSN"
{
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            ZWrite On
            ColorMask RGBA

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 pos : SV_POSITION;
                float3 viewNormal : TEXCOORD0; // Changed to viewNormal
            };

            v2f vert(appdata v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                float3 worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal)); // Normal in world space
                o.viewNormal = mul((float3x3)UNITY_MATRIX_V, worldNormal); // Transform normal to view space
                return o;
            }

            float3 frag(v2f i) : SV_Target
            {
                return i.viewNormal * 0.5 + 0.5; // Remapping from [-1,1] to [0,1] for visualization
            }
            ENDCG
        }
    }
}
