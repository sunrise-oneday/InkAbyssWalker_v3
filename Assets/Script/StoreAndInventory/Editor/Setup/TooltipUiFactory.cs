#if UNITY_EDITOR
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

namespace StoreAndInventory.Editor
{
    public static class TooltipUiFactory
    {
        const float PanelWidth = 280f;
        const float PanelHeight = 300f;
        const float ActionButtonHeight = 36f;
        const float PanelPadding = 10f;
        const float BodyButtonGap = 6f;

        public static ItemTooltipUI EnsureFixedInfoPanel(
            Transform parent,
            Canvas canvas,
            string panelName = "ItemInfoPanel",
            Vector2 anchorMin = default,
            Vector2 anchorMax = default,
            Vector2 pivot = default,
            Vector2 anchoredPosition = default)
        {
            if (parent == null) return null;

            var existing = parent.Find(panelName)?.GetComponent<ItemTooltipUI>();
            if (existing != null)
            {
                UpgradeLegacyPanel(existing, canvas, followMouse: false, dismissOnOutsideClick: true, hideAfterActionClick: false);
                return existing;
            }

            if (anchorMin == default && anchorMax == default)
            {
                anchorMin = new Vector2(1f, 1f);
                anchorMax = new Vector2(1f, 1f);
                pivot = new Vector2(1f, 1f);
                anchoredPosition = new Vector2(-12f, -80f);
            }

            return CreatePanel(parent, canvas, panelName, followMouse: false, dismissOnOutsideClick: true,
                hideAfterActionClick: false, anchorMin, anchorMax, pivot, anchoredPosition);
        }

        public static ItemTooltipUI EnsureClickPopupInfoPanel(Transform parent, Canvas canvas, string panelName = "ItemInfoPanel")
        {
            if (parent == null) return null;

            var existing = parent.Find(panelName)?.GetComponent<ItemTooltipUI>();
            if (existing != null)
            {
                UpgradeLegacyPanel(existing, canvas, followMouse: false, dismissOnOutsideClick: true, hideAfterActionClick: true);
                return existing;
            }

            return CreatePanel(parent, canvas, panelName, followMouse: false, dismissOnOutsideClick: true,
                hideAfterActionClick: true,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero);
        }

        public static ItemTooltipUI EnsureMouseFollowInfoPanel(
            Transform parent,
            Canvas canvas,
            string panelName = "ItemInfoPanel")
        {
            if (parent == null) return null;

            var existing = parent.Find(panelName)?.GetComponent<ItemTooltipUI>();
            if (existing != null)
            {
                UpgradeLegacyPanel(existing, canvas, followMouse: true, dismissOnOutsideClick: true, hideAfterActionClick: false);
                return existing;
            }

            return CreatePanel(parent, canvas, panelName, followMouse: true, dismissOnOutsideClick: true,
                hideAfterActionClick: false,
                new Vector2(0f, 1f), new Vector2(0f, 1f), new Vector2(0f, 1f), Vector2.zero);
        }

