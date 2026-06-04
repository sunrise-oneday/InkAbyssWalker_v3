using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.Profiling;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

public class CustomPostProcessingPass : ScriptableRenderPass
{
    // 所有自定义后处理基类
    private List<CustomPostProcessing> myCustomPostProcessing;
    // 当前active组件下标
    private List<int> mActiveIndex;

    //每个组件的ProfilingSampler
    private string mProfilerTag;
    private List<ProfilingSampler> myProfilerSampler;

    //声明RT(RenderTexture/RenderTarget)
    private RTHandle mSourceRT;
    private RTHandle mDestinationRT;
    private RTHandle mTempRT0;
    private RTHandle mTempRT1;
    private string mTempRT0Name => "_TemporaryRenderTexture0";
    private string mTempRT1Name => "_TemporaryRenderTexture1";

    /// <summary>
    /// 构造函数，初始化Pass的名称、自定义效果列表，并为每个效果创建ProfilingSampler，分配两个临时RTHandle。
    /// </summary>
    /// <param name="profilerTag">Profiler中显示的标签</param>
    /// <param name="customPostProcessings">该Pass要管理的自定义后处理效果列表</param>
    public CustomPostProcessingPass(string profilerTag, List<CustomPostProcessing> customPostProcessings)
    {
        mProfilerTag = profilerTag;
        myCustomPostProcessing = customPostProcessings;
        mActiveIndex = new List<int>(customPostProcessings.Count);
        // 为每个后处理效果创建一个ProfilingSampler，名称使用效果对象的ToString()
        myProfilerSampler = customPostProcessings.Select(c => new ProfilingSampler(c.ToString())).ToList();

        // 分配两个临时RTHandle，用于渲染链中的乒乓缓冲
        mTempRT0 = RTHandles.Alloc(mTempRT0Name, name: mTempRT0Name);
        mTempRT1 = RTHandles.Alloc(mTempRT1Name, name: mTempRT1Name);
    }

    /// <summary>
    /// 设置该Pass需要执行的后处理效果：调用每个效果的Setup方法，并筛选出处于激活状态的效果。
    /// </summary>
    /// <returns>是否存在至少一个激活的后处理效果</returns>
    public bool SetupCustomPostProcessing()
    {
        mActiveIndex.Clear();
        for (int i = 0; i < myCustomPostProcessing.Count; i++)
        {
            myCustomPostProcessing[i].Setup();
            // 如果该效果处于激活状态，则将其索引加入激活列表
            if (myCustomPostProcessing[i].IsActive())
            {
                mActiveIndex.Add(i);
            }
        }
        return mActiveIndex.Count != 0;
    }

    /// <summary>
    /// 相机设置阶段调用：根据当前相机描述符重新分配临时纹理（确保分辨率、格式等匹配）
    /// </summary>
    /// <param name="cmd">命令缓冲区</param>
    /// <param name="renderingData">当前帧渲染数据</param>
    public override void OnCameraSetup(CommandBuffer cmd, ref RenderingData renderingData)
    {
        // 获取相机目标描述符，并强制MSAA为1（后处理通常不需要MSAA），关闭深度缓冲区
        //MSAA 是 Multisample Anti-Aliasing（多重采样抗锯齿）.用于减少 3D 渲染画面边缘“锯齿”（Aliasing）的一种经典抗锯齿技术
        var descriptor = renderingData.cameraData.cameraTargetDescriptor;
        descriptor.msaaSamples = 1;
        descriptor.depthBufferBits = 0;

        // 如果临时纹理需要重新分配（分辨率或格式变化），则重新分配
        RenderingUtils.ReAllocateIfNeeded(ref mTempRT0, descriptor, name: mTempRT0Name);
        RenderingUtils.ReAllocateIfNeeded(ref mTempRT1, descriptor, name: mTempRT1Name);
    }

    /// <summary>
    /// 执行Pass的核心逻辑：按顺序调用每个激活的后处理效果的Render方法，使用乒乓缓冲进行链式处理。
    /// </summary>
    /// <param name="context">渲染上下文</param>
    /// <param name="renderingData">当前帧渲染数据</param>
    public override void Execute(ScriptableRenderContext context, ref RenderingData renderingData)
    {
        // 获取一个命令缓冲区，并清空（先执行一次空的以确保上下文同步，实际可以优化）
        var cmd = CommandBufferPool.Get(mProfilerTag);
        context.ExecuteCommandBuffer(cmd);
        cmd.Clear();
        bool RT1Used = false;   // 标记是否使用了第二个临时纹理（用于多效果链）

        // 获取当前相机的颜色目标作为源和目标（起始时源和目标相同，之后会更新）
        mDestinationRT = renderingData.cameraData.renderer.cameraColorTargetHandle;
        mSourceRT = renderingData.cameraData.renderer.cameraColorTargetHandle;

        // 情况1：只有一个激活的后处理效果，直接渲染到 mTempRT0
        if (mActiveIndex.Count == 1)
        {
            int index = mActiveIndex[0];
            using (new ProfilingScope(cmd, myProfilerSampler[index]))
            {
                // 调用效果的Render方法，将源（相机颜色目标）处理到临时纹理0
                myCustomPostProcessing[index].Render(cmd, ref renderingData, mSourceRT, mTempRT0);
            }
        }
        //情况2：有多个激活的后处理效果，使用乒乓缓冲链式处理
        else
        {
            RT1Used = true;
            // 首先将相机颜色目标拷贝到临时纹理0
            Blitter.BlitCameraTexture(cmd, mSourceRT, mTempRT0);
            // 依次对每个效果进行处理：当前输入为 mTempRT0，输出到 mTempRT1，然后交换纹理
            for (int i = 0; i < mActiveIndex.Count; i++)
            {
                int index = mActiveIndex[i];
                var customPostProcessing = myCustomPostProcessing[index];
                using (new ProfilingScope(cmd, myProfilerSampler[index]))
                {
                    customPostProcessing.Render(cmd, ref renderingData, mTempRT0, mTempRT1);
                }
                // 交换两个临时纹理，以便下一次迭代继续使用
                CoreUtils.Swap(ref mTempRT0, ref mTempRT1);
            }
        }
        // 将最终的临时纹理（mTempRT0）拷贝到目标颜色纹理（相机最终输出）
        Blitter.BlitCameraTexture(cmd, mTempRT0, mDestinationRT);

        // 释放临时RT（根据是否使用了第二个临时纹理决定释放哪个）
        cmd.ReleaseTemporaryRT(Shader.PropertyToID(mTempRT0.name));
        if (RT1Used)
            cmd.ReleaseTemporaryRT(Shader.PropertyToID(mTempRT1.name));

        // 执行命令缓冲区并释放回池中
        context.ExecuteCommandBuffer(cmd);
        CommandBufferPool.Release(cmd);
    }

    public void Dispose()
    {
        mSourceRT?.Release();
        mDestinationRT?.Release();
        // 注意：此处未释放 mTempRT0 和 mTempRT1，可能需要补充，但RTHandles通常由Alloc分配的需手动Release
    }
}
