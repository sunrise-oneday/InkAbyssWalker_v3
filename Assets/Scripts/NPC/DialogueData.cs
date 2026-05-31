using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// NPC 对话数据（ScriptableObject）
/// 每个 NPC 持有一个 DialogueData 资产，包含完整的对话序列。
/// 右键 Assets > Create > InkAbyss > NPC Dialogue Data 创建。
/// </summary>
[CreateAssetMenu(fileName = "NPCDialogue", menuName = "InkAbyss/NPC Dialogue Data")]
public class DialogueData : ScriptableObject
{
    [System.Serializable]
    public class DialogueLine
    {
        [Header("对话内容")]
        [SerializeField] private string speakerName = "NPC";
        [TextArea(2, 6)]
        [SerializeField] private string text = "";

        [Header("显示设置")]
        [Tooltip("文字逐字显示的速度（秒/字符），0 使用全局默认值")]
        [SerializeField] private float typewriterSpeed = 0f;

        [Header("可选事件")]
        [Tooltip("此行显示时触发的事件")]
        [SerializeField] private UnityEngine.Events.UnityEvent onLineShow;

        public string SpeakerName => speakerName;
        public string Text => text;
        public float TypewriterSpeed => typewriterSpeed;
        public UnityEngine.Events.UnityEvent OnLineShow => onLineShow;
    }

    [Header("对话序列")]
    [SerializeField] private List<DialogueLine> lines = new List<DialogueLine>();

    [Header("全局设置")]
    [Tooltip("默认逐字显示速度（秒/字符），DialogueLine 中设 0 时使用此值")]
    [SerializeField] private float defaultTypewriterSpeed = 0.03f;

    public List<DialogueLine> Lines => lines;
    public float DefaultTypewriterSpeed => defaultTypewriterSpeed;

    /// <summary>获取指定行的有效打字速度</summary>
    public float GetLineSpeed(int index)
    {
        if (index < 0 || index >= lines.Count) return defaultTypewriterSpeed;
        float lineSpeed = lines[index].TypewriterSpeed;
        return lineSpeed > 0f ? lineSpeed : defaultTypewriterSpeed;
    }
}
