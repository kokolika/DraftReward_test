// LootService.cs
// Pure C# class — NOT a MonoBehaviour.
// Owns all loot generation logic: rarity rolling, pity tracking, item selection.
// The UI layer calls this service; it never touches Unity UI APIs.
// This enforces the separation between gameplay logic and UI (per test requirements).

using System;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using RewardDraft.Core;

namespace RewardDraft.Core
{
    public class LootService
    {
        // ── State ─────────────────────────────────────────────────────────────
        private readonly LootSettingsSO _settings;
        private List<ItemData> _allItems = new List<ItemData>();

        // Pity timer: counts consecutive rolls that produced no Rare+ item
        private int _consecutiveNonRarePlusRolls = 0;

        // For deterministic seed option (bonus)
        private System.Random _rng;

        // ── Constructor ───────────────────────────────────────────────────────

        /// <summary>
        /// Creates a LootService with the given settings.
        /// Optionally pass a seed for reproducible rolls (bonus feature).
        /// </summary>
        public LootService(LootSettingsSO settings, int? seed = null)
        {
            _settings = settings ?? throw new ArgumentNullException(nameof(settings));
            _rng      = seed.HasValue ? new System.Random(seed.Value) : new System.Random();
        }

        // ── Item Database ──────────────────────────────────────────────────────

        /// <summary>
        /// Loads the item database from a list of ItemData (typically from JSON).
        /// Replaces any previously loaded items.
        /// </summary>
        public void LoadItems(List<ItemData> items)
        {
            if (items == null || items.Count == 0)
                throw new ArgumentException("Item list is null or empty.");

            _allItems = new List<ItemData>(items);
            Debug.Log($"[LootService] Loaded {_allItems.Count} items.");

            LogRarityDistribution();
        }

        /// <summary>Returns how many items are currently loaded.</summary>
        public int ItemCount => _allItems.Count;

        // ── Core: Generate Draft ───────────────────────────────────────────────

        /// <summary>
        /// Generates a draft offer of N distinct items using weighted rarity rolls.
        /// Respects the pity timer if enabled in settings.
        /// </summary>
        /// <returns>List of ItemData to display as choices. Never null.</returns>
        public List<ItemData> GenerateDraft()
        {
            if (!_settings.ValidateWeights(out string weightError))
                throw new InvalidOperationException($"[LootService] Invalid weights: {weightError}");

            int needed = _settings.numberOfChoices;

            if (_allItems.Count < needed)
            {
                Debug.LogWarning($"[LootService] Database has {_allItems.Count} items but {needed} choices requested. "
                               + "Returning all available items.");
                needed = _allItems.Count;
            }

            var chosen    = new List<ItemData>(needed);
            var usedIds   = new HashSet<string>();
            int attempts  = 0;
            int maxAttempts = needed * 50; // safety cap against infinite loops

            while (chosen.Count < needed && attempts < maxAttempts)
            {
                attempts++;

                // 1. Roll a rarity
                Rarity rolledRarity = RollRarity();

                // 2. Get all items of that rarity not yet chosen
                var candidates = _allItems
                    .Where(i => i.rarityEnum == rolledRarity && !usedIds.Contains(i.id))
                    .ToList();

                // 3. If no candidates for this rarity, try any unchosen item
                if (candidates.Count == 0)
                    candidates = _allItems.Where(i => !usedIds.Contains(i.id)).ToList();

                if (candidates.Count == 0) break; // all items exhausted

                // 4. Pick randomly from candidates
                ItemData picked = candidates[_rng.Next(candidates.Count)];

                chosen.Add(picked);
                usedIds.Add(picked.id);

                // 5. Update pity counter
                bool isRarePlus = rolledRarity == Rarity.Rare
                               || rolledRarity == Rarity.Epic
                               || rolledRarity == Rarity.Legendary;

                if (isRarePlus)
                    _consecutiveNonRarePlusRolls = 0;
                else
                    _consecutiveNonRarePlusRolls++;
            }

            if (chosen.Count < _settings.numberOfChoices)
                Debug.LogWarning($"[LootService] Could only generate {chosen.Count}/{needed} distinct items.");

            return chosen;
        }

        // ── Rarity Roll ────────────────────────────────────────────────────────

        /// <summary>
        /// Rolls a rarity using weighted probability.
        /// Applies pity bonus if the pity system is enabled and the streak exceeds the threshold.
        /// </summary>
        private Rarity RollRarity()
        {
            var weights = _settings.GetAdjustedWeights(_consecutiveNonRarePlusRolls);

            float total = weights.Values.Sum();
            if (total <= 0f)
            {
                Debug.LogError("[LootService] All weights are 0. Defaulting to Common.");
                return Rarity.Common;
            }

            float roll   = (float)_rng.NextDouble() * total;
            float cursor = 0f;

            // Walk through rarities in defined order
            foreach (Rarity rarity in new[] { Rarity.Common, Rarity.Rare, Rarity.Epic, Rarity.Legendary })
            {
                cursor += weights[rarity];
                if (roll < cursor) return rarity;
            }

            return Rarity.Legendary; // float precision fallback
        }

        // ── Pity Info ──────────────────────────────────────────────────────────

        /// <summary>Returns the current pity streak (for display or logging).</summary>
        public int PityStreak => _consecutiveNonRarePlusRolls;

        /// <summary>
        /// Returns the effective Rare+ bonus currently active from the pity system.
        /// </summary>
        public float CurrentPityBonus
        {
            get
            {
                if (!_settings.usePitySystem || _consecutiveNonRarePlusRolls <= _settings.pityThreshold)
                    return 0f;
                int past = _consecutiveNonRarePlusRolls - _settings.pityThreshold;
                return Mathf.Min(past * _settings.pityBonusPerRoll, _settings.pityBonusCap);
            }
        }

        // ── Debug ──────────────────────────────────────────────────────────────

        private void LogRarityDistribution()
        {
            int common = 0, rare = 0, epic = 0, legendary = 0;
            foreach (var item in _allItems)
            {
                switch (item.rarityEnum)
                {
                    case Rarity.Common:    common++;    break;
                    case Rarity.Rare:      rare++;      break;
                    case Rarity.Epic:      epic++;      break;
                    case Rarity.Legendary: legendary++; break;
                }
            }
            Debug.Log($"[LootService] Distribution — Common:{common} Rare:{rare} Epic:{epic} Legendary:{legendary}");
        }
    }
}
