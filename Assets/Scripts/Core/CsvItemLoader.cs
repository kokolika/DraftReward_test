// CsvItemLoader.cs
// Runtime MonoBehaviour that loads item data from StreamingAssets/Items.csv
// CSV format: id,displayName,description,rarity
// First row is the header — it is skipped automatically.
// Fires the same events as JsonItemLoader so it is a drop-in alternative.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using RewardDraft.Core;

namespace RewardDraft.Core
{
    public class CsvItemLoader : MonoBehaviour
    {
        [Header("File path within StreamingAssets")]
        [SerializeField] private string _fileName = "Items.csv";

        [Tooltip("Column index for each field (0-based). Default matches: id,displayName,description,rarity")]
        [SerializeField] private int _colId          = 0;
        [SerializeField] private int _colDisplayName = 1;
        [SerializeField] private int _colDescription = 2;
        [SerializeField] private int _colRarity      = 3;

        /// <summary>Raised when items finish loading successfully.</summary>
        public event Action<List<ItemData>> OnItemsLoaded;

        /// <summary>Raised if loading or parsing fails.</summary>
        public event Action<string> OnLoadError;

        /// <summary>True once loading has completed (success or failure).</summary>
        public bool IsLoaded { get; private set; } = false;

        // ── Public API ────────────────────────────────────────────────────────

        public void Load()
        {
            StartCoroutine(LoadRoutine());
        }

        // ── Implementation ────────────────────────────────────────────────────

        private IEnumerator LoadRoutine()
        {
            string path = BuildPath();
            Debug.Log($"[CsvItemLoader] Loading from: {path}");

            using UnityWebRequest request = UnityWebRequest.Get(path);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string err = $"Failed to load '{_fileName}': {request.error}";
                Debug.LogError($"[CsvItemLoader] {err}");
                IsLoaded = true;
                OnLoadError?.Invoke(err);
                yield break;
            }

            ParseAndDispatch(request.downloadHandler.text);
        }

        private void ParseAndDispatch(string csv)
        {
            try
            {
                var items = new List<ItemData>();
                string[] lines = csv.Split(new[] { "\r\n", "\n", "\r" },
                                           StringSplitOptions.RemoveEmptyEntries);

                if (lines.Length < 2)
                {
                    string err = "CSV file has no data rows (only header or empty).";
                    Debug.LogError($"[CsvItemLoader] {err}");
                    IsLoaded = true;
                    OnLoadError?.Invoke(err);
                    return;
                }

                int maxCol = Mathf.Max(_colId, _colDisplayName, _colDescription, _colRarity);

                // Start at index 1 — skip header row
                for (int i = 1; i < lines.Length; i++)
                {
                    string line = lines[i].Trim();
                    if (string.IsNullOrEmpty(line)) continue;

                    string[] cols = SplitCsvLine(line);

                    if (cols.Length <= maxCol)
                    {
                        Debug.LogWarning($"[CsvItemLoader] Row {i} has only {cols.Length} columns, expected {maxCol + 1}. Skipping.");
                        continue;
                    }

                    var item = new ItemData
                    {
                        id          = cols[_colId].Trim(),
                        displayName = cols[_colDisplayName].Trim(),
                        description = cols[_colDescription].Trim(),
                        rarity      = cols[_colRarity].Trim()
                    };

                    if (string.IsNullOrEmpty(item.id))
                    {
                        Debug.LogWarning($"[CsvItemLoader] Row {i} has empty id. Skipping.");
                        continue;
                    }

                    item.InitializeRarity();
                    items.Add(item);
                }

                if (items.Count == 0)
                {
                    string err = "CSV parsed but no valid items found.";
                    Debug.LogError($"[CsvItemLoader] {err}");
                    IsLoaded = true;
                    OnLoadError?.Invoke(err);
                    return;
                }

                Debug.Log($"[CsvItemLoader] Loaded {items.Count} items from CSV.");
                IsLoaded = true;
                OnItemsLoaded?.Invoke(items);
            }
            catch (Exception ex)
            {
                string err = $"CSV parse error: {ex.Message}";
                Debug.LogError($"[CsvItemLoader] {err}");
                IsLoaded = true;
                OnLoadError?.Invoke(err);
            }
        }

        /// <summary>
        /// Splits one CSV line respecting quoted fields.
        /// "Hello, World","Test" → ["Hello, World", "Test"]
        /// </summary>
        private string[] SplitCsvLine(string line)
        {
            var fields = new List<string>();
            bool inQuotes = false;
            var current = new System.Text.StringBuilder();

            for (int i = 0; i < line.Length; i++)
            {
                char c = line[i];

                if (c == '"')
                {
                    // Handle escaped quotes ""
                    if (inQuotes && i + 1 < line.Length && line[i + 1] == '"')
                    {
                        current.Append('"');
                        i++;
                    }
                    else
                    {
                        inQuotes = !inQuotes;
                    }
                }
                else if (c == ',' && !inQuotes)
                {
                    fields.Add(current.ToString());
                    current.Clear();
                }
                else
                {
                    current.Append(c);
                }
            }

            fields.Add(current.ToString());
            return fields.ToArray();
        }

        private string BuildPath()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return Path.Combine(Application.streamingAssetsPath, _fileName);
#elif UNITY_WEBGL && !UNITY_EDITOR
            return Path.Combine(Application.streamingAssetsPath, _fileName);
#else
            return "file://" + Path.Combine(Application.streamingAssetsPath, _fileName);
#endif
        }
    }
}
