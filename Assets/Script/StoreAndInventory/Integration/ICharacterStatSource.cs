namespace StoreAndInventory
{
    /// <summary>
    /// 背包/UI 只读基础属性（不含装备 statMods 加成）。
    /// </summary>
    public interface ICharacterStatSource
    {
        bool IsBound { get; }

        float Get(StatType stat);
    }
}
