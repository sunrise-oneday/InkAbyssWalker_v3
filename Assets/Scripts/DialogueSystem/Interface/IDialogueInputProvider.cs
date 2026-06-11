using System;

namespace DialogueSystem
{
    /// <summary>
    /// 对话输入提供者接口
    /// 解耦：不再直接依赖 InputManager/GameplayInputReader/InputContextSwitcher
    /// 每个项目自行实现此接口，桥接自己的输入系统
    /// </summary>
    public interface IDialogueInputProvider
    {
        /// <summary>当玩家确认/推进对话时触发</summary>
        event Action OnConfirmPressed;

        /// <summary>当玩家选择选项时触发（参数：选项索引）</summary>
        event Action<int> OnOptionSelected;

        /// <summary>暂停游戏输入（如暂停探索移动）</summary>
        void PauseGameInput();

        /// <summary>恢复游戏输入</summary>
        void RestoreGameInput();

        /// <summary>启用/禁用对话输入监听</summary>
        void SetDialogueInputActive(bool active);
    }
}
