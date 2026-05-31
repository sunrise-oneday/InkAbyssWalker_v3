using UnityEngine;
using UnityEngine.EventSystems;

namespace Battle.UI
{
    /// <summary>
    /// 意图悬浮提示触发器
    /// 挂在意图图标上，鼠标悬停时显示意图描述
    /// </summary>
    public class IntentTooltipTrigger : MonoBehaviour, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("引用")]
        [SerializeField] private EntityHUD entityHUD;

        [Header("调试")]
        [SerializeField] private bool enableDebug = true;

        private void Start()
        {
            // 自动获取父物体的EntityHUD
            if (entityHUD == null)
            {
                entityHUD = GetComponentInParent<EntityHUD>();
            }

            if (enableDebug)
            {
                Debug.Log($"[意图触发器] 初始化完成 - {gameObject.name}, entityHUD: {(entityHUD != null ? "OK" : "NULL")}, RaycastTarget: {GetComponent<UnityEngine.UI.Image>()?.raycastTarget}");
            }
        }

        /// <summary>
        /// 鼠标进入时显示悬浮提示
        /// </summary>
        public void OnPointerEnter(PointerEventData eventData)
        {
            if (enableDebug)
            {
                Debug.Log($"[意图触发器] 鼠标进入 - {gameObject.name}");
            }

            if (entityHUD != null)
            {
                entityHUD.ShowIntentTooltip();
            }
        }

        /// <summary>
        /// 鼠标离开时隐藏悬浮提示
        /// </summary>
        public void OnPointerExit(PointerEventData eventData)
        {
            if (enableDebug)
            {
                Debug.Log($"[意图触发器] 鼠标离开 - {gameObject.name}");
            }

            if (entityHUD != null)
            {
                entityHUD.HideIntentTooltip();
            }
        }
    }
}
