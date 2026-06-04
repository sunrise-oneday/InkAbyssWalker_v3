#ifndef POSTPROCESSING_INCLUDED
#define POSTPROCESSING_INCLUDED

// 包含URP核心库：提供常用宏、函数（如线性空间转换等）
#include"Packages/com.unity.render-pipelines.universal/ShaderLibrary/Core.hlsl"
// 包含Unity内置输入（如投影参数、屏幕参数等）
#include"Packages/com.unity.render-pipelines.universal/ShaderLibrary/UnityInput.hlsl"
// 包含颜色处理工具（如亮度计算、ACES色调映射等）
#include "Packages/com.unity.render-pipelines.core/ShaderLibrary/Color.hlsl"

// 声明主纹理 _BlitTexture，用于接收源图像（通常由后处理链传入）
TEXTURE2D(_BlitTexture);
// 采样器：线性过滤 + 钳位模式
SAMPLER(sampler_LinearClamp);

// 声明深度纹理，用于读取场景深度信息（可选）
TEXTURE2D(_CameraDepthTexture);
// 深度纹理采样器
SAMPLER(sampler_CameraDepthTexture);

// 顶点着色器输出的结构体
struct Varyings
{
    float2 uv : TEXCOORD0;          // 屏幕空间UV坐标
    float4 vertex : SV_POSITION;    // 裁剪空间顶点位置
    UNITY_VERTEX_OUTPUT_STEREO      // 立体渲染（VR）相关宏
};

// 用于生成全屏三角形/四边形的屏幕空间数据
struct ScreenSpaceData
{
    float4 positionCS;  // 裁剪空间坐标
    float2 uv;          // 对应的UV坐标
};

// 根据顶点ID生成全屏三角形的屏幕空间数据（无需顶点缓冲区）
ScreenSpaceData GetScreenSpaceData(uint vertexID : SV_VertexID)
{
    ScreenSpaceData output;
    // 生成覆盖NDC（-1到1）的三个顶点，形成覆盖整个屏幕的三角形
    // 顶点顺序：0: (-1,-1), 1: (-1, 3), 2: (3,-1)
    output.positionCS = float4(vertexID <= 1 ? -1.0 : 3.0, vertexID == 1 ? 3.0 : -1.0, 0.0, 1.0);
    // 对应的UV：顶点0:(0,0), 顶点1:(0,2), 顶点2:(2,0)
    output.uv = float2(vertexID <= 1 ? 0.0 : 2.0, vertexID == 1 ? 2.0 : 0.0);
    // 如果图形API的投影矩阵导致Y轴翻转（例如DirectX），需要翻转UV的Y方向
    if (_ProjectionParams.x < 0.0)
        output.uv.y = 1.0 - output.uv.y;
    return output;
}

// 从主纹理采样颜色（通过UV坐标）
half4 GetSource(float2 uv)
{
    return SAMPLE_TEXTURE2D(_BlitTexture, sampler_LinearClamp, uv);
}
// 从Varyings结构体中提取UV并采样
half4 GetSource(Varyings input)
{
    return GetSource(input.uv);
}

// 采样深度纹理，返回深度值（范围0-1，线性或非线性取决于纹理设置）
float SampleDepth(float2 uv)
{
    #if defined(UNITY_STEREO_INSTANCING_ENABLED) || defined(UNITY_STEREO_MULTIVIEW_ENABLED)
    // VR环境下采样纹理数组，根据当前眼睛索引选择层
    return SAMPLE_TEXTURE2D_ARRAY(_CameraDepthTexture, sampler_CameraDepthTexture, uv, unity_StereoEyeIndex).r;
    #else
    // 普通单目渲染
    return SAMPLE_DEPTH_TEXTURE(_CameraDepthTexture, sampler_CameraDepthTexture, uv);
    #endif
}

// 通过Varyings结构体采样深度
float SampleDepth(Varyings input)
{
    return SampleDepth(input.uv);
}

// 计算颜色的亮度（灰度值）
half GetLuminance(half3 colorLinear)
{
    #if _TONEMAP_ACES
    // 如果启用了ACES色调映射，使用ACES定义的亮度公式
    return AcesLuminance(colorLinear);
    #else
    // 默认使用标准亮度公式 (0.2126*R + 0.7152*G + 0.0722*B)
    return Luminance(colorLinear);
    #endif
}

// 全屏三角形绘制的顶点着色器（输入顶点ID，输出Varyings）
Varyings Vert(uint vertexID : SV_VertexID)
{
    Varyings output;
    ScreenSpaceData data = GetScreenSpaceData(vertexID);
    output.vertex = data.positionCS;
    output.uv = data.uv;
    return output;
}

#endif // POSTPROCESSING_INCLUDED
