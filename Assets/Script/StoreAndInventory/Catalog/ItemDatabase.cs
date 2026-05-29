using System.Collections.Generic;
using UnityEngine;

namespace StoreAndInventory
{
    public class ItemDatabase : MonoBehaviour
    {
        [Header("启动时按 id 建立索引；重复 / 空 id 会打 LogError")]
        [SerializeField] List<ItemBase> definitions = new();

        Dictionary<string, ItemBase> _byId;

        void Awake()
        {
            BuildIndex();
        }

        void BuildIndex()
        {
            _byId = new Dictionary<string, ItemBase>(definitions.Count);
            foreach (var def in definitions)
            {
                if (def == null) continue;

                if (string.IsNullOrEmpty(def.id))
                {
                    Debug.LogError($"[ItemDatabase] 空 id: {def.name}");
                    continue;
                }

                if (_byId.ContainsKey(def.id))
                {
                    Debug.LogError($"[ItemDatabase] 重复 id: {def.id} (已有: {_byId[def.id].name}, 新增: {def.name})");
                    continue;
                }

                _byId[def.id] = def;
            }

            StoreInventoryLog.Info($"[ItemDatabase] Built index: {_byId.Count} items.");
        }

        public bool TryGet(string id, out ItemBase item)
        {
            if (_byId == null || string.IsNullOrEmpty(id))
            {
                item = null;
                return false;
            }
            return _byId.TryGetValue(id, out item);
        }

        public int Count => _byId?.Count ?? 0;
        public IEnumerable<ItemBase> All => _byId?.Values;
    }
}
