Shader "metaaaaaaaaaaaa/LookAtCenter_Fixed"
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

            float3 rotateY(float3 pos, float angle)
            {
                float s = sin(angle);
                float c = cos(angle);
                return float3(
                    c * pos.x - s * pos.z,
                    pos.y,
                    s * pos.x + c * pos.z
                );
            }

            v2f vert (appdata v, uint id : SV_VertexID)
            {
                v2f o;
                float PI = acos(-1.0);
                
                int instanceID = id / 4; // 各オブジェクト単位のID
                int rowID = instanceID % 5;
                int sideID = instanceID / 5;

                float offsetX = (rowID - 2) * _Spacing;
                float offsetZ = (sideID == 0) ? -_Dist : _Dist;

                // 本体を消す処理
                if (instanceID == 0 && _HideSelf > 0.5)
                {
                    v.vertex.xyz = float3(0, -9999, 0);
                }
                else
                {
                    // 並べる
                    v.vertex.x += offsetX;
                    v.vertex.z += offsetZ;

                    // 回転して中央を見る
                    float lookAtAngle = atan2(-v.vertex.x, -v.vertex.z);
                    v.vertex.xyz = rotateY(v.vertex.xyz, lookAtAngle);
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