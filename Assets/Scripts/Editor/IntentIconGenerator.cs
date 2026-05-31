#if UNITY_EDITOR
using UnityEngine;
using UnityEditor;

/// <summary>
/// 意图图标生成器
/// 在 Editor 中生成默认的意图图标
/// </summary>
public class IntentIconGenerator
{
    [MenuItem("Tools/Generate Intent Icons")]
    public static void GenerateIntentIcons()
    {
        // 创建文件夹
        if (!AssetDatabase.IsValidFolder("Assets/Resources/IntentIcons"))
        {
            AssetDatabase.CreateFolder("Assets/Resources", "IntentIcons");
        }

        // 生成各种意图图标
        CreateIntentIcon("Intent_Attack", Color.red);
        CreateIntentIcon("Intent_MultiAttack", new Color(1f, 0.5f, 0f));
        CreateIntentIcon("Intent_SpecialAttack", new Color(0.8f, 0f, 0f));
        CreateIntentIcon("Intent_Block", Color.blue);
        CreateIntentIcon("Intent_Heal", Color.green);
        CreateIntentIcon("Intent_Buff", Color.yellow);
        CreateIntentIcon("Intent_Strengthen", new Color(1f, 0.8f, 0f));
        CreateIntentIcon("Intent_Debuff", Color.magenta);
        CreateIntentIcon("Intent_Summon", new Color(0.5f, 0f, 1f));
        CreateIntentIcon("Intent_Unknown", Color.gray);

        AssetDatabase.Refresh();
        Debug.Log("[IntentIconGenerator] 意图图标生成完成！请在 Unity 中点击 Tools > Fix Battle Prefabs 修复预制体");
    }

    private static void CreateIntentIcon(string name, Color color)
    {
        // 创建 64x64 的纹理
        Texture2D texture = new Texture2D(64, 64, TextureFormat.RGBA32, false);

        // 填充背景色（深色圆形背景）
        Color[] pixels = new Color[64 * 64];
        Color darkColor = new Color(color.r * 0.3f, color.g * 0.3f, color.b * 0.3f, 1f);

        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(32, 32));
                if (dist <= 28)
                {
                    // 圆形背景
                    pixels[y * 64 + x] = darkColor;
                }
                else if (dist <= 30)
                {
                    // 边框
                    pixels[y * 64 + x] = color;
                }
                else
                {
                    // 透明
                    pixels[y * 64 + x] = new Color(0, 0, 0, 0);
                }
            }
        }

        // 在中心绘制亮色圆形
        for (int y = 0; y < 64; y++)
        {
            for (int x = 0; x < 64; x++)
            {
                float dist = Vector2.Distance(new Vector2(x, y), new Vector2(32, 32));
                if (dist <= 15)
                {
                    pixels[y * 64 + x] = color;
                }
            }
        }

        texture.SetPixels(pixels);
        texture.Apply();

        // 保存为 PNG
        byte[] bytes = texture.EncodeToPNG();
        string path = $"Assets/Resources/IntentIcons/{name}.png";
        System.IO.File.WriteAllBytes(path, bytes);
        AssetDatabase.Refresh();

        // 设置为 Sprite
        TextureImporter importer = AssetImporter.GetAtPath(path) as TextureImporter;
        if (importer != null)
        {
            importer.textureType = TextureImporterType.Sprite;
            importer.spriteImportMode = SpriteImportMode.Single;
            importer.spritePixelsPerUnit = 64;
            importer.filterMode = FilterMode.Point;
            importer.SaveAndReimport();
        }
    }
}
#endif
