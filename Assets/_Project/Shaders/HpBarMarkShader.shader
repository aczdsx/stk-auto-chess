Shader "Custom/HpBarMarkShader"
{
    Properties
    {
        _MainTex ("Sprite Texture", 2D) = "white" {}
        _Color ("Color", Color) = (1,1,1,1)
        _MarkColor ("Mark Color", Color) = (0,0,0,1)
        _LongMarkHeight ("Long Mark Height", Range(0,1)) = 0.8
        _ShortMarkHeight ("Short Mark Height", Range(0,1)) = 0.5
        _LongMarkWidth ("Long Mark Width", Range(0,0.1)) = 0.005
        _MarkWidth ("Mark Width", Range(0,0.1)) = 0.005
        _LongMarkUnit ("Long Mark Unit", Float) = 10000
        _ShortMarkUnit ("Short Mark Unit", Float) = 1000
        _CurrentHP ("Current HP", Float) = 100
        _MaxHP ("Max HP", Float) = 100
    }
    SubShader
    {
        Tags 
        { 
            "RenderType"="Transparent" 
            "Queue"="Transparent+1" 
            "IgnoreProjector"="True" 
        }
        LOD 100

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        Lighting Off
        ZWrite Off
        ZTest Always

        Pass
        {
            CGPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma target 2.0

            #include "UnityCG.cginc"

            struct appdata
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct v2f
            {
                float2 uv : TEXCOORD0;
                float4 vertex : SV_POSITION;
            };

            sampler2D _MainTex;
            float4 _MainTex_ST;
            fixed4 _Color;
            fixed4 _MarkColor;
            float _LongMarkHeight;
            float _ShortMarkHeight;
            float _LongMarkWidth;
            float _MarkWidth;
            float _LongMarkUnit;
            float _ShortMarkUnit;
            float _CurrentHP;
            float _MaxHP;

            v2f vert (appdata v)
            {
                v2f o;
                o.vertex = UnityObjectToClipPos(v.vertex);
                o.uv = v.uv;
                return o;
            }

            fixed4 frag (v2f i) : SV_Target
            {
                // UV를 반대로 (오른쪽에서 왼쪽으로)
                float reversedUV = 1.0 - i.uv.x;
                
                // 현재 체력 비율
                float currentHpRatio = _CurrentHP / _MaxHP;
                
                // Early return: 현재 픽셀이 현재 체력보다 앞에 있으면 표시하지 않음
                if (reversedUV > currentHpRatio)
                {
                    return fixed4(0, 0, 0, 0);
                }
                
                // 픽셀 단위로 정확한 계산을 위해 스크린 공간 미분 사용
                float pixelWidth = abs(ddx(reversedUV)) + abs(ddy(reversedUV));
                
                // UV x 좌표를 체력 값으로 변환
                float hpValue = reversedUV * _MaxHP;
                
                // 큰 단위 눈금선 위치 계산
                float markLongValue = floor(hpValue / _LongMarkUnit + 0.5) * _LongMarkUnit;
                float markLongUV = markLongValue / _MaxHP;
                float distToLongMark = abs(reversedUV - markLongUV);
                
                // 큰 단위 눈금선 두께 및 안티앨리어싱 범위
                float halfLongMarkWidth = max(_LongMarkWidth * 0.5, pixelWidth * 0.5);
                float longAaRange = halfLongMarkWidth * 2.0;
                if (longAaRange < pixelWidth * 2.0) longAaRange = pixelWidth * 2.0;
                
                float markLongAlpha = 1.0 - smoothstep(0, longAaRange, distToLongMark);
                
                // 큰 단위 눈금선 체크 (긴 줄)
                if (markLongAlpha > 0.01)
                {
                    const float markCenter = 0.5;
                    float markTop = markCenter + _LongMarkHeight * 0.5;
                    float markBottom = markCenter - _LongMarkHeight * 0.5;
                    
                    if (i.uv.y >= markBottom && i.uv.y <= markTop)
                    {
                        fixed4 col = _MarkColor;
                        col.a *= markLongAlpha;
                        return col;
                    }
                }
                
                // 작은 단위 눈금선 위치 계산
                float markShortValue = floor(hpValue / _ShortMarkUnit + 0.5) * _ShortMarkUnit;
                float markShortUV = markShortValue / _MaxHP;
                float distToLongFromShort = abs(markShortUV - markLongUV);
                
                // 작은 단위가 큰 단위와 겹치지 않는지 확인 (실제 두께 기준)
                float minDistance = max(_LongMarkWidth, _MarkWidth);
                
                // Early return: 겹치거나 큰 단위 범위 내에 있으면 스킵
                if (distToLongFromShort <= minDistance || distToLongMark < longAaRange)
                {
                    return fixed4(0, 0, 0, 0);
                }
                
                // 작은 단위 눈금선 두께 및 안티앨리어싱 범위
                float halfMarkWidth = max(_MarkWidth * 0.5, pixelWidth * 0.5);
                float shortAaRange = halfMarkWidth * 2.0;
                if (shortAaRange < pixelWidth * 2.0) shortAaRange = pixelWidth * 2.0;
                
                float distToShortMark = abs(reversedUV - markShortUV);
                float markShortAlpha = 1.0 - smoothstep(0, shortAaRange, distToShortMark);
                
                if (markShortAlpha > 0.01)
                {
                    float markTop = 1.0;
                    float markBottom = 1.0 - _ShortMarkHeight;
                    
                    if (i.uv.y >= markBottom && i.uv.y <= markTop)
                    {
                        fixed4 col = _MarkColor;
                        col.a *= markShortAlpha;
                        return col;
                    }
                }
                
                return fixed4(0, 0, 0, 0);
            }
            ENDCG
        }
    }
    FallBack "Sprites/Default"
}

