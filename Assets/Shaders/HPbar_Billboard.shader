Shader "2DGames/URP/HPbar_Billboard"
{
    Properties
    {
        [Header(Textures)]
        [HideInInspector]_MainTex ("Sprite Texture", 2D) = "white" {}
        [NoScaleOffset] _BorderTex ("Border Texture (边框贴图)", 2D) = "white" {}
        _HPNoiseTex ("HP Noise Texture (血条表面肌理贴图)", 2D) = "white" {} // 建议拖入一张噪波图或粗糙金属图
        _NoiseIntensity ("Noise Intensity (肌理强度)", Range(0, 1)) = 0.5

        [Header(HP Fill Window)]
        _FillRangeX ("左右填充边界 (Left Min, Right Max)", Vector) = (0.05, 0.95, 0, 0)
        _FillRangeY ("上下填充边界 (Bottom Min, Top Max)", Vector) = (0.1, 0.9, 0, 0)

        [Header(Sliding Highlight)]
        [HDR]_HighlightColor ("Edge Highlight Color (边缘高光颜色)", Color) = (1.5, 1.5, 1.5, 1) // 使用 HDR 强化发光
        _HighlightWidth ("Highlight Width (高光线宽)", Range(0.005, 0.1)) = 0.02
        _HighlightPower ("Highlight Power (边缘虚化强度)", Range(1, 10)) = 3.0

        [Header(Wobble Edge Logic)]//边缘浮动控制参数
        _WobbleStrength ("Wobble Strength (浮动幅度)", Range(0, 0.5)) = 0.03
        _WobbleSpeed ("Wobble Speed (浮动速度)", Float) = 15.0
        _WobbleDensity ("Wobble Density (波浪密度)", Float) = 3.14

        [Header(HPbar Logic)]
        _FlashSpeed ("Flash Speed", Range(0,10)) = 5
        _CurrentHP ("Current HP", Range(0,1)) = 1
        _CurrentHPColor ("Current HP Color (当前血条颜色)", Color) = (1, 0, 0, 1) // 材质球直接控制颜色
        _DelayHP ("Delay HP", Range(0,1)) = 1
        _DelayColor ("Delay Color (拖尾黄色)", Color) = (1,1,0,1)

        [Header(Billboarding)]
        _VerticalBillboarding ("垂直控制参数,只能填写0或1", Float) = 1
    }
    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "DisableBatching" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        ZWrite Off
        Cull Off

        HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float _CurrentHP;
                float _FlashSpeed;
                float _DelayHP;
                float4 _DelayColor;
                float _VerticalBillboarding;

                float _WobbleDensity;
                float _WobbleSpeed;
                float _WobbleStrength;

                float4 _FillRangeX;
                float4 _FillRangeY;

                // 高光与肌理属性
                float4 _HPNoiseTex_ST;
                float _NoiseIntensity;
                float4 _HighlightColor;
                float _HighlightWidth;
                float _HighlightPower;

                // 核心修复 1：补上缺失的颜色变量声明
                float4 _CurrentHPColor;
            CBUFFER_END

            TEXTURE2D(_BorderTex);
            SAMPLER(sampler_BorderTex);
            TEXTURE2D(_HPNoiseTex);
            SAMPLER(sampler_HPNoiseTex);

            struct VertexInput
            {
                float4 vertex : POSITION;
                float2 uv : TEXCOORD0;
            };

            struct VertexOutput
            {
                float4 posCS : SV_POSITION;
                float2 uv : TEXCOORD0;
                float flash : TEXCOORD1;
            };
        ENDHLSL

        Pass
        {
            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            VertexOutput vert(VertexInput v)
            {
                VertexOutput o;

                // -------------------- 公告牌核心算法 --------------------
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
                // ----------------------------------------------------

                o.posCS = TransformObjectToHClip(float4(localPos, 1));

                // 核心修复 2：这里必须传递最原始的 v.uv，绝对不能用噪声图的 ST 缩放它！
                // 否则边框贴图和 _FillRange 范围会全部错位。
                o.uv = v.uv;

                o.flash = cos(_Time.y * _FlashSpeed) * 0.5 + 0.5;
                return o;
            }

            half4 frag(VertexOutput i) : SV_Target
            {
                // 1. 采样上层边框贴图（由于 i.uv 恢复纯净，这里采样完全正确）
                float4 borderCol = SAMPLE_TEXTURE2D(_BorderTex, sampler_BorderTex, i.uv);

                // 2. 确定血条填充区域
                float inFillArea = step(_FillRangeX.x, i.uv.x) * step(i.uv.x, _FillRangeX.y)
                                 * step(_FillRangeY.x, i.uv.y) * step(i.uv.y, _FillRangeY.y);

                // 3. 将填充区域内的坐标重映射至标准的 [0, 1] 空间
                float rangeX = max(0.001, _FillRangeX.y - _FillRangeX.x);
                float rangeY = max(0.001, _FillRangeY.y - _FillRangeY.x);

                float2 fillUV;
                fillUV.x = saturate((i.uv.x - _FillRangeX.x) / rangeX);
                fillUV.y = saturate((i.uv.y - _FillRangeY.x) / rangeY);

                //血条左右浮动偏移
                float offset = sin((fillUV.y - 0.5) * _WobbleDensity + _Time.y * _WobbleSpeed) * _WobbleStrength;
                // 安全性修正：满血(>0.999)或空血(<0.001)时禁止扰动，防止穿帮
                float isAlive = step(0.001, _CurrentHP);
                float isNotFull = 1.0 - step(0.999, _CurrentHP);
                offset *= isAlive * isNotFull;
                // 附加偏移量后的实际边缘阈值
                float currentHPEdge = _CurrentHP + offset;
                float delayHPEdge = _DelayHP + offset; // 让拖尾也保持一致的波浪边缘

                // 4. 计算主血条和延迟血条的 Mask
                float healthbarMask = step(fillUV.x, currentHPEdge) * inFillArea;
                float delayMask = step(fillUV.x, delayHPEdge) * inFillArea;

                // 5. 使用材质球直接指定的颜色
                float4 healthColor = _CurrentHPColor;

                // 【细节一：叠加上一层或深或浅的表面肌理】
                // 核心修复 3：仅在此处采样时使用 _HPNoiseTex_ST 缩放偏移，且只乘 rgb 避免维度报错
                float3 hpNoise = SAMPLE_TEXTURE2D(_HPNoiseTex, sampler_HPNoiseTex, fillUV * _HPNoiseTex_ST.xy + _HPNoiseTex_ST.zw).rgb;
                healthColor.rgb = lerp(healthColor.rgb, healthColor.rgb * hpNoise, _NoiseIntensity);

                // 【细节二：计算血条尽头处的滑动白色高光】
                float distToHP = abs(fillUV.x - currentHPEdge);
                float tipHighlight = saturate(1.0 - (distToHP / _HighlightWidth));
                tipHighlight = pow(tipHighlight, _HighlightPower) * step(fillUV.x, currentHPEdge);
                float3 highlightRGB = _HighlightColor.rgb * tipHighlight;

                // 核心修复 4：只操作 rgb，保留 a 避免报错
                healthColor.rgb += highlightRGB;

                // 濒血闪烁（只影响颜色 rgb，不破坏透明度）
                if (_CurrentHP < 0.2)
                    healthColor.rgb *= i.flash;

                // 6. 混合血条、黄色拖尾与底色
                float3 barColor = _DelayColor.rgb * delayMask * (1.0 - healthbarMask) + healthColor.rgb * healthbarMask;
                float3 bgColor = float3(0.03, 0.03, 0.03); // 暗底色

                // float3 fillRGB = lerp(bgColor, barColor, max(healthbarMask, delayMask));
                float3 fillRGB = barColor;

                // 7. 【图层混合】边框盖在血条之上
                float3 finalRGB = lerp(fillRGB, borderCol.rgb, borderCol.a);
                float finalAlpha = max(borderCol.a, max(healthbarMask, delayMask));

                return half4(finalRGB, finalAlpha);
            }
            ENDHLSL
        }
    }
    Fallback "Hidden/Universal Render Pipeline/FallbackError"
}
