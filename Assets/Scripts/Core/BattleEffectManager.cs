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

    #region 魔女时间（子弹时间）

    /// <summary>触发魔女时间，将游戏时间缩放至 0.2 倍速，持续指定真实时长</summary>
    public void WitchTime(float duration)
    {
        StartCoroutine(WitchTimeRoutine(duration));
    }

    private IEnumerator WitchTimeRoutine(float duration)
    {
        Time.timeScale = 0.2f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1.0f;
    }

    #endregion

    #region 镜头抖动

    /// <summary>触发战斗镜头抖动，指定持续时间和幅度</summary>
    public void ShakeCamera(float duration, float magnitude)
    {
        StartCoroutine(CameraShakeRoutine(duration, magnitude));
    }

    private IEnumerator CameraShakeRoutine(float duration, float magnitude)
    {
        Camera battleCam = Camera.main;
        if (battleCam == null) yield break;

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
    }

    #endregion

    #region HitStop（攻击顿挫）

    /// <summary>触发 HitStop，将游戏时间几乎冻结（0.05 倍速），持续指定真实时长</summary>
    public void HitStop(float duration)
    {
        StartCoroutine(HitStopRoutine(duration));
    }

    private IEnumerator HitStopRoutine(float duration)
    {
        Time.timeScale = 0.05f;
        yield return new WaitForSecondsRealtime(duration);
        Time.timeScale = 1.0f;
    }

    #endregion
}
