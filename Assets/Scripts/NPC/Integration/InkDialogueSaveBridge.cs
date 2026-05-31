using UnityEngine;
using DialogueSystem;

/// <summary>
/// 对话存档桥：将对话状态嵌入 PlayerPrefs（简化版本）
/// 可以扩展为使用 StoreSaveService 的 SaveBundle 模式
/// </summary>
public class InkDialogueSaveBridge : MonoBehaviour, IDialogueSaveProvider
{
    private const string Prefix = "dialogue.";

    public void SaveDialogueState(string dialogueId, string variablesJson, string historyJson)
    {
        if (string.IsNullOrEmpty(dialogueId))
        {
            Debug.LogWarning("[InkDialogueSaveBridge] dialogueId为空");
            return;
        }

        if (!string.IsNullOrEmpty(variablesJson))
        {
            PlayerPrefs.SetString($"{Prefix}{dialogueId}.vars", variablesJson);
        }

        if (!string.IsNullOrEmpty(historyJson))
        {
            PlayerPrefs.SetString($"{Prefix}{dialogueId}.hist", historyJson);
        }

        PlayerPrefs.Save();
    }

    public (string variablesJson, string historyJson) LoadDialogueState(string dialogueId)
    {
        if (string.IsNullOrEmpty(dialogueId))
        {
            Debug.LogWarning("[InkDialogueSaveBridge] dialogueId为空");
            return (null, null);
        }

        string vars = PlayerPrefs.GetString($"{Prefix}{dialogueId}.vars", null);
        string hist = PlayerPrefs.GetString($"{Prefix}{dialogueId}.hist", null);
        return (vars, hist);
    }
}
