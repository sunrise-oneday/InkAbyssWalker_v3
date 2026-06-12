using UnityEngine;
using System.Collections.Generic;

/// <summary>
/// 敌人头顶 Billboard 血条控制器 — 使用 HUDBar.shader 渲染，支持延迟拖尾、濒血闪烁、肌理效果。
/// 拖尾进度采用历史队列，精确滞后实际血量 1.5 秒（不受 TimeScale 影响）。
/// 挂载方式：作为敌人预制体的子物体（血条位置），同一个敌人身上需要有 CharacterStats 组件。
/// </summary>
[RequireComponent(typeof(MeshRenderer))]
public class HPBarBillboardController : MonoBehaviour
{
    [Header("数据源")]
    [Tooltip("留空则自动从父级 BattleEntity 获取")]
    [SerializeField] private CharacterStats targetStats;

    [Header("拖尾延迟参数")]
    [Tooltip("延迟血条（黄色拖尾）落后实际血量的秒数")]
    [SerializeField] private float trailDelaySeconds = 1.5f;

    [Header("Shader 属性名")]
    [SerializeField] private string currentHPProp = "_CurrentHP";
    [SerializeField] private string delayHPProp = "_DelayHP";
    [SerializeField] private string flashSpeedProp = "_FlashSpeed";
    [SerializeField] private string verticalBillboardProp = "_VerticalBillboarding";

    private MeshRenderer _meshRenderer;
    private MaterialPropertyBlock _props;

    // ---- 当前状态 ----
    private float _targetHP01 = 1f;   // 实际血量（立即反映到 _CurrentHP）
    private float _delayHP01 = 1f;    // 拖尾血量（1.5s 前的 _targetHP01，反映到 _DelayHP）

    // ---- 历史队列：记录每次血量变化的时间 + 值 ----
    private struct HPSnapshot
    {
        public float realTime;  // Time.realtimeSinceStartup（不受 TimeScale 影响）
        public float hp01;      // 当时的血量百分比
    }
    private readonly Queue<HPSnapshot> _hpHistory = new Queue<HPSnapshot>();
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
            _delayHP01 = initial;
            _hpHistory.Enqueue(new HPSnapshot { realTime = Time.realtimeSinceStartup, hp01 = initial });
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

    private void Update()
    {
        // 每帧处理历史队列：找出 1.5s 前血量是什么
        float cutTime = Time.realtimeSinceStartup - trailDelaySeconds;

        // 出队所有早于截断时间的记录
        while (_hpHistory.Count > 0 && _hpHistory.Peek().realTime < cutTime)
        {
            _hpHistory.Dequeue();
        }

        // 目标拖尾值：队列中最早那条记录的血量（恰好是 trailDelaySeconds 前的值）
        // 队列空了说明已经追上了，目标就是当前实际血量
        float targetDelay = _hpHistory.Count > 0 ? _hpHistory.Peek().hp01 : _targetHP01;

        // 平滑插值：用指数平滑取代直接跳变，避免黄条突然"啪嗒"一下
        float diff = Mathf.Abs(_delayHP01 - targetDelay);
        if (diff > 0.0001f)
        {
            const float lerpSpeed = 4f;          // 收敛速度：越大追得越快
            float lerpFactor = 1f - Mathf.Exp(-lerpSpeed * Time.unscaledDeltaTime);
            _delayHP01 = Mathf.Lerp(_delayHP01, targetDelay, lerpFactor);
            UpdateShaderHP();
        }
    }

    private void OnHPChanged()
    {
        if (targetStats == null) return;

        float maxHP = targetStats.maxHP;
        float newTarget = maxHP > 0 ? Mathf.Clamp01((float)targetStats.currentHP / maxHP) : 0f;

        // 只有当值确实变化时才记录快照
        if (Mathf.Abs(newTarget - _targetHP01) > 0.0001f)
        {
            _targetHP01 = newTarget;
            // 记录当前时间点的血量快照
            _hpHistory.Enqueue(new HPSnapshot
            {
                realTime = Time.realtimeSinceStartup,
                hp01 = _targetHP01
            });

            // 立即刷新 _CurrentHP（实际血量瞬间变化）
            UpdateShaderCurrentHP();
        }
    }

    /// <summary>
    /// 仅刷新 _CurrentHP（实际血量瞬间变化），不碰 _DelayHP
    /// </summary>
    private void UpdateShaderCurrentHP()
    {
        if (_meshRenderer == null) return;

        _meshRenderer.GetPropertyBlock(_props);
        _props.SetFloat(currentHPProp, _targetHP01);
        _meshRenderer.SetPropertyBlock(_props);
    }

    /// <summary>
    /// 同时刷新 _CurrentHP 和 _DelayHP
    /// </summary>
    private void UpdateShaderHP()
    {
        if (_meshRenderer == null) return;

        _meshRenderer.GetPropertyBlock(_props);
        _props.SetFloat(currentHPProp, _targetHP01);
        _props.SetFloat(delayHPProp, _delayHP01);
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
            _props.SetFloat(delayHPProp, _delayHP01);
            _props.SetFloat(flashSpeedProp, 5f);
            _meshRenderer.SetPropertyBlock(_props);
        }
    }
#endif
}
