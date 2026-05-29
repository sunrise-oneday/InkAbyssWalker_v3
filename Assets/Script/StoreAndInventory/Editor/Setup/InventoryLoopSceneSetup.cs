#if UNITY_EDITOR
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using StoreAndInventory.Test;

namespace StoreAndInventory.Editor
{
    public static class InventoryLoopSceneSetup
    {
        const string MenuPath = "MoYuan/Setup Inventory Loop (Mid Plan B)";
        const string InventoryItemPrefabPath = "Assets/prefab/InventoryItem.prefab";
        const string SellItemPrefabPath = "Assets/prefab/SellItem.prefab";
        const string AttributeTextPrefabPath = "Assets/prefab/AttributeText.prefab";

        static readonly StatType[] AllStats =
        {
            StatType.Attack, StatType.MaxHp, StatType.Defense, StatType.Speed, StatType.CritRate
        };

        static readonly float[] DefaultStatValues = { 10f, 100f, 5f, 8f, 0.05f };

        [MenuItem(MenuPath)]
        public static void SetupScene()
        {
            var inventory = Object.FindObjectOfType<Inventory>();
            var database = Object.FindObjectOfType<ItemDatabase>();
            var wallet = Object.FindObjectOfType<WalletService>();
            var shopService = Object.FindObjectOfType<ShopService>();
            var shopUI = Object.FindObjectOfType<ShopUI>(true);
            var inventoryUI = Object.FindObjectOfType<InventoryUI>(true);
            var testController = Object.FindObjectOfType<ItemTestController>();

            if (inventory == null || database == null || shopService == null)
            {
                EditorUtility.DisplayDialog("Inventory Loop Setup",
                    "Scene must contain Inventory, ItemDatabase, and ShopService.",
                    "OK");
                return;
            }

            EnsureInventoryItemPrefab();
            var sellItemPrefab = EnsureSellItemPrefab();
            var itemPrefab = AssetDatabase.LoadAssetAtPath<InventoryItemUI>(InventoryItemPrefabPath);

            var testCharacter = EnsureTestCharacter();
            var equipmentService = EnsureEquipmentService(inventory, database);

            var panel = FindInventoryPanel();
            if (panel == null)
            {
                EditorUtility.DisplayDialog("Inventory Loop Setup",
                    "InventoryPanel not found under Canvas.",
                    "OK");
                return;
            }

            Undo.RegisterFullObjectHierarchyUndo(panel, "Setup Inventory Loop UI");
            inventoryUI = SetupInventoryPanel(panel, inventory, database, testCharacter, equipmentService, itemPrefab);

            if (shopUI != null)
                SetupShopSellTab(shopUI, shopService, wallet, inventory, sellItemPrefab);

            WireTestController(testController, shopUI, shopService, inventoryUI, wallet, testCharacter);

            EnsureStoreSaveService(inventory, wallet, equipmentService, shopService, testCharacter, testController);

            var canvas = inventoryUI != null ? inventoryUI.GetComponentInParent<Canvas>() : null;
            PanelControllerSetup.Ensure(canvas, shopUI, shopService, inventoryUI, testController);

            EditorUtility.SetDirty(inventoryUI);
            if (shopUI != null) EditorUtility.SetDirty(shopUI);
            if (testController != null) EditorUtility.SetDirty(testController);

            Debug.Log("[InventoryLoopSetup] Mid Plan B wired. Play: B=Shop, I=Inventory.");
        }

        static GameObject FindInventoryPanel()
        {
            var all = Object.FindObjectsOfType<RectTransform>(true);
            for (var i = 0; i < all.Length; i++)
            {
                if (all[i].name == "InventoryPanel")
                    return all[i].gameObject;
            }

            return null;
        }

