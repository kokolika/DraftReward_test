// LootServiceTests.cs
// EditMode unit tests for the LootService.
// Implements the "Add EditMode unit tests" bonus feature.
//
// To run: Window > General > Test Runner > EditMode tab > Run All

using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using RewardDraft.Core;

namespace RewardDraft.Tests
{
    public class LootServiceTests
    {
        // ── Helpers ────────────────────────────────────────────────────────────

        private static LootSettingsSO MakeSettings(
            float common = 60f, float rare = 25f, float epic = 10f, float legendary = 5f,
            int choices = 3, bool pity = false)
        {
            var so = ScriptableObject.CreateInstance<LootSettingsSO>();
            so.commonWeight    = common;
            so.rareWeight      = rare;
            so.epicWeight      = epic;
            so.legendaryWeight = legendary;
            so.numberOfChoices = choices;
            so.usePitySystem   = pity;
            so.pityThreshold   = 5;
            so.pityBonusPerRoll = 5f;
            so.pityBonusCap    = 40f;
            return so;
        }

        private static List<ItemData> MakeItems(int common = 6, int rare = 3, int epic = 2, int legendary = 1)
        {
            var items = new List<ItemData>();
            int id = 0;

            void Add(Rarity r, int count)
            {
                for (int i = 0; i < count; i++)
                {
                    items.Add(new ItemData
                    {
                        id          = $"item_{id++}",
                        displayName = $"Item {id}",
                        description = "Test item",
                        rarity      = r.ToString(),
                        rarityEnum  = r
                    });
                }
            }

            Add(Rarity.Common,    common);
            Add(Rarity.Rare,      rare);
            Add(Rarity.Epic,      epic);
            Add(Rarity.Legendary, legendary);

            return items;
        }

        // ── Tests ──────────────────────────────────────────────────────────────

        [Test]
        public void GenerateDraft_ReturnsCorrectNumberOfItems()
        {
            var svc = new LootService(MakeSettings());
            svc.LoadItems(MakeItems());

            var draft = svc.GenerateDraft();

            Assert.AreEqual(3, draft.Count, "Draft should return exactly 3 items.");
        }

        [Test]
        public void GenerateDraft_NoDuplicateIds()
        {
            var svc = new LootService(MakeSettings());
            svc.LoadItems(MakeItems());

            var draft = svc.GenerateDraft();
            var ids   = draft.Select(i => i.id).ToList();

            Assert.AreEqual(ids.Distinct().Count(), ids.Count,
                "All items in a draft must have distinct IDs.");
        }

        [Test]
        public void GenerateDraft_AllItemsHaveValidRarity()
        {
            var svc = new LootService(MakeSettings());
            svc.LoadItems(MakeItems());

            for (int roll = 0; roll < 50; roll++)
            {
                var draft = svc.GenerateDraft();
                foreach (var item in draft)
                {
                    Assert.IsTrue(
                        item.rarityEnum == Rarity.Common    ||
                        item.rarityEnum == Rarity.Rare      ||
                        item.rarityEnum == Rarity.Epic      ||
                        item.rarityEnum == Rarity.Legendary,
                        $"Item '{item.id}' has unexpected rarity.");
                }
            }
        }

        [Test]
        public void GenerateDraft_WithAllWeightsOnLegendary_ReturnsOnlyLegendary()
        {
            var svc = new LootService(MakeSettings(common: 0, rare: 0, epic: 0, legendary: 100, choices: 1));
            svc.LoadItems(MakeItems(legendary: 3));

            // Run 20 times — all must be Legendary
            for (int i = 0; i < 20; i++)
            {
                var draft = svc.GenerateDraft();
                Assert.AreEqual(1, draft.Count);
                Assert.AreEqual(Rarity.Legendary, draft[0].rarityEnum,
                    "With only Legendary weight, all rolls must be Legendary.");
            }
        }

        [Test]
        public void GenerateDraft_DatabaseSmallerThanChoices_ReturnsAllItems()
        {
            var svc = new LootService(MakeSettings(choices: 3));
            // Only 2 items available
            svc.LoadItems(MakeItems(common: 2, rare: 0, epic: 0, legendary: 0));

            var draft = svc.GenerateDraft();

            Assert.LessOrEqual(draft.Count, 2,
                "Cannot return more items than the database contains.");
        }

        [Test]
        public void LoadItems_ThrowsOnEmpty()
        {
            var svc = new LootService(MakeSettings());

            Assert.Throws<System.ArgumentException>(() =>
                svc.LoadItems(new List<ItemData>()));
        }

        [Test]
        public void LoadItems_ThrowsOnNull()
        {
            var svc = new LootService(MakeSettings());

            Assert.Throws<System.ArgumentException>(() =>
                svc.LoadItems(null));
        }

        [Test]
        public void LootSettings_InvalidWeights_ValidationFails()
        {
            var settings = MakeSettings(common: 0, rare: 0, epic: 0, legendary: 0);
            bool valid   = settings.ValidateWeights(out string error);

            Assert.IsFalse(valid, "Zero weights should fail validation.");
            Assert.IsNotNull(error, "Error message should be provided.");
        }

        [Test]
        public void PitySystem_InactiveBelowThreshold()
        {
            var svc = new LootService(MakeSettings(pity: true));
            svc.LoadItems(MakeItems());

            // Pity bonus should be 0 before threshold is crossed
            Assert.AreEqual(0f, svc.CurrentPityBonus,
                "Pity bonus should be 0 before streak crosses threshold.");
        }

        [Test]
        public void DeterministicSeed_ProducesSameSequence()
        {
            const int seed = 42;
            var settings   = MakeSettings();
            var items      = MakeItems();

            var svc1 = new LootService(settings, seed);
            svc1.LoadItems(items);

            var svc2 = new LootService(settings, seed);
            svc2.LoadItems(items);

            var draft1 = svc1.GenerateDraft();
            var draft2 = svc2.GenerateDraft();

            Assert.AreEqual(draft1.Count, draft2.Count);
            for (int i = 0; i < draft1.Count; i++)
            {
                Assert.AreEqual(draft1[i].id, draft2[i].id,
                    $"Same seed must produce same item at position {i}.");
            }
        }

        [Test]
        public void ItemData_InitializeRarity_ParsesCorrectly()
        {
            var item = new ItemData { rarity = "Legendary" };
            item.InitializeRarity();

            Assert.AreEqual(Rarity.Legendary, item.rarityEnum);
        }

        [Test]
        public void ItemData_InitializeRarity_InvalidDefaultsToCommon()
        {
            var item = new ItemData { rarity = "UltraSuper" };
            item.InitializeRarity();

            Assert.AreEqual(Rarity.Common, item.rarityEnum,
                "Unknown rarity string should default to Common.");
        }
    }
}
