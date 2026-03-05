// ItemDatabaseSO.cs
// ScriptableObject holding the full list of available item definitions.
// This is the in-editor authoring path. The JSON import path populates
// the LootService directly, bypassing this SO at runtime.

using System.Collections.Generic;
using UnityEngine;

namespace RewardDraft
{
    [CreateAssetMenu(fileName = "ItemDatabase", menuName = "RewardDraft/Item Database", order = 2)]
    public class ItemDatabaseSO : ScriptableObject
    {
        [Header("All available items")]
        public List<ItemDefinitionSO> items = new List<ItemDefinitionSO>();

        /// <summary>
        /// Returns all items of a given rarity.
        /// </summary>
        public List<ItemDefinitionSO> GetByRarity(Core.Rarity rarity)
        {
            var result = new List<ItemDefinitionSO>();
            foreach (var item in items)
                if (item.rarity == rarity)
                    result.Add(item);
            return result;
        }

        /// <summary>Validates the database and logs warnings for any issues.</summary>
        public bool Validate(out string error)
        {
            if (items == null || items.Count == 0)
            {
                error = "ItemDatabaseSO is empty.";
                return false;
            }

            var ids = new HashSet<string>();
            foreach (var item in items)
            {
                if (item == null)       { error = "Null entry in item database."; return false; }
                if (string.IsNullOrEmpty(item.id)) { error = $"Item '{item.name}' has no id."; return false; }
                if (!ids.Add(item.id))  { error = $"Duplicate item id: '{item.id}'."; return false; }
            }

            error = null;
            return true;
        }
    }
}