        static InventoryUI SetupInventoryPanel(
            GameObject panel,
            Inventory inventory,
            ItemDatabase database,
            TestCharacter testCharacter,
            EquipmentService equipmentService,
            InventoryItemUI itemPrefab)
        {
            var oldShopUi = panel.GetComponent<ShopUI>();
            if (oldShopUi != null)
                Undo.DestroyObjectImmediate(oldShopUi);

            var ui = panel.GetComponent<InventoryUI>();
            if (ui == null)
                ui = Undo.AddComponent<InventoryUI>(panel);

            var content = panel.transform.Find("InventoryItemView/Content")
                          ?? panel.transform.Find("InventoryItemView/Viewport/Content");

            var statLines = EnsureStatLines(panel.transform);
            var runeSlots = EnsureRuneSlots(panel.transform);
            var canvas = panel.GetComponentInParent<Canvas>();
            var itemInfoPanel = TooltipUiFactory.EnsureClickPopupInfoPanel(
                canvas != null ? canvas.transform : panel.transform,
                canvas);
            var useButton = panel.transform.Find("UseButton")?.GetComponent<Button>()
                            ?? EnsureButton(panel.transform, "UseButton", "Use", new Vector2(-80f, 12f));
            var closeButton = panel.transform.Find("CloseButton")?.GetComponent<Button>()
                              ?? EnsureButton(panel.transform, "CloseButton", "Close", new Vector2(-12f, -12f), anchorTopRight: true);

            if (closeButton != null)
            {
                Undo.RecordObject(closeButton, "Rewire CloseButton");
                closeButton.onClick = new Button.ButtonClickedEvent();
            }

            var uiSo = new SerializedObject(ui);
            uiSo.FindProperty("inventory").objectReferenceValue = inventory;
            uiSo.FindProperty("database").objectReferenceValue = database;
            uiSo.FindProperty("testCharacter").objectReferenceValue = testCharacter;
            uiSo.FindProperty("equipmentService").objectReferenceValue = equipmentService;
            uiSo.FindProperty("itemPrefab").objectReferenceValue = itemPrefab;
            if (content != null)
                uiSo.FindProperty("content").objectReferenceValue = content;
            uiSo.FindProperty("statLines").arraySize = statLines.Length;
            for (var i = 0; i < statLines.Length; i++)
                uiSo.FindProperty("statLines").GetArrayElementAtIndex(i).objectReferenceValue = statLines[i];
            uiSo.FindProperty("runeSlots").arraySize = runeSlots.Length;
            for (var i = 0; i < runeSlots.Length; i++)
                uiSo.FindProperty("runeSlots").GetArrayElementAtIndex(i).objectReferenceValue = runeSlots[i];
            uiSo.FindProperty("itemInfoPanel").objectReferenceValue = itemInfoPanel;
            uiSo.FindProperty("useButton").objectReferenceValue = useButton;
            uiSo.FindProperty("closeButton").objectReferenceValue = closeButton;
            uiSo.ApplyModifiedPropertiesWithoutUndo();

            return ui;
        }

        static AttributeStatLineUI[] EnsureStatLines(Transform panel)
        {
            var container = panel.Find("CharacterStatsPanel");
            if (container == null)
            {
                var go = new GameObject("CharacterStatsPanel", typeof(RectTransform), typeof(VerticalLayoutGroup));
                Undo.RegisterCreatedObjectUndo(go, "Create CharacterStatsPanel");
                go.transform.SetParent(panel, false);
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(12f, -12f);
                rect.sizeDelta = new Vector2(180f, 160f);
                var layout = go.GetComponent<VerticalLayoutGroup>();
                layout.spacing = 4f;
                layout.childAlignment = TextAnchor.UpperLeft;
                container = go.transform;
            }

            var attrPrefab = AssetDatabase.LoadAssetAtPath<GameObject>(AttributeTextPrefabPath);
            var lines = new List<AttributeStatLineUI>();

            for (var i = 0; i < AllStats.Length; i++)
            {
                var stat = AllStats[i];
                var childName = $"StatLine_{stat}";
                Transform lineTf = container.Find(childName);
                GameObject lineGo;

                if (lineTf == null)
                {
                    if (attrPrefab != null)
                    {
                        lineGo = (GameObject)PrefabUtility.InstantiatePrefab(attrPrefab, container);
                        lineGo.name = childName;
                    }
                    else
                    {
                        lineGo = new GameObject(childName, typeof(RectTransform), typeof(TextMeshProUGUI));
                        lineGo.transform.SetParent(container, false);
                    }

                    Undo.RegisterCreatedObjectUndo(lineGo, "Create StatLine");
                }
                else
                {
                    lineGo = lineTf.gameObject;
                }

                var statLine = lineGo.GetComponent<AttributeStatLineUI>();
                if (statLine == null)
                    statLine = Undo.AddComponent<AttributeStatLineUI>(lineGo);

                var tmp = lineGo.GetComponentInChildren<TextMeshProUGUI>(true);
                if (tmp == null)
                    tmp = lineGo.GetComponent<TextMeshProUGUI>();

                var so = new SerializedObject(statLine);
                so.FindProperty("statType").enumValueIndex = (int)stat;
                so.FindProperty("valueText").objectReferenceValue = tmp;
                so.ApplyModifiedPropertiesWithoutUndo();

                lines.Add(statLine);
            }

            return lines.ToArray();
        }

