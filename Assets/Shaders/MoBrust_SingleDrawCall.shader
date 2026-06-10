Shader "2DGames/URP/MoBrust_Ultimate"
{
    Properties
    {
        _MainTex ("泼溅贴图 (RGB打包)", 2D) = "white" {}
    }

    SubShader
    {
        Tags
        {
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
        }

        Blend SrcAlpha OneMinusSrcAlpha
        Cull Off
        ZWrite Off

        Pass
        {
            Name "PoMoBrustUltimate"
            Tags { "LightMode" = "SRPDefaultUnlit" }

            HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);

            // 【核心修复：精准匹配 Unity 的顶点流打包规则】
            struct Attributes
            {
                float3 positionOS : POSITION;
                float4 colorLeft  : COLOR;      // 接收自带 Color (用作左侧渐变色)
                float4 texcoord0  : TEXCOORD0;  // 包含: UV.xy 和 Mask.xy
                float4 texcoord1  : TEXCOORD1;  // 包含: Mask.z 和 RightColor.rgb
                float  texcoord2  : TEXCOORD2;  // 包含: RightColor.a
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float3 mask       : TEXCOORD1;
                float4 colorLeft  : COLOR0;
                float4 colorRight : COLOR1;
                float  baseUVx    : TEXCOORD2;  // 用于颜色渐变的纯净UV.x
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                
                // 1. 拆包 UV
                output.uv = input.texcoord0.xy * _MainTex_ST.xy + _MainTex_ST.zw;
                output.baseUVx = input.texcoord0.x; // 记录未经平移缩放的原始 UV.x
                
                // 2. 拆包 通道遮罩 (Mask)
                output.mask = float3(input.texcoord0.zw, input.texcoord1.x);
                
                // 3. 拆包 颜色
                output.colorLeft = input.colorLeft;
                output.colorRight = float4(input.texcoord1.yzw, input.texcoord2.x);
                
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                // 采样合并后的贴图
                float4 texColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv);
                
                // 根据传入的遮罩提取目标通道的灰度值
                float maskValue = dot(texColor.rgb, input.mask);

                // 按 UV.x 进行左右渐变融合
                // 如果你想上下渐变，把 input.baseUVx 改成 input.uv.y 即可
                float4 uvGradientColor = lerp(input.colorLeft, input.colorRight, saturate(input.baseUVx));

                float masterAlpha = input.colorLeft.a;//剥离 Alpha 渐变，直接使用原生顶点的 Alpha (colorLeft.a) 作为全局透明度

                // 最终颜色 = 贴图灰度形状 * 渐变色
                float4 finalColor;
                finalColor.rgb = maskValue * uvGradientColor.rgb;
                finalColor.a   = maskValue * masterAlpha;

                return finalColor;
            }
            ENDHLSL
        }
    }
}