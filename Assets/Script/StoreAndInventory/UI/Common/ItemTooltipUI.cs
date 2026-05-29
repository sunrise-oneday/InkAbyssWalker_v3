using System;
using System.Text;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace StoreAndInventory
{
    public class ItemTooltipUI : MonoBehaviour
    {
        const float ScreenClampPadding = 8f;

        [SerializeField] RectTransform panel;
        [SerializeField] Canvas rootCanvas;
        [SerializeField] CanvasGroup canvasGroup;
        [SerializeField] ScrollRect scrollRect;
        [SerializeField] Vector2 screenOffset = new(18f, -18f);
        [SerializeField] bool followMouse;
        [SerializeField] bool dismissOnOutsideClick = true;
        [SerializeField] bool hideAfterActionClick;

        [Header("One field → one UI element")]
        [SerializeField] TextMeshProUGUI nameText;
        [SerializeField] TextMeshProUGUI categoryText;
        [SerializeField] TextMeshProUGUI priceText;
        [SerializeField] TextMeshProUGUI stockText;
        [SerializeField] TextMeshProUGUI descriptionText;
        [SerializeField] TextMeshProUGUI statsText;
        [SerializeField] TextMeshProUGUI skillsText;
        [SerializeField] TextMeshProUGUI effectsText;
        [SerializeField] TextMeshProUGUI extraText;

        [Header("Action button (layout independent from InfoBody scroll area)")]
        [SerializeField] Button actionButton;
        [SerializeField] TextMeshProUGUI actionButtonText;
        [SerializeField] bool hideActionButtonWhenNoLabel = true;

        readonly StringBuilder _sb = new(128);
        bool visible;
        int suppressDismissUntilFrame;
        Action actionCallback;

        public bool IsVisible => visible;

        void Awake()
        {
            if (panel == null)
                panel = transform as RectTransform;

            if (rootCanvas == null)
                rootCanvas = GetComponentInParent<Canvas>();

            if (scrollRect == null)
                scrollRect = GetComponentInChildren<ScrollRect>(true);

            if (actionButton != null)
                actionButton.onClick.AddListener(HandleActionClick);

            if (gameObject.activeSelf && !visible)
                ApplyHiddenState();
        }

        void OnDestroy()
        {
            if (actionButton != null)
                actionButton.onClick.RemoveListener(HandleActionClick);
        }

        void LateUpdate()
        {
            if (visible && followMouse)
                PlaceAtScreenPoint(Input.mousePosition);

            TryDismissOnOutsideClick();
        }

        void TryDismissOnOutsideClick()
        {
            if (!visible || !dismissOnOutsideClick) return;
            if (Time.frameCount <= suppressDismissUntilFrame) return;

            if (Input.GetMouseButtonDown(0) && !IsPointerOverPanel())
                Hide();
        }

        public void Show(ItemBase item, ShopEntry entry, ShopService shopService, Vector2 screenPosition)
        {
            if (item == null)
            {
                Hide();
                return;
            }

            BindFields(item, entry, shopService);
            ShowInternal();

            if (followMouse)
                PlaceAtScreenPoint(screenPosition);
        }

        public void ShowFixed(ItemBase item, ShopEntry entry, ShopService shopService)
        {
            if (item == null)
            {
                Hide();
                return;
            }

            BindFields(item, entry, shopService);
            ShowInternal();
        }

        public void ShowFixed(ItemBase item)
        {
            if (item == null)
            {
                Hide();
                return;
            }

            BindFields(item, null, null);
            ShowInternal();
        }

        public void ShowAtClickPosition(ItemBase item, Vector2 screenPosition)
        {
            if (item == null)
            {
                Hide();
                return;
            }

            BindFields(item, null, null);
            ShowInternal();
            PlaceAtScreenPoint(screenPosition);
        }

        [Obsolete("Use ShowAtClickPosition for one-shot placement.")]
        public void ShowAtMouse(ItemBase item, Vector2 screenPosition) => ShowAtClickPosition(item, screenPosition);

        void ShowInternal()
        {
            visible = true;
            suppressDismissUntilFrame = Time.frameCount + 1;

            if (canvasGroup != null)
            {
                canvasGroup.alpha = 1f;
                canvasGroup.blocksRaycasts = true;
            }

            ResetScroll();

            if (!gameObject.activeSelf)
                gameObject.SetActive(true);
        }

        void ResetScroll()
        {
            if (scrollRect == null) return;
            Canvas.ForceUpdateCanvases();
            scrollRect.verticalNormalizedPosition = 1f;
        }

        /// <summary>
        /// label 非空时始终显示按钮；不可交互时显示原因文案（如 Sold out）。
        /// </summary>
        public void ConfigureAction(string label, Action onClick, bool interactable = true)
        {
            var show = !string.IsNullOrEmpty(label);
            actionCallback = show && interactable ? onClick : null;

            if (actionButton != null)
            {
                if (hideActionButtonWhenNoLabel)
                    actionButton.gameObject.SetActive(show);
                actionButton.interactable = interactable && onClick != null;
            }

            if (actionButtonText != null)
                actionButtonText.text = show ? label : string.Empty;
        }

        public void Hide()
        {
            visible = false;
            suppressDismissUntilFrame = 0;
            actionCallback = null;
            ConfigureAction(null, null);
            ApplyHiddenState();
        }

        void ApplyHiddenState()
        {
            if (gameObject.activeSelf)
                gameObject.SetActive(false);
        }

        void HandleActionClick()
        {
            actionCallback?.Invoke();
            if (hideAfterActionClick)
                Hide();
        }

        void BindFields(ItemBase item, ShopEntry entry, ShopService shopService)
        {
            SetField(nameText, item.Name);
            SetField(categoryText, CategoryLabel(item.Category));

            if (entry != null && shopService != null)
            {
                SetField(priceText, $"Price: {shopService.GetPrice(entry)} Ink");

                if (item.Category == ItemCategory.Equipment)
                {
                    var remaining = shopService.GetRemainingStock(item.id);
                    if (remaining >= 0)
                        SetField(stockText, $"Stock: {remaining}");
                    else
                        SetField(stockText, "Stock: Unlimited");
                }
                else
                {
                    SetField(stockText, null);
                }
            }
            else
            {
                SetField(priceText, item.basePrice > 0 ? $"Base price: {item.basePrice}" : null);
                SetField(stockText, null);
            }

            SetField(descriptionText, item.description);
            SetField(statsText, BuildStatsText(item));
            SetField(skillsText, BuildSkillsText(item));
            SetField(effectsText, BuildEffectsText(item));
            SetField(extraText, BuildExtraText(item));
        }

        static string CategoryLabel(ItemCategory category)
        {
            return category switch
            {
                ItemCategory.Equipment => "Type: Rune",
                ItemCategory.Consumable => "Type: Consumable",
                ItemCategory.StoryItem => "Type: Story",
                _ => $"Type: {category}"
            };
        }

        string BuildStatsText(ItemBase item)
        {
            if (item is not Equipment equip || equip.statMods == null || equip.statMods.Count == 0)
                return null;

            _sb.Clear();
            for (var i = 0; i < equip.statMods.Count; i++)
            {
                var mod = equip.statMods[i];
                if (_sb.Length > 0) _sb.Append('\n');
                _sb.Append(FormatStatMod(mod));
            }

            return _sb.ToString();
        }

        string BuildSkillsText(ItemBase item)
        {
            if (item is not Equipment equip || equip.skillMods == null || equip.skillMods.Count == 0)
                return null;

            _sb.Clear();
            for (var i = 0; i < equip.skillMods.Count; i++)
            {
                var mod = equip.skillMods[i];
                if (_sb.Length > 0) _sb.Append('\n');
                _sb.Append($"{mod.targetKind} {mod.targetId}: {mod.modType} {mod.value:+#.##;-#.##;0}");
            }

            return _sb.ToString();
        }

        string BuildEffectsText(ItemBase item)
        {
            if (item is not Equipment equip || equip.extraEffects == null || equip.extraEffects.Count == 0)
                return null;

            _sb.Clear();
            for (var i = 0; i < equip.extraEffects.Count; i++)
            {
                var effect = equip.extraEffects[i];
                if (effect == null) continue;
                if (_sb.Length > 0) _sb.Append('\n');
                var label = !string.IsNullOrEmpty(effect.displayNameKey)
                    ? effect.displayNameKey
                    : effect.effectTag;
                _sb.Append(label);
            }

            return _sb.Length > 0 ? _sb.ToString() : null;
        }

        string BuildExtraText(ItemBase item)
        {
            switch (item)
            {
                case Consumable consumable:
                    _sb.Clear();
                    _sb.Append($"Use: {consumable.useContext}");
                    _sb.Append('\n');
                    _sb.Append($"Consume: {consumable.consumeOnUse}");
                    return _sb.ToString();

                case StoryItem story:
                    _sb.Clear();
                    if (!string.IsNullOrEmpty(story.questFlagId))
                        _sb.Append($"Quest: {story.questFlagId}");
                    if (!string.IsNullOrEmpty(story.extraStoryText))
                    {
                        if (_sb.Length > 0) _sb.Append("\n\n");
                        _sb.Append(story.extraStoryText);
                    }

                    return _sb.Length > 0 ? _sb.ToString() : null;

                default:
                    return null;
            }
        }

        static string FormatStatMod(StatModifier mod)
        {
            var statName = StatLabel(mod.stat);
            if (Mathf.Abs(mod.flat) > 0.001f && Mathf.Abs(mod.percent) > 0.001f)
                return $"{statName} +{mod.flat:0.##} / +{mod.percent * 100f:0.#}%";

            if (Mathf.Abs(mod.flat) > 0.001f)
                return $"{statName} +{mod.flat:0.##}";

            if (Mathf.Abs(mod.percent) > 0.001f)
                return $"{statName} +{mod.percent * 100f:0.#}%";

            return statName;
        }

        static string StatLabel(StatType stat)
        {
            return stat switch
            {
                StatType.Attack => "Attack",
                StatType.MaxHp => "Max HP",
                StatType.Defense => "Defense",
                StatType.Speed => "Speed",
                StatType.CritRate => "Crit Rate",
                _ => stat.ToString()
            };
        }

        static void SetField(TextMeshProUGUI text, string value)
        {
            if (text == null) return;

            var show = !string.IsNullOrEmpty(value);
            text.gameObject.SetActive(show);
            if (!show) return;

            text.text = value;
        }

        bool IsPointerOverPanel()
        {
            if (panel == null) return false;

            var cam = rootCanvas != null && rootCanvas.renderMode != RenderMode.ScreenSpaceOverlay
                ? rootCanvas.worldCamera
                : null;

            if (RectTransformUtility.RectangleContainsScreenPoint(panel, Input.mousePosition, cam))
                return true;

            if (actionButton != null && actionButton.gameObject.activeInHierarchy)
            {
                var actionRect = actionButton.transform as RectTransform;
                if (actionRect != null &&
                    RectTransformUtility.RectangleContainsScreenPoint(actionRect, Input.mousePosition, cam))
                    return true;
            }

            return false;
        }

        void PlaceAtScreenPoint(Vector2 screenPosition)
        {
            if (panel == null) return;

            var canvas = rootCanvas != null ? rootCanvas : GetComponentInParent<Canvas>();
            if (canvas == null) return;

            var canvasRect = canvas.transform as RectTransform;
            var panelParent = panel.parent as RectTransform;
            if (canvasRect == null || panelParent == null) return;

            var cam = canvas.renderMode == RenderMode.ScreenSpaceOverlay
                ? null
                : canvas.worldCamera;

            if (!RectTransformUtility.ScreenPointToLocalPointInRectangle(
                    panelParent, screenPosition, cam, out var localPoint))
                return;

            panel.anchoredPosition = localPoint + screenOffset;
            ClampPanelInsideCanvas(canvasRect, cam);
        }

        void ClampPanelInsideCanvas(RectTransform canvasRect, Camera cam)
        {
            if (panel == null || canvasRect == null) return;

            Canvas.ForceUpdateCanvases();

            var panelMin = GetScreenPoint(panel, new Vector2(0f, 0f), cam);
            var panelMax = GetScreenPoint(panel, new Vector2(1f, 1f), cam);
            var canvasMin = GetScreenPoint(canvasRect, new Vector2(0f, 0f), cam);
            var canvasMax = GetScreenPoint(canvasRect, new Vector2(1f, 1f), cam);

            var shift = Vector2.zero;
            if (panelMin.x < canvasMin.x + ScreenClampPadding)
                shift.x = canvasMin.x + ScreenClampPadding - panelMin.x;
            else if (panelMax.x > canvasMax.x - ScreenClampPadding)
                shift.x = canvasMax.x - ScreenClampPadding - panelMax.x;

            if (panelMin.y < canvasMin.y + ScreenClampPadding)
                shift.y = canvasMin.y + ScreenClampPadding - panelMin.y;
            else if (panelMax.y > canvasMax.y - ScreenClampPadding)
                shift.y = canvasMax.y - ScreenClampPadding - panelMax.y;

            if (shift.sqrMagnitude < 0.001f) return;

            var current = panel.position;
            panel.position = current + new Vector3(shift.x, shift.y, 0f);
        }

        static Vector2 GetScreenPoint(RectTransform rect, Vector2 normalized, Camera cam)
        {
            var world = rect.TransformPoint(new Vector3(
                rect.rect.xMin + rect.rect.width * normalized.x,
                rect.rect.yMin + rect.rect.height * normalized.y,
                0f));
            return RectTransformUtility.WorldToScreenPoint(cam, world);
        }
    }
}
