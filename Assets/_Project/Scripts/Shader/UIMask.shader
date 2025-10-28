Shader "Custom/MaskHole"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        _HoleCenter ("Hole Center", Vector) = (0.5, 0.5, 0)
        _HoleRadius ("Hole Radius", Float) = 0.2
        _AspectRatio ("Aspect Ratio", Float) = 1.0
        _SmoothWidth ("Smooth Width", Range(0, 0.5)) = 0.1
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
            float _AspectRatio;
            float _SmoothWidth;

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
                float2 adjustedCenter = float2(_HoleCenter.x, _HoleCenter.y / _AspectRatio);

                float dist = distance(adjustedUV, adjustedCenter);
                float alpha = smoothstep(_HoleRadius - _SmoothWidth, _HoleRadius, dist);
                
                fixed4 texColor = tex2D(_MainTex, i.uv) * i.color;
                texColor.a *= alpha;
                return texColor;
            }
            ENDCG
        }
    }
}
