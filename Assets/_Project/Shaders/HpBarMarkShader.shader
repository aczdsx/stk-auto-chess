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
                // 기본적으로 투명
                fixed4 col = fixed4(0, 0, 0, 0);
                
                // UV를 반대로 (오른쪽에서 왼쪽으로)
                float reversedUV = 1.0 - i.uv.x;
                
                // 현재 체력 비율
                float currentHpRatio = _CurrentHP / _MaxHP;
                
                // 현재 픽셀이 현재 체력보다 앞에 있으면 표시하지 않음
                if (reversedUV > currentHpRatio)
                {
                    return col;
                }
                
                // 픽셀 단위로 정확한 계산을 위해 스크린 공간 미분 사용
                float pixelWidth = abs(ddx(reversedUV)) + abs(ddy(reversedUV));
                
                // Y 좌표 범위 체크용
                float markCenter = 0.5;
                
                // UV x 좌표를 체력 값으로 변환 (반대로 된 UV 사용)
                float hpValue = reversedUV * _MaxHP;
                
                // 큰 단위 눈금선 위치를 UV 좌표로 직접 계산 (더 정확한 방법)
                float markLongValue = floor(hpValue / _LongMarkUnit + 0.5) * _LongMarkUnit;
                float markLongUV = markLongValue / _MaxHP;
                
                // 큰 단위 눈금선 두께 (픽셀 단위로 보정, 최소값 보장)
                float baseLongMarkWidth = _LongMarkWidth * 0.5;
                float halfLongMarkWidth = max(baseLongMarkWidth, pixelWidth * 0.5);
                float longAaRange = max(halfLongMarkWidth * 2.0, pixelWidth * 2.0);
                
                // 스텝 함수를 사용해서 더 명확한 경계 생성 (동적 범위 적용)
                float distToLongMark = abs(reversedUV - markLongUV);
                float markLongAlpha = 1.0 - smoothstep(0, longAaRange, distToLongMark);
                
                // 큰 단위 눈금선 체크 (긴 줄)
                if (markLongAlpha > 0.01)
                {
                    float markTop = markCenter + _LongMarkHeight * 0.5;
                    float markBottom = markCenter - _LongMarkHeight * 0.5;
                    
                    if (i.uv.y >= markBottom && i.uv.y <= markTop)
                    {
                        col = _MarkColor;
                        col.a *= markLongAlpha;
                        return col;
                    }
                }
                
                // 작은 단위 눈금선 위치를 UV 좌표로 직접 계산
                float markShortValue = floor(hpValue / _ShortMarkUnit + 0.5) * _ShortMarkUnit;
                float markShortUV = markShortValue / _MaxHP;
                
                // 작은 단위 눈금선 두께 (픽셀 단위로 보정, 최소값 보장)
                float baseMarkWidth = _MarkWidth * 0.5;
                float halfMarkWidth = max(baseMarkWidth, pixelWidth * 0.5);
                float shortAaRange = max(halfMarkWidth * 2.0, pixelWidth * 2.0);
                
                // 작은 단위가 큰 단위와 겹치지 않는지 확인
                // 실제 눈금선 두께를 기준으로 체크 (안티앨리어싱 범위가 아닌)
                float actualLongMarkWidth = _LongMarkWidth * 0.5;
                float actualShortMarkWidth = _MarkWidth * 0.5;
                float distToLongFromShort = abs(markShortUV - markLongUV);
                float minDistance = max(actualLongMarkWidth, actualShortMarkWidth) * 2.0;
                
                // 작은 단위 눈금선 체크 (짧은 줄) - 큰 단위와 겹치지 않을 때만
                // 그리고 현재 픽셀이 큰 단위 눈금선의 영향 범위에 있지 않을 때만
                if (distToLongFromShort > minDistance)
                {
                    // 현재 픽셀이 큰 단위 눈금선의 영향 범위에 있는지 체크
                    float distToLongFromCurrentPixel = abs(reversedUV - markLongUV);
                    bool isInLongRange = distToLongFromCurrentPixel < longAaRange;
                    
                    if (!isInLongRange)
                    {
                        float distToShortMark = abs(reversedUV - markShortUV);
                        float markShortAlpha = 1.0 - smoothstep(0, shortAaRange, distToShortMark);
                        
                        if (markShortAlpha > 0.01)
                        {
                            // 작은 눈금은 위로 정렬
                            float markTop = 1.0;
                            float markBottom = 1.0 - _ShortMarkHeight;
                            
                            if (i.uv.y >= markBottom && i.uv.y <= markTop)
                            {
                                col = _MarkColor;
                                col.a *= markShortAlpha;
                                return col;
                            }
                        }
                    }
                }
                
                return col;
            }
            ENDCG
        }
    }
    FallBack "Sprites/Default"
}

