# DraftReward\_test

a draft reward unity project for technical test evaluation





\# Reward Draft System — Unity 6 URP 2D

GD Systems Implementation Test Submission ----- KHALID MERGOUSSE



========================================================================================================================

------------------------------------------------------------------------------------------------------------------------

@ Main Scene

------------------------------------------------------------------------------------------------------------------------

&nbsp;- Assets/Scenes/RewardDraft-test.unity



Open this scene and press Play. Item data loads automatically from

`StreamingAssets/Items.json` on startup. No additional setup required.



------------------------------------------------------------------------------------------------------------------------

@ Project Structure

------------------------------------------------------------------------------------------------------------------------



Assets/

├── Prefabs/

│   ├── Item\_Prefab.prefab            — the item visual prefabe

│

├── Resources/

│   └── LootSettings/

│       └── LootSettings.asset        — LootSettingsSO instance (weights, pity config)

│

├── Scenes/

│   └── RewardDraft-test.unity             — ★ MAIN SCENE — open this and press Play

│

├── ScriptableObjects/

│   └── SO\_scripts/

│       ├── LootSettingsSO.cs         — Required SO: rarity weights, pity system, draft config

│       ├── ItemDefinitionSO.cs       — Per-item authoring SO (optional in-editor path)

│       └── ItemDatabaseSO.cs         — Item collection SO (optional in-editor path)

│

├── Scripts/

│   ├── Core/

│   │   ├── ItemData.cs               — ItemData class, Rarity enum, ItemDatabase JSON wrapper

│   │   ├── LootService.cs            — Weighted rarity roll, draft generation, pity tracker

│   │   ├── InventoryModel.cs         — Inventory state, OnItemAdded event

│   │   ├── JsonItemLoader.cs         — Async load of StreamingAssets/Items.json

│   │   ├── CsvItemLoader.cs          — Async load of StreamingAssets/Items.csv (Bonus)

│   │   └── RunLogger.cs              — Writes RunLog.json after every pick (Bonus)

│   │

│   ├── Editor/

│   │   └── RewardDraftEditorWindow.cs — 4-tab control panel: Items / Settings / Simulator / Log

│   │

│   ├── Tests/

│   │   └── LootServiceTests.cs       — 12 EditMode unit tests for LootService (Bonus)

│   │

│   └── UI/

│       ├── RewardDraftController.cs  — Orchestrator: wires all systems, handles Roll + Pick

│       ├── RewardCardUI.cs           — Populates one reward card, fires OnPicked event

│       └── InventoryUI.cs            — Subscribes to InventoryModel, appends colored rows

│

└── StreamingAssets/

&nbsp;   ├── Items.json                    — 12 items (6C / 3R / 2E / 1L) — primary data source

&nbsp;   └── Items.csv                     — Same 12 items in CSV format (Bonus)



------------------------------------------------------------------------------------------------------------------------

@ Loot Generation Logic

------------------------------------------------------------------------------------------------------------------------



&nbsp; - Step 1 — Roll a Rarity



&nbsp;  `LootService.RollRarity()` uses weighted probability:



```

Total weight = sum of all rarity weights

Roll         = Random(0.0, Total)



Walk \[Common → Rare → Epic → Legendary] accumulating weights.

Return the rarity where the accumulator first exceeds the roll.

```



Default weights (configurable in `LootSettings.asset`):



| Rarity    | Weight | Probability |

|-----------|--------|-------------|

| Common    | 60     | 60%         |

| Rare      | 25     | 25%         |

| Epic      | 10     | 10%         |

| Legendary | 5      | 5%          |



Weights do not need to sum to 100. Only their relative proportions matter.

`common=6, rare=2, epic=1, legendary=1` produces identical probabilities

to `common=60, rare=20, epic=10, legendary=10`.



\### Step 2 — Pick an Item



From all items of the rolled rarity not yet selected in this draft,

pick one at random using `System.Random`.



&nbsp; - Step 3 — Fallback



If no items of the rolled rarity remain unchosen (e.g. only 1 Legendary

exists and it was already picked), the system falls back to any unchosen

item of any rarity. This ensures the draft always fills all 3 slots when

the database has enough items total.



&nbsp; - Step 4 — Repeat



Steps 1–3 repeat until 3 distinct items are chosen.



------------------------------------------------------------------------------------------------------------------------

@ How Duplicates Are Prevented

------------------------------------------------------------------------------------------------------------------------



A `HashSet<string> usedIds` is populated as each item is selected.

Before adding any item to the draft, its `id` is checked against this set.



```csharp

if (usedIds.Contains(item.id)) continue; // skip — already chosen

usedIds.Add(item.id);                    // mark as used

```



`HashSet` lookup is O(1) — instant regardless of database size.



Duplicates are prevented \*\*per draft only\*\*. The same item can appear

again in a later draft phase. This is intentional — it preserves the

roguelite feel where items recycle across phases.



------------------------------------------------------------------------------------------------------------------------

@ JSON Import — Option A

------------------------------------------------------------------------------------------------------------------------



&nbsp; - File: `Assets/StreamingAssets/Items.json`



