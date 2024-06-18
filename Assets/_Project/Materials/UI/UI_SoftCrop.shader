Shader "Custom/UI_SoftCrop"
{
    Properties
    {
        _MainTex ("Texture", 2D) = "white" {}
        [Header(Left Crop)]
        [Space(10)]
        _LeftX ("Left X", Range(-1, 1)) = 0.0
        _SoftnessLeft ("Softness Left", Range(-1, 1)) = 0.1
        _LeftAlpha ("Left Alpha", Range(0, 1)) = 1.0
        [Space(10)]
        [Header(Right Crop)]
        [Space(10)]
        _RightX ("Right X", Range(-1, 1)) = 1.0
        _SoftnessRight ("Softness Right", Range(-1, 1)) = 0.1
        _RightAlpha ("Right Alpha", Range(0, 1)) = 1.0
        [Space(10)]
        [Header(Global Alpha)]
        [Space(10)]
        _GlobalAlpha ("Global Alpha", Range(0, 1)) = 1.0
    }
    SubShader
    {
        Tags { "Queue"="Overlay" "RenderType"="Transparent" }
        Blend SrcAlpha OneMinusSrcAlpha
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
                float4 pos : POSITION;
                float2 uv : TEXCOORD0;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float _LeftX;
            float _RightX;
            float _SoftnessLeft;
            float _SoftnessRight;
            float _LeftAlpha;
            float _RightAlpha;
            float _GlobalAlpha;

            v2f vert (appdata_t v)
            {
                v2f o;
                o.pos = UnityObjectToClipPos(v.vertex);
                o.uv = TRANSFORM_TEX(v.uv, _MainTex);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                fixed4 color = tex2D(_MainTex, i.uv);

                float leftEdge = _LeftX;
                float rightEdge = _RightX;
                float softnessLeft = _SoftnessLeft * 0.5;
                float softnessRight = _SoftnessRight * 0.5;
                float leftAlpha = _LeftAlpha;
                float rightAlpha = _RightAlpha;
                float globalAlpha = _GlobalAlpha;

                float alpha = 1.0;
                if (i.uv.x < leftEdge)
                {
                    alpha = lerp(leftAlpha, 1.0, smoothstep(leftEdge - softnessLeft, leftEdge, i.uv.x));
                }
                else if (i.uv.x > rightEdge)
                {
                    alpha = lerp(rightAlpha, 1.0, smoothstep(rightEdge + softnessRight, rightEdge, i.uv.x));
                }

                color.a *= alpha * globalAlpha;

                return color;
            }
            ENDCG
        }
    }
}
