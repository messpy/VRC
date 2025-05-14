Shader "Custom/ProceduralBuildingWindows_Grid3"
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
                // 法線を計算して上向きの面を判定
                float3 worldNormal = normalize(cross(ddx(i.worldPos), ddy(i.worldPos)));
                if (worldNormal.y > 0.8) // Y方向に強く向いていたら屋上
                {
                    return _BaseColor; // 屋上は基礎色
                }

                // 窓を適用する高さの範囲を指定
                float minHeight = 2.0; // 窓を適用する最小高さ
                float maxHeight = 150.0; // 窓を適用する最大高さ
                if (i.worldPos.y < minHeight || i.worldPos.y > maxHeight)
                {
                    return _BaseColor; // 高さ範囲外は基礎色
                }

                // 窓のセルサイズを計算
                float2 cellSize = _WindowSize.xy + _WindowSpacing.xy;
                float2 posXY = float2(i.worldPos.x, i.worldPos.y);

                // fmodの負数対策: 大きな正数を足して正規化
                float2 cellPos = fmod(posXY + 10000.0, cellSize);

                // 窓の範囲内チェック
                bool insideWindow = cellPos.x < _WindowSize.x && cellPos.y < _WindowSize.y;

                // 面の縁からの距離を計算
                float edgeDistance = min(min(cellPos.x, cellPos.y), min(_WindowSize.x - cellPos.x, _WindowSize.y - cellPos.y));

                // 窓が範囲内かつ縁から十分離れている場合のみ描画
                if (insideWindow && edgeDistance > _EdgeThreshold)
                {
                    return _WindowColor; // 窓の色
                }

                // 窓の範囲外または縁に近い場合は基礎色
                return _BaseColor;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
