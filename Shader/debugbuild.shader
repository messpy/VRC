Shader "Custom/DebugBuildingShader"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.1, 0.1, 0.1, 1) // ビルの基礎色
        _WindowColor("Window Color", Color) = (1.0, 1.0, 0.8, 1) // 窓の色
        _WindowSize("Window Size", Vector) = (0.15, 0.3, 0, 0) // 窓の幅・高さ
        _WindowSpacing("Window Spacing", Vector) = (0.15, 0.15, 0, 0) // 窓と窓の間隔
        _EdgeThreshold("Edge Threshold", Float) = 0.1 // 面の縁からの除外距離
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 200

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
            };

            float4 _BaseColor;
            float4 _WindowColor;
            float4 _WindowSize;
            float4 _WindowSpacing;
            float _EdgeThreshold;

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 法線を計算
                float3 worldNormal = normalize(cross(ddx(i.worldPos), ddy(i.worldPos)));

                // デバッグ用: 法線を色で可視化
                float3 normalColor = worldNormal * 0.5 + 0.5;

                // デバッグ用: 高さを色で可視化 (高さを 0～10 の範囲で正規化)
                float heightColor = saturate((i.worldPos.y - 0.0) / 10.0);

                // デバッグ用: 窓の範囲を色で可視化
                float2 cellSize = _WindowSize.xy + _WindowSpacing.xy;
                float2 posXY = float2(i.worldPos.x, i.worldPos.y);
                float2 cellPos = fmod(posXY + 1000.0, cellSize);
                bool insideWindow = cellPos.x < _WindowSize.x && cellPos.y < _WindowSize.y;

                // 窓の範囲を赤色、基礎色を青色、法線を緑色で可視化
                if (insideWindow)
                {
                    return fixed4(1.0, 0.0, 0.0, 1.0); // 窓の範囲は赤
                }
                else
                {
                    return fixed4(normalColor.r, heightColor, normalColor.b, 1.0); // 法線と高さを混ぜた色
                }
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
