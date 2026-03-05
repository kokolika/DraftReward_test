# Reward Draft System
**Unity 6.3 LTS · URP 2D · GD Systems Implementation Test**
*by Khalid Mergousse*

---

A small Unity project implementing a roguelite-style reward draft system. The player rolls three item choices, picks one, and builds an inventory over multiple phases. Items have weighted rarities, a pity system kicks in on bad luck streaks, and everything is driven by data loaded from JSON or CSV at runtime.

---

## Getting Started

Open `Assets/Scenes/RewardDraft-test.unity` and press Play.

Items load automatically from `StreamingAssets/Items.json` — no setup needed.

---

## Project Structure

```
Assets/
├── Prefabs/
│   └── Item_Prefab.prefab
│
├── Resources/
│   └── LootSettings/
│       └── LootSettings.asset
│
├── Scenes/
│   └── RewardDraft-test.unity          ← run this
│
├── ScriptableObjects/
│   └── SO_scripts/
│       ├── LootSettingsSO.cs
│       ├── ItemDefinitionSO.cs
│       └── ItemDatabaseSO.cs
│
├── Scripts/
│   ├── Core/
│   │   ├── ItemData.cs
│   │   ├── LootService.cs
│   │   ├── InventoryModel.cs
│   │   ├── JsonItemLoader.cs
│   │   ├── CsvItemLoader.cs
│   │   └── RunLogger.cs
│   │
│   ├── Editor/
│   │   └── RewardDraftEditorWindow.cs
│   │
│   ├── Tests/
│   │   └── LootServiceTests.cs
│   │
│   └── UI/
│       ├── RewardDraftController.cs
│       ├── RewardCardUI.cs
│       └── InventoryUI.cs
│
└── StreamingAssets/
    ├── Items.json
    └── Items.csv
```

---

## How the Loot System Works

Each draft phase runs through these steps:

**1. Roll a rarity**

The system picks a rarity using weighted probability. A random float is rolled between 0 and the total weight, then the code walks through Common → Rare → Epic → Legendary until it finds which bucket the roll landed in.

Default weights:

| Rarity | Weight | Chance |
|--------|--------|--------|
| Common | 60 | 60% |
| Rare | 25 | 25% |
| Epic | 10 | 10% |
| Legendary | 5 | 5% |

Weights don't need to sum to 100 — only the ratios matter. All values are editable in `LootSettings.asset`.

**2. Pick an item**

From all items matching the rolled rarity that haven't been picked yet this draft, one is chosen at random.

**3. Fallback**

If no items of that rarity are available (e.g. the only Legendary was already picked), the system falls back to any remaining unchosen item.

**4. Repeat**

Steps 1–3 run until 3 distinct items are selected.

---

## Preventing Duplicates

A `HashSet<string>` tracks the id of every item picked during a draft. Before any item is added to the results, its id is checked against the set — if it's already there, it gets skipped.

```csharp
if (usedIds.Contains(item.id)) continue;
usedIds.Add(item.id);
```

This only applies within a single draft phase. The same item can show up again in a later roll — intentional, to keep the roguelite feel.

---

## Data Import

### JSON (Option A)

