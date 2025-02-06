Shader "metaaaaaaaaaaaa/LookAtCenter"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Dist ("Distance to Center", Float) = 2.0
        _Spacing ("Spacing Between Figures", Float) = 1.5
        _HideSelf ("Hide Main Character", Range(0,1)) = 0
        _Color ("Color", Color) = (1.0,1.0,1.0,1.0)
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                uint id : SV_VertexID;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                UNITY_FOG_COORDS(1)
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Dist;
            float _Spacing;
            float _HideSelf;
            fixed4 _Color;

            float2 rot(float2 p, float r)
            {
                float c = cos(r);
                float s = sin(r);
                return mul(p, float2x2(c, -s, s, c));
            }

            v2f vert (appdata v)
            {
                v2f o;
                float PI = acos(-1.0);
                
                int index = v.id % 5;
                float offsetX = (index - 2) * _Spacing; // 左右に5人並べる

                if (v.id == 0 && _HideSelf > 0.5)
                {
                    // 本人を消す
                    v.vertex.xyz = float3(0, -9999, 0);
                }
                else if (v.id < 5)
                {
                    // 5人（中央を向く）
                    v.vertex.x += offsetX;
                    v.vertex.z -= _Dist;
                    v.vertex.xz = rot(v.vertex.xz, atan2(-v.vertex.x, -v.vertex.z)); // 中央を向く
                }
                else
                {
                    // 反対側の5人（中央を向く）
                    v.vertex.x += offsetX;
                    v.vertex.z += _Dist;
                    v.vertex.xz = rot(v.vertex.xz, atan2(-v.vertex.x, -v.vertex.z)); // 中央を向く
                }

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                UNITY_TRANSFER_FOG(o,o.vertex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                UNITY_APPLY_FOG(i.fogCoord, col);
                return col * _Color;
            }
            ENDCG
        }
    }
}