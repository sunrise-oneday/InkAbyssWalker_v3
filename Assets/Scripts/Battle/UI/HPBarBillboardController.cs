using UnityEngine;
using System.Collections;

/// <summary>
/// 敌人头顶 Billboard 血条控制器 — 使用 HPbar_Billboard.shader 渲染，支持延迟拖尾、濒血闪烁、肌理效果。
/// 挂载方式：作为敌人预制体的子物体（血条位置），同一个敌人身上需要有 CharacterStats 组件。
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
public class HPBarBillboardController : MonoBehaviour
{
    [Header("数据源")]
    [Tooltip("留空则自动从父级 BattleEntity 获取")]
    [SerializeField] private CharacterStats targetStats;

    [Header("延迟血条平滑参数")]
    [Tooltip("延迟血条（黄色拖尾）追赶主血条的速度")]
    [SerializeField] private float delayCatchUpSpeed = 1.5f;

    [Header("Shader 属性名")]
    [SerializeField] private string currentHPProp = "_CurrentHP";
    [SerializeField] private string delayHPProp = "_DelayHP";
    [SerializeField] private string flashSpeedProp = "_FlashSpeed";
    [SerializeField] private string verticalBillboardProp = "_VerticalBillboarding";

    private MeshRenderer _meshRenderer;
    private MaterialPropertyBlock _props;
    private float _targetHP01 = 1f;
    private float _displayHP01 = 1f;
    private bool _initialized;

    private void Awake()
    {
        _meshRenderer = GetComponent<MeshRenderer>();
        _props = new MaterialPropertyBlock();
        _meshRenderer.GetPropertyBlock(_props);

        // 默认垂直公告牌开启（血条始终面向相机且保持垂直）
        _props.SetFloat(verticalBillboardProp, 1f);
        _meshRenderer.SetPropertyBlock(_props);
    }

    private void Start()
    {
        // 自动从父级查找 CharacterStats
        if (targetStats == null)
        {
            var battleEntity = GetComponentInParent<BattleEntity>();
            if (battleEntity != null)
            {
                targetStats = battleEntity.Stats;
            }
        }

        if (targetStats != null)
        {
            targetStats.OnHPChanged += OnHPChanged;
            // 初始化血条值
            float initial = targetStats.maxHP > 0
                ? (float)targetStats.currentHP / targetStats.maxHP
                : 1f;
            _targetHP01 = initial;
            _displayHP01 = initial;
            UpdateShaderHP();
            _initialized = true;
        }
    }

    private void OnDestroy()
    {
        if (targetStats != null)
        {
            targetStats.OnHPChanged -= OnHPChanged;
        }
    }

    private void OnEnable()
    {
        // 重新激活时，如果已在 Start 中初始化过，重新绑定事件
        if (_initialized && targetStats != null)
        {
            targetStats.OnHPChanged -= OnHPChanged;
            targetStats.OnHPChanged += OnHPChanged;
            // 立即同步当前血量
            OnHPChanged();
        }
    }

    private void OnDisable()
    {
        if (targetStats != null)
        {
            targetStats.OnHPChanged -= OnHPChanged;
        }
    }

    private void OnHPChanged()
    {
        if (targetStats == null) return;

        float maxHP = targetStats.maxHP;
        _targetHP01 = maxHP > 0 ? Mathf.Clamp01((float)targetStats.currentHP / maxHP) : 0f;

        // 停止旧的延迟协程，启动新的
        StopAllCoroutines();
        StartCoroutine(DelayHPCatchUpRoutine());
    }

    /// <summary>
    /// 延迟血条（黄色拖尾）平滑追赶主血条
    /// </summary>
    private IEnumerator DelayHPCatchUpRoutine()
    {
        while (Mathf.Abs(_displayHP01 - _targetHP01) > 0.001f)
        {
            _displayHP01 = Mathf.MoveTowards(_displayHP01, _targetHP01, delayCatchUpSpeed * Time.deltaTime);
            UpdateShaderHP();
            yield return null;
        }

        _displayHP01 = _targetHP01;
        UpdateShaderHP();
    }

    /// <summary>
    /// 将当前主血条和延迟血条写入 MaterialPropertyBlock
    /// </summary>
    private void UpdateShaderHP()
    {
        if (_meshRenderer == null) return;

        _meshRenderer.GetPropertyBlock(_props);
        _props.SetFloat(currentHPProp, _targetHP01);
        _props.SetFloat(delayHPProp, _displayHP01);
        _meshRenderer.SetPropertyBlock(_props);
    }

#if UNITY_EDITOR
    /// <summary>
    /// 编辑器下方便调试：直接拖拽滑块预览血条效果
    /// </summary>
    private void OnValidate()
    {
        if (_meshRenderer == null)
            _meshRenderer = GetComponent<MeshRenderer>();
        if (_props == null)
            _props = new MaterialPropertyBlock();

        if (_meshRenderer != null)
        {
            _meshRenderer.GetPropertyBlock(_props);
            _props.SetFloat(currentHPProp, _targetHP01);
            _props.SetFloat(delayHPProp, _displayHP01);
            _props.SetFloat(flashSpeedProp, 5f);
            _meshRenderer.SetPropertyBlock(_props);
        }
    }
#endif
}
