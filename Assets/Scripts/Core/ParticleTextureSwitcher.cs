using UnityEngine;

/// <summary>
/// 挂在粒子系统上，通过 MaterialPropertyBlock 设置 _Switch 值来选择贴图。
/// 多个粒子系统共用同一个 Material（含三张贴图），各自设不同的 TextureIndex 即可切换，
/// 配合 GPU Instancing 减少 DrawCall。
/// </summary>
[RequireComponent(typeof(ParticleSystem))]
public class ParticleTextureSwitcher : MonoBehaviour
{
    [Tooltip("0 = MainTex1, 1 = MainTex2, 2 = MainTex3")]
    [Range(0, 2)]
    public int TextureIndex = 0;

    private static readonly int SwitchId = Shader.PropertyToID("_Switch");

    private ParticleSystemRenderer _renderer;
    private MaterialPropertyBlock _mpb;
    private int _lastIndex = -1;

    void Awake()
    {
        _renderer = GetComponent<ParticleSystemRenderer>();
        _mpb = new MaterialPropertyBlock();
    }

    void OnEnable()
    {
        ApplyPropertyBlock();
    }

    void Update()
    {
        // Only update MPB when TextureIndex changes
        if (TextureIndex != _lastIndex)
        {
            ApplyPropertyBlock();
        }
    }

    private void ApplyPropertyBlock()
    {
        _renderer.GetPropertyBlock(_mpb);
        _mpb.SetFloat(SwitchId, TextureIndex);
        _renderer.SetPropertyBlock(_mpb);
        _lastIndex = TextureIndex;
    }
}