&nbsp;- Format:



```json

{

&nbsp; "items": \[

&nbsp;   {

&nbsp;     "id": "itm\_potion\_small",

&nbsp;     "displayName": "Small Potion",

&nbsp;     "description": "Restore 10 HP at the start of your next combat.",

&nbsp;     "rarity": "Common"

&nbsp;   },

&nbsp;   {

&nbsp;     "id": "itm\_amulet\_echo",

&nbsp;     "displayName": "Amulet of Echo",

&nbsp;     "description": "The first spell cast each combat automatically triggers twice.",

&nbsp;     "rarity": "Legendary"

&nbsp;   }

&nbsp; ]

}

```



Runtime flow:

1\. `JsonItemLoader.Load()` is called in `Start()`

2\. `UnityWebRequest.Get()` fetches the file asynchronously via coroutine

3\. `JsonUtility.FromJson<ItemDatabase>()` parses the JSON text

4\. `ItemData.InitializeRarity()` converts each rarity string to the `Rarity` enum

5\. `LootService.LoadItems()` receives the list — system is ready



------------------------------------------------------------------------------------------------------------------------

@ CSV Import — Option A (Bonus)

------------------------------------------------------------------------------------------------------------------------



\- File: `Assets/StreamingAssets/Items.csv`



\- Format:

```

id,displayName,description,rarity

itm\_potion\_small,Small Potion,Restore 10 HP at the start of your next combat.,Common

itm\_mana\_crystal,Mana Crystal,Restore 8 MP to all mages after each battle.,Rare

itm\_amulet\_echo,Amulet of Echo,The first spell cast each combat triggers twice.,Legendary

```



`CsvItemLoader` is a drop-in alternative to `JsonItemLoader`. It fires

the same `OnItemsLoaded` event so it connects to `RewardDraftController`

identically. Quoted fields containing commas are handled correctly:



```

itm\_example,Item Name,"Description with, a comma inside",Common

```



To switch from JSON to CSV: on the `JsonItemLoader` GameObject in the

scene, replace the `JsonItemLoader` component with `CsvItemLoader`.

No other changes required.



------------------------------------------------------------------------------------------------------------------------

@ JSON Export — Option B

------------------------------------------------------------------------------------------------------------------------



&nbsp;- File: `Application.persistentDataPath/RunLog.json`



The exact save path is printed to the Console on startup:

```

\[RewardDraftController] Run log will be saved to: C:/Users/.../RunLog.json

```



The file is written \*\*immediately after every pick\*\* — not at session end.

Data is never lost if the application closes unexpectedly.



&nbsp;- Format:

```json

{

&nbsp; "runStartTime": "2025-03-04 14:32:00",

&nbsp; "phases": \[

&nbsp;   {

&nbsp;     "timestamp": "14:32:05",

&nbsp;     "phaseIndex": 1,

&nbsp;     "offeredItems": \[

&nbsp;       { "id": "itm\_iron\_shield",  "displayName": "Iron Shield",    "rarity": "Common"    },

&nbsp;       { "id": "itm\_mana\_crystal", "displayName": "Mana Crystal",   "rarity": "Rare"      },

&nbsp;       { "id": "itm\_amulet\_echo",  "displayName": "Amulet of Echo", "rarity": "Legendary" }

&nbsp;     ],

&nbsp;     "selectedItem":

&nbsp;       { "id": "itm\_amulet\_echo",  "displayName": "Amulet of Echo", "rarity": "Legendary" }

&nbsp;   }

&nbsp; ],

&nbsp; "currentInventory": \[

&nbsp;   { "id": "itm\_amulet\_echo", "displayName": "Amulet of Echo", "rarity": "Legendary" }

&nbsp; ]

}

```



The Run Log can be read directly from the Editor without opening the

file system via \*\*Tools → Reward Draft → Control Panel → Run Log tab\*\*.



------------------------------------------------------------------------------------------------------------------------

@ Bonus Features

------------------------------------------------------------------------------------------------------------------------



&nbsp;- Pity System



&nbsp; After `pityThreshold` (default: 8) consecutive rolls without a Rare+

&nbsp; item, a pity bonus begins accumulating:



```

bonus = (streak - threshold) × pityBonusPerRoll

bonus = min(bonus, pityBonusCap)



Common weight   -= bonus

Rare weight     += bonus / 3

Epic weight     += bonus / 3

Legendary weight+= bonus / 3

```



With default values (threshold=8, rate=3%, cap=40%):



| Streak | Common | Rare+ Combined |

|--------|--------|----------------|

| 0–8    | 60%    | 40%            |

| 11     | 51%    | 49%            |

| 16     | 36%    | 64%            |

| 21+    | 20%    | 80% (capped)   |



The streak resets to 0 the moment a Rare+ item drops.

The live streak and current bonus are displayed in the PityText UI label.

All pity values are configurable in `LootSettings.asset` and via the

Settings tab of the Editor Control Panel.



------------------------------------------------------------------------------------------------------------------------

@ Deterministic Seed

------------------------------------------------------------------------------------------------------------------------



`LootService` accepts an optional integer seed:



