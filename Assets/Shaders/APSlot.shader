Shader "2DGames/URP/APSlot"
{
    Properties
    {
        [PerRendererData][HideInInspector]_MainTex ("Sprite Texture", 2D) = "white" {}
        _APTex ("AP边框贴图", 2D) = "white" {}
        [HDR]_TintColor ("AP显示颜色", Color) = (1,1,1,1)
        _Residue ("AP剩余量",Int) = 9

        [Header(Pixel Layout Settings)]
        _TexWidth ("贴图总宽度(像素)", Float) = 1366
        _Margin ("两端边距(像素)", Float) = 76
        _Spacing ("菱形间距(像素)", Float) = 17
        _DiamondCount ("菱形总数", Float) = 9
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

            CBUFFER_START(UnityPerMaterial)
                float4 _MainTex_ST;
                float4 _APTex_ST;
                float4 _TintColor;
                int _Residue;

                float _TexWidth;
                float _Margin;
                float _Spacing;
                float _DiamondCount;
            CBUFFER_END

            TEXTURE2D(_APTex);
            SAMPLER(sampler_APTex);
            TEXTURE2D(_MainTex);
            SAMPLER(sampler_MainTex);


       ENDHLSL

       Pass{
        Name "BorderFlow"

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
                float2 uv_APTex : TEXCOORD0;
                float2 uv_MainTex : TEXCOORD1;
            };

            VertexOutput vert(VertexInput input)
            {
                VertexOutput output;
                output.color = input.color;
                output.posCS = TransformObjectToHClip(input.posOS);
                // 1. 基础 Sprite UV
                output.uv_MainTex = input.uv * _MainTex_ST.xy + _MainTex_ST.zw;
                output.uv_APTex = input.uv * _APTex_ST.xy + _APTex_ST.zw;

                return output;
            }

            float4 frag(VertexOutput input) : SV_Target
            {
                float var_MainTex = SAMPLE_TEXTURE2D(_MainTex, sampler_MainTex, input.uv_MainTex) * input.color;
                float var_APTex = SAMPLE_TEXTURE2D(_APTex, sampler_APTex, input.uv_APTex).r;

                //将像素单位转换到归一化的 UV 空间 (0.0 到 1.0)
                float uvMargin = _Margin / _TexWidth;
                float uvSpacing = _Spacing / _TexWidth;

                float uvDiamondWidth = (1.0 - (2.0 * uvMargin) - ((_DiamondCount - 1.0) * uvSpacing)) / _DiamondCount;

                // 3. 计算当前的 UV 截断点
                float cutoffUV = 0.0;
                if (_Residue > 0.0)
                {
                    // 截断点 = 左边距 + 剩余个数 * 菱形宽度 + (剩余个数 - 1) * 间距
                    // 这个位置刚好是第 _Residue 个菱形的右边缘位置
                    cutoffUV = uvMargin + _Residue * uvDiamondWidth + (_Residue - 1.0) * uvSpacing;
                }

                // 4. 判断当前像素的 uv.x 是否在截断点左侧
                // 如果 uv.x <= cutoffUV，则 isVisible = 1.0，否则为 0.0
                float isVisible = step(input.uv_APTex.x, cutoffUV);


                float4 finalTintColor = _TintColor * input.color;
                float3 APColor = var_APTex * finalTintColor.rgb;
                float APAlpha = var_APTex * finalTintColor.a * isVisible;
                return float4(APColor, APAlpha);
            }
        ENDHLSL
       }
    }
}
