using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// MP 笔尾条控制器
/// 驱动 HUDBar.shader 的 _CurrentHP（填充比例）和 _DelayHP（延迟跟随）。
///
/// 挂载在 BattleInfoPanel 下带 MPbar_BrushTail 材质的 RawImage 上。
/// </summary>
[RequireComponent(typeof(RawImage))]
public class MPSlotController : MonoBehaviour
{
    [Header("引用（留空自动获取）")]
    [SerializeField] private RawImage _targetRawImage;

    [Header("延迟跟随速度")]
    [SerializeField] private float _delayFollowSpeed = 2f;

    private static readonly int PropCurrentHP = Shader.PropertyToID("_CurrentHP");
    private static readonly int PropDelayHP = Shader.PropertyToID("_DelayHP");

    private Material _matInstance;
    private float _currentDisplay;  // 目标值 (0~1)
    private float _delayDisplay;    // 延迟跟随值

    private void Awake()
    {
        if (_targetRawImage == null)
            _targetRawImage = GetComponent<RawImage>();
        _matInstance = _targetRawImage.material;
    }

    private void Start()
    {
        var res = BattleResourceManager.Instance;
        _currentDisplay = res.maxSharedMP > 0
            ? (float)res.sharedMP / res.maxSharedMP
            : 0f;
        _delayDisplay = _currentDisplay;

        _matInstance.SetFloat(PropCurrentHP, _currentDisplay);
        _matInstance.SetFloat(PropDelayHP, _delayDisplay);
    }

    private void LateUpdate()
    {
        var res = BattleResourceManager.Instance;
        float target = res.maxSharedMP > 0
            ? (float)res.sharedMP / res.maxSharedMP
            : 0f;

        _currentDisplay = target;
        _delayDisplay = Mathf.Lerp(_delayDisplay, target, Time.deltaTime * _delayFollowSpeed);

        _matInstance.SetFloat(PropCurrentHP, _currentDisplay);
        _matInstance.SetFloat(PropDelayHP, _delayDisplay);
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
