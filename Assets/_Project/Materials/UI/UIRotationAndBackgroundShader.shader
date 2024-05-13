Shader "Custom/UIRotationAndBackgroundShader"
{
    Properties
    {
        _BackgroundTex ("Background Texture", 2D) = "white" {}
        _Color ("Background Color", Color) = (1,1,1,1)
        _RotateTex ("Rotate Texture", 2D) = "white" {}
        _RotationSpeed ("Rotation Speed", Float) = 1.0
        _Scale ("Texture Scale", Float) = 1.0
        _BlendIntensity ("Blend Intensity", Float) = 1.0
        _ExtraTex ("Extra Texture", 2D) = "white" {}
        _ExtraColor ("Extra Texture Color", Color) = (1,1,1,1)
        _ExtraTex_ST ("Extra Texture Tiling and Offset", Vector) = (1,1,0,0)
        _ExtraTexScale ("Extra Texture Scale", Float) = 1.0
        _MaskRadius ("Mask Radius", Float) = 0.5  // 원형 마스크의 반지름
    }
    SubShader
    {
        Tags { "Queue" = "Transparent" }

        Pass
        {
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
                float2 uvRotate : TEXCOORD1;
                float2 uvExtra : TEXCOORD2;
                float4 vertex : SV_POSITION;
            };

            sampler2D _BackgroundTex;
            sampler2D _RotateTex;
            sampler2D _ExtraTex;
            float _RotationSpeed;
            float4 _Color;
            float _Scale;
            float _BlendIntensity;
            float4 _ExtraColor;
            float4 _ExtraTex_ST;
            float _ExtraTexScale;
            float _MaskRadius;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                
                float angle = _Time.y * _RotationSpeed;
                float cosA = cos(angle);
                float sinA = sin(angle);
                float2 centeredUV = (v.uv - 0.5) * _Scale;
                o.uvRotate = float2(
                    cosA * centeredUV.x - sinA * centeredUV.y,
                    sinA * centeredUV.x + cosA * centeredUV.y
                ) + 0.5;

                o.uvExtra = (o.uv - 0.5) * _ExtraTexScale + 0.5;
                o.uvExtra = o.uvExtra * _ExtraTex_ST.xy + _ExtraTex_ST.zw;

                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 center = float2(0.5, 0.5);
                float dist = length(i.uvRotate - center);
                float mask = dist < _MaskRadius ? 1.0 : 0.0; // 선명한 마스크 경계

                fixed4 backgroundCol = tex2D(_BackgroundTex, i.uv) * _Color;
                fixed4 rotateCol = tex2D(_RotateTex, i.uvRotate) * mask; // 마스크 적용
                fixed4 extraCol = tex2D(_ExtraTex, i.uvExtra) * _ExtraColor;

                fixed4 result = lerp(lerp(backgroundCol, rotateCol, _BlendIntensity), extraCol, extraCol.a);
                return result;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
