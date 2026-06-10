Shader "2DGames/URP/GeneralCharacterEffect"
{
    Properties
    {
        [PerRendererData][HideInInspector]_MainTex ("Sprite Texture", 2D) = "white" {}
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

        [Header(Keywords)]
        [Toggle(_DEATH_ON)]_DeathKeyword ("触发死亡消融的关键字", Float) = 0
        [Toggle(_INJURED_ON)]_InjuredKeyword ("触发受伤闪光的关键字", Float) = 0
        [Toggle(_PARRY_DEFENDER_ON)] _ParryDefenderKeyword ("防御者发光效果", Float) = 0

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
                
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_DissolveTex);
            SAMPLER(sampler_DissolveTex);

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
                /*float2 uv_DissolveBump : TEXCOORD2;
                float3 normalWS : TEXCOORD3;
                float3 tangentWS : TEXCOORD4;
                float3 bitangentWS : TEXCOORD5;*/
            };

            VertexOutput vert(VertexInput input)
            {
                VertexOutput output;
                output.posCS = TransformObjectToHClip(input.posOS.xyz);
                output.color = input.color; // 修复：必须传递顶点颜色

                output.uv_MainTex = input.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                output.uv_DissolveTex = input.uv * _DissolveTex_ST.xy + _DissolveTex_ST.zw;

                return output;
            }

            float4 frag(VertexOutput input) : SV_Target
            {
                // 采样 Sprite 基础图并应用顶点颜色（包含透明度）
                float4 spriteColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv_MainTex) * input.color;
                
                #ifdef _PARRY_DEFENDER_ON //精准防御逻辑
                    float2 offset = _MainTex_TexelSize.xy * _ParryGlowWidth;
                    float alphaUp = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv_MainTex + float2(0,offset.y)).a;
                    float alphaDown = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv_MainTex - float2(0,offset.y)).a;
                    float alphaLeft = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv_MainTex - float2(offset.x, 0)).a;
                    float alphaRight = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv_MainTex + float2(offset.x, 0)).a;

                    float avgAlpha = 0.25 * (alphaUp + alphaDown + alphaLeft + alphaRight);
                    float innerGlowFactor = spriteColor.a - avgAlpha;
                    float StrengthAlpha = saturate(innerGlowFactor * 4.0);

                    // 将高亮颜色叠加到边缘，并提升整体角色的明度
                    spriteColor.rgb = lerp(spriteColor.rgb, _ParryGlowColor.rgb, innerGlowFactor);
                    spriteColor.rgb += _ParryGlowColor.rgb * 0.3 * spriteColor.a; // 全局淡淡的荧光覆盖
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
