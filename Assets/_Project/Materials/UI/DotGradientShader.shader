Shader "Custom/DotGradientShader"
{
    Properties
    {
        
        _MainTex ("Texture", 2D) = "white" {}
        [Header(Dot Properties)]
        [Space(10)]
        _DotColor ("Dot Color", Color) = (1, 1, 1, 1) // Default to white
        _DotSize ("Dot Size", Range(0.01, 1.0)) = 0.1
        [Space(20)]
        [Header(Gradient Properties)]
        [Space(10)]
         _GradientScale ("Gradient Scale", Range(-2, 2)) = 1.0
        _GradientDirection ("Gradient Direction", Vector) = (0, 1, 0, 0) // (x, y) = (0, 1) for vertical, (1, 0) for horizontal
        _FlipGradient ("Flip Gradient", int) = 0 // 0: no flip, 1: flip
        _Speed ("Speed", Range(0.1, 10.0)) = 1.0
        _Tiling ("Tiling", Vector) = (1, 1, 0, 0)
       
        
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Overlay-1"}
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Alphatest Greater 0

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
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _DotSize;
            float _Speed;
            float4 _Tiling;
            float _GradientScale;
            float4 _GradientDirection;
            int _FlipGradient;
            float4 _DotColor;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float2 uv = i.uv * _Tiling.xy;
                uv.x += _Time.y * _Speed; // Use _Time.y for time in seconds
                uv = frac(uv); // Wrap UV coordinates

                float2 center = frac(uv * _Tiling.xy) - 0.5; // Compute the distance from the center of each tile
                float dist = length(center);

                float alpha = smoothstep(_DotSize, _DotSize - 0.01, dist);

                // Apply overall gradient mask
                float2 gradientUV = i.uv * _GradientScale;
                float gradient;

                if (_GradientDirection.y == 1.0) // Vertical gradient
                {
                    gradient = gradientUV.y;
                }
                else // Horizontal gradient
                {
                    gradient = gradientUV.x;
                }

                if (_FlipGradient == 1)
                {
                    gradient = 1.0 - gradient;
                }

                // Use smoothstep to create a soft gradient mask
                gradient = smoothstep(0.0, 1.0, gradient);

                float finalAlpha = alpha * gradient;

                // Apply the dot color
                float4 dotColor = _DotColor * finalAlpha;

                return dotColor; // Return the dot color with calculated alpha
            }
            ENDCG
        }
    }
}
