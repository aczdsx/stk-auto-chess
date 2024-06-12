Shader "Custom/UI_HexTransition"{
    Properties
    {
        _CircleRadius ("Circle Radius", Float) = 0.5
        _EdgeSoftness ("Edge Softness", Float) = 0.05
        _DotMinScale ("Dot Min Scale", Float) = 0.01
        _DotMaxScale ("Dot Max Scale", Float) = 0.1
        _Tiling ("Tiling", Float) = 10.0
        _Spacing ("Dot Spacing", Float) = 1.0
        _DotColor ("Dot Color", Color) = (1,1,1,1)
        _Invert ("Invert Effect", Range(0,1)) = 0
    }
    SubShader
    {
        Tags { "Queue"="Transparent" "RenderType"="Transparent" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off

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
                float4 pos : SV_POSITION;
            };

            float _CircleRadius;
            float _EdgeSoftness;
            float _DotMinScale;
            float _DotMaxScale;
            float _Tiling;
            float _Spacing;
            float4 _DotColor;
            float _Invert;

            v2f vert (appdata v)
            {
                v2f o;
                o.uv = v.uv;
                o.pos = UnityObjectToClipPos(v.vertex);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                // Normalize and center the UV coordinates around the middle of the viewport
                float2 uv = (i.uv - 0.5) * 2.0;

                // Calculate the distance from the center
                float dist = length(uv);

                // Generate the soft edge circle
                float circle = 1.0 - smoothstep(_CircleRadius - _EdgeSoftness, _CircleRadius, dist);

                // Optionally invert the circle effect
                if (_Invert > 0.5) circle = 1.0 - circle;

                // Calculate the dot size based on the circle value
                float dotSize = lerp(_DotMaxScale, _DotMinScale, circle);

                // Apply the dot pattern
                float2 gridPos = floor(uv * _Tiling);
                float2 gridUV = frac(uv * _Tiling);
                float dotPattern = smoothstep(dotSize, dotSize - 0.01, length(gridUV - 0.5));

                // Set the dot color
                float4 color = _DotColor * dotPattern;
                color.a = dotPattern * _DotColor.a; // Apply the pattern and use _DotColor's alpha

                return color;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
