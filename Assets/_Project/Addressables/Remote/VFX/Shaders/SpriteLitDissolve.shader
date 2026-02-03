Shader "Custom/SpriteLitDissolve"
{
    Properties
    {
        [MainTexture] _MainTex("Main Texture", 2D) = "white" {}
        [MainColor] _TintColor("Tint Color", Color) = (1, 1, 1, 1)
        
        [Header(Normal Map)]
        [Toggle(_USE_NORMALMAP)] _UseNormalMap("Use Normal Map", Float) = 0
        _NormalTex("Normal Map", 2D) = "bump" {}
        
        [Header(Dissolve)]
        _Dissolve("Dissolve", Range(0, 1)) = 0
        _DissolveDirection("Dissolve Direction", Vector) = (0, 1, 0, 0)
        _DirectionStrength("Direction Strength", Range(0, 1)) = 0.5
        [Toggle(_USE_NOISE_TEX)] _UseNoiseTex("Use Noise Texture", Float) = 0
        _NoiseTex("Noise Texture", 2D) = "white" {}
        _NoiseScale("Noise Scale", Float) = 50

        [HideInInspector] _BoundsMin("Bounds Min", Vector) = (-0.5, -0.5, 0, 0)
        [HideInInspector] _BoundsMax("Bounds Max", Vector) = (0.5, 0.5, 0, 0)
        
        [Header(Edge Glow)]
        [HDR] _EdgeColorA("Edge Color A", Color) = (0, 1, 1, 1)
        [HDR] _EdgeColorB("Edge Color B", Color) = (0, 0.5, 1, 1)
        _EdgeThicknessA("Edge Thickness A", Range(0, 0.5)) = 0.05
        _EdgeThicknessB("Edge Thickness B", Range(0, 0.2)) = 0.02
        
        [Header(Rendering)]
        _Cutoff("Alpha Cutoff", Range(0, 1)) = 0.5
        
        [HideInInspector] _RendererColor("Renderer Color", Color) = (1, 1, 1, 1)
        [HideInInspector] _Flip("Flip", Vector) = (1, 1, 1, 1)
        [PerRendererData] _AlphaTex("Alpha Texture", 2D) = "white" {}
        [PerRendererData] _EnableExternalAlpha("Enable External Alpha", Float) = 0
    }
    
    SubShader
    {
        Tags
        {
            "RenderPipeline" = "UniversalPipeline"
            "RenderType" = "TransparentCutout"
            "Queue" = "AlphaTest"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }
        
        Cull Off
        ZWrite On
        Blend One OneMinusSrcAlpha

        Pass
        {
            Name "Sprite Lit"
            Tags { "LightMode" = "Universal2D" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _USE_NORMALMAP
            #pragma multi_compile _ _USE_NOISE_TEX
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_0 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_1 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_2 __
            #pragma multi_compile USE_SHAPE_LIGHT_TYPE_3 __
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NormalTex);
            SAMPLER(sampler_NormalTex);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);
            
            #if USE_SHAPE_LIGHT_TYPE_0
            TEXTURE2D(_ShapeLightTexture0);
            SAMPLER(sampler_ShapeLightTexture0);
            float2 _ShapeLightBlendFactors0;
            float4 _ShapeLightMaskFilter0;
            float4 _ShapeLightInvertedFilter0;
            #endif

            #if USE_SHAPE_LIGHT_TYPE_1
            TEXTURE2D(_ShapeLightTexture1);
            SAMPLER(sampler_ShapeLightTexture1);
            float2 _ShapeLightBlendFactors1;
            float4 _ShapeLightMaskFilter1;
            float4 _ShapeLightInvertedFilter1;
            #endif

            #if USE_SHAPE_LIGHT_TYPE_2
            TEXTURE2D(_ShapeLightTexture2);
            SAMPLER(sampler_ShapeLightTexture2);
            float2 _ShapeLightBlendFactors2;
            float4 _ShapeLightMaskFilter2;
            float4 _ShapeLightInvertedFilter2;
            #endif

            #if USE_SHAPE_LIGHT_TYPE_3
            TEXTURE2D(_ShapeLightTexture3);
            SAMPLER(sampler_ShapeLightTexture3);
            float2 _ShapeLightBlendFactors3;
            float4 _ShapeLightMaskFilter3;
            float4 _ShapeLightInvertedFilter3;
            #endif
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _TintColor;
                float4 _NormalTex_ST;
                float4 _NoiseTex_ST;
                float _Dissolve;
                float4 _DissolveDirection;
                float _DirectionStrength;
                float _NoiseScale;
                float4 _BoundsMin;
                float4 _BoundsMax;
                float4 _EdgeColorA;
                float4 _EdgeColorB;
                float _EdgeThicknessA;
                float _EdgeThicknessB;
                float4 _RendererColor;
                float4 _Flip;
                float _Cutoff;
            CBUFFER_END
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float2 screenUV : TEXCOORD1;
                float3 positionOS : TEXCOORD2;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }
            
            float noise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }
            
            float fbmNoise(float2 uv, float scale)
            {
                float n = 0.0;
                n += 0.5 * noise(uv * scale);
                n += 0.25 * noise(uv * scale * 2.0);
                return n * 1.333;
            }
            
            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionOS = input.positionOS.xyz;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                float4 clipVertex = output.positionCS / output.positionCS.w;
                output.screenUV = ComputeScreenPos(clipVertex).xy;
                output.color = input.color * _TintColor * _RendererColor;
                return output;
            }
            
            float4 frag(Varyings input) : SV_Target
            {
                float4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float4 baseColor = mainTex * input.color;
                
                // 노이즈 값
                #if _USE_NOISE_TEX
                    float2 noiseUV = input.uv * _NoiseTex_ST.xy + _NoiseTex_ST.zw;
                    float noiseVal = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;
                #else
                    float noiseVal = fbmNoise(input.uv, _NoiseScale);
                #endif

                // 바운드 기준 정규화된 로컬 좌표 (0~1)
                float2 normalizedPos = (input.positionOS.xy - _BoundsMin.xy) / (_BoundsMax.xy - _BoundsMin.xy + 0.0001);

                // 방향성 디졸브
                float2 dir = normalize(_DissolveDirection.xy + 0.0001);
                float dirValue = dot(normalizedPos - 0.5, dir) + 0.5;

                // 노이즈 + 방향 혼합
                float finalDissolve = saturate(lerp(noiseVal, dirValue, _DirectionStrength) + noiseVal * (1.0 - _DirectionStrength) * 0.3);
                float dissolveThreshold = _Dissolve * 1.05;
                // _Dissolve가 0이면 완전히 보이도록
                float dissolveAlpha = _Dissolve < 0.001 ? 1.0 : step(dissolveThreshold, finalDissolve);

                float edgeA = step(dissolveThreshold, finalDissolve) - step(dissolveThreshold + _EdgeThicknessA, finalDissolve);
                float edgeB = step(dissolveThreshold + _EdgeThicknessA, finalDissolve) - step(dissolveThreshold + _EdgeThicknessA + _EdgeThicknessB, finalDissolve);
                float3 emission = (edgeA * _EdgeColorA.rgb + edgeB * _EdgeColorB.rgb) * step(0.001, _Dissolve);

                float4 lightColor = float4(0, 0, 0, 0);
                #if USE_SHAPE_LIGHT_TYPE_0
                lightColor += SAMPLE_TEXTURE2D(_ShapeLightTexture0, sampler_ShapeLightTexture0, input.screenUV) * _ShapeLightBlendFactors0.x;
                #endif
                #if USE_SHAPE_LIGHT_TYPE_1
                lightColor += SAMPLE_TEXTURE2D(_ShapeLightTexture1, sampler_ShapeLightTexture1, input.screenUV) * _ShapeLightBlendFactors1.x;
                #endif
                #if USE_SHAPE_LIGHT_TYPE_2
                lightColor += SAMPLE_TEXTURE2D(_ShapeLightTexture2, sampler_ShapeLightTexture2, input.screenUV) * _ShapeLightBlendFactors2.x;
                #endif
                #if USE_SHAPE_LIGHT_TYPE_3
                lightColor += SAMPLE_TEXTURE2D(_ShapeLightTexture3, sampler_ShapeLightTexture3, input.screenUV) * _ShapeLightBlendFactors3.x;
                #endif
                
                float3 litColor = baseColor.rgb * max(lightColor.rgb, float3(0.1, 0.1, 0.1));
                litColor += emission * dissolveAlpha;
                float finalAlpha = baseColor.a * dissolveAlpha;

                clip(finalAlpha - _Cutoff);

                return float4(litColor * finalAlpha, finalAlpha);
            }
            ENDHLSL
        }
        
        Pass
        {
            Name "Sprite Unlit"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _USE_NOISE_TEX
            #pragma multi_compile_instancing

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_NoiseTex);
            SAMPLER(sampler_NoiseTex);

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _TintColor;
                float4 _NormalTex_ST;
                float4 _NoiseTex_ST;
                float _Dissolve;
                float4 _DissolveDirection;
                float _DirectionStrength;
                float _NoiseScale;
                float4 _BoundsMin;
                float4 _BoundsMax;
                float4 _EdgeColorA;
                float4 _EdgeColorB;
                float _EdgeThicknessA;
                float _EdgeThicknessB;
                float4 _RendererColor;
                float4 _Flip;
                float _Cutoff;
            CBUFFER_END
            
            struct Attributes
            {
                float4 positionOS : POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                UNITY_VERTEX_INPUT_INSTANCE_ID
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv : TEXCOORD0;
                float3 positionOS : TEXCOORD1;
                UNITY_VERTEX_OUTPUT_STEREO
            };

            float hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }

            float noise(float2 uv)
            {
                float2 i = floor(uv);
                float2 f = frac(uv);
                f = f * f * (3.0 - 2.0 * f);
                float a = hash(i);
                float b = hash(i + float2(1.0, 0.0));
                float c = hash(i + float2(0.0, 1.0));
                float d = hash(i + float2(1.0, 1.0));
                return lerp(lerp(a, b, f.x), lerp(c, d, f.x), f.y);
            }

            float fbmNoise(float2 uv, float scale)
            {
                float n = 0.0;
                n += 0.5 * noise(uv * scale);
                n += 0.25 * noise(uv * scale * 2.0);
                return n * 1.333;
            }

            Varyings vert(Attributes input)
            {
                Varyings output;
                UNITY_SETUP_INSTANCE_ID(input);
                UNITY_INITIALIZE_VERTEX_OUTPUT_STEREO(output);
                output.positionCS = TransformObjectToHClip(input.positionOS.xyz);
                output.positionOS = input.positionOS.xyz;
                output.uv = TRANSFORM_TEX(input.uv, _MainTex);
                output.color = input.color * _TintColor * _RendererColor;
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float4 baseColor = mainTex * input.color;

                // 노이즈 값
                #if _USE_NOISE_TEX
                    float2 noiseUV = input.uv * _NoiseTex_ST.xy + _NoiseTex_ST.zw;
                    float noiseVal = SAMPLE_TEXTURE2D(_NoiseTex, sampler_NoiseTex, noiseUV).r;
                #else
                    float noiseVal = fbmNoise(input.uv, _NoiseScale);
                #endif

                // 바운드 기준 정규화된 로컬 좌표 (0~1)
                float2 normalizedPos = (input.positionOS.xy - _BoundsMin.xy) / (_BoundsMax.xy - _BoundsMin.xy + 0.0001);

                // 방향성 디졸브
                float2 dir = normalize(_DissolveDirection.xy + 0.0001);
                float dirValue = dot(normalizedPos - 0.5, dir) + 0.5;

                // 노이즈 + 방향 혼합
                float finalDissolve = saturate(lerp(noiseVal, dirValue, _DirectionStrength) + noiseVal * (1.0 - _DirectionStrength) * 0.3);
                float dissolveThreshold = _Dissolve * 1.05;
                // _Dissolve가 0이면 완전히 보이도록
                float dissolveAlpha = _Dissolve < 0.001 ? 1.0 : step(dissolveThreshold, finalDissolve);
                
                float edgeA = step(dissolveThreshold, finalDissolve) - step(dissolveThreshold + _EdgeThicknessA, finalDissolve);
                float edgeB = step(dissolveThreshold + _EdgeThicknessA, finalDissolve) - step(dissolveThreshold + _EdgeThicknessA + _EdgeThicknessB, finalDissolve);
                float3 emission = (edgeA * _EdgeColorA.rgb + edgeB * _EdgeColorB.rgb) * step(0.001, _Dissolve);
                
                float3 finalColor = baseColor.rgb + emission;
                float finalAlpha = baseColor.a * dissolveAlpha;
                
                clip(finalAlpha - _Cutoff);
                return float4(finalColor * finalAlpha, finalAlpha);
            }
            ENDHLSL
        }
    }
    Fallback "Sprites/Default"
}