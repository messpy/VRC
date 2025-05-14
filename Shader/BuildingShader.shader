Shader "Custom/BuildingWindows"
{
    Properties
    {
        _BaseColor("Base Color", Color) = (0.1, 0.1, 0.1, 1.0) // ビルの基礎色
        _WindowColor1("Window Color 1", Color) = (1.0, 1.0, 0.8, 1.0) // 窓の色1
        _WindowColor2("Window Color 2", Color) = (0.5, 0.5, 0.5, 1.0) // 窓の色2
        _WindowWidth("Window Width", Float) = 0.1 // 窓の幅
        _WindowHeight("Window Height", Float) = 0.2 // 窓の高さ
        _Spacing("Spacing", Float) = 0.02 // 窓の間隔
        _CornerOffset("Corner Offset", Float) = 0.05 // 窓を角から離すオフセット
        _MinHeight("Min Height", Float) = 0.0 // 窓を適用する最小Y軸
        _MaxHeight("Max Height", Float) = 1.0 // 窓を適用する最大Y軸
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
                float4 vertex : POSITION; // 頂点の位置
                float3 normal : NORMAL; // 法線ベクトル
                float2 uv : TEXCOORD0; // UV座標
            };

            struct v2f
            {
                float2 uv : TEXCOORD0; // UV座標
                float4 vertex : SV_POSITION; // クリップ空間での頂点位置
                float3 worldPos : TEXCOORD1; // ワールド座標
                float3 worldNormal : TEXCOORD2; // ワールド空間の法線ベクトル
            };

            float4 _BaseColor; // ビルの基礎色
            float4 _WindowColor1; // 窓の色1
            float4 _WindowColor2; // 窓の色2
            float _WindowWidth; // 窓の幅
            float _WindowHeight; // 窓の高さ
            float _Spacing; // 窓の間隔
            float _CornerOffset; // 窓を角から離すオフセット
            float _MinHeight; // 窓を適用する最小Y軸
            float _MaxHeight; // 窓を適用する最大Y軸

            // 擬似乱数生成関数
            float hash(float2 p)
            {
                return frac(sin(dot(p, float2(12.9898, 78.233))) * 43758.5453);
            }

            v2f vert(appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex); // 頂点をクリップ空間に変換
                o.uv = v.uv; // UV座標をそのまま渡す
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz; // ワールド座標を計算
                o.worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal)); // 法線をワールド空間に変換
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                // 法線のY成分を使用して垂直方向を判定
                if (abs(i.worldNormal.y) > 0.3) // 垂直方向でない場合
                {
                    return _BaseColor; // 基礎色を返す
                }

                // ワールド座標のY軸が範囲外の場合、基礎色を返す
                if (i.worldPos.y < _MinHeight || i.worldPos.y > _MaxHeight)
                {
                    return _BaseColor; // ビルの基礎色
                }

                // UV座標をローカル空間に変換し、角からのオフセットを適用
                float2 adjustedUV = frac(i.uv - _CornerOffset); // UV座標を0-1の範囲に収める
                float2 gridUV = frac(adjustedUV / float2(_WindowWidth + _Spacing, _WindowHeight + _Spacing));

                // 窓の描画条件
                if (gridUV.x < _WindowWidth && gridUV.y < _WindowHeight)
                {
                    // ランダム値を生成して窓の色を切り替え
                    float randomValue = hash(floor(adjustedUV / float2(_WindowWidth + _Spacing, _WindowHeight + _Spacing)));
                    if (randomValue > 0.5)
                    {
                        return _WindowColor1; // 窓の色1
                    }
                    else
                    {
                        return _WindowColor2; // 窓の色2
                    }
                }
                else
                {
                    return _BaseColor; // ビルの基礎色
                }
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
