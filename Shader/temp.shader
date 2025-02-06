Shader "MyShader/ThermalVisionWithNoise"
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
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                
                // 照明の影響を取得
                float3 lightDir = normalize(_WorldSpaceLightPos0.xyz);
                o.lightIntensity = max(0, dot(o.worldNormal, lightDir));

                return o;
            }

            // サーモグラフィーの色を決める関数
            fixed4 getThermalColor(float intensity)
            {
                if (intensity > 0.8) return fixed4(1.0, 0.0, 0.0, 1.0); // 赤
                if (intensity > 0.6) return fixed4(1.0, 0.5, 0.0, 1.0); // オレンジ
                if (intensity > 0.4) return fixed4(1.0, 1.0, 0.0, 1.0); // 黄
                if (intensity > 0.2) return fixed4(0.0, 1.0, 1.0, 1.0); // 水色
                return fixed4(0.0, 0.0, 1.0, 1.0); // 青
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

                // ノイズを追加（UV座標を使って疑似ランダム値を生成）
                float noise = random(i.uv * _Time.y) * _NoiseStrength;
                thermalColor.rgb += noise;

                return thermalColor;
            }
            ENDCG
        }
    }
}