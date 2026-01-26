Shader "UI/TextureBlur"
{
    Properties
    {
        [PerRendererData] _MainTex ("Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)
        _BlurSize ("Blur Size", Range(0, 10)) = 3.0
        _Brightness ("Brightness", Range(0, 1)) = 0.5

        _StencilComp ("Stencil Comparison", Float) = 8
        _Stencil ("Stencil ID", Float) = 0
        _StencilOp ("Stencil Operation", Float) = 0
        _StencilWriteMask ("Stencil Write Mask", Float) = 255
        _StencilReadMask ("Stencil Read Mask", Float) = 255
        _ColorMask ("Color Mask", Float) = 15
    }

    SubShader
    {
        Tags
        {
            "Queue"="Transparent"
            "IgnoreProjector"="True"
            "RenderType"="Transparent"
            "PreviewType"="Plane"
            "CanUseSpriteAtlas"="True"
        }

        Stencil
        {
            Ref [_Stencil]
            Comp [_StencilComp]
            Pass [_StencilOp]
            ReadMask [_StencilReadMask]
            WriteMask [_StencilWriteMask]
        }

        Cull Off
        Lighting Off
        ZWrite Off
        ZTest [unity_GUIZTestMode]
        Blend SrcAlpha OneMinusSrcAlpha
        ColorMask [_ColorMask]

        Pass
        {
            Name "Default"
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"
            #include "UnityUI.cginc"

            #pragma multi_compile_local _ UNITY_UI_CLIP_RECT
            #pragma multi_compile_local _ UNITY_UI_ALPHACLIP

            struct appdata_t
            {
                float4 vertex   : POSITION;
                float4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct v2f
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 worldPosition : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            sampler2D _MainTex;
            fixed4 _Color;
            fixed4 _TextureSampleAdd;
            float4 _ClipRect;
            float4 _MainTex_ST;
            float4 _MainTex_TexelSize;
            float _BlurSize;
            float _Brightness;

            v2f vert(appdata_t v)
            {
                v2f OUT;
                UNITY_SETUP_INSTANCE_ID(v);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);
                OUT.worldPosition = v.vertex;
                OUT.vertex = UnityObjectToClipPos(OUT.worldPosition);
                OUT.texcoord = TRANSFORM_TEX(v.texcoord, _MainTex);
                OUT.color = v.color * _Color;
                return OUT;
            }

            fixed4 frag(v2f IN) : SV_Target
            {
                float2 uv = IN.texcoord;
                float2 texelSize = _MainTex_TexelSize.xy;

                // Multi-layer Gaussian blur (더 부드러운 블러)
                fixed4 color = fixed4(0, 0, 0, 0);
                float totalWeight = 0;

                // 3단계 레이어로 블러 적용
                for (int layer = 1; layer <= 3; layer++)
                {
                    float offset = _BlurSize * layer * 0.5;
                    float weight = 1.0 / layer; // 가까울수록 가중치 높음

                    // 8방향 샘플링
                    color += tex2D(_MainTex, uv + float2(texelSize.x * offset, 0)) * weight;
                    color += tex2D(_MainTex, uv + float2(-texelSize.x * offset, 0)) * weight;
                    color += tex2D(_MainTex, uv + float2(0, texelSize.y * offset)) * weight;
                    color += tex2D(_MainTex, uv + float2(0, -texelSize.y * offset)) * weight;
                    color += tex2D(_MainTex, uv + float2(texelSize.x * offset * 0.707, texelSize.y * offset * 0.707)) * weight * 0.7;
                    color += tex2D(_MainTex, uv + float2(-texelSize.x * offset * 0.707, texelSize.y * offset * 0.707)) * weight * 0.7;
                    color += tex2D(_MainTex, uv + float2(texelSize.x * offset * 0.707, -texelSize.y * offset * 0.707)) * weight * 0.7;
                    color += tex2D(_MainTex, uv + float2(-texelSize.x * offset * 0.707, -texelSize.y * offset * 0.707)) * weight * 0.7;

                    totalWeight += weight * 4 + weight * 0.7 * 4;
                }

                // 중심 픽셀 (가장 높은 가중치)
                color += tex2D(_MainTex, uv) * 2.0;
                totalWeight += 2.0;

                color /= totalWeight;

                // Apply brightness
                color.rgb *= _Brightness;

                color = (color + _TextureSampleAdd) * IN.color;

                #ifdef UNITY_UI_CLIP_RECT
                color.a *= UnityGet2DClipping(IN.worldPosition.xy, _ClipRect);
                #endif

                #ifdef UNITY_UI_ALPHACLIP
                clip (color.a - 0.001);
                #endif

                return color;
            }
            ENDCG
        }
    }
}