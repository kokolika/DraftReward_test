// ItemDefinitionSO.cs
// ScriptableObject representing a single item definition.
// Can be created via Assets > Create > RewardDraft > Item Definition
// OR populated at runtime from JSON via JsonItemLoader.

using UnityEngine;
using RewardDraft.Core;

namespace RewardDraft
{
    [CreateAssetMenu(fileName = "NewItem", menuName = "RewardDraft/Item Definition", order = 1)]
    public class ItemDefinitionSO : ScriptableObject
    {
        [Header("Identity")]
        public string id;
        public string displayName;

        [TextArea(2, 4)]
        public string description;

        [Header("Rarity")]
        public Rarity rarity;

        [Header("Visual (Optional)")]
        public Sprite icon; // not required per test spec

        /// <summary>
        /// Converts this ScriptableObject to the plain ItemData class
        /// so the LootService can work with a unified type.
        /// </summary>
        public ItemData ToItemData()
        {
            return new ItemData
            {
                id          = id,
                displayName = displayName,
                description = description,
                rarity      = rarity.ToString(),
                rarityEnum  = rarity
            };
        }

        /// <summary>
        /// Populates this ScriptableObject from a plain ItemData instance.
        /// Used when loading from JSON at runtime.
        /// </summary>
        public void FromItemData(ItemData data)
        {
            id          = data.id;
            displayName = data.displayName;
            description = data.description;
            rarity      = data.rarityEnum;
        }
    }
}