        static EquipmentSlotUI[] EnsureRuneSlots(Transform panel)
        {
            var container = panel.Find("RuneSlotsPanel");
            if (container == null)
            {
                var go = new GameObject("RuneSlotsPanel", typeof(RectTransform), typeof(HorizontalLayoutGroup));
                Undo.RegisterCreatedObjectUndo(go, "Create RuneSlotsPanel");
                go.transform.SetParent(panel, false);
                var rect = go.GetComponent<RectTransform>();
                rect.anchorMin = new Vector2(0f, 1f);
                rect.anchorMax = new Vector2(0f, 1f);
                rect.pivot = new Vector2(0f, 1f);
                rect.anchoredPosition = new Vector2(12f, -180f);
                rect.sizeDelta = new Vector2(220f, 72f);
                var layout = go.GetComponent<HorizontalLayoutGroup>();
                layout.spacing = 8f;
                layout.childAlignment = TextAnchor.MiddleLeft;
                container = go.transform;
            }

            var slots = new List<EquipmentSlotUI>();
            for (var i = 0; i < 3; i++)
            {
                var slotName = $"RuneSlot_{i}";
                Transform slotTf = container.Find(slotName);
                GameObject slotGo;

                if (slotTf == null)
                {
                    slotGo = new GameObject(slotName, typeof(RectTransform), typeof(Image), typeof(Button));
                    Undo.RegisterCreatedObjectUndo(slotGo, "Create RuneSlot");
                    slotGo.transform.SetParent(container, false);
                    var rect = slotGo.GetComponent<RectTransform>();
                    rect.sizeDelta = new Vector2(64f, 64f);

                    var bg = slotGo.GetComponent<Image>();
                    bg.color = new Color(0.85f, 0.85f, 0.85f, 1f);

                    var iconGo = new GameObject("Icon", typeof(RectTransform), typeof(Image));
                    iconGo.transform.SetParent(slotGo.transform, false);
                    var iconRect = iconGo.GetComponent<RectTransform>();
                    iconRect.anchorMin = Vector2.zero;
                    iconRect.anchorMax = Vector2.one;
                    iconRect.offsetMin = new Vector2(4f, 4f);
                    iconRect.offsetMax = new Vector2(-4f, -4f);
                    var icon = iconGo.GetComponent<Image>();
                    icon.preserveAspect = true;
                    icon.enabled = false;

                    var emptyGo = new GameObject("EmptyFrame", typeof(RectTransform), typeof(Image));
                    emptyGo.transform.SetParent(slotGo.transform, false);
                    var emptyRect = emptyGo.GetComponent<RectTransform>();
                    emptyRect.anchorMin = Vector2.zero;
                    emptyRect.anchorMax = Vector2.one;
                    emptyRect.offsetMin = Vector2.zero;
                    emptyRect.offsetMax = Vector2.zero;
                    var empty = emptyGo.GetComponent<Image>();
                    empty.color = new Color(0.7f, 0.7f, 0.7f, 0.5f);

                    var slot = slotGo.GetComponent<EquipmentSlotUI>();
                    if (slot == null)
                        slot = Undo.AddComponent<EquipmentSlotUI>(slotGo);

                    var so = new SerializedObject(slot);
                    so.FindProperty("slotIndex").intValue = i;
                    so.FindProperty("icon").objectReferenceValue = icon;
                    so.FindProperty("emptyFrame").objectReferenceValue = empty;
                    so.FindProperty("button").objectReferenceValue = slotGo.GetComponent<Button>();
                    so.ApplyModifiedPropertiesWithoutUndo();
                    slots.Add(slot);
                }
                else
                {
                    slotGo = slotTf.gameObject;
                    var slot = slotGo.GetComponent<EquipmentSlotUI>();
                    if (slot == null)
                        slot = Undo.AddComponent<EquipmentSlotUI>(slotGo);
                    slots.Add(slot);
                }
            }

            return slots.ToArray();
        }

