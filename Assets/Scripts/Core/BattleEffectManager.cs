using System.Collections;
using UnityEngine;

/// <summary>
/// 战斗视觉特效管理器
/// 负责镜头抖动、HitStop（攻击顿挫）、魔女时间（子弹时间）等纯视觉特效。
/// 从 BattleManager 中解耦分离，职责单一，可独立使用。
/// </summary>
public class BattleEffectManager : MonoBehaviour
{
    private static BattleEffectManager _instance;

    public static BattleEffectManager Instance
    {
        get
        {
            if (_instance == null)
            {
                var go = new GameObject("[BattleEffectManager]");
                _instance = go.AddComponent<BattleEffectManager>();
                DontDestroyOnLoad(go);
            }
            return _instance;
        }
    }

    private void Awake()
    {
        if (_instance != null && _instance != this)
        {
            Destroy(gameObject);
            return;
        }
        _instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ========== P0 生命周期管理（格挡顿帧事件订阅）==========

    private void OnEnable()
    {
        ParryEvents.OnPerfectParry += HandlePerfectParryFreeze;
    }

    private void OnDisable()
    {
        ParryEvents.OnPerfectParry -= HandlePerfectParryFreeze;
    }

    // ---- 协程句柄缓存 ----
    private Coroutine _activeCameraShake;
    private Coroutine _activeWitchTime;
    private Coroutine _activeHitStop;
    private Coroutine _activePerfectParryFreeze;

    #region 魔女时间（子弹时间）

    /// <summary>触发魔女时间，将游戏时间缩放至 0.2 倍速，持续指定真实时长</summary>
    public void WitchTime(float duration)
    {
        StopAllTimeScaleCoroutines();
        _activeWitchTime = StartCoroutine(WitchTimeRoutine(duration));
    }

    private IEnumerator WitchTimeRoutine(float duration)
    {
        Time.timeScale = 0.2f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1.0f;
        _activeWitchTime = null;
    }

    #endregion

    #region 镜头抖动

    /// <summary>触发战斗镜头抖动，指定持续时间和幅度</summary>
    public void ShakeCamera(float duration, float magnitude)
    {
        if (_activeCameraShake != null) StopCoroutine(_activeCameraShake);
        _activeCameraShake = StartCoroutine(CameraShakeRoutine(duration, magnitude));
    }

    private IEnumerator CameraShakeRoutine(float duration, float magnitude)
    {
        Camera battleCam = Camera.main;
        if (battleCam == null) { _activeCameraShake = null; yield break; }

        Vector3 originalPos = battleCam.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            battleCam.transform.position = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
            elapsed += Time.deltaTime;
            yield return null;
        }

        battleCam.transform.position = originalPos;
        _activeCameraShake = null;
    }

    #endregion

    #region HitStop（攻击顿挫）

    /// <summary>触发 HitStop，将游戏时间几乎冻结（0.05 倍速），持续指定真实时长</summary>
    public void HitStop(float duration)
    {
        StopAllTimeScaleCoroutines();
        _activeHitStop = StartCoroutine(HitStopRoutine(duration));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1.0f;
        _activeHitStop = null;
    }

    #endregion

    #region 精准防御顿帧（unscaledDeltaTime 微抖 + TimeScale 顿帧）

    /// <summary>
    /// 事件响应：完美格挡触发 → 实例化粒子 + 极限顿帧 + 微抖
    /// </summary>
    private void HandlePerfectParryFreeze(ParryEventData data)
    {
        // ---- 实例化 PerfectParry 粒子到玩家 eff 子物体 ----
        SpawnPerfectParry(data.Defender);

        // ★ P0 修正：先停止现有 CameraShake（避免顿帧后"延迟爆发"）
        // ApplyDamageFeedback 中调用的 ShakeCamera(0.2f, 0.25f) 使用 Time.deltaTime 计时，
        // 在 0.01x TimeScale 下几乎不推进，0.4s 真时后剩余进度集中爆发会导致剧烈抖动。
        // 此处中断旧的抖动协程，改用 unscaledDeltaTime 驱动的短促微抖。
        if (_activeCameraShake != null)
        {
            StopCoroutine(_activeCameraShake);
            _activeCameraShake = null;
        }

        PerfectParryFreeze(0.4f);
        PerfectParryShake(0.15f, 0.08f); // unscaledDeltaTime 驱动，不受顿帧影响
    }

