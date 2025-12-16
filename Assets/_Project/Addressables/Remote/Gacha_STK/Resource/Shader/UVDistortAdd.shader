Shader "Custom/DistortUVAdd"
{
    Properties
    {
        [HDR]_Color("Color", Color) = (1,1,1,1)
        _MainTex("MainTex", 2D) = "white" {}
        _MainUV("MainUV", Vector) = (1,1,0,0)
        _MainSp("MainSp", Vector) = (0,0,0,0)
        _DisTex("DisTex", 2D) = "white" {}
        _DisUV("DisUV", Vector) = (1,1,0,0)
        _DisInt("DisInt", Range(0 , 1)) = 0
        _AlphaTex("AlphaTex", 2D) = "white" {}
        _MainPow("MainPow", Range(0 , 4)) = 1
    }

    SubShader
    {
        Tags{ "RenderType" = "Transparent" "Queue" = "Transparent" }
        Blend One One
        ZWrite Off
        Cull Off

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
                output.uv = input.uv;
                output.color = input.color;
                return output;
            }

            half4 LightingUnlit(Varyings i)
            {
                return half4(0, 0, 0, i.color.a);
            }

            half4 frag(Varyings i) : SV_TARGET
            {
                float2 mainUV = i.uv * _MainUV.xy + _MainUV.zw;
                float2 disUV = i.uv * _DisUV.xy + _DisUV.zw;
                float4 disTex = tex2D(_DisTex, disUV);
                float2 distortedUV = mainUV + (disTex.rg * disTex.a * _DisInt);
                float4 mainTex = tex2D(_MainTex, distortedUV);
                float4 alphaTex = tex2D(_AlphaTex, i.uv * _AlphaTex_ST.xy + _AlphaTex_ST.zw);
                float4 outputColor = pow(_Color * (mainTex * mainTex.a), _MainPow) * (mainTex * i.color.a) * (alphaTex.r * alphaTex.a);
                return outputColor;
            }
            ENDHLSL
        }
    }
    CustomEditor "ASEMaterialInspector"
}
