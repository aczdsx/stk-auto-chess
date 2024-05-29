Shader "VFX_Klaus/fx_explosion_add_URP"
{
    Properties
    {
        _MainTex("Main Tex", 2D) = "white" {}
    }

    SubShader
    {
        Tags { "Queue" = "Transparent" "IgnoreProjector" = "True" "RenderType" = "Transparent" }
        Blend SrcAlpha One
        ColorMask RGB
        Cull Off
        ZWrite Off
        ZTest LEqual

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0
            #pragma multi_compile _ _ALPHATEST_ON _ALPHAPREMULTIPLY_ON

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Input.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Lighting.hlsl"
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/SurfaceInput.hlsl"

            struct Attributes
            {
                float4 vertex : POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 k_texcoord1 : TEXCOORD1;
            };

            struct Varyings
            {
                float4 position : SV_POSITION;
                float4 color : COLOR;
                float2 texcoord : TEXCOORD0;
                float4 k_texcoord3 : TEXCOORD3;
                float3 worldPos : TEXCOORD1;
                #ifdef SOFTPARTICLES_ON
                float4 projPos : TEXCOORD2;
                #endif
            };

            CBUFFER_START(UnityPerMaterial)
            float4 _MainTex_ST;
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            #ifdef SOFTPARTICLES_ON
            TEXTURE2D(_CameraDepthTexture);
            SAMPLER(sampler_CameraDepthTexture);
            #endif
            CBUFFER_END

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);

                output.position = mul(UNITY_MATRIX_MVP, input.vertex);
                output.color = input.color;
                output.texcoord = TRANSFORM_TEX(input.texcoord, _MainTex);
                output.k_texcoord3 = input.k_texcoord1;
                output.worldPos = mul(unity_ObjectToWorld, input.vertex).xyz;

                #ifdef SOFTPARTICLES_ON
                output.projPos = ComputeScreenPos(output.position);
                #endif

                return output;
            }

            half4 frag(Varyings input) : SV_Target
            {
                #ifdef SOFTPARTICLES_ON
                float sceneZ = LinearEyeDepth(SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, input.projPos.xy));
                float partZ = input.projPos.z;
                float fade = saturate(input.k_texcoord3.w * (sceneZ - partZ));
                input.color.a *= fade;
                #endif

                float2 uv_MainTex = input.texcoord.xy * _MainTex_ST.xy + _MainTex_ST.zw;
                float4 tex2DResult = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, uv_MainTex);
                float clampResult = clamp(((input.k_texcoord3.x * -1.0 * input.k_texcoord3.y) + ((tex2DResult.g + (1.0 - input.k_texcoord3.x)) - 0.0) * (1.0 - (input.k_texcoord3.x * -1.0 * input.k_texcoord3.y)) / (((input.k_texcoord3.x * 0.1) + 1.0) - 0.0)), 0.0, 1.0);

                half4 col = (tex2DResult.r * input.color * clampResult);
                return col;
            }

            ENDHLSL
        }
    }

    FallBack "Unlit/Texture"
}

