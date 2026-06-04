using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

//添加每个公用的属性和函数。
//首先是这个  后处理效果的注入点  ，这里先分三个。并且，加入当前后处理在注入点的执行顺序。
public enum CustomPostProcessInjectionPoint
{
    AfterOpaqueAndSky,
    BeforePostProcess,
    AfterPostProcess
}

//添加IPostProcessComponent必须要override的一些函数(IsActive,IsTileCompatible)
//添加IDisposable必须要override的一些函数(Dispose)
/// <summary>
/// 自定义后处理效果的抽象基类。
/// 继承自 VolumeComponent，并实现 IPostProcessComponent 和 IDisposable 接口。
/// 用于在 Universal Render Pipeline (URP) 中创建可注入渲染流程的自定义后处理效果。
/// </summary>
public abstract class CustomPostProcessing : VolumeComponent, IPostProcessComponent, IDisposable
{
    /// <summary>
    /// 当前后处理效果使用的材质实例。
    /// 派生类应创建并赋值此材质。
    /// </summary>
    protected Material mMaterial;
    /// <summary>
    /// 用于纹理拷贝的内部材质（当未提供自定义材质或 pass 时使用）。
    /// </summary>
    private Material mCopyMaterial;
    private const string mCopyShderName = "Hidden/CustomPostProcess/PostProcessCopy";
    #region IPostProcessComponent
    public abstract bool IsActive();
    /// <summary>
    /// 表示该效果是否兼容瓦片渲染（Tile Compatible）。
    /// 若不支持瓦片渲染，URP 可能会采取额外措施。
    /// </summary>
    /// <returns>默认返回 false，派生类可重写。</returns>
    public virtual bool IsTileCompatible() => false;
    #endregion

    #region IDisposable
    public void Dispose()
    {
        Dispose(true);
        GC.SuppressFinalize(this);
    }
    /// <summary>
    /// 资源释放的具体实现，可由派生类重写以释放自定义资源。
    /// </summary>
    /// <param name="disposing">是否由 Dispose 方法主动调用（true）还是由终结器调用（false）。</param>
    public virtual void Dispose(bool disposing)
    {
        // 基类暂无需要释放的资源，派生类可重写
    }
    #endregion

    //注入点 或者说 注入时机
    public virtual CustomPostProcessInjectionPoint GetInjectionPoint => CustomPostProcessInjectionPoint.AfterPostProcess;
    //在注入点的顺序。数值越小越先执行。
    public virtual int OrderInEvent => 0;
    private int mSourceTextureId = Shader.PropertyToID("_BlitTexture");

    /// <summary>
    /// 当 Volume 组件被启用时调用。
    /// 初始化纹理拷贝材质。
    /// </summary>
    protected override void OnEnable()
    {
        base.OnEnable();
        if (mCopyMaterial == null)
        {
            mCopyMaterial = CoreUtils.CreateEngineMaterial(mCopyShderName);
        }
    }

    // 配置当前后处理所需的资源（例如创建材质、申请临时渲染纹理等）。
    /// 在每一帧执行渲染前调用。
    public abstract void Setup();

    /// <summary>
    /// 执行后处理效果的核心渲染逻辑。
    /// 派生类应实现此方法，将 source 纹理处理后输出到 destination 纹理。
    /// </summary>
    /// <param name="cmd">用于记录渲染命令的 CommandBuffer。</param>
    /// <param name="renderingData">当前帧的渲染数据。</param>
    /// <param name="source">源渲染纹理（输入）。</param>
    /// <param name="destination">目标渲染纹理（输出）。</param>
    public abstract void Render(CommandBuffer cmd, ref RenderingData renderingData, RTHandle source, RTHandle destination);

    /// <summary>
    /// 执行实际的屏幕绘制操作，将源纹理通过材质绘制到目标纹理上。
    /// 派生类可在 Render 方法中调用此辅助函数。
    /// </summary>
    /// <param name="cmd">CommandBuffer 对象。</param>
    /// <param name="source">源 RTHandle。</param>
    /// <param name="destination">目标 RTHandle。</param>
    /// <param name="pass">材质使用的 Pass 索引；-1 表示使用内置拷贝材质执行简单拷贝。</param>
    public virtual void Draw(CommandBuffer cmd, in RTHandle source, in RTHandle destination, int pass = -1)
    {
        cmd.SetGlobalTexture(mSourceTextureId, source);// 将源纹理设置为全局 Shader 属性，供材质使用
        cmd.SetRenderTarget(destination, RenderBufferLoadAction.DontCare, RenderBufferStoreAction.Store);// 设置渲染目标，并指定加载/存储行为（不关心之前内容，直接存储结果）
        if (pass == -1 || mMaterial == null)
            // 如果没有指定 pass 或材质无效，使用默认拷贝材质绘制全屏三角形
            cmd.DrawProcedural(Matrix4x4.identity, mCopyMaterial, 0, MeshTopology.Triangles, 3);
        else cmd.DrawProcedural(Matrix4x4.identity, mMaterial, pass, MeshTopology.Triangles, 3);// 否则使用自定义材质的指定 pass 绘制
    }

}