        static ItemTooltipUI CreatePanel(
            Transform parent,
            Canvas canvas,
            string panelName,
            bool followMouse,
            bool dismissOnOutsideClick,
            bool hideAfterActionClick,
            Vector2 anchorMin,
            Vector2 anchorMax,
            Vector2 pivot,
            Vector2 anchoredPosition)
        {
            var panelGo = new GameObject(panelName, typeof(RectTransform), typeof(CanvasGroup), typeof(Image));
            panelGo.transform.SetParent(parent, false);

            var panelRect = panelGo.GetComponent<RectTransform>();
            panelRect.anchorMin = anchorMin;
            panelRect.anchorMax = anchorMax;
            panelRect.pivot = pivot;
            panelRect.anchoredPosition = anchoredPosition;
            panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            panelGo.GetComponent<Image>().color = new Color(0.08f, 0.08f, 0.08f, 0.94f);

            var infoBody = CreateInfoBody(panelGo.transform);
            var nameText = CreateLine(infoBody, "NameText", 20, FontStyles.Bold);

            var scrollRect = CreateScrollArea(infoBody, out var scrollContent);
            var categoryText = CreateLine(scrollContent, "CategoryText", 14, FontStyles.Italic);
            var priceText = CreateLine(scrollContent, "PriceText", 16, FontStyles.Normal);
            var stockText = CreateLine(scrollContent, "StockText", 14, FontStyles.Normal);
            var descriptionText = CreateLine(scrollContent, "DescriptionText", 14, FontStyles.Normal, wrap: true);
            var statsText = CreateLine(scrollContent, "StatsText", 14, FontStyles.Normal, wrap: true);
            var skillsText = CreateLine(scrollContent, "SkillsText", 14, FontStyles.Normal, wrap: true);
            var effectsText = CreateLine(scrollContent, "EffectsText", 14, FontStyles.Normal, wrap: true);
            var extraText = CreateLine(scrollContent, "ExtraText", 14, FontStyles.Normal, wrap: true);

            var (actionButton, actionButtonText) = CreateActionButton(panelGo.transform);

            var tooltip = panelGo.AddComponent<ItemTooltipUI>();
            var so = new SerializedObject(tooltip);
            so.FindProperty("panel").objectReferenceValue = panelRect;
            so.FindProperty("rootCanvas").objectReferenceValue = canvas;
            so.FindProperty("canvasGroup").objectReferenceValue = panelGo.GetComponent<CanvasGroup>();
            so.FindProperty("scrollRect").objectReferenceValue = scrollRect;
            so.FindProperty("followMouse").boolValue = followMouse;
            so.FindProperty("dismissOnOutsideClick").boolValue = dismissOnOutsideClick;
            so.FindProperty("hideAfterActionClick").boolValue = hideAfterActionClick;
            so.FindProperty("nameText").objectReferenceValue = nameText;
            so.FindProperty("categoryText").objectReferenceValue = categoryText;
            so.FindProperty("priceText").objectReferenceValue = priceText;
            so.FindProperty("stockText").objectReferenceValue = stockText;
            so.FindProperty("descriptionText").objectReferenceValue = descriptionText;
            so.FindProperty("statsText").objectReferenceValue = statsText;
            so.FindProperty("skillsText").objectReferenceValue = skillsText;
            so.FindProperty("effectsText").objectReferenceValue = effectsText;
            so.FindProperty("extraText").objectReferenceValue = extraText;
            so.FindProperty("actionButton").objectReferenceValue = actionButton;
            so.FindProperty("actionButtonText").objectReferenceValue = actionButtonText;
            so.ApplyModifiedPropertiesWithoutUndo();

            panelGo.SetActive(false);
            return tooltip;
        }

        static RectTransform CreateInfoBody(Transform panelRoot)
        {
            var bodyGo = new GameObject("InfoBody", typeof(RectTransform), typeof(VerticalLayoutGroup));
            bodyGo.transform.SetParent(panelRoot, false);
            bodyGo.transform.SetAsFirstSibling();

            var bodyRect = bodyGo.GetComponent<RectTransform>();
            bodyRect.anchorMin = Vector2.zero;
            bodyRect.anchorMax = Vector2.one;
            var bottomReserve = PanelPadding + ActionButtonHeight + BodyButtonGap;
            bodyRect.offsetMin = new Vector2(PanelPadding, bottomReserve);
            bodyRect.offsetMax = new Vector2(-PanelPadding, -PanelPadding);

            var layout = bodyGo.GetComponent<VerticalLayoutGroup>();
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.UpperLeft;
            layout.childControlWidth = true;
            layout.childControlHeight = true;
            layout.childForceExpandWidth = true;
            layout.childForceExpandHeight = false;

            return bodyRect;
        }

        static ScrollRect CreateScrollArea(Transform parent, out RectTransform content)
        {
            var scrollGo = new GameObject("ScrollArea", typeof(RectTransform), typeof(Image), typeof(ScrollRect), typeof(LayoutElement));
            scrollGo.transform.SetParent(parent, false);

            var scrollLayout = scrollGo.GetComponent<LayoutElement>();
            scrollLayout.minHeight = 160f;
            scrollLayout.preferredHeight = 160f;
            scrollLayout.flexibleHeight = 1f;

            var scrollRect = scrollGo.GetComponent<ScrollRect>();
            scrollRect.horizontal = false;
            scrollRect.vertical = true;
            scrollRect.movementType = ScrollRect.MovementType.Clamped;
            scrollGo.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.25f);

            var viewportGo = new GameObject("Viewport", typeof(RectTransform), typeof(Image), typeof(Mask));
            viewportGo.transform.SetParent(scrollGo.transform, false);
            var viewportRect = viewportGo.GetComponent<RectTransform>();
            viewportRect.anchorMin = Vector2.zero;
            viewportRect.anchorMax = Vector2.one;
            viewportRect.offsetMin = Vector2.zero;
            viewportRect.offsetMax = Vector2.zero;
            viewportGo.GetComponent<Image>().color = Color.white;
            viewportGo.GetComponent<Mask>().showMaskGraphic = false;

