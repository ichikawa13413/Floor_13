Shader "Unlit/OutlineShader"
{
    // �C���X�y�N�^�[�ɕ\�������v���p�e�B
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _OutlineColor ("Outline Color", Color) = (1, 0, 0, 1) // �A�E�g���C���̐F (�f�t�H���g�͐�)
        _OutlineWidth ("Outline Width", Range(0, 0.1)) = 0.02 // �A�E�g���C���̑���
    }
    SubShader
    {
        Tags { "RenderType"="Opaque" }
        LOD 100

        // �p�X1: �A�E�g���C���̕`�� (�������g�債�ĕ`��)
        Pass
        {
            Cull Front // �|���S���́u�\�v����`�悵�Ȃ��i���������`�悷��j

            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            
            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL; // �@���x�N�g��
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
                // ���_�ʒu��@�������ɏ������������o�����ƂŊg�傷��
                v.vertex.xyz += v.normal * _OutlineWidth;
                o.vertex = UnityObjectToClipPos(v.vertex);
                return o;
            }
            
            fixed4 frag (v2f i) : SV_Target
            {
                // �A�E�g���C���̐F��Ԃ�
                return _OutlineColor;
            }
            ENDCG
        }

        // �p�X2: �ʏ�̃I�u�W�F�N�g�`��
        Pass
        {
            Cull Back // �|���S���́u���v����`�悵�Ȃ��i�ʏ�̕`��j

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
                // �e�N�X�`���̐F��Ԃ�
                fixed4 col = tex2D(_MainTex, i.uv);
                return col;
            }
            ENDCG
        }
    }
}