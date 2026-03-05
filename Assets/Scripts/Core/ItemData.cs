// ItemData.cs
// Plain C# classes used for JSON deserialization from StreamingAssets/Items.json.
// These are NOT ScriptableObjects — they are simple data containers.
// The LootService converts these into runtime ItemDefinition objects.

using System;
using System.Collections.Generic;

namespace RewardDraft.Core
{
    /// <summary>
    /// Rarity tiers for all items in the system.
    /// Weights are configured in LootSettingsSO.
    /// </summary>
    public enum Rarity
    {
        Common,
        Rare,
        Epic,
        Legendary
    }

    /// <summary>
    /// Plain C# class representing one item.
    /// Used both for JSON deserialization and as the runtime item representation.
    /// </summary>
    [Serializable]
    public class ItemData
    {
        public string id;
        public string displayName;
        public string description;
        public string rarity; // string in JSON, converted to Rarity enum at load time

        // Cached enum value — populated after JSON load
        [NonSerialized]
        public Rarity rarityEnum;

        /// <summary>Parses the rarity string into the enum. Call after JSON deserialization.</summary>
        public void InitializeRarity()
        {
            if (Enum.TryParse(rarity, ignoreCase: true, out Rarity parsed))
                rarityEnum = parsed;
            else
            {
                UnityEngine.Debug.LogWarning($"[ItemData] Unknown rarity '{rarity}' on item '{id}'. Defaulting to Common.");
                rarityEnum = Rarity.Common;
            }
        }
    }

    /// <summary>
    /// Root wrapper for Items.json deserialization.
    /// JSON format: { "items": [ {...}, {...} ] }
    /// </summary>
    [Serializable]
    public class ItemDatabase
    {
        public List<ItemData> items = new List<ItemData>();
    }
}
