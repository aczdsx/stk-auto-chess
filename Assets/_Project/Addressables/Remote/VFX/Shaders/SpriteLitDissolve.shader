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
        _NoiseScale("Noise Scale", Float) = 50
        
        [Header(Edge Glow)]
        [HDR] _EdgeColorA("Edge Color A", Color) = (0, 1, 1, 1)
        [HDR] _EdgeColorB("Edge Color B", Color) = (0, 0.5, 1, 1)
        _EdgeThicknessA("Edge Thickness A", Range(0, 0.1)) = 0.05
        _EdgeThicknessB("Edge Thickness B", Range(0, 0.05)) = 0.02
        
        [Header(Rendering)]
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
            "RenderType" = "Transparent"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
            "PreviewType" = "Plane"
            "CanUseSpriteAtlas" = "True"
        }
        
        Cull Off
        ZWrite Off
        Blend One OneMinusSrcAlpha
        
        Pass
        {
            Name "Sprite Lit"
            Tags { "LightMode" = "Universal2D" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile _ _USE_NORMALMAP
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
                float _Dissolve;
                float3 _DissolveDirection;
                float _NoiseScale;
                float4 _EdgeColorA;
                float4 _EdgeColorB;
                float _EdgeThicknessA;
                float _EdgeThicknessB;
                float4 _RendererColor;
                float4 _Flip;
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
            
            // Deterministic hash - Android compatible
            float hash(float2 p)
            {
                float3 p3 = frac(float3(p.xyx) * 0.1031);
                p3 += dot(p3, p3.yzx + 33.33);
                return frac((p3.x + p3.y) * p3.z);
            }
            
            // Simple noise - optimized
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
            
            // FBM noise (2 octaves - mobile optimized)
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
                // Sample main texture
                float4 mainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                float4 baseColor = mainTex * input.color;
                baseColor.rgb *= baseColor.a;
                
                // Early out if fully transparent
                if (baseColor.a < 0.001)
                    return float4(0, 0, 0, 0);
                
                // === DISSOLVE CALCULATION ===
                // Dissolve: 0 = fully visible, 1 = fully invisible
                float3 dissolveDir = normalize(_DissolveDirection);
                float dissolvePos = dot(input.positionOS, dissolveDir);
                dissolvePos = dissolvePos * 0.5 + 0.5; // remap to 0~1
                
                float noiseVal = fbmNoise(input.uv, _NoiseScale);
                float finalDissolve = dissolvePos + noiseVal * 0.3; // range ~0 to ~1.3
                
                // Scale threshold to ensure complete dissolve at _Dissolve = 1
                float dissolveThreshold = _Dissolve * 1.5;
                
                // Pixels with finalDissolve >= threshold are visible
                float dissolveAlpha = step(dissolveThreshold, finalDissolve);
                
                // === EDGE GLOW ===
                // Edge A: between threshold and threshold + EdgeThicknessA
                float edgeA = step(dissolveThreshold, finalDissolve) 
                            - step(dissolveThreshold + _EdgeThicknessA, finalDissolve);
                // Edge B: between threshold + EdgeThicknessA and threshold + EdgeThicknessA + EdgeThicknessB
                float edgeB = step(dissolveThreshold + _EdgeThicknessA, finalDissolve) 
                            - step(dissolveThreshold + _EdgeThicknessA + _EdgeThicknessB, finalDissolve);
                
                float3 emission = edgeA * _EdgeColorA.rgb + edgeB * _EdgeColorB.rgb;
                
                // === 2D LIGHTING ===
                float4 lightColor = float4(0, 0, 0, 0);
                
                #if USE_SHAPE_LIGHT_TYPE_0
                float4 shapeLight0 = SAMPLE_TEXTURE2D(_ShapeLightTexture0, sampler_ShapeLightTexture0, input.screenUV);
                lightColor += shapeLight0 * _ShapeLightBlendFactors0.x;
                #endif
                
                #if USE_SHAPE_LIGHT_TYPE_1
                float4 shapeLight1 = SAMPLE_TEXTURE2D(_ShapeLightTexture1, sampler_ShapeLightTexture1, input.screenUV);
                lightColor += shapeLight1 * _ShapeLightBlendFactors1.x;
                #endif
                
                #if USE_SHAPE_LIGHT_TYPE_2
                float4 shapeLight2 = SAMPLE_TEXTURE2D(_ShapeLightTexture2, sampler_ShapeLightTexture2, input.screenUV);
                lightColor += shapeLight2 * _ShapeLightBlendFactors2.x;
                #endif
                
                #if USE_SHAPE_LIGHT_TYPE_3
                float4 shapeLight3 = SAMPLE_TEXTURE2D(_ShapeLightTexture3, sampler_ShapeLightTexture3, input.screenUV);
                lightColor += shapeLight3 * _ShapeLightBlendFactors3.x;
                #endif
                
                // Apply lighting (fallback to white if no lights)
                float3 litColor = baseColor.rgb * max(lightColor.rgb, float3(0.1, 0.1, 0.1));
                
                // Add emission
                litColor += emission * dissolveAlpha;
                
                // Final alpha
                float finalAlpha = baseColor.a * dissolveAlpha;
                
                // Clip fully dissolved pixels
                clip(finalAlpha - 0.001);
                
                return float4(litColor * finalAlpha, finalAlpha);
            }
            ENDHLSL
        }
        
        // Unlit fallback pass
        Pass
        {
            Name "Sprite Unlit"
            Tags { "LightMode" = "UniversalForward" }
            
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma multi_compile_instancing
            
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _TintColor;
                float4 _NormalTex_ST;
                float _Dissolve;
                float3 _DissolveDirection;
                float _NoiseScale;
                float4 _EdgeColorA;
                float4 _EdgeColorB;
                float _EdgeThicknessA;
                float _EdgeThicknessB;
                float4 _RendererColor;
                float4 _Flip;
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
                
                if (baseColor.a < 0.001)
                    return float4(0, 0, 0, 0);
                
                // === DISSOLVE ===
                // Dissolve: 0 = fully visible, 1 = fully invisible
                float3 dissolveDir = normalize(_DissolveDirection);
                float dissolvePos = dot(input.positionOS, dissolveDir) * 0.5 + 0.5;
                float noiseVal = fbmNoise(input.uv, _NoiseScale);
                float finalDissolve = dissolvePos + noiseVal * 0.3;
                
                // Scale threshold to ensure complete dissolve
                float dissolveThreshold = _Dissolve * 1.5;
                float dissolveAlpha = step(dissolveThreshold, finalDissolve);
                
                // Edge glow
                float edgeA = step(dissolveThreshold, finalDissolve) 
                            - step(dissolveThreshold + _EdgeThicknessA, finalDissolve);
                float edgeB = step(dissolveThreshold + _EdgeThicknessA, finalDissolve) 
                            - step(dissolveThreshold + _EdgeThicknessA + _EdgeThicknessB, finalDissolve);
                float3 emission = edgeA * _EdgeColorA.rgb + edgeB * _EdgeColorB.rgb;
                
                float3 finalColor = baseColor.rgb + emission;
                float finalAlpha = baseColor.a * dissolveAlpha;
                
                // Clip fully dissolved pixels
                clip(finalAlpha - 0.001);
                
                return float4(finalColor * finalAlpha, finalAlpha);
            }
            ENDHLSL
        }
    }
    
    Fallback "Sprites/Default"
}
