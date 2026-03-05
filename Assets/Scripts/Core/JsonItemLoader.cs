// JsonItemLoader.cs
// MonoBehaviour that loads item data from StreamingAssets/Items.json at runtime.
// Implements Option A (JSON Import) from the test requirements.
// Uses a coroutine with UnityWebRequest for WebGL/Android compatibility.

using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UnityEngine.Networking;
using RewardDraft.Core;

namespace RewardDraft.Core
{
    public class JsonItemLoader : MonoBehaviour
    {
        [Header("File path within StreamingAssets")]
        [SerializeField] private string _fileName = "Items.json";

        /// <summary>Raised when items finish loading. Passes the loaded list.</summary>
        public event Action<List<ItemData>> OnItemsLoaded;

        /// <summary>Raised if loading fails. Passes the error message.</summary>
        public event Action<string> OnLoadError;

        /// <summary>True once loading has completed (success or failure).</summary>
        public bool IsLoaded { get; private set; } = false;

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Starts async loading of the JSON file.
        /// Raises OnItemsLoaded or OnLoadError when done.
        /// </summary>
        public void Load()
        {
            StartCoroutine(LoadRoutine());
        }

        // ── Implementation ────────────────────────────────────────────────────

        private IEnumerator LoadRoutine()
        {
            string path = BuildPath();
            Debug.Log($"[JsonItemLoader] Loading from: {path}");

            using UnityWebRequest request = UnityWebRequest.Get(path);
            yield return request.SendWebRequest();

            if (request.result != UnityWebRequest.Result.Success)
            {
                string error = $"Failed to load '{_fileName}': {request.error}";
                Debug.LogError($"[JsonItemLoader] {error}");
                IsLoaded = true;
                OnLoadError?.Invoke(error);
                yield break;
            }

            string json = request.downloadHandler.text;
            ParseAndDispatch(json);
        }

        private void ParseAndDispatch(string json)
        {
            try
            {
                ItemDatabase database = JsonUtility.FromJson<ItemDatabase>(json);

                if (database == null || database.items == null || database.items.Count == 0)
                {
                    string error = "Items.json parsed to empty database. Check file format.";
                    Debug.LogError($"[JsonItemLoader] {error}");
                    IsLoaded = true;
                    OnLoadError?.Invoke(error);
                    return;
                }

                // Initialize rarity enum for each item
                foreach (var item in database.items)
                    item.InitializeRarity();

                Debug.Log($"[JsonItemLoader] Loaded {database.items.Count} items from JSON.");
                IsLoaded = true;
                OnItemsLoaded?.Invoke(database.items);
            }
            catch (Exception ex)
            {
                string error = $"JSON parse error: {ex.Message}";
                Debug.LogError($"[JsonItemLoader] {error}");
                IsLoaded = true;
                OnLoadError?.Invoke(error);
            }
        }

        // ── Path Builder ──────────────────────────────────────────────────────

        /// <summary>
        /// Builds the correct URI for StreamingAssets across platforms.
        /// StreamingAssets requires UnityWebRequest on Android/WebGL.
        /// </summary>
        private string BuildPath()
        {
#if UNITY_ANDROID && !UNITY_EDITOR
            return Path.Combine(Application.streamingAssetsPath, _fileName);
#elif UNITY_WEBGL && !UNITY_EDITOR
            return Path.Combine(Application.streamingAssetsPath, _fileName);
#else
            // Editor, Standalone, iOS: file:// URI is fine
            return "file://" + Path.Combine(Application.streamingAssetsPath, _fileName);
#endif
        }
    }
}