Items are loaded from `StreamingAssets/Items.json` at runtime using `UnityWebRequest` (required for Android/WebGL compatibility — `File.ReadAllText` doesn't work on compressed StreamingAssets).

```json
{
  "items": [
    {
      "id": "itm_potion_small",
      "displayName": "Small Potion",
      "description": "Restore 10 HP at the start of your next combat.",
      "rarity": "Common"
    },
    {
      "id": "itm_amulet_echo",
      "displayName": "Amulet of Echo",
      "description": "The first spell cast each combat automatically triggers twice.",
      "rarity": "Legendary"
    }
  ]
}
```

### CSV (Bonus)

`CsvItemLoader` is a drop-in alternative that reads `StreamingAssets/Items.csv` instead. It fires the same `OnItemsLoaded` event so nothing else in the project needs to change.

```
id,displayName,description,rarity
itm_potion_small,Small Potion,Restore 10 HP.,Common
itm_amulet_echo,Amulet of Echo,First spell triggers twice.,Legendary
```

Quoted fields with commas inside are handled correctly.

To switch: replace the `JsonItemLoader` component on its GameObject with `CsvItemLoader`. That's it.

---

## Data Export

After every pick, `RunLogger` writes `RunLog.json` to `Application.persistentDataPath`. The exact path is printed in the Console when you press Play.

The file saves immediately on every pick — not at session end — so data is never lost if the app closes unexpectedly.

```json
{
  "runStartTime": "2025-03-04 14:32:00",
  "phases": [
    {
      "timestamp": "14:32:05",
      "phaseIndex": 1,
      "offeredItems": [
        { "id": "itm_iron_shield", "displayName": "Iron Shield", "rarity": "Common" },
        { "id": "itm_mana_crystal", "displayName": "Mana Crystal", "rarity": "Rare" },
        { "id": "itm_amulet_echo", "displayName": "Amulet of Echo", "rarity": "Legendary" }
      ],
      "selectedItem": { "id": "itm_amulet_echo", "displayName": "Amulet of Echo", "rarity": "Legendary" }
    }
  ],
  "currentInventory": [
    { "id": "itm_amulet_echo", "displayName": "Amulet of Echo", "rarity": "Legendary" }
  ]
}
```

You can also read it directly inside Unity via **Tools → Reward Draft → Control Panel → Run Log tab**.

---

## Bonus Features

### Pity System

After 8 consecutive rolls without a Rare+ item, the system starts nudging the weights in your favor. Common weight goes down, Rare/Epic/Legendary share the bonus equally. It caps at +40% so it never becomes guaranteed.

| Streak | Common | Rare+ |
|--------|--------|-------|
| 0–8 | 60% | 40% |
| 11 | 51% | 49% |
| 16 | 36% | 64% |
| 21+ | 20% | 80% (cap) |

Resets the moment something Rare+ drops. The live streak shows in the PityText label on screen. All thresholds are configurable in `LootSettings.asset`.

---

### Deterministic Seed

Pass an optional seed to `LootService` and every run produces the exact same sequence:

```csharp
// normal random gameplay
var service = new LootService(settings);

// same seed = same rolls every time
var service = new LootService(settings, seed: 42);
```

Useful for testing and debugging. The Simulator tab in the Editor window exposes a seed field so you can try it without entering Play mode.

---

### Unit Tests

12 EditMode tests covering the core loot logic. Run them via `Window → General → Test Runner → EditMode → Run All`.

| Test | Checks |
|------|--------|
| `GenerateDraft_ReturnsCorrectNumberOfItems` | Always returns 3 items |
| `GenerateDraft_NoDuplicateIds` | No duplicates in one draft |
| `GenerateDraft_AllItemsHaveValidRarity` | Valid rarities across 50 rolls |
| `GenerateDraft_WithAllWeightsOnLegendary` | Only Legendary when weight = 100 |
| `GenerateDraft_DatabaseSmallerThanChoices` | Fallback when DB < 3 items |
| `LoadItems_ThrowsOnEmpty` | Empty list throws |
| `LoadItems_ThrowsOnNull` | Null throws |
| `LootSettings_InvalidWeights_ValidationFails` | Zero weights fail validation |
| `PitySystem_InactiveBelowThreshold` | No bonus before threshold |
| `DeterministicSeed_ProducesSameSequence` | Same seed = same results |
| `ItemData_InitializeRarity_ParsesCorrectly` | String to enum works |
| `ItemData_InitializeRarity_InvalidDefaultsToCommon` | Bad string defaults to Common |

---

### Editor Control Panel

Open via **Tools → Reward Draft → Control Panel**

| Tab | What it does |
|-----|-------------|
| Items | Add, edit, delete items. Filter by name or rarity. Save back to JSON or CSV. |
| Settings | Tweak all loot settings with a live probability bar showing the result. |
| Simulator | Run test drafts without pressing Play. Supports seed input. Shows drop stats. |
| Run Log | Read, refresh, or delete RunLog.json without leaving the Editor. |

---

## Architecture Notes

The main design goal was keeping loot logic and UI completely separate.

`LootService` is a plain C# class with no Unity dependencies — no MonoBehaviour, no scene reference. This means it's fully unit-testable and could be dropped into any other project as-is.

`InventoryModel` uses a C# event (`OnItemAdded`) instead of letting the UI poll every frame. The UI just subscribes and updates when something changes.

Rarity weights are stored in a `ScriptableObject` (`LootSettingsSO`) so designers can tune them in the Inspector without touching code or recompiling.

`RunLogger` writes to disk immediately on every pick rather than at session end — a small thing that avoids data loss on unexpected closes.

---

## Known Limitations

- No item icons — not required per the test spec
- Inventory resets when the app closes (RunLog.json keeps the history)
- `JsonUtility` doesn't support Dictionary or polymorphism — kept the data model flat on purpose. Newtonsoft.Json would be the upgrade path if needed
- Single scene only — the draft just re-triggers indefinitely, no game loop

---

*Unity 6.3 LTS (6000.3.8f1) · URP 2D · TextMeshPro · No third-party packages*