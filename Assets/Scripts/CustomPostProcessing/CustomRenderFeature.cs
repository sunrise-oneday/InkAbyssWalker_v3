using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CustomRenderFeature : ScriptableRendererFeature
{
    private List<CustomPostProcessing> mCustomPostProcessings;

    // 针对不同的注入点，创建不同的 Pass 实例
    private CustomPostProcessingPass m_AfterOpaqueAndSkyPass;
    private CustomPostProcessingPass m_BeforePostProcessPass;
    private CustomPostProcessingPass m_AfterPostProcessPass;

    // URP 14 适配：用于官方后处理结束之后（AfterPostProcess）的系统临界 RT 句柄
    private RTHandle m_AfterPostProcessTexture;

    public override void Create()
    {
        // 1. 利用全局 Volume 堆栈，获取所有继承自 CustomPostProcessing 的后处理组件
        var stack = VolumeManager.instance.stack;
        if (stack == null) return;

        // 动态抓取当前堆栈中存在的所有自定义组件实例
        mCustomPostProcessings = VolumeManager.instance.baseComponentTypeArray
            .Where(t => t.IsSubclassOf(typeof(CustomPostProcessing)) && stack.GetComponent(t) != null)
            .Select(t => stack.GetComponent(t) as CustomPostProcessing)
            .ToList();
        if (mCustomPostProcessings.Count == 0) return;

        // 筛选出需要在 AfterOpaqueAndSky 阶段执行的效果，并按 OrderInEvent 排序
        var afterOpaqueAndSkyCPPs = mCustomPostProcessings
                            .Where(c => c.GetInjectionPoint == CustomPostProcessInjectionPoint.AfterOpaqueAndSky)
                            .OrderBy(c => c.OrderInEvent).ToList();
        m_AfterOpaqueAndSkyPass = new CustomPostProcessingPass("Custom PostProcess after Skybox",
                                                                afterOpaqueAndSkyCPPs);
        m_AfterOpaqueAndSkyPass.renderPassEvent = RenderPassEvent.AfterRenderingOpaques;

        //筛选出需要在 BeforePostProcess 阶段执行的效果，并按 OrderInEvent 排序
        var beforePostProcessCPPs = mCustomPostProcessings
                                    .Where(c => c.GetInjectionPoint == CustomPostProcessInjectionPoint.BeforePostProcess)
                                    .OrderBy(c => c.OrderInEvent).ToList();
        m_BeforePostProcessPass = new CustomPostProcessingPass("Custom PostProcess before PostProcess",
                                                                beforePostProcessCPPs);
        m_BeforePostProcessPass.renderPassEvent = RenderPassEvent.BeforeRenderingPostProcessing;

        //筛选出需要在 AfterPostProcess 阶段执行的效果，并按 OrderInEvent 排序
        var afterPostProcessCPPs = mCustomPostProcessings
                                    .Where(c => c.GetInjectionPoint == CustomPostProcessInjectionPoint.AfterPostProcess)
                                    .OrderBy(c => c.OrderInEvent).ToList();
        m_AfterPostProcessPass = new CustomPostProcessingPass("Custom PostProcess after PostProcess",
                                                                afterPostProcessCPPs);
        m_AfterPostProcessPass.renderPassEvent = RenderPassEvent.AfterRenderingPostProcessing;


    }

    /// <summary>
    /// 向渲染器中添加渲染Pass（每一帧都会调用）
    /// </summary>
    /// <param name="renderer">当前使用的ScriptableRenderer</param>
    /// <param name="renderingData">当前帧的渲染数据</param>
    public override void AddRenderPasses(ScriptableRenderer renderer, ref RenderingData renderingData)
    {
        //过滤无关相机，如果是预览相机或反射探针，则直接返回
        if (renderingData.cameraData.cameraType == CameraType.Preview ||
            renderingData.cameraData.cameraType == CameraType.Reflection) return;
        // 仅当当前相机启用了后处理效果时，才添加自定义后处理Pass
        if (renderingData.cameraData.postProcessEnabled)
        {
            // 尝试设置 AfterOpaqueAndSky 阶段的效果，如果存在激活的效果则添加Pass
            if (m_AfterOpaqueAndSkyPass.SetupCustomPostProcessing())
            {
                m_AfterOpaqueAndSkyPass.ConfigureInput(ScriptableRenderPassInput.Color);
                renderer.EnqueuePass(m_AfterOpaqueAndSkyPass);

            }
            //原理同上
            if (m_BeforePostProcessPass.SetupCustomPostProcessing())
            {
                m_BeforePostProcessPass.ConfigureInput(ScriptableRenderPassInput.Color);
                renderer.EnqueuePass(m_BeforePostProcessPass);
            }
            if (m_AfterPostProcessPass.SetupCustomPostProcessing())
            {
                m_AfterPostProcessPass.ConfigureInput(ScriptableRenderPassInput.Color);
                renderer.EnqueuePass(m_AfterPostProcessPass);
            }
        }
    }
    /// <summary>
    /// 释放资源时调用，清理自定义后处理效果实例和渲染Pass
    /// </summary>
    /// <param name="disposing">是否为主动释放（true）还是由GC调用（false）</param>
    protected override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        if (disposing && mCustomPostProcessings != null)
        {
            foreach (var item in mCustomPostProcessings)
            {
                item.Dispose();
            }
        }
        m_AfterOpaqueAndSkyPass.Dispose();
        m_BeforePostProcessPass.Dispose();
        m_AfterPostProcessPass.Dispose();
    }
}
