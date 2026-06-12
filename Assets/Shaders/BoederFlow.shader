Shader "2DGames/URP/BoederFlow"
{
    Properties
    {
        [PerRendererData][HideInInspector]_MainTex ("Sprite Texture", 2D) = "white" {}
        _FlowTex ("流光贴图", 2D) = "white" {}
        _FlowSpeed ("流光速度", Float) = 1.0
        [HDR]_TintColor ("流光颜色", Color) = (1,1,1,1)

        /* [Header(Break Effect)]
        _BreakTex      ("破碎噪声贴图 (Voronoi)", 2D) = "white" {}
        _BreakProgress ("破碎进度",  Range(0,1))    = 0
        [HDR]_BreakEdgeColor ("破碎边缘颜色", Color) = (1, 0.4, 0.1, 1)
        _BreakEdgeWidth ("破碎边缘宽度", Range(0.001, 0.8)) = 0.05 */
    }

    SubShader
    {
       Tags{
            "RenderType" = "Transparent"
            "RenderPipeline" = "UniversalPipeline"
            "Queue" = "Transparent"
            "IgnoreProjector" = "True"
       }
       // 2D/Sprite 必须的渲染状态：开启混合，关闭深度写入，关闭剔除
       Blend SrcAlpha OneMinusSrcAlpha
       Cull Off
       ZWrite Off

       HLSLINCLUDE
            #include "Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float _FlowSpeed;
                float4 _TintColor;
                float4 _FlowTex_ST;

                /* float4 _BreakTex_ST;
                float _BreakProgress;
                float4 _BreakEdgeColor;
                float _BreakEdgeWidth; */
            CBUFFER_END

            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);
            TEXTURE2D(_FlowTex);
            SAMPLER(sampler_FlowTex);
            /* TEXTURE2D(_BreakTex);
            SAMPLER(sampler_BreakTex); */

       ENDHLSL

       Pass{
        Name "BorderFlow"
        Tags { "LightMode" = "Universal2D" }

        HLSLPROGRAM
            #pragma vertex vert
            #pragma fragment frag

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
                float2 uv : TEXCOORD0;
                float2 flowUV : TEXCOORD1;
            };

            VertexOutput vert(VertexInput input)
            {
                VertexOutput output;
                output.color = input.color;
                output.posCS = TransformObjectToHClip(input.posOS);
                // 1. 基础 Sprite UV
                output.uv = input.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                // 2. 流动效果 UV
                float timeOffset = frac(_Time.y * _FlowSpeed);
                output.flowUV = input.uv * _FlowTex_ST.xy + _FlowTex_ST.zw;
                output.flowUV.x += timeOffset;
                return output;
            }

            float4 frag(VertexOutput input) : SV_Target
            {
                float4 spriteColor = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv) * input.color;
                float4 var_FlowTex = SAMPLE_TEXTURE2D(_FlowTex, sampler_FlowTex, input.flowUV);

                /* // ② 破碎效果 ─────────────────────────────────────────
                float2 breakUV  = input.uv * _BreakTex_ST.xy + _BreakTex_ST.zw;
                float  noise    = SAMPLE_TEXTURE2D(_BreakTex, sampler_BreakTex, breakUV).r;
                clip(noise - _BreakProgress);
                // 破碎边缘：在 clip 临界区附近叠加高亮
                float edgeFactor = 1.0 - saturate((noise - _BreakProgress) / _BreakEdgeWidth); */

                float3 flowColor = var_FlowTex.rgb * _TintColor.rgb * var_FlowTex.a;
                float4 finalColor = spriteColor;
                finalColor.rgb += flowColor * finalColor.a;
                return finalColor;
            }
        ENDHLSL
       }
    }
}
