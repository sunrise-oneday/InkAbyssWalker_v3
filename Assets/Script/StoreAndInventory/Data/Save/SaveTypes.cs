using System;
using System.Collections.Generic;

namespace StoreAndInventory
{
    // Contract for external save systems; chunk export is implemented by StoreSaveService.
    public interface ISaveBlock
    {
        string Key { get; }
        int Version { get; }
        string ToJson();
        bool TryFromJson(string json, out string error);
    }

    [Serializable]
    public class SaveChunk
    {
        public string key;
        public int version;
        public string json;
    }

    [Serializable]
    public class SaveBundle
    {
        public int bundleVersion = 1;
        public List<SaveChunk> chunks = new();
    }

    public static class SaveKeys
    {
        public const int InventoryVersion = 1;
        public const int EquipmentVersion = 1;
        public const int WalletVersion = 1;
        public const int CharacterVersion = 1;
        public const int ShopRuntimeVersion = 1;

        public const string Inventory = "inv.v1";
        public const string Equipment = "equip.v1";
        public const string Wallet = "wallet.v1";
        public const string Character = "character.v1";

        public static string Shop(string shopId) => $"shop.{shopId}.v1";
    }

    [Serializable]
    public class EquipmentSaveData
    {
        public int runeSlotCount = 3;
        public EquipmentData equipment = new();
        public EquippedCacheData cache = new();
    }

    [Serializable]
    public class ShopRuntimeArchive
    {
        public List<ShopRuntimeData> shops = new();
    }
}
