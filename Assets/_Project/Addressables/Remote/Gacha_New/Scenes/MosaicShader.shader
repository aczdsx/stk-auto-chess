Shader "Custom/URP_MosaicTransition"
{
    Properties
    {
        [MainTexture] _BaseMap("Base Texture", 2D) = "white" {}
        _MosaicAmount("Mosaic Intensity", Range(0, 1)) = 0 // 0: 선명함, 1: 최대 모자이크
        _MaxPixelation("Max Pixelation", Float) = 200.0   // 모자이크 칸 수 (낮을수록 더 뭉개짐)
    }

    SubShader
    {
        Tags { "RenderType" = "Opaque" "RenderPipeline" = "UniversalPipeline" }
        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            struct Attributes
            {
                float4 positionOS : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv : TEXCOORD0;
            };

            TEXTURE2D(_BaseMap);
            SAMPLER(sampler_BaseMap);

            CBUFFER_START(UnityPerMaterial)
                float4 _BaseMap_ST;
                float _MosaicAmount;
                float _MaxPixelation;
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.uv = TRANSFORM_TEX(input.uv, _BaseMap);
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;

                // _MosaicAmount가 0보다 클 때만 모자이크 계산 (최적화)
                if (_MosaicAmount > 0.01)
                {
                    // 선명도 역산: Amount가 커질수록 해상도가 낮아지게 설정
                    // 1 / (1 + (1 - Amount) * Max) 식을 활용해 부드러운 전환 구현
                    float pixelScale = lerp(500.0, 10.0, _MosaicAmount); 
                    uv = floor(uv * pixelScale) / pixelScale;
                }

                return SAMPLE_TEXTURE2D(_BaseMap, sampler_BaseMap, uv);
            }
            ENDHLSL
        }
    }
}