Shader "2DGames/URP/GeneralCharacterEffect"
{
    Properties
    {
        [PerRendererData][HideInInspector] _MainTex ("Sprite Texture", 2D) = "white" {}
        [Header(Injured)]
        _FlashColor ("受伤时人物的闪烁颜色", Color) = (1, 0, 0, 1)
        _FlashIntensity ("受伤时人物的闪烁强度", Range(0, 1)) = 0.4

        [Header(DeathDissolve)]
        _DissolveTex ("死亡时的消融纹理", 2D) = "white" {}
        _DissolveProgress ("消融进度", Range(0, 1)) = 0 // 代替复杂的 _Time.y 逻辑，更推荐用 C# 直接控制此值
        _LineWidth ("消融线宽", Range(0, 0.6)) = 0.1
        _DissolveSpeed ("消融速度", Range(0, 1)) = 0.5
        [HDR]_DissolveInternalColor ("消融内部颜色", Color) = (0, 0, 0, 1)
        [HDR]_DissolveExternalColor ("消融外部颜色", Color) = (1, 1, 1, 1)
        
        [Header(Perfect Parry)]
        [HDR]_ParryGlowColor ("完美防御发光颜色 (建议高亮蓝色)", Color) = (0, 0.5, 1, 1)
        _ParryGlowWidth ("发光边缘宽度", Range(0, 5)) = 2.0
        /*[Toggle(_PARRY_ATTACKER_ON)]_ParryAttackerKeyword ("攻击者受挫/色散残影", Float) = 0
        _ParryGhostOffset ("残影偏移量", Range(0, 0.1)) = 0.02*/

        [Header(Perfect Dodge)]
        [Toggle(_DODGE_ON)] _DodgeKeyword ("开启完美闪避效果", Float) = 0
        _DodgeProgress ("闪避进度", Range(0, 1)) = 0 // 由 C# 控制，0 -> 1 -> 0 循环
        _DodgeMaxScale ("闪避最大膨胀比例", Range(1, 2)) = 1.25
        _DodgeNoiseTex ("闪避噪声纹理", 2D) = "white" {} // 请将您上传的噪声图赋到此处
        _DodgeGlitchAmount ("故障偏移强度", Range(0, 0.2)) = 0.05
        _DodgeGlitchFrequency ("故障条纹频率", Range(1, 100)) = 15.0
        _DodgeGlitchSpeed ("故障动画速度", Range(0, 10)) = 2.0
        [HDR]_DodgeColorInternal ("闪避内部虚影颜色", Color) = (0, 0.6, 1, 1)
        [HDR]_DodgeColorExternal ("闪避外部发光颜色", Color) = (0, 1, 0.8, 1)
        _DodgeGlowIntensity ("虚影发光强度", Range(1, 5)) = 1.5
        _DodgeAlpha ("最虚化时透明度", Range(0, 1)) = 0.4
        _DodgeBlend ("虚化混合度 (0=原色, 1=纯色虚影)", Range(0, 1)) = 0.8

        [Header(Keywords)]
        [Toggle(_DEATH_ON)]_DeathKeyword ("触发死亡消融的关键字", Float) = 0
        [Toggle(_INJURED_ON)]_InjuredKeyword ("触发受伤闪光的关键字", Float) = 0
        [Toggle(_PARRY_DEFENDER_ON)] _ParryDefenderKeyword ("防御者发光效果", Float) = 0
        [Toggle(_DODGE_ON)]_DodgeKeyword ("闪避虚影效果", Float) = 0
    }

    SubShader
    {
       Tags{
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
       }
       // 2D/Sprite 必须的渲染状态：开启混合，关闭深度写入，关闭剔除
       Blend SrcAlpha OneMinusSrcAlpha
       Cull Off
       ZWrite Off

       HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
            // Lighting.hlsl 未使用，已移除


            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _MainTex_TexelSize;
                float4 _FlashColor;
                float  _FlashIntensity;

                float4 _DissolveTex_ST;
                float  _DissolveProgress;
                float  _LineWidth;
                float  _DissolveSpeed;
                float4 _DissolveInternalColor;
                float4 _DissolveExternalColor;
                float  _ParryDefenderKeyword;
                float4 _ParryGlowColor;
                float  _ParryGlowWidth;
                // float  _ParryAttackerKeyword;
                // float  _ParryGhostOffset;
                // Perfect Dodge
                float  _DodgeKeyword;
                float  _DodgeProgress;
                float  _DodgeMaxScale;
                float4 _DodgeNoiseTex_ST;
                float  _DodgeGlitchAmount;
                float  _DodgeGlitchFrequency;
                float  _DodgeGlitchSpeed;
                float4 _DodgeColorInternal;
                float4 _DodgeColorExternal;
                float  _DodgeGlowIntensity;
                float  _DodgeAlpha;
                float  _DodgeBlend;
                
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_DissolveTex);
            SAMPLER(sampler_DissolveTex);
            TEXTURE2D(_DodgeNoiseTex);
            SAMPLER(sampler_DodgeNoiseTex);

       ENDHLSL

       Pass{
        Name "GeneralCharacterEffectPass"
        Tags { "LightMode" = "Universal2D" }

        HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #pragma shader_feature_local _INJURED_ON
            #pragma shader_feature_local _DEATH_ON
            #pragma shader_feature_local _PARRY_DEFENDER_ON
            #pragma shader_feature_local _DODGE_ON

            struct VertexInput
            {
                float4 posOS : POSITION;
                float4 color : COLOR; // 接收 Sprite 渲染器传入的顶点颜色(包括透明度)
                float2 uv : TEXCOORD0;
            };

            struct VertexOutput
            {
                float4 posCS : SV_POSITION;
                float4 color : COLOR;
                float2 uv_MainTex : TEXCOORD0;
                float2 uv_DissolveTex : TEXCOORD1;
                float2 uv_DodgeNoiseTex : TEXCOORD2;
                /*float2 uv_DissolveBump : TEXCOORD2;
                float3 normalWS : TEXCOORD3;
                float3 tangentWS : TEXCOORD4;
                float3 bitangentWS : TEXCOORD5;*/
            };

            VertexOutput vert(VertexInput input)
            {
                VertexOutput output;
                float3 posOS = input.posOS.xyz;

                #ifdef _DODGE_ON
                    // 正弦缩放曲线：0 -> 1 -> 0，正好对应 _DodgeProgress 的 0 -> 0.5 -> 1
                    float scaleCurve = sin(_DodgeProgress * 3.14159265);
                    // 2D 顶点根据原点膨胀（如果 Sprite 的 Pivot 在脚底，则会往上及两侧膨胀）
                    float currentScale = 1.0 + (_DodgeMaxScale - 1.0) * scaleCurve;
                    posOS.xy *= currentScale;
                #endif

                output.posCS = TransformObjectToHClip(posOS);
                output.color = input.color;

                output.uv_MainTex = input.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                output.uv_DissolveTex = input.uv * _DissolveTex_ST.xy + _DissolveTex_ST.zw;
                
                #ifdef _DODGE_ON
                    output.uv_DodgeNoiseTex = input.uv * _DodgeNoiseTex_ST.xy + _DodgeNoiseTex_ST.zw;
                #else
                    output.uv_DodgeNoiseTex = float2(0, 0);
                #endif

                return output;
            }

            float4 frag(VertexOutput input) : SV_Target
            {
                float2 mainUV = input.uv_MainTex;
                float scaleCurve = 0.0;
                float4 dodgeEffectMask = float4(0.0, 0.0, 0.0, 0.0);

                #ifdef _DODGE_ON
                    scaleCurve = sin(_DodgeProgress * 3.14159265);
                    
                    // 采样闪避噪声图
                    float noiseVal = SAMPLE_TEXTURE2D(_DodgeNoiseTex, sampler_DodgeNoiseTex, input.uv_DodgeNoiseTex).r;

                    // 1. 移植并修改 CyberPunkAnimation 逻辑：生成基于 UV.y 的锯齿波遮罩
                    // 加入 _Time.y 扰动使即便处于静止状态下条纹也具有微弱生命力
                    float baseMask = abs(frac(input.uv_MainTex.y * _DodgeGlitchFrequency - _DodgeProgress * _DodgeGlitchSpeed - _Time.y * 0.5) - 0.5) * 2.0;
                    baseMask = min(1.0, baseMask * 2.0);

                    // 2. 用 Noise 偏移锯齿波
                    baseMask += (noiseVal - 0.5) * 0.5;

                    // 3. 产生多级 SmoothStep 遮罩并存入 dodgeEffectMask (对应原函数的 effectMask)
                    dodgeEffectMask.x = smoothstep(0.0, 0.9, baseMask);
                    dodgeEffectMask.y = smoothstep(0.2, 0.8, baseMask);
                    dodgeEffectMask.z = smoothstep(0.4, 0.6, baseMask);
                    dodgeEffectMask.w = scaleCurve; // 使用 W 分量存储全局进度

                    // 4. 水平方向的 UV 抖动（故障撕裂）。抖动强度随着进度达到正弦峰值（0.5 处）而变大
                    float jitter = (noiseVal - 0.5) * _DodgeGlitchAmount * (1.0 - dodgeEffectMask.y) * scaleCurve;
                    mainUV.x += jitter;
                #endif

                // 采样原图。若开启闪避，则采样被抖动/故障偏移后的 UV 坐标
                float4 spriteColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, mainUV) * input.color;
                
                #ifdef _PARRY_DEFENDER_ON //精准防御逻辑
                    float2 offset = _MainTex_TexelSize.xy * _ParryGlowWidth;
                    float alphaUp = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, mainUV + float2(0,offset.y)).a;
                    float alphaDown = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, mainUV - float2(0,offset.y)).a;
                    float alphaLeft = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, mainUV - float2(offset.x, 0)).a;
                    float alphaRight = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, mainUV + float2(offset.x, 0)).a;

                    float avgAlpha = 0.25 * (alphaUp + alphaDown + alphaLeft + alphaRight);
                    float innerGlowFactor = spriteColor.a - avgAlpha;
                    float StrengthAlpha = saturate(innerGlowFactor * 4.0);

                    // 将高亮颜色叠加到边缘，并提升整体角色的明度
                    spriteColor.rgb = lerp(spriteColor.rgb, _ParryGlowColor.rgb, innerGlowFactor);
                    spriteColor.rgb += _ParryGlowColor.rgb * 0.3 * spriteColor.a; // 全局淡淡的荧光覆盖
                #endif
                
                #ifdef _DODGE_ON // 完美闪避着色逻辑
                    // 5. 将原函数的 effectMask 分解并应用于颜色插值
                    float3 dodgeGhostColor = lerp(_DodgeColorInternal.rgb, _DodgeColorExternal.rgb, dodgeEffectMask.x);
                    dodgeGhostColor *= _DodgeGlowIntensity;

                    // 虚影混合：_DodgeBlend 决定了是保留原画细节（0），还是化作纯赛博能量虚影（1）
                    float finalBlend = scaleCurve * _DodgeBlend;
                    spriteColor.rgb = lerp(spriteColor.rgb, dodgeGhostColor, finalBlend);

                    // 全局亮色能量边缘的淡淡荧光覆盖
                    spriteColor.rgb += _DodgeColorExternal.rgb * 0.2 * scaleCurve * spriteColor.a;

                    // 虚化透明度：角色最虚化时，透明度向 _DodgeAlpha 渐变，达成空灵的虚影效果
                    spriteColor.a *= lerp(1.0, _DodgeAlpha, scaleCurve);
                #endif

                // 1. 死亡消融逻辑
                #ifdef _DEATH_ON
                    float4 var_DissolveTex = SAMPLE_TEXTURE2D(_DissolveTex, sampler_DissolveTex, input.uv_DissolveTex);

                    // 当噪声图的值小于当前消融进度时，剪切掉该像素
                    clip(var_DissolveTex.r - _DissolveProgress);

                    // 计算消融边缘（只有靠近剪切边缘的像素才会有发光系数）
                    float edgeDist = var_DissolveTex.r - _DissolveProgress;
                    float edgeFactor = 1.0 - smoothstep(0.0, _LineWidth, edgeDist);

                    // 在消融边缘区域进行颜色插值（从内部发光到外部发光）
                    float3 dissolveColor = lerp(_DissolveInternalColor.rgb, _DissolveExternalColor.rgb, edgeFactor);

                    // 【核心改进：引入焦黑碳化边缘 (Burned Edge)】
                    // 我们在最外侧（靠近 0 的区域）乘上一个渐变到 0 的黑色系数
                    // 这样发光边缘的最外圈会变成黑色，形成高对比度的暗色轮廓线，在重叠时强行隔离视觉
                    float burnFactor = smoothstep(0.0, _LineWidth * 0.1, edgeDist); // 在边缘最外侧 25% 的线宽内渐变到黑色
                    dissolveColor *= burnFactor;

                    // 只在消融进行中（_DissolveProgress > 0）且靠近边缘时叠加发光颜色
                    float applyEdgeGlow = edgeFactor * step(0.001, _DissolveProgress);
                    spriteColor.rgb = lerp(spriteColor.rgb, dissolveColor, applyEdgeGlow);
                #endif

                // 2. 受伤闪光逻辑（放在消融后计算，确保闪光能盖住消融边缘）
                #ifdef _INJURED_ON
                    // 修复：只对 .rgb 通道插值，绝不污染原图的 .a 通道，避免方形色块
                    spriteColor.rgb = lerp(spriteColor.rgb, _FlashColor.rgb, _FlashIntensity);
                #endif


                return spriteColor;
            }
        ENDHLSL
       }
    }
}
