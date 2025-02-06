Shader "metaaaaaaaaaaaa/ParallelRows_Fixed"
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

        Pass // 正しく5人×2列を配置
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_fog
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
                float2 uv : TEXCOORD0;
                uint id : SV_VertexID;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Dist;
            float _Spacing;
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
                
                // インスタンス単位で処理する
                int instanceID = id / 4; // 各オブジェクト単位のID
                int rowID = instanceID % 5; // 0~4（並ぶ位置）
                int sideID = instanceID / 5; // 0 = 手前, 1 = 奥

                float offsetX = (rowID - 2) * _Spacing; // 横に5人並ぶ
                float offsetZ = (sideID == 0) ? -_Dist : _Dist; // 手前と奥のグループ分け

                // メッシュ全体を移動
                float3 basePos = v.vertex.xyz;
                basePos.x += offsetX;
                basePos.z += offsetZ;

                // 角度調整（向かい合わせる）
                if (sideID == 1)
                {
                    basePos = rotateY(basePos, 3.14159); // 180度回転
                }

                v.vertex.xyz = basePos;

                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 col = tex2D(_MainTex, i.uv);
                return col * _Color;
            }
            ENDCG
        }
    }
}