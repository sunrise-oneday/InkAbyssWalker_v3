Shader "2DGames/URP/SamplingTex_SingleDrawCall"
{
    Properties
    {
        _MainTex1 ("采样贴图 1", 2D) = "white" {}
        _MainTex2 ("采样贴图 2", 2D) = "white" {}
        _MainTex3 ("采样贴图 3", 2D) = "white" {}
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

            // 完美符合 SRP Batcher 规范
            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex1_ST;
                float4 _MainTex2_ST;
                float4 _MainTex3_ST;
            CBUFFER_END

            TEXTURE2D(_MainTex1);
            SAMPLER(sampler_MainTex1);
            TEXTURE2D(_MainTex2);
            SAMPLER(sampler_MainTex2);
            TEXTURE2D(_MainTex3);
            SAMPLER(sampler_MainTex3);

            struct Attributes
            {
                float3 positionOS : POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                // 【核心：从粒子系统接收 0, 1, 2 的切换值】
                float  switchVal  : TEXCOORD1; 
            };

            struct Varyings
            {
                float4 positionCS : SV_POSITION;
                float2 uv         : TEXCOORD0;
                float4 color      : COLOR;
                float  switchVal  : TEXCOORD1;
            };

            Varyings vert(Attributes input)
            {
                Varyings output = (Varyings)0;
                output.positionCS = TransformObjectToHClip(input.positionOS);
                output.uv = input.uv.xy * _MainTex1_ST.xy + _MainTex1_ST.zw;
                output.color = input.color;
                
                // 将接收到的值传给片元着色器
                output.switchVal = input.switchVal; 
                
                return output;
            }

            float4 frag(Varyings input) : SV_Target
            {
                float4 texColor  = SAMPLE_TEXTURE2D(_MainTex1, sampler_MainTex1, input.uv);
                float4 texColor2 = SAMPLE_TEXTURE2D(_MainTex2, sampler_MainTex2, input.uv);
                float4 texColor3 = SAMPLE_TEXTURE2D(_MainTex3, sampler_MainTex3, input.uv);

                // 使用传进来的值进行权重计算
                float sw = input.switchVal;
                float w2 = saturate(sw);
                float w3 = saturate(sw - 1.0);
                float w1 = (1.0 - w2) * (1.0 - w3);

                float4 finalColor = texColor * w1 + texColor2 * (w2 - w3) + texColor3 * w3;
                
                return finalColor * input.color;
            }
            ENDHLSL
        }
    }
}