Shader "Custom/HighlightShader"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _Color ("Main Color", Color) = (1,1,1,1)
        _HighlightColor ("Highlight Color", Color) = (1,1,0,1)
        _OutlineWidth ("Outline Width", Range(0.0, 0.1)) = 0.02
        _GlowIntensity ("Glow Intensity", Range(0.0, 5.0)) = 2.0
        _PulseSpeed ("Pulse Speed", Range(0.0, 10.0)) = 2.0
        _EnableHighlight ("Enable Highlight", Range(0,1)) = 0
    }

    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        // パス1: アウトライン描画
        Pass
        {
            Name "Outline"
            Cull Front
            ZWrite Off
            ZTest LEqual
            Blend SrcAlpha OneMinusSrcAlpha

            HLSLPROGRAM
            #pragma vertex VertOutline
            #pragma fragment FragOutline
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            CBUFFER_START(UnityPerMaterial)
                float4 _HighlightColor;
                float _OutlineWidth;
                float _GlowIntensity;
                float _PulseSpeed;
                float _EnableHighlight;
            CBUFFER_END

            Varyings VertOutline(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                // 法線方向に頂点を押し出してアウトライン作成
                float3 normalWS = TransformObjectToWorldNormal(input.normalOS);
                float3 positionWS = TransformObjectToWorld(input.positionOS.xyz);
                positionWS += normalWS * _OutlineWidth;
                
                output.positionCS = TransformWorldToHClip(positionWS);
                return output;
            }

            half4 FragOutline(Varyings input) : SV_Target
            {
                // 時間によるパルス効果
                float pulse = sin(_Time.y * _PulseSpeed) * 0.3 + 0.7;
                float4 outlineColor = _HighlightColor;
                outlineColor.rgb *= _GlowIntensity * pulse;
                outlineColor.a *= _EnableHighlight;
                
                return outlineColor;
            }
            ENDHLSL
        }

        // パス2: メインオブジェクト描画
        Pass
        {
            Name "Forward"
            Tags { "LightMode" = "UniversalForward" }
            Blend SrcAlpha OneMinusSrcAlpha
            ZWrite On
            ZTest LEqual

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS
            #pragma multi_compile _ _MAIN_LIGHT_SHADOWS_CASCADE
            #pragma multi_compile _ _SHADOWS_SOFT

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float3 normalOS : NORMAL;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float2 uv : TEXCOORD0;
                float4 positionCS : SV_POSITION;
                float3 positionWS : TEXCOORD1;
                float3 normalWS : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _Color;
                float4 _HighlightColor;
                float _OutlineWidth;
                float _GlowIntensity;
                float _PulseSpeed;
                float _EnableHighlight;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);

                output.positionWS = TransformObjectToWorld(input.positionOS.xyz);
                output.positionCS = TransformWorldToHClip(output.positionWS);
                output.normalWS = TransformObjectToWorldNormal(input.normalOS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                half4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                half4 mainColor = texColor * _Color;

                // ライティング計算
                Light mainLight = GetMainLight();
                float3 lightDir = normalize(mainLight.direction);
                float3 normalWS = normalize(input.normalWS);
                float NdotL = saturate(dot(normalWS, lightDir));
                
                half3 lighting = mainLight.color * NdotL + SampleSH(normalWS) * 0.3;
                mainColor.rgb *= lighting;

                // ハイライトが有効な場合、わずかに明るくする
                if (_EnableHighlight > 0.5)
                {
                    mainColor.rgb += _HighlightColor.rgb * 0.1;
                }

                return mainColor;
            }
            ENDHLSL
        }
    }
    FallBack "Sprites/Default"
}