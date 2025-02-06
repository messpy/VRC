Shader "HoliShader/ThermalVisionWithNoise"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {} // 元のテクスチャ
        _NoiseStrength ("Noise Strength", Range(0, 1)) = 0.1 // ノイズの強さ
    }

    SubShader
    {
        Tags { "RenderType"="Opaque" }
        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "UnityCG.cginc"

            sampler2D _MainTex;
            float _NoiseStrength;

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
                float4 vertex : SV_POSITION;
                float lightIntensity : TEXCOORD2;
            };

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;

                // 法線をワールド空間に変換
                o.worldNormal = normalize(mul((float3x3)unity_ObjectToWorld, v.normal));

                // ライトの方向を計算（方向性ライトとして扱う）
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);

                // 照明の影響を計算
                o.lightIntensity = max(0.0, dot(o.worldNormal, lightDir));

                return o;
            }

            // サーモグラフィーの色を決める関数
            fixed4 getThermalColor(float intensity)
            {
                if (intensity > 0.85) return fixed4(1.0, 0.0, 0.0, 1.0); // 赤
                if (intensity > 0.70) return fixed4(1.0, 0.5, 0.0, 1.0); // オレンジ
                if (intensity > 0.55) return fixed4(1.0, 1.0, 0.0, 1.0); // 黄
                if (intensity > 0.40) return fixed4(0.0, 1.0, 1.0, 1.0); // 水色
                return fixed4(0.2, 0.2, 0.5, 1.0); // デフォルトを暗めの青紫に
            }

            // ノイズ生成関数
            float random(float2 uv)
            {
                return frac(sin(dot(uv, float2(12.9898, 78.233))) * 43758.5453);
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 texColor = tex2D(_MainTex, i.uv); // 元のテクスチャ

                // 明るさに応じて色を変える
                fixed4 thermalColor = getThermalColor(i.lightIntensity);

                // ノイズを追加（時間変化を追加し、UV座標の影響を調整）
                float noise = random(i.uv * (_Time.y * 0.1)) * _NoiseStrength;
                thermalColor.rgb += noise;

                // 色を加算して最終的な出力を決定
                return thermalColor;
            }
            ENDCG
        }
    }
}