            var contentGo = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup), typeof(ContentSizeFitter));
            contentGo.transform.SetParent(viewportGo.transform, false);
            content = contentGo.GetComponent<RectTransform>();
            content.anchorMin = new Vector2(0f, 1f);
            content.anchorMax = new Vector2(1f, 1f);
            content.pivot = new Vector2(0.5f, 1f);
            content.anchoredPosition = Vector2.zero;
            content.sizeDelta = new Vector2(0f, 0f);

            var contentLayout = contentGo.GetComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(4, 4, 4, 4);
            contentLayout.spacing = 4f;
            contentLayout.childAlignment = TextAnchor.UpperLeft;
            contentLayout.childControlWidth = true;
            contentLayout.childControlHeight = true;
            contentLayout.childForceExpandWidth = true;
            contentLayout.childForceExpandHeight = false;

            var fitter = contentGo.GetComponent<ContentSizeFitter>();
            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;
            fitter.horizontalFit = ContentSizeFitter.FitMode.Unconstrained;

            scrollRect.viewport = viewportRect;
            scrollRect.content = content;
            return scrollRect;
        }

        static (Button, TextMeshProUGUI) CreateActionButton(Transform panelRoot)
        {
            var btnGo = new GameObject("ActionButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(panelRoot, false);

            var btnRect = btnGo.GetComponent<RectTransform>();
            btnRect.anchorMin = new Vector2(0f, 0f);
            btnRect.anchorMax = new Vector2(1f, 0f);
            btnRect.pivot = new Vector2(0.5f, 0f);
            btnRect.anchoredPosition = new Vector2(0f, PanelPadding);
            btnRect.sizeDelta = new Vector2(-PanelPadding * 2f, ActionButtonHeight);

            btnGo.GetComponent<Image>().color = new Color(0.25f, 0.55f, 0.25f, 1f);

            var labelGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
            labelGo.transform.SetParent(btnGo.transform, false);
            var labelRect = labelGo.GetComponent<RectTransform>();
            labelRect.anchorMin = Vector2.zero;
            labelRect.anchorMax = Vector2.one;
            labelRect.offsetMin = Vector2.zero;
            labelRect.offsetMax = Vector2.zero;

            var label = labelGo.GetComponent<TextMeshProUGUI>();
            label.text = "Action";
            label.alignment = TextAlignmentOptions.Center;
            label.fontSize = 16;
            label.color = Color.white;

            btnGo.SetActive(false);
            return (btnGo.GetComponent<Button>(), label);
        }

        static void UpgradeLegacyPanel(ItemTooltipUI tooltip, Canvas canvas, bool followMouse, bool dismissOnOutsideClick, bool hideAfterActionClick)
        {
            var so = new SerializedObject(tooltip);
            if (so.FindProperty("rootCanvas").objectReferenceValue == null)
                so.FindProperty("rootCanvas").objectReferenceValue = canvas;

            if (so.FindProperty("panel").objectReferenceValue == null)
                so.FindProperty("panel").objectReferenceValue = tooltip.GetComponent<RectTransform>();

            so.FindProperty("followMouse").boolValue = followMouse;
            so.FindProperty("dismissOnOutsideClick").boolValue = dismissOnOutsideClick;
            so.FindProperty("hideAfterActionClick").boolValue = hideAfterActionClick;

            var panelRect = tooltip.GetComponent<RectTransform>();
            if (panelRect != null && panelRect.sizeDelta.y <= 0.01f)
                panelRect.sizeDelta = new Vector2(PanelWidth, PanelHeight);

            var legacyFitter = tooltip.GetComponent<ContentSizeFitter>();
            if (legacyFitter != null)
                Object.DestroyImmediate(legacyFitter);

            var hadRootLayout = tooltip.GetComponent<VerticalLayoutGroup>() != null;
            EnsureIndependentButtonLayout(tooltip.transform, panelRect, hadRootLayout);

            if (so.FindProperty("scrollRect").objectReferenceValue == null)
            {
                var scroll = tooltip.GetComponentInChildren<ScrollRect>(true);
                if (scroll == null && panelRect != null)
                {
                    var body = panelRect.Find("InfoBody") ?? panelRect;
                    scroll = CreateScrollArea(body, out var content);
                    MoveLegacyTextIntoScroll(tooltip.transform, content);
                }

                so.FindProperty("scrollRect").objectReferenceValue = scroll;
            }

            TryWireFieldDeep(tooltip.transform, so, "nameText", "NameText");
            TryWireFieldDeep(tooltip.transform, so, "categoryText", "CategoryText");
            TryWireFieldDeep(tooltip.transform, so, "priceText", "PriceText");
            TryWireFieldDeep(tooltip.transform, so, "stockText", "StockText");
            TryWireFieldDeep(tooltip.transform, so, "descriptionText", "DescriptionText");
            TryWireFieldDeep(tooltip.transform, so, "statsText", "StatsText");
            TryWireFieldDeep(tooltip.transform, so, "skillsText", "SkillsText");
            TryWireFieldDeep(tooltip.transform, so, "effectsText", "EffectsText");
            TryWireFieldDeep(tooltip.transform, so, "extraText", "ExtraText");

            if (so.FindProperty("actionButton").objectReferenceValue == null)
            {
                var actionBtn = tooltip.transform.Find("ActionButton")?.GetComponent<Button>();
                if (actionBtn == null)
                {
                    CreateActionButton(tooltip.transform);
                    actionBtn = tooltip.transform.Find("ActionButton")?.GetComponent<Button>();
                }

                so.FindProperty("actionButton").objectReferenceValue = actionBtn;
                so.FindProperty("actionButtonText").objectReferenceValue =
                    actionBtn?.transform.Find("Label")?.GetComponent<TextMeshProUGUI>();
            }

            var rarity = tooltip.transform.Find("RarityText");
            if (rarity != null)
                rarity.gameObject.SetActive(false);

            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void EnsureIndependentButtonLayout(Transform root, RectTransform panelRect, bool applyDefaultButtonLayout)
        {
            if (root == null || panelRect == null) return;

            var rootVlg = root.GetComponent<VerticalLayoutGroup>();
            if (rootVlg != null)
                Object.DestroyImmediate(rootVlg);

            var actionTf = root.Find("ActionButton");
            var bodyTf = root.Find("InfoBody");
            var createdBody = bodyTf == null;

            if (createdBody)
            {
                CreateInfoBody(root);
                bodyTf = root.Find("InfoBody");
            }

            if (bodyTf == null) return;

            for (var i = root.childCount - 1; i >= 0; i--)
            {
                var child = root.GetChild(i);
                if (child == bodyTf || child.name == "ActionButton")
                    continue;

                if (child.name == "NameText" || child.name == "ScrollArea")
                    child.SetParent(bodyTf, false);
            }

            if (actionTf == null) return;

            actionTf.SetParent(root, false);
            actionTf.SetAsLastSibling();

            var hadLayoutElement = actionTf.GetComponent<LayoutElement>() != null;
            if (hadLayoutElement)
                Object.DestroyImmediate(actionTf.GetComponent<LayoutElement>());

            if (applyDefaultButtonLayout || createdBody || hadLayoutElement)
                ApplyIndependentActionButtonLayout(actionTf as RectTransform);
        }

        static void ApplyIndependentActionButtonLayout(RectTransform btnRect)
        {
            if (btnRect == null) return;

            btnRect.anchorMin = new Vector2(0f, 0f);
            btnRect.anchorMax = new Vector2(1f, 0f);
            btnRect.pivot = new Vector2(0.5f, 0f);
            btnRect.anchoredPosition = new Vector2(0f, PanelPadding);
            btnRect.sizeDelta = new Vector2(-PanelPadding * 2f, ActionButtonHeight);
        }

        static void MoveLegacyTextIntoScroll(Transform root, RectTransform scrollContent)
        {
            var names = new[]
            {
                "CategoryText", "PriceText", "StockText", "DescriptionText",
                "StatsText", "SkillsText", "EffectsText", "ExtraText"
            };

            for (var i = 0; i < names.Length; i++)
            {
                var child = root.Find(names[i]);
                if (child != null)
                    child.SetParent(scrollContent, false);
            }
        }

        static void TryWireFieldDeep(Transform root, SerializedObject so, string prop, string childName)
        {
            if (so.FindProperty(prop).objectReferenceValue != null) return;

            var direct = root.Find(childName)?.GetComponent<TextMeshProUGUI>();
            if (direct != null)
            {
                so.FindProperty(prop).objectReferenceValue = direct;
                return;
            }

            var body = root.Find("InfoBody");
            if (body != null)
            {
                var inBody = body.Find(childName)?.GetComponent<TextMeshProUGUI>();
                if (inBody != null)
                {
                    so.FindProperty(prop).objectReferenceValue = inBody;
                    return;
                }
            }

            var scrollContent = root.GetComponentInChildren<ScrollRect>(true)?.content;
            if (scrollContent == null) return;

            var nested = scrollContent.Find(childName)?.GetComponent<TextMeshProUGUI>();
            if (nested != null)
                so.FindProperty(prop).objectReferenceValue = nested;
        }

        static TextMeshProUGUI CreateLine(Transform parent, string name, int fontSize, FontStyles style, bool wrap = false)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI), typeof(LayoutElement));
            go.transform.SetParent(parent, false);

            var layout = go.GetComponent<LayoutElement>();
            layout.minHeight = fontSize + 6f;

            var tmp = go.GetComponent<TextMeshProUGUI>();
            tmp.fontSize = fontSize;
            tmp.fontStyle = style;
            tmp.color = Color.white;
            tmp.alignment = TextAlignmentOptions.TopLeft;
            tmp.enableWordWrapping = wrap;
            tmp.richText = true;
            tmp.text = name;
            return tmp;
        }
    }
}
#endif
