using UnityEngine;

namespace StoreAndInventory
{
    /// <summary>
    /// 解析场景中的 <see cref="MainCharacterStatSource"/>（优先 Inspector 引用，否则 Find）。
    /// </summary>
    public static class CharacterStatSourceLocator
    {
        const string DefaultPlayerTag = "Player";

        public static MainCharacterStatSource Resolve(MainCharacterStatSource assigned = null)
        {
            if (assigned != null)
                return assigned;

            var found = Object.FindObjectOfType<MainCharacterStatSource>();
            if (found != null)
                return found;

            var playerGo = GameObject.FindGameObjectWithTag(DefaultPlayerTag);
            if (playerGo == null)
                return null;

            return playerGo.GetComponent<MainCharacterStatSource>()
                   ?? playerGo.GetComponentInChildren<MainCharacterStatSource>(true);
        }

        public static ICharacterStatSource ResolveInterface(MainCharacterStatSource assigned = null)
        {
            return Resolve(assigned);
        }
    }
}
