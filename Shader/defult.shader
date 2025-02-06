Shader "カテゴリ名/シェーダー名"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {} // テクスチャ（画像）
        _Color ("Color", Color) = (1,1,1,1) // 色
    }

    SubShader
    {
        Pass
        {
            CGPROGRAM
            #pragma vertex vert   // 頂点シェーダー
            #pragma fragment frag // フラグメントシェーダー
            ENDCG
        }
    }
}