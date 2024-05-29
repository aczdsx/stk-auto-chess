Shader "Card_SSR_URP"
{
    Properties
    {
        _TextureSample0("Texture Sample 0", 2D) = "white" {}
        _TextureSample1("Texture Sample 1", 2D) = "white" {}
        _Speed("Speed", Float) = 0.1
        [HideInInspector] _texcoord("", 2D) = "white" {}
        [HideInInspector] __dirty("", Int) = 1
    }

    SubShader
    {
        Tags { "RenderType" = "Transparent" "Queue" = "Transparent+0" "IgnoreProjector" = "True" "IsEmissive" = "true" }
        Cull Back
        ZWrite Off
        Blend One One

        Pass
        {
            Name "ForwardLit"
            Tags { "LightMode" = "UniversalForward" }
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            TEXTURE2D(_TextureSample0);
            SAMPLER(sampler_TextureSample0);
            TEXTURE2D(_TextureSample1);
            SAMPLER(sampler_TextureSample1);
            float _Speed;
            float4 _TextureSample0_ST;

            Varyings vert(Attributes IN)
            {
                Varyings OUT;
                OUT.positionHCS = mul(UNITY_MATRIX_MVP, IN.positionOS);
                OUT.uv = TRANSFORM_TEX(IN.uv, _TextureSample0);
                OUT.color = IN.color;
                return OUT;
            }

            half4 frag(Varyings IN) : SV_Target
            {
                float mulTime15 = _Time.y * _Speed;
                float cos4 = cos(mulTime15);
                float sin4 = sin(mulTime15);
                float2 rotator4 = mul(IN.uv - float2(0.5, 0.5), float2x2(cos4, -sin4, sin4, cos4)) + float2(0.5, 0.5);
                float2 uv_TextureSample0 = IN.uv * _TextureSample0_ST.xy + _TextureSample0_ST.zw;

                float temp_output_2_0 = SAMPLE_TEXTURE2D(_TextureSample1, sampler_TextureSample1, rotator4).r *
                                        SAMPLE_TEXTURE2D(_TextureSample0, sampler_TextureSample0, uv_TextureSample0).r;

                float4 finalColor = (temp_output_2_0 * IN.color);
                finalColor.a = IN.color.a * temp_output_2_0;

                return finalColor;
            }
            ENDHLSL
        }
    }
    CustomEditor "ASEMaterialInspector"
}
