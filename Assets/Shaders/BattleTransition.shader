Shader "2DGames/URP/BattleTransition"
{
    Properties
    {
        _CrackMask ("碎裂效果的蒙版贴图", 2D) = "white" {}
    }
    SubShader
    {
        Tags { "RenderType"="Opaque"
                "RenderPipleline" = "UniversalPipeline"}
        ZTest Always ZWrite Off Cull Off

        HLSLINCLUDE
            #include "Assets/Scripts/Common/CustomPostProcessing.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _CrackMask_ST;
                float  _Progress;
                float  _MaxZoom;
                float  _BlurStrength;
                float  _RGBSplitAmount;
                float  _PulseDensity;
                float  _PulseSpeed;
                int    _BlurSamples;
            CBUFFER_END
            TEXTURE2D(_CrackMask);
            SAMPLER(sampler_CrackMask);
        ENDHLSL

        Pass
        {
            Name "BattleTransition"
            HLSLPROGRAM
            #pragma vertex Vert
            #pragma fragment Frag

            real4 Frag(Varyings input) : SV_Target
            {
                float2 uv = input.uv;
                float2 centerUV = uv - 0.5;
                //采样破碎纹理
                half maskValue = saturate(SAMPLE_TEXTURE2D(_CrackMask, sampler_CrackMask, uv).r) ;

                // 让裂缝在转场前期爆闪发光，随着转场进度(0->1)呈现正弦波式的亮起又消散
                float crackIntensity = maskValue * sin(_Progress * 3.14159);
                // 赋予裂缝科幻的高光颜色！崩铁常用水蓝色或紫红色。这里使用极高亮度的白蓝色叠加
                // 乘以 3.0 强制制造 HDR 过曝发光感
                half3 crackGlow = half3(0.3, 0.1, 0.5) * crackIntensity * 3.0;
                // --------------------------------------------------------
                // 质变核心 2：微观撕裂而非果冻扭曲
                // --------------------------------------------------------
                // 大幅降低 UV 扰动强度，仅在裂缝处做极细微的推离，避免画面变成水波纹
                float distortion = maskValue * _Progress * 0.03;
                float2 distortedUV = uv + centerUV * distortion;

                // --- 细节2：非线性中心拉近 (Zoom) ---
                // 使用 pow() 让拉近的过程“先慢后快”，产生被黑洞吸入的爆发力
                float currentZoom = lerp(1.0, 1.0 - _MaxZoom, pow(_Progress,2.0));
                float2 zoomedUV = (distortedUV - 0.5) * currentZoom + 0.5;

                //径向模糊 + 边缘色散分离
                half3 finalColor = half3(0,0,0);
                float currentBlur = pow(_Progress,1.5) * _BlurStrength;
                float currentRGB = _Progress * _RGBSplitAmount;

                int safeSamples = max(1,_BlurSamples);
                for(int i = 0; i < safeSamples; i++)
                {
                    float scale = 1.0 - (currentBlur * (float(i) / float(safeSamples - 1)));
                    float2 sampleUV = (zoomedUV - 0.5) * scale + 0.5;

                    // 优化：色散现象通常在镜头边缘更明显，中心点不色散
                    float edgeFactor = length(sampleUV - 0.5) * 2.0;
                    float2 rUV = sampleUV - centerUV * currentRGB * edgeFactor;
                    float2 bUV = sampleUV + centerUV * currentRGB * edgeFactor;

                    // 使用你库中的 GetSource 进行采样
                    half r = GetSource(rUV).r;
                    half g = GetSource(sampleUV).g;
                    half b = GetSource(bUV).b;

                    finalColor += half3(r, g, b);
                }
                finalColor /= safeSamples;

                float jumpyProgress = floor(pow(_Progress,2.0) * 3.0) /3.0;

                // 裂隙从屏幕中心向外蔓延：dist 0=中心, dist越大越靠边缘
                float dist = length(centerUV) * 2.0; // 归一化到 0(中心) ~ 1.4(角落)
                float spreadEdge = jumpyProgress  * 1.5;  // 蔓延半径随进度扩大

                float jaggedDistance = dist - maskValue *0.14;
                float spreadMask = step(jaggedDistance, spreadEdge);


                // 在蔓延边缘处形成脉冲波纹（仅边缘发光，内部已裂开、外部未到达）
                float edgeDist = abs(dist - spreadEdge);
                float pulseWave = sin(edgeDist * 20.0 - _Time.y * 15.0);
                float pulse = saturate(sin(edgeDist * _PulseDensity - _Time.y * _PulseSpeed));
                float edgeFade = 1.0 - smoothstep(0.0, 0.15, edgeDist); // 只在边缘附近可见
                // 裂隙 = 贴图采样值 × 蔓延遮罩 + 脉冲边缘光
                float crackVis = maskValue * spreadMask + pulse * edgeFade * 0.3;
                finalColor += crackGlow * crackVis;

                // --- 细节4：末端过曝闪白 (Flash Bang) ---
                // 崩铁转场最后屏幕会炸开一片白光，掩盖新场景加载
                // 当进度 > 0.7 时，快速向纯白色过渡
                float flashBite = smoothstep(0.6, 1.0, _Progress);
                finalColor = lerp(finalColor, half3(1.0, 1.0, 1.0), flashBite * 0.5);

                return half4(finalColor, 1.0);
            }
            ENDHLSL
        }
    }
}
