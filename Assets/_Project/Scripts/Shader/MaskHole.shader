Shader "Custom/MaskHole"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _HoleCenter ("Hole Center", Vector) = (0.5, 0.5, 0)
        _HoleRadius ("Hole Radius", Float) = 0.2
        _HoleCenter2 ("Hole Center 2", Vector) = (0.5, 0.5, 0)
        _HoleRadius2 ("Hole Radius 2", Float) = 0.0
        _AspectRatio ("Aspect Ratio", Float) = 1.0
        _SmoothWidth ("Smooth Width", Range(0, 0.5)) = 0.1
        _MaskAlpha ("Mask Alpha", Range(0, 1)) = 1.0
    }
    SubShader
    {
        Tags
        {
            "RenderType"="Transparent"
            "Queue"="Transparent" // UI는 주로 Transparent Queue 사용
        }

        // UI에서 일반적으로 사용되는 설정
        ZWrite Off
        ZTest Always
        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off

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
                float4 color : COLOR; // Vertex color
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float3 _HoleCenter;
            float _HoleRadius;
            float3 _HoleCenter2;
            float _HoleRadius2;
            float _AspectRatio;
            float _SmoothWidth;
            float _MaskAlpha;

            v2f vert(appdata_t v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                o.color = v.color;
                return o;
            }

            fixed4 frag(v2f i) : SV_Target
            {
                float2 adjustedUV = float2(i.uv.x, i.uv.y / _AspectRatio);

                // 첫 번째 구멍
                float2 adjustedCenter1 = float2(_HoleCenter.x, _HoleCenter.y / _AspectRatio);
                float dist1 = distance(adjustedUV, adjustedCenter1);
                float alpha1 = smoothstep(_HoleRadius - _SmoothWidth, _HoleRadius, dist1);

                // 두 번째 구멍
                float2 adjustedCenter2 = float2(_HoleCenter2.x, _HoleCenter2.y / _AspectRatio);
                float dist2 = distance(adjustedUV, adjustedCenter2);
                float alpha2 = smoothstep(_HoleRadius2 - _SmoothWidth, _HoleRadius2, dist2);

                // 두 구멍 결합 (둘 중 하나라도 구멍이면 투명)
                float alpha = min(alpha1, alpha2);

                // _MaskAlpha 적용 (0 = 마스크 투명/전체 보임, 1 = 마스크 불투명/구멍만 보임)
                alpha *= _MaskAlpha;

                fixed4 texColor = tex2D(_MainTex, i.uv) * i.color;
                texColor.a *= alpha;
                return texColor;
            }
            ENDCG
        }
    }
}
