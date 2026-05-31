#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// 预制体修复工具
/// </summary>
public class PrefabFixer
{
    [MenuItem("Tools/Fix Battle Prefabs")]
    public static void FixBattlePrefabs()
    {
        FixBattleEnemyPrefab();
        FixPlayerHUDPrefab();
        AssetDatabase.Refresh();
        Debug.Log("[PrefabFixer] 预制体修复完成！");
    }

    private static void FixBattleEnemyPrefab()
    {
        string prefabPath = "Assets/Resources/Prefab/BattleEnemy.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning("[PrefabFixer] 找不到 BattleEnemy.prefab");
            return;
        }

        // 加载预制体进行编辑
        GameObject instance = PrefabUtility.LoadPrefabContents(prefabPath);

        // 查找或创建 EntityHUD
        EntityHUD hud = instance.GetComponentInChildren<EntityHUD>();
        if (hud == null)
        {
            Debug.Log("[PrefabFixer] BattleEnemy 缺少 EntityHUD 组件");
            PrefabUtility.UnloadPrefabContents(instance);
            return;
        }

        // 使用 SerializedObject 修改 prefab 中的值
        SerializedObject serializedHUD = new SerializedObject(hud);

        // 查找意图容器
        Transform intentContainer = instance.transform.Find("IntentContainer");
        if (intentContainer == null)
        {
            // 创建意图容器
            GameObject containerObj = new GameObject("IntentContainer");
            containerObj.transform.SetParent(hud.transform);
            containerObj.AddComponent<RectTransform>();
            intentContainer = containerObj.transform;
            Debug.Log("[PrefabFixer] 创建了 IntentContainer");
        }

        // 查找意图图标
        Transform intentIcon = intentContainer.Find("IntentIcon");
        if (intentIcon == null)
        {
            GameObject iconObj = new GameObject("IntentIcon");
            iconObj.transform.SetParent(intentContainer);
            RectTransform rt = iconObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(40, 40);
            UnityEngine.UI.Image image = iconObj.AddComponent<UnityEngine.UI.Image>();
            image.color = Color.white;
            intentIcon = iconObj.transform;
            Debug.Log("[PrefabFixer] 创建了 IntentIcon");
        }

        // 设置序列化属性
        SerializedProperty intentContainerProp = serializedHUD.FindProperty("intentContainer");
        if (intentContainerProp != null)
        {
            intentContainerProp.objectReferenceValue = intentContainer.gameObject;
        }

        SerializedProperty intentIconProp = serializedHUD.FindProperty("intentIcon");
        if (intentIconProp != null)
        {
            intentIconProp.objectReferenceValue = intentIcon.GetComponent<UnityEngine.UI.Image>();
        }

        // 查找意图数值文本
        Transform intentValueText = intentContainer.Find("IntentValueText");
        if (intentValueText == null)
        {
            GameObject textObj = new GameObject("IntentValueText");
            textObj.transform.SetParent(intentIcon);
            RectTransform rt = textObj.AddComponent<RectTransform>();
            rt.sizeDelta = new Vector2(40, 20);
            rt.anchoredPosition = new Vector2(0, -25);
            TMPro.TextMeshProUGUI tmp = textObj.AddComponent<TMPro.TextMeshProUGUI>();
            tmp.text = "";
            tmp.fontSize = 14;
            tmp.alignment = TMPro.TextAlignmentOptions.Center;
            intentValueText = textObj.transform;
            Debug.Log("[PrefabFixer] 创建了 IntentValueText");
        }

        SerializedProperty intentValueTextProp = serializedHUD.FindProperty("intentValueText");
        if (intentValueTextProp != null)
        {
            intentValueTextProp.objectReferenceValue = intentValueText.GetComponent<TMPro.TextMeshProUGUI>();
        }

        // 添加 IntentTooltipTrigger 到意图图标
        if (intentIcon.GetComponent<Battle.UI.IntentTooltipTrigger>() == null)
        {
            intentIcon.gameObject.AddComponent<Battle.UI.IntentTooltipTrigger>();
            Debug.Log("[PrefabFixer] 添加了 IntentTooltipTrigger");
        }

        // 确保 Image 可以接收鼠标事件
        UnityEngine.UI.Image iconImage = intentIcon.GetComponent<UnityEngine.UI.Image>();
        if (iconImage != null)
        {
            iconImage.raycastTarget = true;
        }

        serializedHUD.ApplyModifiedProperties();

        // 保存预制体
        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        PrefabUtility.UnloadPrefabContents(instance);
        Debug.Log("[PrefabFixer] BattleEnemy.prefab 修复完成");
    }

    private static void FixPlayerHUDPrefab()
    {
        string prefabPath = "Assets/Resources/Prefab/UI/Player HUD HP.prefab";
        GameObject prefab = AssetDatabase.LoadAssetAtPath<GameObject>(prefabPath);
        if (prefab == null)
        {
            Debug.LogWarning("[PrefabFixer] 找不到 Player HUD HP.prefab");
            return;
        }

        GameObject instance = PrefabUtility.LoadPrefabContents(prefabPath);

        EntityHUD hud = instance.GetComponent<EntityHUD>();
        if (hud == null)
        {
            Debug.LogWarning("[PrefabFixer] Player HUD HP 缺少 EntityHUD 组件");
            PrefabUtility.UnloadPrefabContents(instance);
            return;
        }

        // 玩家不需要意图显示，但需要 buffContainer
        SerializedObject serializedHUD = new SerializedObject(hud);

        // 检查 buffContainer 是否配置
        SerializedProperty buffContainerProp = serializedHUD.FindProperty("buffContainer");
        if (buffContainerProp != null && buffContainerProp.objectReferenceValue == null)
        {
            Transform buffContainer = instance.transform.Find("buffContainer");
            if (buffContainer != null)
            {
                buffContainerProp.objectReferenceValue = buffContainer;
                Debug.Log("[PrefabFixer] 配置了 buffContainer");
            }
        }

        serializedHUD.ApplyModifiedProperties();

        PrefabUtility.SaveAsPrefabAsset(instance, prefabPath);
        PrefabUtility.UnloadPrefabContents(instance);
        Debug.Log("[PrefabFixer] Player HUD HP.prefab 修复完成");
    }
}
#endif
