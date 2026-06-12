using UnityEngine;
using UnityEngine.UI;
using System.Collections;

/// <summary>
/// AP 菱形点阵控制器
/// 驱动 APSlot.shader 的 _Residue（剩余 AP 数量）和 _TintColor（颜色）。
///
/// 功能：
/// - 轮询 BattleResourceManager.sharedAP，检测变化后自动播放动画
/// - AP 增加时逐个点亮（fillInterval/颗），减少时逐个熄灭（unfillInterval/颗）
/// - AP 满时呼吸光效（_TintColor 亮度正弦脉动）
///
/// 挂载方式：
/// 在 BattleInfoPanel 的层级下创建一个 RawImage，设 Material=APSlot.mat，
/// 挂载此组件。无需额外代码集成。
/// </summary>
[RequireComponent(typeof(RawImage))]
public class APSlotController : MonoBehaviour
{
    [Header("引用（留空自动从当前对象获取 RawImage）")]
    [SerializeField] private RawImage _targetRawImage;

    [Header("动画速度")]
    [SerializeField] private float _fillInterval = 0.12f;     // 每颗菱形点亮间隔（秒）
    [SerializeField] private float _unfillInterval = 0.08f;   // 每颗菱形熄灭间隔（秒）

    [Header("满 AP 呼吸光效")]
    [SerializeField] private float _breathSpeed = 2f;         // 呼吸频率
    [SerializeField] private float _breathIntensity = 0.15f;  // 亮度变化幅度（乘数偏移）

    [Header("颜色")]
    [SerializeField] private Color _filledColor = new Color(1.0f, 0.55f, 0.0f);  // 亮橙
    [SerializeField] private Color _dimColor = new Color(0.15f, 0.15f, 0.15f);  // 暗灰（预留，当前 shader 用 _Residue 控制可见性，暂不启用）

    // Shader 属性缓存
    private static readonly int PropResidue = Shader.PropertyToID("_Residue");
    private static readonly int PropTintColor = Shader.PropertyToID("_TintColor");

    private Material _matInstance;
    private int _cachedAP;       // BattleResourceManager 中的实时 AP
    private int _maxAP;
    private int _displayAP;      // 动画当前显示到的 AP 值（协程中逐步增减）
    private Coroutine _animCoroutine;
    private bool _wasFull;       // 上一帧 displayAP 是否满

    private void Awake()
    {
        if (_targetRawImage == null)
            _targetRawImage = GetComponent<RawImage>();

        // 获取材质实例（Unity 自动为 RawImage.material 创建实例副本）
        _matInstance = _targetRawImage.material;
    }

    private void Start()
    {
        var res = BattleResourceManager.Instance;
        _cachedAP = res.sharedAP;
        _maxAP = res.maxSharedAP;
        _displayAP = _cachedAP;
        _wasFull = _displayAP >= _maxAP;

        // 初始写入 Shader
        _matInstance.SetInt(PropResidue, _displayAP);
        _matInstance.SetColor(PropTintColor, _filledColor);
    }

    private void LateUpdate()
    {
        int currentAP = BattleResourceManager.Instance.sharedAP;

        // ── 检测 AP 变化，启动动画 ──
        if (currentAP != _cachedAP)
        {
            _cachedAP = currentAP;
            RestartAnim();
        }

        // ── 满 AP 呼吸光效 ──
        if (_displayAP >= _maxAP)
        {
            if (!_wasFull)
                _wasFull = true;

            float breath = 1f + Mathf.Abs(Mathf.Sin(Time.time * _breathSpeed) * _breathIntensity);
            Color breathColor = _filledColor * breath;
            breathColor.a = 1f;
            _matInstance.SetColor(PropTintColor, breathColor);
        }
        else
        {
            if (_wasFull)
            {
                _wasFull = false;
                _matInstance.SetColor(PropTintColor, _filledColor);
            }
        }
    }

    /// <summary>重启追赶协程（停止当前动画，从 _displayAP 开始向 _cachedAP 靠拢）</summary>
    private void RestartAnim()
    {
        if (_animCoroutine != null)
            StopCoroutine(_animCoroutine);
        _animCoroutine = StartCoroutine(ChaseRoutine());
    }

    /// <summary>
    /// 持续追赶协程。
    /// _displayAP 逐颗向 _cachedAP 靠拢，如果中途 _cachedAP 再次变化，
    /// 方向自动切换（增加时逐个点亮、减少时逐个熄灭）。
    /// </summary>
    private IEnumerator ChaseRoutine()
    {
        while (_displayAP != _cachedAP)
        {
            bool isFilling = _cachedAP > _displayAP;
            float stepDelay = isFilling ? _fillInterval : _unfillInterval;

            _displayAP += isFilling ? 1 : -1;
            _matInstance.SetInt(PropResidue, _displayAP);
            yield return new WaitForSeconds(stepDelay);
        }

        _animCoroutine = null;
    }

    private void OnDestroy()
    {
        if (_matInstance != null)
        {
            Destroy(_matInstance);
            _matInstance = null;
        }
    }
}
