using System.Collections.Generic;
using UnityEngine;

namespace StoreAndInventory
{
    /// <summary>
    /// 背包/装备/货币/角色/商店限购的存档块 Capture 与 Apply（内存 JSON）。
    /// 主工程对接：CaptureAllJson() 写入主存档节点；读档后 ApplyAllJson()。
    /// 示例见 Obsidian：商店背包执行/整合与优化/02_接入主工程清单 §4。
    /// </summary>
    public class StoreSaveService : MonoBehaviour
    {
        public const int BundleVersion = 1;

        [SerializeField] Inventory inventory;
        [SerializeField] WalletService wallet;
        [SerializeField] EquipmentService equipment;
        [SerializeField] ShopService shop;
        [SerializeField] TestCharacter testCharacter;

        void Awake()
        {
            if (inventory == null) inventory = FindObjectOfType<Inventory>();
            if (wallet == null) wallet = FindObjectOfType<WalletService>();
            if (equipment == null) equipment = FindObjectOfType<EquipmentService>();
            if (shop == null) shop = FindObjectOfType<ShopService>();
            if (testCharacter == null) testCharacter = FindObjectOfType<TestCharacter>();
        }

        public SaveBundle CaptureAll()
        {
            var bundle = new SaveBundle { bundleVersion = BundleVersion };

            if (inventory != null)
            {
                bundle.chunks.Add(new SaveChunk
                {
                    key = SaveKeys.Inventory,
                    version = SaveKeys.InventoryVersion,
                    json = JsonUtility.ToJson(inventory.Data)
                });
            }

            if (equipment != null)
            {
                bundle.chunks.Add(new SaveChunk
                {
                    key = SaveKeys.Equipment,
                    version = SaveKeys.EquipmentVersion,
                    json = JsonUtility.ToJson(equipment.CaptureSaveData())
                });
            }

            if (wallet != null)
            {
                bundle.chunks.Add(new SaveChunk
                {
                    key = SaveKeys.Wallet,
                    version = SaveKeys.WalletVersion,
                    json = JsonUtility.ToJson(wallet.Data)
                });
            }

            if (testCharacter != null)
            {
                bundle.chunks.Add(new SaveChunk
                {
                    key = SaveKeys.Character,
                    version = SaveKeys.CharacterVersion,
                    json = JsonUtility.ToJson(testCharacter.Data)
                });
            }

            if (shop != null)
            {
                var archive = shop.ExportRuntimeArchive();
                if (archive?.shops != null)
                {
                    for (var i = 0; i < archive.shops.Count; i++)
                    {
                        var runtime = archive.shops[i];
                        if (runtime == null || string.IsNullOrEmpty(runtime.shopId)) continue;

                        bundle.chunks.Add(new SaveChunk
                        {
                            key = SaveKeys.Shop(runtime.shopId),
                            version = SaveKeys.ShopRuntimeVersion,
                            json = JsonUtility.ToJson(runtime)
                        });
                    }
                }
            }

            return bundle;
        }

