Shader "2DGames/URP/HUDBar"
{
    Properties
    {
        [Header(Textures)]
        [HideInInspector]_MainTex ("Sprite Texture", 2D) = "white" {}
        [NoScaleOffset] _BorderTex ("Border Texture (边框贴图)", 2D) = "white" {}
        _HPNoiseTex ("Noise Texture (表面肌理贴图)", 2D) = "white" {}
        _NoiseIntensity ("Noise Intensity (肌理强度)", Range(0, 1)) = 0.5

        [Header(Billboarding)]
        [Toggle(_Bill_ON)]_Billboarding ("广告牌是否开启", Float) = 0
        _VerticalBillboarding ("垂直控制参数(0或1)", Float) = 1

        [Header(Fill Window)]
        _FillRangeX ("左右填充边界 (Left Min, Right Max)", Vector) = (0.05, 0.95, 0, 0)
        _FillRangeY ("上下填充边界 (Bottom Min, Top Max)", Vector) = (0.1, 0.9, 0, 0)

        [Header(Bar Common)]
        _CurrentHP ("Current Value (当前进度)", Range(0,1)) = 1
        _DelayHP ("Delay Value (拖尾进度)", Range(0,1)) = 1
        _DelayColor ("Delay Color (拖尾颜色)", Color) = (1,1,0,1)

        [Header(Bar Mode)]
        [Enum(HP,0,MP,1,Ultimate,2)] _BarMode ("Bar Mode (HP/MP/终结技)", Float) = 0

        [Header(HP Mode)]
        [HDR]_CurrentHPColor ("Current HP Color (当前血条颜色)", Color) = (1, 0, 0, 1)
        _FlashSpeed ("Flash Speed (濒血闪烁速度)", Range(0,10)) = 5

        [Header(HP Edge Highlight)]
        [HDR]_HighlightColor ("Edge Highlight Color (边缘高光颜色)", Color) = (1.5, 1.5, 1.5, 1)
        _HighlightWidth ("Highlight Width (高光线宽)", Range(0.005, 0.1)) = 0.02
        _HighlightPower ("Highlight Power (边缘虚化强度)", Range(1, 10)) = 3.0

        [Header(HP Wobble Edge)]
        _WobbleStrength ("Wobble Strength (浮动幅度)", Range(0, 0.5)) = 0.03
        _WobbleSpeed ("Wobble Speed (浮动速度)", Float) = 15.0
        _WobbleDensity ("Wobble Density (波浪密度)", Float) = 3.14

        [Header(MP Mode)]
        [HDR] _ColorLeft ("Gradient Left (左侧颜色)", Color) = (0.0, 0.8, 1.0, 1)
        [HDR] _ColorRight ("Gradient Right (右侧颜色)", Color) = (0.0, 0.2, 0.8, 1)

        [Header(MP Brush Tail)]
        [NoScaleOffset] _TailTex ("Tail Texture (毛笔末梢贴图,仅需Alpha)", 2D) = "white" {}
        _TailLength ("Tail Length (笔触占整条比例长度)", Range(0.01, 0.5)) = 0.15

        [Header(Ultimate Mode)]
        [HDR] _UltColorLow ("Low Energy Color (低能量颜色)", Color) = (0.8, 0.3, 0.0, 1)
        [HDR] _UltColorHigh ("High Energy Color (高能量颜色)", Color) = (1.0, 0.85, 0.0, 1)
        _UltPulseSpeed ("Pulse Speed (满能量脉冲速度)", Range(0, 10)) = 3.0
        _UltGlowIntensity ("Full Glow Intensity (满能量发光强度)", Range(0, 2)) = 0.6
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _CurrentHP;
                float _DelayHP;
                float4 _DelayColor;
                float _VerticalBillboarding;
                float _Billboarding;
                float _BarMode;

                // HP mode
                float _FlashSpeed;
                float4 _CurrentHPColor;
                float4 _HighlightColor;
                float _HighlightWidth;
                float _HighlightPower;
                float _WobbleStrength;
                float _WobbleSpeed;
                float _WobbleDensity;

                // MP mode
                float4 _ColorLeft;
                float4 _ColorRight;
                float _TailLength;

                // Ultimate mode
                float4 _UltColorLow;
                float4 _UltColorHigh;
                float _UltPulseSpeed;
                float _UltGlowIntensity;

                float4 _FillRangeX;
                float4 _FillRangeY;

                float4 _HPNoiseTex_ST;
                float _NoiseIntensity;
            CBUFFER_END

            TEXTURE2D(_BorderTex);
            SAMPLER(sampler_BorderTex);
            TEXTURE2D(_HPNoiseTex);
            SAMPLER(sampler_HPNoiseTex);
            TEXTURE2D(_TailTex);
            SAMPLER(sampler_TailTex);

            struct VertexInput
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct VertexOutput
            {
                float4 posCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float time : TEXCOORD1;
            };
        ENDHLSL

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag
            #pragma shader_feature_local _Bill_ON

            VertexOutput vert(VertexInput v)
            {
                VertexOutput o = (VertexOutput)0;
                #ifdef _Bill_ON
                float3 center = float3(0, 0, 0);
                float3 viewer = TransformWorldToObject(_WorldSpaceCameraPos);
                float3 normalDir = viewer - center;

                normalDir.y *= _VerticalBillboarding;
                normalDir = normalize(normalDir);

                float3 upDir = abs(normalDir.y) > 0.999 ? float3(0, 0, 1) : float3(0, 1, 0);
                float3 rightDir = normalize(cross(upDir, normalDir));
                upDir = normalize(cross(normalDir, rightDir));

                float3 centerOffs = v.vertex.xyz - center;
                float3 localPos = center
                                + rightDir * -centerOffs.x
                                + upDir   * centerOffs.y
                                + normalDir * centerOffs.z;
                o.posCS = TransformObjectToHClip(float4(localPos, 1));
                #else
                o.posCS = TransformObjectToHClip(v.vertex.xyz);
                #endif

                o.uv = v.uv;
                o.time = _Time.y;
                return o;
            }

            half4 frag(VertexOutput i) : SV_Target
            {
                float4 borderCol = SAMPLE_TEXTURE2D(_BorderTex, sampler_BorderTex, i.uv);

                float inFillArea = step(_FillRangeX.x, i.uv.x) * step(i.uv.x, _FillRangeX.y)
                                 * step(_FillRangeY.x, i.uv.y) * step(i.uv.y, _FillRangeY.y);

                float rangeX = max(0.001, _FillRangeX.y - _FillRangeX.x);
                float rangeY = max(0.001, _FillRangeY.y - _FillRangeY.x);

                float2 fillUV;
                fillUV.x = saturate((i.uv.x - _FillRangeX.x) / rangeX);
                fillUV.y = saturate((i.uv.y - _FillRangeY.x) / rangeY);

                // 通用肌理采样（三种模式共用）
                float3 noiseRGB = SAMPLE_TEXTURE2D(_HPNoiseTex, sampler_HPNoiseTex, fillUV * _HPNoiseTex_ST.xy + _HPNoiseTex_ST.zw).rgb;

                float3 finalRGB;
                float finalAlpha;

                if (_BarMode < 0.5)
                {
                    // ==================== HP Mode (0) ====================
                    float wobble = sin((fillUV.y - 0.5) * _WobbleDensity + i.time * _WobbleSpeed) * _WobbleStrength;
                    float isAlive = step(0.001, _CurrentHP);
                    float isNotFull = 1.0 - step(0.999, _CurrentHP);
                    wobble *= isAlive * isNotFull;

                    float currentHPEdge = _CurrentHP + wobble;
                    float delayHPEdge = _DelayHP + wobble;

                    float healthbarMask = step(fillUV.x, currentHPEdge) * inFillArea;
                    float delayMask = step(fillUV.x, delayHPEdge) * inFillArea;

                    float4 healthColor = _CurrentHPColor;
                    healthColor.rgb = lerp(healthColor.rgb, healthColor.rgb * noiseRGB, _NoiseIntensity);

                    float distToHP = abs(fillUV.x - currentHPEdge);
                    float tipHighlight = saturate(1.0 - (distToHP / _HighlightWidth));
                    tipHighlight = pow(tipHighlight, _HighlightPower) * step(fillUV.x, currentHPEdge);
                    healthColor.rgb += _HighlightColor.rgb * tipHighlight;

                    float flash = cos(i.time * _FlashSpeed) * 0.5 + 0.5;
                    if (_CurrentHP < 0.2)
                        healthColor.rgb *= flash;

                    float3 barColor = _DelayColor.rgb * delayMask * (1.0 - healthbarMask) + healthColor.rgb * healthbarMask;

                    finalRGB = lerp(barColor, borderCol.rgb, borderCol.a);
                    finalAlpha = max(borderCol.a, max(healthbarMask, delayMask));
                }
                else if (_BarMode < 1.5)
                {
                    // ==================== MP Mode (1) ====================
                    float currentHPEdge = _CurrentHP;
                    float tailStart = currentHPEdge - _TailLength;

                    float2 tailUV = float2((fillUV.x - tailStart) / max(0.001, _TailLength), fillUV.y);
                    float tailAlpha = SAMPLE_TEXTURE2D(_TailTex, sampler_TailTex, tailUV).a;

                    float isSolid = step(fillUV.x, tailStart);
                    float isTail = step(tailStart, fillUV.x) * step(fillUV.x, currentHPEdge);

                    float barMask = (isSolid + isTail * tailAlpha) * inFillArea;
                    float delayMask = step(fillUV.x, _DelayHP) * inFillArea;

                    float4 mainColor = lerp(_ColorLeft, _ColorRight, fillUV.x);
                    mainColor.rgb = lerp(mainColor.rgb, mainColor.rgb * noiseRGB, _NoiseIntensity);

                    float3 finalBarColor = lerp(_DelayColor.rgb, mainColor.rgb, barMask > 0 ? 1.0 : 0.0);
                    float combinedMask = max(barMask, delayMask);

                    finalRGB = lerp(finalBarColor, borderCol.rgb, borderCol.a);
                    finalAlpha = max(borderCol.a, combinedMask);
                }
                else
                {
                    // ==================== Ultimate Mode (2) ====================
                    float currentEdge = _CurrentHP;
                    float delayEdge = _DelayHP;

                    float barMask = step(fillUV.x, currentEdge) * inFillArea;
                    float delayMask = step(fillUV.x, delayEdge) * inFillArea;

                    // 能量从暗橙渐变到金黄
                    float4 energyColor = lerp(_UltColorLow, _UltColorHigh, fillUV.x);
                    energyColor.rgb = lerp(energyColor.rgb, energyColor.rgb * noiseRGB, _NoiseIntensity);

                    // 满能量脉冲发光
                    float isFull = step(0.98, _CurrentHP);
                    float pulse = sin(i.time * _UltPulseSpeed) * 0.5 + 0.5;
                    energyColor.rgb += energyColor.rgb * _UltGlowIntensity * pulse * isFull;

                    // 能量前端高亮边缘
                    float distToEdge = abs(fillUV.x - currentEdge);
                    float edgeGlow = saturate(1.0 - distToEdge / 0.04) * barMask;
                    energyColor.rgb += float3(1.0, 0.9, 0.5) * edgeGlow * 0.5;

                    float3 barColor = _DelayColor.rgb * delayMask * (1.0 - barMask) + energyColor.rgb * barMask;

                    finalRGB = lerp(barColor, borderCol.rgb, borderCol.a);
                    finalAlpha = max(borderCol.a, max(barMask, delayMask));
                }

                return half4(finalRGB, finalAlpha);
            }
            ENDHLSL
        }
    }
    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}