        static Button EnsureButton(Transform parent, string name, string label, Vector2 anchoredPos, bool anchorTopRight = false)
        {
            var existing = parent.Find(name);
            GameObject go;

            if (existing != null)
            {
                go = existing.gameObject;
            }
            else
            {
                go = new GameObject(name, typeof(RectTransform), typeof(Image), typeof(Button));
                Undo.RegisterCreatedObjectUndo(go, "Create Button");
                go.transform.SetParent(parent, false);

                var rect = go.GetComponent<RectTransform>();
                if (anchorTopRight)
                {
                    rect.anchorMin = new Vector2(1f, 1f);
                    rect.anchorMax = new Vector2(1f, 1f);
                    rect.pivot = new Vector2(1f, 1f);
                }
                else
                {
                    rect.anchorMin = new Vector2(0f, 0f);
                    rect.anchorMax = new Vector2(0f, 0f);
                    rect.pivot = new Vector2(0f, 0f);
                }

                rect.anchoredPosition = anchoredPos;
                rect.sizeDelta = new Vector2(72f, 32f);
                go.GetComponent<Image>().color = new Color(0.25f, 0.45f, 0.75f, 1f);

                var textGo = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI));
                textGo.transform.SetParent(go.transform, false);
                var textRect = textGo.GetComponent<RectTransform>();
                textRect.anchorMin = Vector2.zero;
                textRect.anchorMax = Vector2.one;
                textRect.offsetMin = Vector2.zero;
                textRect.offsetMax = Vector2.zero;
                var tmp = textGo.GetComponent<TextMeshProUGUI>();
                tmp.text = label;
                tmp.alignment = TextAlignmentOptions.Center;
                tmp.fontSize = 16;
                tmp.color = Color.white;
            }

