// RunLogger.cs
// Implements Option B (JSON Export) from the test requirements.
// Records every loot phase: what was offered, what was picked, current inventory.
// Writes RunLog.json to Application.persistentDataPath.

using System;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using RewardDraft.Core;

namespace RewardDraft.Core
{
    // ── Data structures for serialization ─────────────────────────────────────

    [Serializable]
    public class LootPhaseRecord
    {
        public string timestamp;
        public int phaseIndex;
        public List<ItemRecord> offeredItems = new List<ItemRecord>();
        public ItemRecord selectedItem;
    }

    [Serializable]
    public class ItemRecord
    {
        public string id;
        public string displayName;
        public string rarity;
    }

    [Serializable]
    public class RunLogData
    {
        public string runStartTime;
        public List<LootPhaseRecord> phases = new List<LootPhaseRecord>();
        public List<ItemRecord> currentInventory = new List<ItemRecord>();
    }

    // ── RunLogger ──────────────────────────────────────────────────────────────

    public class RunLogger
    {
        private readonly RunLogData _log = new RunLogData();
        private readonly string _outputPath;

        public RunLogger()
        {
            _log.runStartTime = DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss");
            _outputPath = Path.Combine(Application.persistentDataPath, "RunLog.json");
        }

        /// <summary>
        /// Records one loot phase (what was offered + what was picked).
        /// Call this after the player picks an item.
        /// </summary>
        public void RecordPhase(int phaseIndex, List<ItemData> offered, ItemData selected, IEnumerable<ItemData> inventory)
        {
            var record = new LootPhaseRecord
            {
                timestamp  = DateTime.Now.ToString("HH:mm:ss"),
                phaseIndex = phaseIndex,
                selectedItem = ToRecord(selected)
            };

            foreach (var item in offered)
                record.offeredItems.Add(ToRecord(item));

            _log.phases.Add(record);

            // Snapshot current inventory
            _log.currentInventory.Clear();
            foreach (var item in inventory)
                _log.currentInventory.Add(ToRecord(item));

            // Write immediately so data isn't lost if the app closes
            Save();
        }

        /// <summary>Saves the current log state to disk.</summary>
        public void Save()
        {
            try
            {
                string json = JsonUtility.ToJson(_log, prettyPrint: true);
                File.WriteAllText(_outputPath, json);
                Debug.Log($"[RunLogger] Saved RunLog.json to: {_outputPath}");
            }
            catch (Exception ex)
            {
                Debug.LogError($"[RunLogger] Failed to save RunLog.json: {ex.Message}");
            }
        }

        /// <summary>Returns the path where RunLog.json is written.</summary>
        public string OutputPath => _outputPath;

        private ItemRecord ToRecord(ItemData item) => new ItemRecord
        {
            id          = item.id,
            displayName = item.displayName,
            rarity      = item.rarityEnum.ToString()
        };
    }
}
