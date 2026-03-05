// LootSettingsSO.cs
// ScriptableObject that holds all configurable parameters for the loot system.
// This is the REQUIRED ScriptableObject per the test specification.
// All values are tunable from the Unity Inspector without touching code.

using UnityEngine;
using System.Collections.Generic;
using RewardDraft.Core;

namespace RewardDraft
{
    [CreateAssetMenu(fileName = "LootSettings", menuName = "RewardDraft/Loot Settings", order = 3)]
    public class LootSettingsSO : ScriptableObject
    {
        [Header("Reward Draft")]
        [Tooltip("Number of distinct items offered each draft phase.")]
        [Range(1, 6)]
        public int numberOfChoices = 3;

        [Header("Rarity Weights")]
        [Tooltip("Relative weight for Common items. Higher = more likely.")]
        [Min(0f)] public float commonWeight    = 60f;

        [Tooltip("Relative weight for Rare items.")]
        [Min(0f)] public float rareWeight      = 25f;

        [Tooltip("Relative weight for Epic items.")]
        [Min(0f)] public float epicWeight      = 10f;

        [Tooltip("Relative weight for Legendary items.")]
        [Min(0f)] public float legendaryWeight =  5f;

        [Header("Pity System (Bonus Feature)")]
        [Tooltip("Enable the pity timer that guarantees Rare+ drops after long dry streaks.")]
        public bool usePitySystem = true;

        [Tooltip("After this many consecutive non-Rare rolls, the pity bonus begins accumulating.")]
        [Min(1)] public int pityThreshold = 8;

        [Tooltip("Additional percentage added to combined Rare+ weight for each roll past the threshold.")]
        [Range(0f, 20f)] public float pityBonusPerRoll = 3f;

        [Tooltip("Maximum total pity bonus applied (caps the bonus to prevent guaranteed drops before hard pity).")]
        [Range(0f, 100f)] public float pityBonusCap = 40f;

        [Header("Duplicate Policy")]
        [Tooltip("Prevent the same item appearing more than once in the same draft offer.")]
        public bool preventDuplicates = true;

        // ── Runtime helpers ───────────────────────────────────────────────────

        /// <summary>
        /// Returns the base weight for a given rarity (ignoring pity).
        /// </summary>
        public float GetBaseWeight(Rarity rarity)
        {
            return rarity switch
            {
                Rarity.Common    => commonWeight,
                Rarity.Rare      => rareWeight,
                Rarity.Epic      => epicWeight,
                Rarity.Legendary => legendaryWeight,
                _                => 0f
            };
        }

        /// <summary>
        /// Builds the weight table for all rarities, applying pity bonus if active.
        /// </summary>
        /// <param name="consecutiveNonRarePlus">How many consecutive rolls without Rare+.</param>
        /// <returns>Dictionary of Rarity → adjusted weight.</returns>
        public Dictionary<Rarity, float> GetAdjustedWeights(int consecutiveNonRarePlus)
        {
            float bonus = 0f;

            if (usePitySystem && consecutiveNonRarePlus > pityThreshold)
            {
                int rollsPastThreshold = consecutiveNonRarePlus - pityThreshold;
                bonus = Mathf.Min(rollsPastThreshold * pityBonusPerRoll, pityBonusCap);
            }

            // Distribute bonus equally across Rare / Epic / Legendary
            float bonusPerTier = bonus / 3f;

            var weights = new Dictionary<Rarity, float>
            {
                [Rarity.Common]    = Mathf.Max(0f, commonWeight    - bonus),
                [Rarity.Rare]      = rareWeight      + bonusPerTier,
                [Rarity.Epic]      = epicWeight       + bonusPerTier,
                [Rarity.Legendary] = legendaryWeight  + bonusPerTier,
            };

            return weights;
        }

        /// <summary>Validates that weights sum to a positive total.</summary>
        public bool ValidateWeights(out string error)
        {
            float total = commonWeight + rareWeight + epicWeight + legendaryWeight;
            if (total <= 0f)
            {
                error = "All rarity weights are zero. At least one must be > 0.";
                return false;
            }
            error = null;
            return true;
        }
    }
}
