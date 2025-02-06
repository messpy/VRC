Shader "metaaaaaaaaaaaa/ParallelRows"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Dist ("Distance Between Rows", Float) = 2.0
        _Spacing ("Spacing Between Figures", Float) = 1.5
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
            fixed4 _Color;

            float2 rot(float2 p, float r)
            {
                float c = cos(r);
                float s = sin(r);
                return mul(p, float2x2(c, -s, s, c));
            }

            v2f vert (appdata v, uint id : SV_VertexID)
            {
                v2f o;
                float PI = acos(-1.0);
                
                // 5人ずつ並べるためのオフセット
                int index = id % 5;
                float offsetX = (index - 2) * _Spacing; // 左右に並べる

                if (id < 5)
                {
                    // 自分を含めた側（左側の列）
                    v.vertex.x += offsetX;
                    v.vertex.z -= _Dist;  // 近い側
                }
                else
                {
                    // 反対側の5人
                    v.vertex.x += offsetX;
                    v.vertex.z += _Dist;  // 遠い側
                }

                // 全員が中央を向くように回転
                if (id >= 5)
                {
                    v.vertex.xz = rot(v.vertex.xz, PI); // 反対側の5人を180度回転
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