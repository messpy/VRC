Shader "metaaaaaaaaaaaa/Aligned"
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

        Pass // 並列コピー
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
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _Dist;
            float _Spacing;
            fixed4 _Color;

            v2f vert (appdata v, uint id : SV_VertexID)
            {
                v2f o;
                
                // 5人ずつ並べるためのID計算
                int index = id % 5;  // 0~4 のループ
                int row = id / 5;  // 0 or 1（前列 or 後列）

                // 配置調整
                float offsetX = (index - 2) * _Spacing; // 横に5人並ぶ
                float offsetZ = (row == 0) ? -_Dist : _Dist; // 前列は手前、後列は奥に配置

                // 位置を適用
                v.vertex.x += offsetX;
                v.vertex.z += offsetZ;

                // **向きの調整**
                if (row == 0)
                {
                    v.vertex.z -= 0.2; // 手前グループは少し奥に移動（誤差補正）
                }
                else
                {
                    v.vertex.z += 0.2; // 奥グループは少し手前に移動（誤差補正）
                }

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