    /// <summary>
    /// 精准防御专用顿帧：TimeScale=0.01，持续指定真实时长后恢复。
    /// 与 HitStop/WitchTime 互斥——先中断正在进行的时间缩放再覆盖。
    /// </summary>
    public void PerfectParryFreeze(float realDuration)
    {
        StopAllTimeScaleCoroutines();
        _activePerfectParryFreeze = StartCoroutine(PerfectParryFreezeRoutine(realDuration));
    }

    private IEnumerator PerfectParryFreezeRoutine(float realDuration)
    {
        Time.timeScale = 0.01f;
        yield return new WaitForSecondsRealtime(realDuration);
        Time.timeScale = 1.0f;
        _activePerfectParryFreeze = null;
    }

    /// <summary>
    /// 实例化 PerfectParry 粒子到玩家的 eff 子物体，播完后自动销毁。
    /// </summary>
    private void SpawnPerfectParry(PlayerBattleEntity defender)
    {
        if (defender == null) return;

        // 查找玩家的 eff 子物体
        Transform eff = defender.transform.Find("eff");
        if (eff == null)
        {
            Debug.LogWarning("[PerfectParry] 玩家身上未找到 eff 子物体，回退到根节点");
            eff = defender.transform;
        }

        var prefab = Resources.Load<GameObject>("Prefab/PerfectParry");
        if (prefab == null)
        {
            Debug.LogWarning("[PerfectParry] 未找到 Prefab/PerfectParry 预制体");
            return;
        }

        var go = Instantiate(prefab, eff);
        go.name = "PerfectParry_Effect";

        // 确保脚本组件存在
        var controller = go.GetComponent<PerfectParry>();
        if (controller == null)
            controller = go.AddComponent<PerfectParry>();

        controller.PlayAndDestroy();
    }

    /// <summary>
    /// 完美格挡专用镜头微抖，使用 unscaledDeltaTime 驱动，不受 TimeScale=0.01 冻结影响。
    /// 替代 ShakeCamera（其使用 Time.deltaTime，在顿帧期间被冻住会导致延迟爆发）。
    /// </summary>
    public void PerfectParryShake(float duration, float magnitude)
    {
        _activeCameraShake = StartCoroutine(PerfectParryShakeRoutine(duration, magnitude));
    }

    private IEnumerator PerfectParryShakeRoutine(float duration, float magnitude)
    {
        Camera battleCam = Camera.main;
        if (battleCam == null) { _activeCameraShake = null; yield break; }

        // ★ 注意：originalPos 在协程开始时快照一次。
        // 如果相机在顿帧期间还有动态跟随逻辑，originalPos 会是抖动开始时的位置，
        // 结束后会硬 snap 回去。当前战斗相机无动态跟随，不受影响。
        Vector3 originalPos = battleCam.transform.position;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            float x = Random.Range(-1f, 1f) * magnitude;
            float y = Random.Range(-1f, 1f) * magnitude;
            battleCam.transform.position = new Vector3(originalPos.x + x, originalPos.y + y, originalPos.z);
            elapsed += Time.unscaledDeltaTime; // ★ 关键：不受 TimeScale 影响
            yield return null;
        }

        battleCam.transform.position = originalPos;
        _activeCameraShake = null;
    }

    #endregion

    #region TimeScale 互斥机制

    /// <summary>
    /// 中断所有正在进行的时间缩放协程。
    /// ⚠️ P1 修正：绝对不在此处重置 Time.timeScale。
    /// 原因：重置为 1.0 会导致 0.05→1.0→0.01 的瞬时闪回，
    /// FixedUpdate（物理）和 Animator（动画状态机）对 TimeScale 跳变敏感，
    /// 可能引发碰撞穿透或动画采样跳帧。让新协程直接覆盖 timeScale 即可。
    /// </summary>
    private void StopAllTimeScaleCoroutines()
    {
        if (_activeWitchTime != null)
        {
            StopCoroutine(_activeWitchTime);
            _activeWitchTime = null;
        }
        if (_activeHitStop != null)
        {
            StopCoroutine(_activeHitStop);
            _activeHitStop = null;
        }
        if (_activePerfectParryFreeze != null)
        {
            StopCoroutine(_activePerfectParryFreeze);
            _activePerfectParryFreeze = null;
        }
        // ★ P1 修正：删除 Time.timeScale = 1.0f
    }

    #endregion
}
