Shader "Custom/HollowCircleShader"
{
    Properties
    {
        _Color ("Color", Color) = (1,1,1,1)
        _MainTex ("Texture", 2D) = "white" {}
        _Scale ("Scale", Float) = 1.0
        _InnerRadius ("Inner Radius", Float) = 0.25
        _MaskX ("Mask X", Range(0,1)) = 1.0 // 마스킹 범위 추가
        _MaskY ("Mask Y", Range(0,1)) = 1.0 // 마스킹 범위 추가
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha // 알파 블렌딩 설정

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "UnityCG.cginc"

            struct appdata_t
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
            float4 _Color;
            float _Scale;
            float _InnerRadius; // 내부 반지름 변수 추가
            float _MaskX; // 마스킹 범위 변수 추가
            float _MaskY; // 마스킹 범위 변수 추가

            v2f vert (appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            half4 frag (v2f i) : SV_Target
            {
                float2 uv = (i.uv - 0.5) * _Scale + 0.5;
                float dist = length(uv * 2.0 - 1.0);
                half4 color = _Color;

                // 반대로 마스킹 범위 내에서만 색상 적용
                if (uv.x >= (1.0 - _MaskX) && uv.y >= (1.0 - _MaskY))
                {
                    // 내부 반지름과 외부 반지름 사이에서만 색상을 적용
                    if (dist > _InnerRadius && dist <= 0.5)
                    {
                        color.a *= smoothstep(0.5, 0.495, dist);
                    }
                    else
                    {
                        color.a = 0; // 이외의 픽셀은 투명하게 처리
                    }
                }
                else
                {
                    color.a = 0; // 마스킹 범위를 벗어난 픽셀은 투명하게 처리
                }

                return color;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
