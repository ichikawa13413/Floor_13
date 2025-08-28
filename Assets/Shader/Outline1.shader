Shader "Unlit/OutlineShader"
{
    // インスペクターに表示されるプロパティ
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (1, 0, 0, 1) // アウトラインの色 (デフォルトは赤)
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.02 // アウトラインの太さ
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        // パス1: アウトラインの描画 (裏側を拡大して描画)
        Pass
        {
            Cull Front // ポリゴンの「表」側を描画しない（裏側だけ描画する）

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL; // 法線ベクトル
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
            };

            float _OutlineWidth;
            fixed4 _OutlineColor;

            v2f vert (appdata v)
            {
                v2f o;
                // 頂点位置を法線方向に少しだけ押し出すことで拡大する
                v.vertex.xyz += v.normal * _OutlineWidth;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // アウトラインの色を返す
                return _OutlineColor;
            }
            ENDCG
        }

        // パス2: 通常のオブジェクト描画
        Pass
        {
            Cull Back // ポリゴンの「裏」側を描画しない（通常の描画）

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            
            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // テクスチャの色を返す
                fixed4 col = tex2D(_MainTex, i.uv);
                return col;
            }
            ENDCG
        }
    }
}