Shader "Custom/DistortUV"
{
    Properties
    {
        [HDR]_Color("Color", Color) = (1,1,1,1)
        _MainTex("MainTex", 2D) = "white" {}
        _MainUV("MainUV", Vector) = (1,1,0,0)
        _MainSp("MainSp", Vector) = (0,0,0,0)
        _DisTex("DisTex", 2D) = "white" {}
        _DisUV("DisUV", Vector) = (1,1,0,0)
        _DisInt("DisInt", Range( 0 , 1)) = 0
        _AlphaTex("AlphaTex", 2D) = "white" {}
        _MainPow("MainPow", Range( 0 , 4)) = 1
    }

    SubShader
    {
        Tags{ "RenderType" = "Transparent" "Queue" = "Transparent" "IgnoreProjector" = "True" "IsEmissive" = "true" }
        Cull Off
        ZWrite Off
        Blend SrcAlpha OneMinusSrcAlpha

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
                float4 color : COLOR;
            };

            struct Varyings
            {
                float4 positionHCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float4 color : COLOR;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            float4 _Color;
            float4 _MainUV;
            float2 _MainSp;
            sampler2D _DisTex;
            float4 _DisUV;
            float _DisInt;
            sampler2D _AlphaTex;
            float4 _AlphaTex_ST;
            float _MainPow;

            Varyings vert(Attributes input)
            {
                Varyings output;
                output.positionHCS = TransformObjectToHClip(input.positionOS);
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color;
                return output;
            }

            half4 frag(Varyings i) : SV_TARGET
            {
                float2 uv_TexCoord63 = i.uv * _DisUV.xy;
                float2 panner95 = _Time.y * _DisUV.zw + uv_TexCoord63;
                float4 tex2DNode64 = tex2D(_DisTex, panner95);

                float2 uv_TexCoord59 = i.uv * _MainUV.xy + _MainUV.zw + (tex2DNode64.r * tex2DNode64.a * _DisInt);
                float2 panner108 = _Time.y * _MainSp + uv_TexCoord59;

                float4 tex2DNode57 = tex2D(_MainTex, panner108);
                float4 temp_output_62_0 = tex2DNode57 * tex2DNode57.a;
                float4 temp_cast_0 = _MainPow;

                float3 emission = _Color.rgb * pow(temp_output_62_0.rgb, temp_cast_0) * i.color.rgb;
                float2 uv_AlphaTex = i.uv * _AlphaTex_ST.xy + _AlphaTex_ST.zw;
                float4 tex2DNode104 = tex2D(_AlphaTex, uv_AlphaTex);

                half alpha = temp_output_62_0.a * i.color.a * tex2DNode104.r * tex2DNode104.a;

                return half4(emission, alpha);
            }
            ENDHLSL
        }
    }
    CustomEditor "ASEMaterialInspector"
}