```csharp

// Random rolls (normal gameplay — default)

var service = new LootService(settings);



// Reproducible rolls — identical sequence every run

var service = new LootService(settings, seed: 42);

```



The same seed always produces the same draft sequence regardless of

how many times the game is run. The Simulator tab in the Editor

Control Panel exposes a seed field to test this interactively.



------------------------------------------------------------------------------------------------------------------------

@ EditMode Unit Tests

------------------------------------------------------------------------------------------------------------------------



&nbsp; - Location: `Assets/Scripts/Tests/LootServiceTests.cs`



&nbsp; - Run via: `Window → General → Test Runner → EditMode → Run All`



| Test                                                | What it verifies                                |

|-----------------------------------------------------|-------------------------------------------------|

| `GenerateDraft\_ReturnsCorrectNumberOfItems`         | Always returns exactly 3 items                  |

| `GenerateDraft\_NoDuplicateIds`                      | No item appears twice in one draft              |

| `GenerateDraft\_AllItemsHaveValidRarity`             | No unexpected rarity values across 50 rolls     |

| `GenerateDraft\_WithAllWeightsOnLegendary`           | Weight=100 Legendary → only Legendary drops     |

| `GenerateDraft\_DatabaseSmallerThanChoices`          | Graceful fallback when DB has fewer than 3 items|

| `LoadItems\_ThrowsOnEmpty`                           | Empty list throws ArgumentException             |

| `LoadItems\_ThrowsOnNull`                            | Null throws ArgumentException                   |

| `LootSettings\_InvalidWeights\_ValidationFails`       | All-zero weights fail validation                |

| `PitySystem\_InactiveBelowThreshold`                 | Pity bonus = 0 before streak crosses threshold  |

| `DeterministicSeed\_ProducesSameSequence`            | Same seed = same roll sequence                  |

| `ItemData\_InitializeRarity\_ParsesCorrectly`         | "Legendary" string → Rarity.Legendary           |

| `ItemData\_InitializeRarity\_InvalidDefaultsToCommon` | Unknown string → defaults to Common             |



------------------------------------------------------------------------------------------------------------------------

@ Editor Control Panel

------------------------------------------------------------------------------------------------------------------------



&nbsp; Open via: `Tools → Reward Draft → Control Panel`



| Tab       | What it does                                                                  |

|-----------|-------------------------------------------------------------------------------|

| Items     | View, add, edit, delete items. Filter by name or rarity. Save to JSON or CSV. |

| Settings  | Edit all LootSettingsSO values. Live probability bar. Pity system summary.    |

| Simulator | Run 1–20 test drafts without pressing Play. Optional seed. Rarity statistics. |

| Run Log   | Read, refresh, open folder, or delete RunLog.json from inside the Editor.     |



------------------------------------------------------------------------------------------------------------------------

@ Design Decisions

------------------------------------------------------------------------------------------------------------------------



-Pure C# LootService:

All loot logic lives in a plain C# class with zero Unity dependencies.

This makes it unit-testable without PlayMode, reusable across projects,

and keeps the architecture clean. The controller is a thin orchestrator —

it wires events and delegates; it contains no loot math.



\- JSON as primary data source:

Items.json is the single source for item data at runtime.

Adding or changing items requires only a text editor — no Unity Editor,

no recompile. ItemDefinitionSO and ItemDatabaseSO are provided as an

optional in-editor authoring path for teams that prefer the Inspector.



\- Event-driven inventory:

`InventoryModel` raises `OnItemAdded`. `InventoryUI` subscribes.

The UI never polls — it only updates when something changes. Additional

observers (analytics, achievements) can subscribe without touching UI code.



\- Weights as relative values:

Rarity weights do not need to sum to 100. The system normalises them

internally. This makes it easier for designers to reason about ratios

without managing a fixed budget.



\- Immediate log writes:

`RunLogger` writes to disk after every single pick, not at session end.

This ensures data is never lost if the application is force-quit

between phases.



------------------------------------------------------------------------------------------------------------------------

@ Known Limitations

------------------------------------------------------------------------------------------------------------------------



\- No sprites: Per the test specification, no item icons are displayed.

\- Session-only inventory: The inventory clears when the application

&nbsp; closes. RunLog.json preserves the data between sessions.

\- JsonUtility limitations: Unity's built-in JsonUtility does not

&nbsp; support Dictionary or polymorphism. The flat ItemDatabase structure

&nbsp; was chosen for JsonUtility compatibility. For more complex schemas,

&nbsp; Newtonsoft.Json (available as a Unity package) is the upgrade path.

\- Single scene: The test does not require a game loop or multiple

&nbsp; scenes. The draft can be re-triggered indefinitely from the same scene.

\- RAND recalculates on edit: `RAND()` and `RANDBETWEEN()` in the

&nbsp; balancing spreadsheet recalculate on every file edit. To freeze values:

&nbsp; select cells → Ctrl+C → Paste Special → Values Only.



------------------------------------------------------------------------------------------------------------------------



Unity 6.3 LTS (6000.3.8f1) · URP 2D · TextMeshPro · No third-party packages



