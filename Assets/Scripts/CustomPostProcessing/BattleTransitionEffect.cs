using System;
using UnityEngine;
using UnityEngine.Rendering;
using UnityEngine.Rendering.Universal;

[Serializable, VolumeComponentMenu("CustomPostProcess/Battle Transition (崩铁转场)")]
public class BattleTransitionEffect : CustomPostProcessing
{
    // [核心驱动参数] 0代表无效果，1代表转场完成
    public ClampedFloatParameter progress = new ClampedFloatParameter(0f, 0f, 1f);

    // 视觉调节参数
    public TextureParameter crackMask = new TextureParameter(null);// 碎裂蒙版 (拖入一张类似玻璃碎裂、或闪电裂纹的黑白贴图)
    public FloatParameter maxZoom = new FloatParameter(0.5f); // 画面最大放大程度
    public FloatParameter blurStrength = new FloatParameter(0.2f); // 径向模糊最大强度
    public FloatParameter rgbSplitAmount = new FloatParameter(0.05f); // 色散(色差)最大距离
    public IntParameter blurSamples = new IntParameter(6); // 采样次数，影响性能和模糊顺滑度
    public FloatParameter pulseDensity = new FloatParameter(15f);  // 脉冲波纹密度
    public FloatParameter pulseSpeed = new FloatParameter(8f);     // 脉冲流动速度


    private Material m_Material;
    private const string ShaderName = "2DGames/URP/BattleTransition";

    public override bool IsActive() => progress.value > 0 && active;// 只有当 progress 大于 0 且组件勾选开启时，才激活此后处理
    // 我们选择在所有的后期处理（如泛光、色彩分级）之后再做转场，防止被其他后处理扭曲
    public override CustomPostProcessInjectionPoint GetInjectionPoint => CustomPostProcessInjectionPoint.BeforePostProcess;
    public override void Setup()
    {
        if (m_Material == null)
        {
            Shader shader = Shader.Find(ShaderName);
            if (shader != null)
            {
                m_Material = CoreUtils.CreateEngineMaterial(shader);
            }
            else
            {
                Debug.LogError($"找不到Shader: {ShaderName}");
            }
        }
    }
    public override void Render(CommandBuffer cmd, ref RenderingData renderingData, RTHandle source, RTHandle destination)
    {
        if (m_Material == null)
        {
            // 如果材质为空（Shader丢失或编译报错），直接把原画面原样拷贝过去，绝不能 return 空黑图！
            Blitter.BlitCameraTexture(cmd, source, destination);
            return;
        }

        //向Shader传递参数
        m_Material.SetFloat("_Progress", progress.value);
        m_Material.SetFloat("_MaxZoom", maxZoom.value);
        m_Material.SetFloat("_BlurStrength", blurStrength.value);
        m_Material.SetFloat("_RGBSplitAmount", rgbSplitAmount.value);
        m_Material.SetInt("_BlurSamples", blurSamples.value);
        m_Material.SetFloat("_PulseDensity", pulseDensity.value);
        m_Material.SetFloat("_PulseSpeed", pulseSpeed.value);
        m_Material.SetTexture("_CrackMask", crackMask.value != null ? crackMask.value : Texture2D.blackTexture);

        // 使用 URP 原生的 Blitter 进行绘制，传入 material 和 pass index(0)
        Blitter.BlitCameraTexture(cmd, source, destination, m_Material, 0);
    }

    public override void Dispose(bool disposing)
    {
        base.Dispose(disposing);
        CoreUtils.Destroy(m_Material); // 清理材质防泄漏
    }
}