            return go.GetComponent<Button>();
        }

        static TestCharacter EnsureTestCharacter()
        {
            var existing = Object.FindObjectOfType<TestCharacter>();
            if (existing != null)
                return existing;

            var go = new GameObject("TestCharacter");
            Undo.RegisterCreatedObjectUndo(go, "Create TestCharacter");
            var character = go.AddComponent<TestCharacter>();

            var so = new SerializedObject(character);
            var entries = so.FindProperty("stats").FindPropertyRelative("entries");
            entries.arraySize = AllStats.Length;
            for (var i = 0; i < AllStats.Length; i++)
            {
                var elem = entries.GetArrayElementAtIndex(i);
                elem.FindPropertyRelative("stat").enumValueIndex = (int)AllStats[i];
                elem.FindPropertyRelative("baseValue").floatValue = DefaultStatValues[i];
            }

            so.ApplyModifiedPropertiesWithoutUndo();
            return character;
        }

        static EquipmentService EnsureEquipmentService(Inventory inventory, ItemDatabase database)
        {
            var existing = Object.FindObjectOfType<EquipmentService>();
            if (existing != null)
            {
                var so = new SerializedObject(existing);
                so.FindProperty("inventory").objectReferenceValue = inventory;
                so.FindProperty("database").objectReferenceValue = database;
                so.ApplyModifiedPropertiesWithoutUndo();
                return existing;
            }

            var serviceRoot = GameObject.Find("Service");
            var go = serviceRoot != null
                ? new GameObject("EquipmentService")
                : new GameObject("EquipmentService");

            if (serviceRoot != null)
                go.transform.SetParent(serviceRoot.transform, false);

            Undo.RegisterCreatedObjectUndo(go, "Create EquipmentService");
            var service = go.AddComponent<EquipmentService>();
            var serviceSo = new SerializedObject(service);
            serviceSo.FindProperty("inventory").objectReferenceValue = inventory;
            serviceSo.FindProperty("database").objectReferenceValue = database;
            serviceSo.FindProperty("runeSlotCount").intValue = 3;
            serviceSo.ApplyModifiedPropertiesWithoutUndo();
            return service;
        }

        static void SetupShopSellTab(ShopUI shopUI, ShopService shopService, WalletService wallet, Inventory inventory, SellItemUI sellPrefab)
        {
            Undo.RegisterFullObjectHierarchyUndo(shopUI.gameObject, "Setup Shop Sell Tab");

            var buyTab = EnsureButton(shopUI.transform, "BuyTabButton", "Buy", new Vector2(12f, -48f));
            var sellTab = EnsureButton(shopUI.transform, "SellTabButton", "Sell", new Vector2(92f, -48f));

            var shopUiSo = new SerializedObject(shopUI);
            shopUiSo.FindProperty("shopService").objectReferenceValue = shopService;
            shopUiSo.FindProperty("wallet").objectReferenceValue = wallet;
            shopUiSo.FindProperty("inventory").objectReferenceValue = inventory;
            shopUiSo.FindProperty("sellItemPrefab").objectReferenceValue = sellPrefab;
            shopUiSo.FindProperty("buyTabButton").objectReferenceValue = buyTab;
            shopUiSo.FindProperty("sellTabButton").objectReferenceValue = sellTab;

            var content = shopUI.transform.Find("ShopItemView/Content")
                          ?? shopUI.transform.Find("ShopItemView/Viewport/Content");
            if (content != null)
                shopUiSo.FindProperty("content").objectReferenceValue = content;

            var ink = shopUI.transform.Find("InkHeader")?.GetComponent<TextMeshProUGUI>();
            if (ink != null)
                shopUiSo.FindProperty("inkText").objectReferenceValue = ink;

            shopUiSo.ApplyModifiedPropertiesWithoutUndo();
        }

        static void WireTestController(
            ItemTestController testController,
            ShopUI shopUI,
            ShopService shopService,
            InventoryUI inventoryUI,
            WalletService wallet,
            TestCharacter testCharacter)
        {
            if (testController == null) return;

            var so = new SerializedObject(testController);
            so.FindProperty("wallet").objectReferenceValue = wallet;
            so.FindProperty("testCharacter").objectReferenceValue = testCharacter;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        static void EnsureStoreSaveService(
            Inventory inventory,
            WalletService wallet,
            EquipmentService equipment,
            ShopService shop,
            TestCharacter testCharacter,
            ItemTestController testController)
        {
            var existing = Object.FindObjectOfType<StoreSaveService>();
            if (existing == null)
            {
                var serviceRoot = GameObject.Find("Service");
                var go = new GameObject("StoreSaveService");
                if (serviceRoot != null)
                    go.transform.SetParent(serviceRoot.transform, false);
                Undo.RegisterCreatedObjectUndo(go, "Create StoreSaveService");
                existing = go.AddComponent<StoreSaveService>();
            }

            var saveSo = new SerializedObject(existing);
            saveSo.FindProperty("inventory").objectReferenceValue = inventory;
            saveSo.FindProperty("wallet").objectReferenceValue = wallet;
            saveSo.FindProperty("equipment").objectReferenceValue = equipment;
            saveSo.FindProperty("shop").objectReferenceValue = shop;
            saveSo.FindProperty("testCharacter").objectReferenceValue = testCharacter;
            saveSo.ApplyModifiedPropertiesWithoutUndo();

            if (testController == null) return;

            var testRoot = testController.transform;
            var externalBtn = EnsureButton(testRoot, "ExternalQueryButton", "ExtQuery", new Vector2(12f, -80f));
            var saveBtn = EnsureButton(testRoot, "SaveRoundTripButton", "SaveTest", new Vector2(100f, -80f));

            var testSo = new SerializedObject(testController);
            testSo.FindProperty("inventory").objectReferenceValue = inventory;
            testSo.FindProperty("equipmentService").objectReferenceValue = equipment;
            testSo.FindProperty("storeSaveService").objectReferenceValue = existing;
            testSo.FindProperty("externalQueryButton").objectReferenceValue = externalBtn;
            testSo.FindProperty("saveRoundTripButton").objectReferenceValue = saveBtn;
            testSo.ApplyModifiedPropertiesWithoutUndo();
        }

        static void EnsureInventoryItemPrefab()
        {
            var path = InventoryItemPrefabPath;
            var go = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            if (go == null) return;

            var shopItemUi = go.GetComponent<ShopItemUI>();
            if (shopItemUi != null)
                Object.DestroyImmediate(shopItemUi, true);

            var invUi = go.GetComponent<InventoryItemUI>();
            if (invUi == null)
                invUi = go.AddComponent<InventoryItemUI>();

            var icon = go.GetComponent<Image>();
            var countTmp = go.transform.Find("ItemCount")?.GetComponent<TextMeshProUGUI>();

            var so = new SerializedObject(invUi);
            so.FindProperty("iconImage").objectReferenceValue = icon;
            so.FindProperty("countText").objectReferenceValue = countTmp;
            so.FindProperty("backgroundImage").objectReferenceValue = icon;
            so.ApplyModifiedPropertiesWithoutUndo();
            EditorUtility.SetDirty(go);
            AssetDatabase.SaveAssets();
        }

        static SellItemUI EnsureSellItemPrefab()
        {
            var existing = AssetDatabase.LoadAssetAtPath<SellItemUI>(SellItemPrefabPath);
            if (existing != null)
                return existing;

            var template = AssetDatabase.LoadAssetAtPath<GameObject>(InventoryItemPrefabPath);
            if (template == null)
            {
                Debug.LogWarning("[InventoryLoopSetup] InventoryItem prefab missing; SellItem not created.");
                return null;
            }

            var instance = (GameObject)PrefabUtility.InstantiatePrefab(template);
            instance.name = "SellItem";

            var invUi = instance.GetComponent<InventoryItemUI>();
            if (invUi != null)
                Object.DestroyImmediate(invUi, true);

            var sellUi = instance.AddComponent<SellItemUI>();
            var icon = instance.GetComponent<Image>();
            var countTmp = instance.transform.Find("ItemCount")?.GetComponent<TextMeshProUGUI>();

            var priceGo = new GameObject("SellPrice", typeof(RectTransform), typeof(TextMeshProUGUI));
            priceGo.transform.SetParent(instance.transform, false);
            var priceRect = priceGo.GetComponent<RectTransform>();
            priceRect.anchorMin = new Vector2(0.5f, 0f);
            priceRect.anchorMax = new Vector2(0.5f, 0f);
            priceRect.pivot = new Vector2(0.5f, 0f);
            priceRect.anchoredPosition = new Vector2(0f, -36f);
            priceRect.sizeDelta = new Vector2(80f, 20f);
            var priceTmp = priceGo.GetComponent<TextMeshProUGUI>();
            priceTmp.fontSize = 14;
            priceTmp.alignment = TextAlignmentOptions.Center;
            priceTmp.color = Color.black;

            var sellBtn = instance.GetComponent<Button>();
            if (sellBtn == null)
                sellBtn = instance.AddComponent<Button>();

            var so = new SerializedObject(sellUi);
            so.FindProperty("iconImage").objectReferenceValue = icon;
            so.FindProperty("countText").objectReferenceValue = countTmp;
            so.FindProperty("priceText").objectReferenceValue = priceTmp;
            so.FindProperty("sellButton").objectReferenceValue = sellBtn;
            so.ApplyModifiedPropertiesWithoutUndo();

            PrefabUtility.SaveAsPrefabAsset(instance, SellItemPrefabPath);
            Object.DestroyImmediate(instance);
            AssetDatabase.SaveAssets();
            return AssetDatabase.LoadAssetAtPath<SellItemUI>(SellItemPrefabPath);
        }
    }
}
#endif
