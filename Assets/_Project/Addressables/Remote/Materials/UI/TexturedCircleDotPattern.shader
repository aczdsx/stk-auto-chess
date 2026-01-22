Shader "Custom/TexturedCircleDotPattern"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
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
        Tags { "Queue"="Transparent" "IgnoreProjector"="True" "RenderType"="Transparent" "PreviewType"="Plane" }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

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
                UNITY_FOG_COORDS(1)
            };

            sampler2D _MainTex;
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
                UNITY_TRANSFER_FOG(o,o.pos);
                return o;
            }

            float4 frag(v2f i) : SV_Target
            {
                // Normalize and center the UV coordinates around the middle of the viewport
                float2 uv = (i.uv - 0.5) * 2.0;
                float2 spacingUV = uv * _Spacing;

                // Calculate the distance from the center
                float dist = length(spacingUV);

                // Generate the soft edge circle
                float circle = 1.0 - smoothstep(_CircleRadius - _EdgeSoftness, _CircleRadius, dist);

                // Optionally invert the circle effect
                if (_Invert > 0.5) circle = 1.0 - circle;

                // Calculate the dot size based on the circle grayscale value
                float dotSize = lerp(_DotMinScale, _DotMaxScale, circle);

                // Apply the dot pattern
                float2 gridPos = floor(spacingUV * _Tiling);
                float2 gridUV = frac(spacingUV * _Tiling);
                float dotPattern = smoothstep(dotSize, dotSize - 0.01, length(gridUV - 0.5));

                // Texture sampling
                float4 texColor = tex2D(_MainTex, i.uv);

                // Combine texture color with dot color
                float4 color = texColor * _DotColor;
                color.a = dotPattern * circle; // Apply the pattern and circle alpha

                // Apply clipping for UI
                #ifdef UNITY_UI_CLIP
                color.a *= UnityGet2DClipping(i.uv);
                #endif

                return color;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}