        public bool ApplyAll(SaveBundle bundle, out string error)
        {
            error = null;
            if (bundle == null)
            {
                error = "bundle is null";
                return false;
            }

            if (bundle.chunks == null)
            {
                error = "bundle.chunks is null";
                return false;
            }

            ShopRuntimeArchive shopArchive = null;

            for (var i = 0; i < bundle.chunks.Count; i++)
            {
                var chunk = bundle.chunks[i];
                if (chunk == null || string.IsNullOrEmpty(chunk.key))
                    continue;

                if (chunk.key == SaveKeys.Inventory)
                {
                    if (inventory == null) continue;
                    var data = JsonUtility.FromJson<InventoryData>(chunk.json);
                    inventory.LoadFromData(data);
                    continue;
                }

                if (chunk.key == SaveKeys.Equipment)
                {
                    if (equipment == null) continue;
                    var data = JsonUtility.FromJson<EquipmentSaveData>(chunk.json);
                    equipment.ApplySaveData(data);
                    continue;
                }

                if (chunk.key == SaveKeys.Wallet)
                {
                    if (wallet == null) continue;
                    var data = JsonUtility.FromJson<CurrencyWallet>(chunk.json);
                    wallet.LoadFromData(data);
                    continue;
                }

                if (chunk.key == SaveKeys.Character)
                {
                    if (testCharacter == null) continue;
                    var data = JsonUtility.FromJson<CharacterStatsData>(chunk.json);
                    testCharacter.LoadFromData(data);
                    continue;
                }

                if (chunk.key.StartsWith("shop.") && chunk.key.EndsWith(".v1"))
                {
                    shopArchive ??= new ShopRuntimeArchive();
                    var runtime = JsonUtility.FromJson<ShopRuntimeData>(chunk.json);
                    if (runtime != null)
                        shopArchive.shops.Add(runtime);
                }
            }

            if (shop != null && shopArchive != null)
                shop.ImportRuntimeArchive(shopArchive);

            return true;
        }

        public string CaptureAllJson()
        {
            return JsonUtility.ToJson(CaptureAll());
        }

        public bool ApplyAllJson(string json, out string error)
        {
            error = null;
            if (string.IsNullOrEmpty(json))
            {
                error = "json empty";
                return false;
            }

            var bundle = JsonUtility.FromJson<SaveBundle>(json);
            return ApplyAll(bundle, out error);
        }

        public bool RoundTripSelfTest(out string error)
        {
            error = null;

            var before = CaptureAll();
            if (before?.chunks == null || before.chunks.Count == 0)
            {
                error = "capture produced no chunks";
                return false;
            }

            if (!ApplyAll(before, out error))
                return false;

            var after = CaptureAll();
            if (after?.chunks == null)
            {
                error = "second capture failed";
                return false;
            }

            if (before.chunks.Count != after.chunks.Count)
            {
                error = $"chunk count {before.chunks.Count} vs {after.chunks.Count}";
                return false;
            }

            for (var i = 0; i < before.chunks.Count; i++)
            {
                var a = before.chunks[i];
                var b = FindChunk(after, a.key);
                if (b == null)
                {
                    error = $"missing chunk {a.key}";
                    return false;
                }

                if (a.json != b.json)
                {
                    error = $"json mismatch for {a.key}";
                    return false;
                }
            }

            return true;
        }

        static SaveChunk FindChunk(SaveBundle bundle, string key)
        {
            for (var i = 0; i < bundle.chunks.Count; i++)
            {
                var c = bundle.chunks[i];
                if (c != null && c.key == key)
                    return c;
            }

            return null;
        }

        public void LogExternalQuerySnapshot()
        {
            if (equipment != null)
            {
                var statMods = equipment.GetAllStatMods();
                Debug.Log($"[StoreSave] GetAllStatMods count={statMods.Count}");
                for (var i = 0; i < statMods.Count; i++)
                {
                    var m = statMods[i];
                    Debug.Log($"[StoreSave]   stat={m.stat} flat={m.flat} pct={m.percent}");
                }

                var skillMods = equipment.GetAllSkillMods();
                Debug.Log($"[StoreSave] GetAllSkillMods count={skillMods.Count}");

                var effects = equipment.GetAllExtraEffects();
                Debug.Log($"[StoreSave] GetAllExtraEffects count={effects.Count}");
                for (var i = 0; i < effects.Count; i++)
                    Debug.Log($"[StoreSave]   fx={(effects[i] != null ? effects[i].effectTag : "null")}");
            }

            if (inventory != null)
            {
                var battle = inventory.GetConsumables(UseContext.Battle);
                var any = inventory.GetConsumables(null);
                Debug.Log($"[StoreSave] GetConsumables battle={battle.Count} all={any.Count}");
            }
        }
    }
}
