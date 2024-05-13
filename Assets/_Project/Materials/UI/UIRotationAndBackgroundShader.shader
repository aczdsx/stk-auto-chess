Shader "Custom/UIRotationAndBackgroundShader"
{
    Properties
    {
        _BackgroundTex ("Background Texture", 2D) = "white" {}
        _Color ("Background Color", Color) = (1,1,1,1)
        _RotateTex ("Rotate Texture", 2D) = "white" {}
        _RotationSpeed ("Rotation Speed", Float) = 1.0
        _Scale ("Texture Scale", Float) = 1.0
        _BlendIntensity ("Blend Intensity", Float) = 1.0 // Soft Light 효과의 강도
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
                float4 vertex : SV_POSITION;
            };

            sampler2D _BackgroundTex;
            sampler2D _RotateTex;
            float _RotationSpeed;
            float4 _Color;
            float _Scale;
            float _BlendIntensity; // Soft Light 효과의 강도를 저장하는 변수

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.uvRotate = v.uv;

                float angle = _Time.y * _RotationSpeed;
                float cosA = cos(angle);
                float sinA = sin(angle);
                float2 centeredUV = (v.uv - 0.5) * _Scale;
                o.uvRotate = float2(
                    cosA * centeredUV.x - sinA * centeredUV.y,
                    sinA * centeredUV.x + cosA * centeredUV.y
                ) + 0.5;

                return o;
            }

            fixed4 SoftLightBlend(fixed4 base, fixed4 blend)
            {
                fixed4 result;
                result.rgb = lerp(
                    sqrt(base.rgb) * (2.0 * blend.rgb) + base.rgb * (1.0 - 2.0 * blend.rgb),
                    1.0 - (1.0 - base.rgb) * (1.0 - blend.rgb * 2.0),
                    step(0.5, blend.rgb)
                );
                result.a = base.a; // Alpha channel remains unaffected
                return result;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 backgroundCol = tex2D(_BackgroundTex, i.uv) * _Color;
                fixed4 rotateCol = tex2D(_RotateTex, i.uvRotate);
                fixed4 softLightResult = SoftLightBlend(backgroundCol, rotateCol);

                // Apply soft light blending with adjustable intensity
                return lerp(backgroundCol, softLightResult, _BlendIntensity);
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
