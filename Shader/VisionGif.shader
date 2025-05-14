Shader "HoliShader/VisionGif"
{
    Properties
    {
        [Header(Common)]
        _Textures ("Texture Array",  2DArray) = "white" {}
        [HDR] _Color("Color",Color) = (1, 1, 1, 1)
        _FrameDuration ("Frame Duration (seconds)", Float) = 0.1 // フレーム切り替え時間
        [Enum(UnityEngine.Rendering.BlendMode)]
        _SrcFactor("Src Factor", Float) = 5     // SrcAlpha
        [Enum(UnityEngine.Rendering.BlendMode)]
        _DstFactor("Dst Factor", Float) = 10    // OneMinusSrcAlpha

        [Space(30)]
        [Header(Playback Option)]
        [Toggle] _EnableManualTime ("Manual Time Operation", Float) = 0
        _ManualTime ("Playback Time", Float) = 0

        [Space(30)]
        [Header(Clipping Option)]
        [Toggle] _EnableGB ("Cutout GreenBack", Float) = 0
        _GBTh ("GreenBack Threshold", Range(0, 1)) = 0.1
        [KeywordEnum(None, Clamp, Clipping)] _CMode("Clipping Mode", Float) = 0

        [Space(30)]
        [Header(Distance Fade Option)]
        _NearFadeDistance ("Near Fade", Float) = 1
        _FarFadeDistance ("Far Fade", Float) = 10

        [Space(30)]
        [Header(Layout Option)]
        _UVx ("UV Shift X-Axis", Float) = 0
        _UVy ("UV Shift Y-Axis", Float) = 0
        _ScaleX ("Scale X-Axis", Float) = 1
        _ScaleY ("Scale Y-Axis", Float) = 1
    }
    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Overlay" }

        Blend[_SrcFactor][_DstFactor]
        Cull Off
        ZTest Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _CMODE_NONE _CMODE_CLAMP _CMODE_CLIPPING

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;

                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
                float3 viewPos : TEXCOORD1;
                float distanceFade :TEXCOORD2;

                UNITY_VERTEX_INPUT_INSTANCE_ID
                UNITY_VERTEX_OUTPUT_STEREO
            };

            UNITY_DECLARE_TEX2DARRAY(_Textures);
            uniform float4 _Textures_ST;
            uniform float4 _Color;
            uniform float _FrameDuration; // フレーム切り替え時間
            uniform float _EnableManualTime;
            uniform float _ManualTime;
            uniform float _EnableGB;
            uniform float _GBTh;
            uniform float _NearFadeDistance;
            uniform float _FarFadeDistance;
            uniform float _UVx;
            uniform float _UVy;
            uniform float _ScaleX;
            uniform float _ScaleY;

            uniform float _VRChatMirrorMode;

            // Rotate Function
            float2 rot(float2 p, float a) { return float2(p.x * cos(a) - p.y * sin(a), p.x * sin(a) + p.y * cos(a)); }

            bool IsInMirror()
            {
                return _VRChatMirrorMode != 0;
            }

            v2f vert (appdata v)
            {
                v2f o;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_OUTPUT(v2f, o);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(o);

                o.uv = v.uv;
                o.viewPos = float4(v.vertex.xyz, 1);

                #ifdef UNITY_SINGLE_PASS_STEREO
                    float3 CameraPos = 0.5 * (unity_StereoWorldSpaceCameraPos[0] + unity_StereoWorldSpaceCameraPos[1]);
                #else
                    float3 CameraPos = _WorldSpaceCameraPos;
                #endif
                float3 cp = mul(unity_ObjectToWorld, float4(0, 0, 0, 1)).xyz;
                float FadeDistance = distance(CameraPos, cp);
                if(FadeDistance > _FarFadeDistance || IsInMirror())
                {
                    o.vertex = float4(0, 0, 0, 1);
                    return o;
                }
                o.distanceFade = smoothstep(1, 0, (clamp(FadeDistance, _NearFadeDistance, _FarFadeDistance) - _NearFadeDistance) / (_FarFadeDistance - _NearFadeDistance));
                o.vertex = UnityViewToClipPos(o.viewPos);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                UNITY_SETUP_STEREO_EYE_INDEX_POST_VERTEX(i);

                float2 uv = i.viewPos.xy / -i.viewPos.z;

                // Layout
                uv *= float2(1 / _ScaleX, 1 / _ScaleY);
                uv += float2(_UVx, _UVy) + 0.5;

                // 範囲外切り落とし
                #ifdef _CMODE_CLIPPING
                    clip(0.5 - abs(uv.x - 0.5));
                    clip(0.5 - abs(uv.y - 0.5));
                #endif

                float width;
                float height;
                float totalFrames;
                _Textures.GetDimensions(width, height, totalFrames);

                // フレーム切り替え時間を使用して現在のフレームを計算
                uint pages = fmod((_EnableManualTime ? _ManualTime : _Time.y) / _FrameDuration, totalFrames);

                #ifdef _CMODE_CLAMP
                    uv = clamp(uv, 0 , 1);
                #endif

                fixed4 col = UNITY_SAMPLE_TEX2DARRAY(_Textures, float3(uv, pages));

                // GB処理
                if(_EnableGB) if(col.r < _GBTh && col.g > 1-_GBTh && col.b < _GBTh) discard;
                return _Color * col * float4(1, 1, 1, i.distanceFade);
            }
            ENDCG
        }
    }
}
