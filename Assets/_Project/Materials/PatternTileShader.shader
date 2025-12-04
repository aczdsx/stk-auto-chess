Shader "Custom/PatternTileShader"
{
    Properties
    {
        _PatternTex ("Pattern Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,0,0,1)
        _TileScale ("Tile Scale", Float) = 0.1
        _TilingOffset ("Tiling Offset", Vector) = (0,0,0,0)
        _ScrollSpeed ("Scroll Speed", Float) = 0.0
        [Enum(Y,0,X,1,Z,2)] _Axis ("Axis", Float) = 0
        _AlphaMin ("Alpha Min", Range(0,1)) = 0.0
        _AlphaMax ("Alpha Max", Range(0,1)) = 1.0
        _AlphaPulseDuration ("Alpha Pulse Duration", Float) = 1.0
    }
    SubShader
    {
        Tags { "RenderType"="Transparent" "Queue"="Transparent" "IgnoreProjector"="True" }
        LOD 200

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 3.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float3 normal : NORMAL;
            };

            struct v2f
            {
                float4 vertex : SV_POSITION;
                float3 worldPos : TEXCOORD0;
                float3 worldNormal : TEXCOORD1;
            };

            sampler2D _PatternTex;
            float4 _PatternTex_ST;
            float4 _Color;
            float _TileScale;
            float4 _TilingOffset;
            float _ScrollSpeed;
            float _Axis;
            float _AlphaMin;
            float _AlphaMax;
            float _AlphaPulseDuration;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.worldPos = mul(unity_ObjectToWorld, v.vertex).xyz;
                o.worldNormal = UnityObjectToWorldNormal(v.normal);
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                float3 absNormal = abs(i.worldNormal);
                float2 uv;
                float scrollOffset = _Time.y * _ScrollSpeed;
                
                // 축에 따라 투명 처리할 면 결정
                if (_Axis == 0) // Y축 기준
                {
                    // 위아래 면 투명 처리
                    if (absNormal.y > absNormal.x && absNormal.y > absNormal.z)
                    {
                        return fixed4(0, 0, 0, 0);
                    }
                    
                    // Y축 기준: XZ 평면 사용
                    if (absNormal.x > absNormal.z)
                    {
                        // 좌우 측면 - Z, Y 좌표 사용
                        float zCoord = i.worldPos.z;
                        if (i.worldNormal.x < 0)
                        {
                            zCoord = -zCoord;
                            uv = float2(zCoord, i.worldPos.y) * _TileScale + _TilingOffset.xy;
                            uv.x += 1.0;
                        }
                        else
                        {
                            uv = float2(zCoord, i.worldPos.y) * _TileScale + _TilingOffset.xy;
                            uv.x += 3.0;
                        }
                    }
                    else
                    {
                        // 앞뒤 측면 - X, Y 좌표 사용
                        float xCoord = i.worldPos.x;
                        if (i.worldNormal.z > 0)
                        {
                            xCoord = -xCoord;
                            uv = float2(xCoord, i.worldPos.y) * _TileScale + _TilingOffset.xy;
                        }
                        else
                        {
                            uv = float2(xCoord, i.worldPos.y) * _TileScale + _TilingOffset.xy;
                            uv.x += 2.0;
                        }
                    }
                }
                else if (_Axis == 1) // X축 기준
                {
                    // 좌우 면 투명 처리
                    if (absNormal.x > absNormal.y && absNormal.x > absNormal.z)
                    {
                        return fixed4(0, 0, 0, 0);
                    }
                    
                    // X축 기준: YZ 평면 사용
                    if (absNormal.y > absNormal.z)
                    {
                        // 위아래 측면 - Z, X 좌표 사용
                        float zCoord = i.worldPos.z;
                        if (i.worldNormal.y < 0)
                        {
                            zCoord = -zCoord;
                            uv = float2(zCoord, i.worldPos.x) * _TileScale + _TilingOffset.xy;
                            uv.x += 1.0;
                        }
                        else
                        {
                            uv = float2(zCoord, i.worldPos.x) * _TileScale + _TilingOffset.xy;
                            uv.x += 3.0;
                        }
                    }
                    else
                    {
                        // 앞뒤 측면 - Y, X 좌표 사용
                        float yCoord = i.worldPos.y;
                        if (i.worldNormal.z > 0)
                        {
                            yCoord = -yCoord;
                            uv = float2(yCoord, i.worldPos.x) * _TileScale + _TilingOffset.xy;
                        }
                        else
                        {
                            uv = float2(yCoord, i.worldPos.x) * _TileScale + _TilingOffset.xy;
                            uv.x += 2.0;
                        }
                    }
                }
                else // Z축 기준
                {
                    // 앞뒤 면 투명 처리
                    if (absNormal.z > absNormal.x && absNormal.z > absNormal.y)
                    {
                        return fixed4(0, 0, 0, 0);
                    }
                    
                    // Z축 기준: XY 평면 사용
                    if (absNormal.x > absNormal.y)
                    {
                        // 좌우 측면 - Y, Z 좌표 사용
                        float yCoord = i.worldPos.y;
                        if (i.worldNormal.x < 0)
                        {
                            yCoord = -yCoord;
                            uv = float2(yCoord, i.worldPos.z) * _TileScale + _TilingOffset.xy;
                            uv.x += 1.0;
                        }
                        else
                        {
                            uv = float2(yCoord, i.worldPos.z) * _TileScale + _TilingOffset.xy;
                            uv.x += 3.0;
                        }
                    }
                    else
                    {
                        // 위아래 측면 - X, Z 좌표 사용
                        float xCoord = i.worldPos.x;
                        if (i.worldNormal.y > 0)
                        {
                            xCoord = -xCoord;
                            uv = float2(xCoord, i.worldPos.z) * _TileScale + _TilingOffset.xy;
                        }
                        else
                        {
                            uv = float2(xCoord, i.worldPos.z) * _TileScale + _TilingOffset.xy;
                            uv.x += 2.0;
                        }
                    }
                }
                
                // 흐르는 효과: 시간에 따라 X 방향으로 UV 이동
                uv.x += scrollOffset;
                
                // 패턴 텍스처 샘플링 (타일링 반복)
                float2 tiledUV = frac(uv);
                fixed4 pattern = tex2D(_PatternTex, tiledUV);
                
                // 패턴의 밝기 계산 (grayscale 변환)
                float brightness = dot(pattern.rgb, float3(0.299, 0.587, 0.114));
                
                // Alpha 펄스 효과: a ~ b 범위에서 x초 동안 왔다 갔다
                float pulseTime = _Time.y / _AlphaPulseDuration;
                float pulseValue = (sin(pulseTime * 2.0 * 3.14159265359) + 1.0) * 0.5; // 0~1 범위
                float pulseAlpha = lerp(_AlphaMin, _AlphaMax, pulseValue);
                
                // 검정색(어두운 부분)에 색상 적용, 흰색(밝은 부분)은 투명
                fixed4 col = _Color;
                col.a = (1.0 - brightness) * pulseAlpha;
                
                return col;
            }
            ENDCG
        }
    }
    FallBack "Diffuse"
}

