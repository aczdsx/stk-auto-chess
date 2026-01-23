Shader "Custom/SpriteShiny"
{
    Properties
    {
        [PerRendererData] _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Tint", Color) = (1,1,1,1)

        [Header(Shiny Effect)]
        _ShinyProgress ("Shiny Progress", Range(-0.5, 1.5)) = -0.5
        _ShinyWidth ("Shiny Width", Range(0.01, 0.5)) = 0.1
        _ShinyAngle ("Shiny Angle", Range(0, 360)) = 45
        _ShinySoftness ("Shiny Softness", Range(0.01, 0.5)) = 0.05
        _ShinyColor ("Shiny Color", Color) = (1, 1, 1, 0.5)
        _ShinyBrightness ("Shiny Brightness", Range(0, 2)) = 1

        [MaterialToggle] PixelSnap ("Pixel snap", Float) = 0
        [HideInInspector] _RendererColor ("RendererColor", Color) = (1,1,1,1)
        [HideInInspector] _Flip ("Flip", Vector) = (1,1,1,1)
        [PerRendererData] _AlphaTex ("External Alpha", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha ("Enable External Alpha", Float) = 0
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

        Cull Off
        Lighting Off
        ZWrite Off
        Blend One OneMinusSrcAlpha

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile_instancing
            #pragma multi_compile_local _ PIXELSNAP_ON
            #pragma multi_compile _ ETC1_EXTERNAL_ALPHA
            #include "UnitySprites.cginc"

            float _ShinyProgress;
            float _ShinyWidth;
            float _ShinyAngle;
            float _ShinySoftness;
            float4 _ShinyColor;
            float _ShinyBrightness;

            struct v2f_shiny
            {
                float4 vertex   : SV_POSITION;
                fixed4 color    : COLOR;
                float2 texcoord : TEXCOORD0;
                float2 localUV  : TEXCOORD1;  // 로컬 좌표 기반 UV (atlas 대응)
                UNITY_VERTEX_OUTPUT_STEREO
            };

            v2f_shiny vert(appdata_t IN)
            {
                v2f_shiny OUT;

                UNITY_SETUP_INSTANCE_ID(IN);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(OUT);

                OUT.vertex = UnityFlipSprite(IN.vertex, _Flip);
                OUT.vertex = UnityObjectToClipPos(OUT.vertex);
                OUT.texcoord = IN.texcoord;
                OUT.color = IN.color * _Color * _RendererColor;

                // 버텍스 로컬 좌표를 0~1 범위로 정규화 (atlas 대응)
                // SpriteRenderer 메시는 보통 pivot 기준 -0.5~0.5 또는 0~1 범위
                OUT.localUV = IN.vertex.xy + 0.5;

                #ifdef PIXELSNAP_ON
                OUT.vertex = UnityPixelSnap(OUT.vertex);
                #endif

                return OUT;
            }

            fixed4 frag(v2f_shiny IN) : SV_Target
            {
                fixed4 c = SampleSpriteTexture(IN.texcoord) * IN.color;

                // Shiny effect - 로컬 UV 기반으로 계산 (atlas 대응)
                float angleRad = radians(_ShinyAngle);
                float2 dir = float2(cos(angleRad), sin(angleRad));

                // 로컬 UV를 사용해서 shiny 위치 계산
                float projected = dot(IN.localUV - 0.5, dir) + 0.5;

                // Calculate shiny band
                float dist = abs(projected - _ShinyProgress);
                float shiny = 1.0 - smoothstep(_ShinyWidth - _ShinySoftness, _ShinyWidth + _ShinySoftness, dist);

                // Apply shiny effect only where there's alpha
                float3 shinyAdd = _ShinyColor.rgb * shiny * _ShinyBrightness * _ShinyColor.a * c.a;
                c.rgb += shinyAdd;

                c.rgb *= c.a;
                return c;
            }
            ENDCG
        }
    }
}
