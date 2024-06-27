Shader "Hidden/OutlineShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "black" {}
    }
    SubShader
    {
        // No culling or depth
        Cull Off ZWrite Off ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 posWorld : TEXCOORD1;
                float4 vertex : SV_POSITION;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.posWorld = mul(unity_ObjectToWorld, v.vertex);
                return o;
            }

            float4x4 _CameraViewToWorld;
            // sampler2D _MainTex;
            Texture2D _MainTex;
            sampler2D _CameraDepthTexture;
            sampler2D _CameraNormalTexture;

            
            float _DepthOutlineScale;
            float _NormalOutlineScale;
            float _DepthThreshold;
            float _NormalThreshold;
            float _HighlightPower;
            float _ShadowPower;
            
            uniform float4 _MainTex_TexelSize;

            SamplerState point_clamp_sampler;
            float _Zoom;
            
            float3 getNormal(float2 uv)
            {
                return tex2D(_CameraNormalTexture, uv).rgb;
            }

            float3 ViewNormalToWorld(float3 viewNormal) {
                return normalize(mul((float3x3)_CameraViewToWorld, float4(normalize(viewNormal * 2 - 1), 0)));
            }

            float DiffuseComponent(float3 worldNormal) {
                return dot(normalize(worldNormal), normalize(_WorldSpaceLightPos0.xyz));
            }

            float DiffuseForView(float3 viewNormal)
            {
                return DiffuseComponent(ViewNormalToWorld(viewNormal)) * (_HighlightPower - _ShadowPower) + _ShadowPower;
            }

            float getDepth(float2 uv)
            {
                return tex2D(_CameraDepthTexture, uv);
            }

            void get_neighbour_uvs(float2 uv, float distance, out float2 neighbours[4])
            {
                float2 pixel_size = 1. / (_ScreenParams.xy);
                neighbours[0] = uv + float2(0, pixel_size.y) * distance;
                neighbours[1] = uv - float2(0, pixel_size.y) * distance;
                neighbours[2] = uv + float2(pixel_size.x, 0) * distance;
                neighbours[3] = uv - float2(pixel_size.x, 0) * distance;
            }
            
            // void get_neighbour_uvs3(float2 uv, float distance, out float2 neighbours[12])
            // {
            //     float2 pixel_size = 1. / (_ScreenParams.xy - 2);
            //     neighbours[0] = uv + float2(0, pixel_size.y) * distance;
            //     neighbours[1] = uv - float2(0, pixel_size.y) * distance;
            //     neighbours[2] = uv + float2(pixel_size.x, 0) * distance;
            //     neighbours[3] = uv - float2(pixel_size.x, 0) * distance;
            //     neighbours[4] = uv + float2(0, pixel_size.y * 2) * distance;
            //     neighbours[5] = uv - float2(0, pixel_size.y * 2) * distance;
            //     neighbours[6] = uv + float2(pixel_size.x * 2, 0) * distance;
            //     neighbours[7] = uv - float2(pixel_size.x * 2, 0) * distance;
            //     neighbours[8] = uv + float2(0, pixel_size.y * 3) * distance;
            //     neighbours[9] = uv - float2(0, pixel_size.y * 3) * distance;
            //     neighbours[10] = uv + float2(pixel_size.x * 3, 0) * distance;
            //     neighbours[11] = uv - float2(pixel_size.x * 3, 0) * distance;
            //
            // }
            
            #ifndef OUTLINE_INCLUDED
            #define OUTLINE_INCLUDED
            fixed3 outline_color(float2 uv)
            {
                fixed4 base_color = _MainTex.Sample(point_clamp_sampler, uv);
                // fixed3 base_color = tex2D(_MainTex, uv).rgb;
                
                float depth = getDepth(uv);
                float3 normal = getNormal(uv);
                
                float2 neighbour_depths[4];
                float2 neighbour_normals[4];
                
                get_neighbour_uvs(uv, _DepthOutlineScale, neighbour_depths);
                get_neighbour_uvs(uv, _NormalOutlineScale, neighbour_normals);
                
                float depth_diff_sum = 0.;

                [unroll]
                for (int d = 0; d < 4; d++)
                    depth_diff_sum += depth - getDepth(neighbour_depths[d]);

                float3 normal_edge_bias = normalize(float3(1, 1, 1));
                float dotSum = 0.0;
                [unroll]
                for (int n = 0; n < 4; n++)
                {
                    float3 normal_diff = normal - getNormal(neighbour_normals[n]);
                    float normal_diff_weight = smoothstep(-.01, .01, dot(normal_diff, normal_edge_bias));

                    dotSum += dot(normal_diff, normal_diff) * normal_diff_weight;
                }
                float normal_edge = step(_NormalThreshold, sqrt(dotSum));
                float depth_edge = step(_DepthThreshold / 10000., depth_diff_sum);
                
                if (depth_diff_sum < 0.0)
                {
                    return base_color;
                }
                if (depth_edge > 0.0)
                {
                    return lerp(base_color, base_color * (_ShadowPower - 1), depth_edge);
                    // return lerp(base_color, fixed3(0, 0, 1), depth_edge);

                }
                return lerp(base_color, base_color * DiffuseForView(normal), normal_edge);
                // return lerp(base_color, fixed3(1, 0, 0), normal_edge);

            }
            #endif

            fixed4 frag (v2f i) : SV_Target
            {
                return fixed4(outline_color(i.uv), 1);
            }
            ENDCG
        }
    }
}
