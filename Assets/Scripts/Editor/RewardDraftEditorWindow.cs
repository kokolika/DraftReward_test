// RewardDraftEditorWindow.cs
// Unity Editor Window — complete control panel for the Reward Draft system.
// Open via: Tools > Reward Draft > Control Panel
//
// Tabs:
//   1. Items      — view, add, edit, delete items. Export to JSON or CSV.
//   2. Settings   — edit LootSettingsSO values with live probability preview.
//   3. Simulator  — run test drafts inside the Editor without pressing Play.
//   4. Run Log    — read the last RunLog.json written at runtime.

using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using RewardDraft.Core;

namespace RewardDraft.Editor
{
    public class RewardDraftEditorWindow : EditorWindow
    {
        // ── Window Entry Point ────────────────────────────────────────────────

        [MenuItem("Tools/Reward Draft/Control Panel")]
        public static void Open()
        {
            var window = GetWindow<RewardDraftEditorWindow>("Reward Draft");
            window.minSize = new Vector2(700, 500);
            window.Show();
        }

        // ── State ─────────────────────────────────────────────────────────────

        private int _tab = 0;
        private readonly string[] _tabNames = { "📦  Items", "⚙️  Settings", "🎲  Simulator", "📋  Run Log" };

        // ── Items tab ─────────────────────────────────────────────────────────
        private List<ItemData>  _items            = new List<ItemData>();
        private Vector2         _itemsScroll;
        private string          _filterText       = "";
        private Rarity?         _filterRarity     = null;
        private bool            _itemsDirty       = false;
        private string          _itemsStatusMsg   = "";
        private Color           _itemsStatusColor = Color.white;

        // New item form
        private ItemData _newItem = new ItemData { rarity = "Common" };
        private bool     _showAddForm = false;

        // Edit
        private int      _editingIndex = -1;
        private ItemData _editingItem  = null;

        // ── Settings tab ─────────────────────────────────────────────────────
        private LootSettingsSO _lootSettings;
        private SerializedObject _serializedSettings;

        // ── Simulator tab ─────────────────────────────────────────────────────
        private List<List<ItemData>> _simulatorHistory = new List<List<ItemData>>();
        private int    _simulatorRolls   = 1;
        private int?   _simulatorSeed    = null;
        private bool   _useSeed          = false;
        private string _seedText         = "42";
        private Vector2 _simScroll;
        private Dictionary<string, int> _rarityCount = new Dictionary<string, int>();

        // ── Run Log tab ───────────────────────────────────────────────────────
        private string  _runLogText   = "";
        private Vector2 _logScroll;
        private string  _logPath      = "";

        // ── Styles ────────────────────────────────────────────────────────────
        private GUIStyle _headerStyle;
        private GUIStyle _cardStyle;
        private GUIStyle _statusStyle;
        private bool     _stylesInitialized = false;

        // ── Rarity Colors ─────────────────────────────────────────────────────
        private static readonly Color ColCommon    = new Color(0.70f, 0.70f, 0.70f);
        private static readonly Color ColRare      = new Color(0.30f, 0.55f, 0.95f);
        private static readonly Color ColEpic      = new Color(0.70f, 0.30f, 0.95f);
        private static readonly Color ColLegendary = new Color(1.00f, 0.70f, 0.10f);

        // ─────────────────────────────────────────────────────────────────────

        private void OnEnable()
        {
            LoadItemsFromJson();
            FindLootSettings();
            _logPath = Path.Combine(Application.persistentDataPath, "RunLog.json");
        }

        private void InitStyles()
        {
            if (_stylesInitialized) return;

            _headerStyle = new GUIStyle(EditorStyles.boldLabel)
            {
                fontSize  = 13,
                alignment = TextAnchor.MiddleLeft,
                padding   = new RectOffset(4, 0, 6, 6)
            };

            _cardStyle = new GUIStyle(GUI.skin.box)
            {
                padding = new RectOffset(8, 8, 6, 6),
                margin  = new RectOffset(0, 0, 2, 2)
            };

            _statusStyle = new GUIStyle(EditorStyles.helpBox)
            {
                fontSize  = 11,
                alignment = TextAnchor.MiddleLeft,
                padding   = new RectOffset(8, 8, 4, 4)
            };

            _stylesInitialized = true;
        }

        // ══════════════════════════════════════════════════════════════════════
        // MAIN DRAW
        // ══════════════════════════════════════════════════════════════════════

        private void OnGUI()
        {
            InitStyles();

            // Tab bar
            EditorGUILayout.Space(4);
            _tab = GUILayout.Toolbar(_tab, _tabNames, GUILayout.Height(32));
            EditorGUILayout.Space(4);

            switch (_tab)
            {
                case 0: DrawItemsTab();    break;
                case 1: DrawSettingsTab(); break;
                case 2: DrawSimulator();   break;
                case 3: DrawRunLog();      break;
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // TAB 1 — ITEMS
        // ══════════════════════════════════════════════════════════════════════

        private void DrawItemsTab()
        {
            // ── Toolbar ───────────────────────────────────────────────────────
            EditorGUILayout.BeginHorizontal(EditorStyles.toolbar);

            GUILayout.Label("🔍", GUILayout.Width(18));
            _filterText = EditorGUILayout.TextField(_filterText, EditorStyles.toolbarSearchField, GUILayout.Width(180));

            GUILayout.Space(8);
            GUILayout.Label("Rarity:", GUILayout.Width(44));
            string[] rarityOptions = { "All", "Common", "Rare", "Epic", "Legendary" };
            int currentFilter = _filterRarity == null ? 0 : (int)_filterRarity.Value + 1;
            int newFilter = EditorGUILayout.Popup(currentFilter, rarityOptions, EditorStyles.toolbarPopup, GUILayout.Width(90));
            _filterRarity = newFilter == 0 ? (Rarity?)null : (Rarity)(newFilter - 1);

            GUILayout.FlexibleSpace();

            // Item counts
            GUI.color = Color.gray;
            GUILayout.Label($"{_items.Count} items  |  " +
                            $"C:{_items.Count(i => i.rarityEnum == Rarity.Common)}  " +
                            $"R:{_items.Count(i => i.rarityEnum == Rarity.Rare)}  " +
                            $"E:{_items.Count(i => i.rarityEnum == Rarity.Epic)}  " +
                            $"L:{_items.Count(i => i.rarityEnum == Rarity.Legendary)}",
                            EditorStyles.toolbarButton);
            GUI.color = Color.white;

            EditorGUILayout.EndHorizontal();

            // ── Action Buttons ────────────────────────────────────────────────
            EditorGUILayout.BeginHorizontal();
            if (GUILayout.Button("➕  Add Item", GUILayout.Height(28)))
            {
                _showAddForm   = !_showAddForm;
                _newItem       = new ItemData { rarity = "Common" };
                _newItem.InitializeRarity();
            }

            GUI.enabled = _itemsDirty;
            if (GUILayout.Button("💾  Save to JSON", GUILayout.Height(28)))
                SaveItemsToJson();
            if (GUILayout.Button("💾  Save to CSV", GUILayout.Height(28)))
                SaveItemsToCsv();
            GUI.enabled = true;

            if (GUILayout.Button("🔄  Reload", GUILayout.Height(28)))
                LoadItemsFromJson();

            EditorGUILayout.EndHorizontal();

            // ── Status Message ────────────────────────────────────────────────
            if (!string.IsNullOrEmpty(_itemsStatusMsg))
            {
                GUI.color = _itemsStatusColor;
                GUILayout.Label(_itemsStatusMsg, _statusStyle);
                GUI.color = Color.white;
            }

            // ── Add Form ──────────────────────────────────────────────────────
            if (_showAddForm)
            {
                EditorGUILayout.Space(4);
                using (new EditorGUILayout.VerticalScope(_cardStyle))
                {
                    GUILayout.Label("NEW ITEM", _headerStyle);
                    _newItem.id          = EditorGUILayout.TextField("ID",          _newItem.id ?? "");
                    _newItem.displayName = EditorGUILayout.TextField("Display Name",_newItem.displayName ?? "");
                    _newItem.description = EditorGUILayout.TextField("Description", _newItem.description ?? "");

                    int rarIdx = Mathf.Max(0, Array.IndexOf(Enum.GetNames(typeof(Rarity)), _newItem.rarity));
                    rarIdx = EditorGUILayout.Popup("Rarity", rarIdx, Enum.GetNames(typeof(Rarity)));
                    _newItem.rarity     = Enum.GetNames(typeof(Rarity))[rarIdx];
                    _newItem.InitializeRarity();

                    EditorGUILayout.BeginHorizontal();
                    if (GUILayout.Button("✅  Add", GUILayout.Height(26)))
                    {
                        if (string.IsNullOrEmpty(_newItem.id))
                        {
                            SetStatus("ID cannot be empty.", Color.red);
                        }
                        else if (_items.Any(i => i.id == _newItem.id))
                        {
                            SetStatus($"ID '{_newItem.id}' already exists.", Color.red);
                        }
                        else
                        {
                            _items.Add(_newItem);
                            _newItem     = new ItemData { rarity = "Common" };
                            _showAddForm = false;
                            _itemsDirty  = true;
                            SetStatus("Item added. Click Save to write to file.", Color.yellow);
                        }
                    }
                    if (GUILayout.Button("✖  Cancel", GUILayout.Height(26)))
                        _showAddForm = false;
                    EditorGUILayout.EndHorizontal();
                }
                EditorGUILayout.Space(4);
            }

            // ── Item List ─────────────────────────────────────────────────────
            DrawColumnHeaders();

            _itemsScroll = EditorGUILayout.BeginScrollView(_itemsScroll);

            var filtered = _items.Where(item =>
                (string.IsNullOrEmpty(_filterText) ||
                 item.displayName.IndexOf(_filterText, StringComparison.OrdinalIgnoreCase) >= 0 ||
                 item.id.IndexOf(_filterText, StringComparison.OrdinalIgnoreCase) >= 0) &&
                (_filterRarity == null || item.rarityEnum == _filterRarity.Value)
            ).ToList();

            for (int i = 0; i < filtered.Count; i++)
            {
                ItemData item      = filtered[i];
                int realIndex = _items.IndexOf(item);
                bool isEditing = _editingIndex == realIndex;

                GUI.backgroundColor = GetRarityBg(item.rarityEnum);
                using (new EditorGUILayout.HorizontalScope(_cardStyle))
                {
                    GUI.backgroundColor = Color.white;

                    if (isEditing)
                    {
                        // ── Edit row ──
                        _editingItem.id          = EditorGUILayout.TextField(_editingItem.id, GUILayout.Width(130));
                        _editingItem.displayName = EditorGUILayout.TextField(_editingItem.displayName, GUILayout.Width(150));
                        _editingItem.description = EditorGUILayout.TextField(_editingItem.description, GUILayout.Width(200));

                        int rarIdx = Mathf.Max(0, Array.IndexOf(Enum.GetNames(typeof(Rarity)), _editingItem.rarity));
                        rarIdx = EditorGUILayout.Popup(rarIdx, Enum.GetNames(typeof(Rarity)), GUILayout.Width(80));
                        _editingItem.rarity = Enum.GetNames(typeof(Rarity))[rarIdx];
                        _editingItem.InitializeRarity();

                        if (GUILayout.Button("✅", GUILayout.Width(28), GUILayout.Height(20)))
                        {
                            _items[realIndex] = _editingItem;
                            _editingIndex     = -1;
                            _itemsDirty       = true;
                            SetStatus("Item updated. Click Save to write to file.", Color.yellow);
                        }
                        if (GUILayout.Button("✖", GUILayout.Width(28), GUILayout.Height(20)))
                            _editingIndex = -1;
                    }
                    else
                    {
                        // ── Display row ──
                        GUI.color = GetRarityColor(item.rarityEnum);
                        GUILayout.Label(item.id,          GUILayout.Width(130));
                        GUILayout.Label(item.displayName, GUILayout.Width(150));
                        GUI.color = Color.white;
                        GUILayout.Label(item.description, GUILayout.Width(200));

                        // Rarity badge
                        GUI.backgroundColor = GetRarityColor(item.rarityEnum);
                        GUI.color           = Color.white;
                        GUILayout.Label(item.rarityEnum.ToString(), EditorStyles.miniButton, GUILayout.Width(80));
                        GUI.backgroundColor = Color.white;

                        GUILayout.FlexibleSpace();
                        if (GUILayout.Button("✏️", GUILayout.Width(28), GUILayout.Height(20)))
                        {
                            _editingIndex = realIndex;
                            _editingItem  = new ItemData
                            {
                                id          = item.id,
                                displayName = item.displayName,
                                description = item.description,
                                rarity      = item.rarity,
                                rarityEnum  = item.rarityEnum
                            };
                        }
                        GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                        if (GUILayout.Button("🗑", GUILayout.Width(28), GUILayout.Height(20)))
                        {
                            if (EditorUtility.DisplayDialog("Delete Item",
                                $"Delete '{item.displayName}'?", "Delete", "Cancel"))
                            {
                                _items.RemoveAt(realIndex);
                                _itemsDirty = true;
                                SetStatus("Item deleted. Click Save to write to file.", Color.yellow);
                            }
                        }
                        GUI.backgroundColor = Color.white;
                    }
                }
            }

            EditorGUILayout.EndScrollView();

            // ── Dirty warning ─────────────────────────────────────────────────
            if (_itemsDirty)
            {
                GUI.color = Color.yellow;
                EditorGUILayout.HelpBox("⚠  Unsaved changes — click Save to JSON or Save to CSV to persist.", MessageType.Warning);
                GUI.color = Color.white;
            }
        }

        private void DrawColumnHeaders()
        {
            using (new EditorGUILayout.HorizontalScope(EditorStyles.toolbar))
            {
                GUILayout.Label("ID",           EditorStyles.toolbarButton, GUILayout.Width(130));
                GUILayout.Label("Display Name", EditorStyles.toolbarButton, GUILayout.Width(150));
                GUILayout.Label("Description",  EditorStyles.toolbarButton, GUILayout.Width(200));
                GUILayout.Label("Rarity",       EditorStyles.toolbarButton, GUILayout.Width(80));
                GUILayout.FlexibleSpace();
                GUILayout.Label("Actions",      EditorStyles.toolbarButton, GUILayout.Width(62));
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // TAB 2 — SETTINGS
        // ══════════════════════════════════════════════════════════════════════

        private void DrawSettingsTab()
        {
            if (_lootSettings == null)
            {
                EditorGUILayout.HelpBox("No LootSettingsSO found in the project.\n" +
                    "Create one via: Right-click > Create > RewardDraft > Loot Settings",
                    MessageType.Warning);
                if (GUILayout.Button("Create LootSettings Asset", GUILayout.Height(32)))
                    CreateLootSettingsAsset();
                return;
            }

            _serializedSettings ??= new SerializedObject(_lootSettings);
            _serializedSettings.Update();

            GUILayout.Label("LOOT SETTINGS", _headerStyle);

            // ── Draft Config ──────────────────────────────────────────────────
            using (new EditorGUILayout.VerticalScope(_cardStyle))
            {
                GUILayout.Label("Draft Configuration", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_serializedSettings.FindProperty("numberOfChoices"),
                    new GUIContent("Reward Choices"));
                EditorGUILayout.PropertyField(_serializedSettings.FindProperty("preventDuplicates"),
                    new GUIContent("Prevent Duplicates"));
            }

            EditorGUILayout.Space(6);

            // ── Rarity Weights ────────────────────────────────────────────────
            using (new EditorGUILayout.VerticalScope(_cardStyle))
            {
                GUILayout.Label("Rarity Weights", EditorStyles.boldLabel);

                float c = _lootSettings.commonWeight;
                float r = _lootSettings.rareWeight;
                float e = _lootSettings.epicWeight;
                float l = _lootSettings.legendaryWeight;
                float total = c + r + e + l;

                DrawWeightSlider("Common",    ref c, ColCommon,    total);
                DrawWeightSlider("Rare",      ref r, ColRare,      total);
                DrawWeightSlider("Epic",      ref e, ColEpic,      total);
                DrawWeightSlider("Legendary", ref l, ColLegendary, total);

                if (GUI.changed)
                {
                    _lootSettings.commonWeight    = c;
                    _lootSettings.rareWeight      = r;
                    _lootSettings.epicWeight      = e;
                    _lootSettings.legendaryWeight = l;
                    EditorUtility.SetDirty(_lootSettings);
                }

                EditorGUILayout.Space(4);

                // Probability bar
                if (total > 0f)
                {
                    GUILayout.Label("Probability Distribution:", EditorStyles.miniLabel);
                    DrawProbabilityBar(c/total, r/total, e/total, l/total);
                    EditorGUILayout.Space(2);
                    using (new EditorGUILayout.HorizontalScope())
                    {
                        DrawLegendItem("Common",    ColCommon,    c/total);
                        DrawLegendItem("Rare",      ColRare,      r/total);
                        DrawLegendItem("Epic",      ColEpic,      e/total);
                        DrawLegendItem("Legendary", ColLegendary, l/total);
                    }
                }
            }

            EditorGUILayout.Space(6);

            // ── Pity System ───────────────────────────────────────────────────
            using (new EditorGUILayout.VerticalScope(_cardStyle))
            {
                GUILayout.Label("Pity System (Bonus Feature)", EditorStyles.boldLabel);
                EditorGUILayout.PropertyField(_serializedSettings.FindProperty("usePitySystem"),
                    new GUIContent("Enable Pity System"));

                if (_lootSettings.usePitySystem)
                {
                    EditorGUILayout.PropertyField(_serializedSettings.FindProperty("pityThreshold"),
                        new GUIContent("Activation Threshold", "Bonus starts after this many non-Rare+ rolls"));
                    EditorGUILayout.PropertyField(_serializedSettings.FindProperty("pityBonusPerRoll"),
                        new GUIContent("Bonus Per Roll (%)", "Added to Rare+ weight per roll past threshold"));
                    EditorGUILayout.PropertyField(_serializedSettings.FindProperty("pityBonusCap"),
                        new GUIContent("Maximum Bonus (%)", "Cap on total pity bonus applied"));

                    EditorGUILayout.Space(4);
                    float maxBonus = _lootSettings.pityBonusCap;
                    float thresh   = _lootSettings.pityThreshold;
                    float rate     = _lootSettings.pityBonusPerRoll;
                    int rollsToCap = rate > 0 ? Mathf.CeilToInt(maxBonus / rate) : 999;

                    EditorGUILayout.HelpBox(
                        $"Pity activates after {thresh} consecutive non-Rare+ rolls.\n" +
                        $"Rare+ weight increases by {rate}% per roll.\n" +
                        $"Bonus caps at +{maxBonus}% after {rollsToCap} rolls past threshold.\n" +
                        $"Total rolls before hard cap: {thresh + rollsToCap}",
                        MessageType.Info);
                }
            }

            _serializedSettings.ApplyModifiedProperties();

            EditorGUILayout.Space(6);
            if (GUILayout.Button("💾  Save Settings", GUILayout.Height(30)))
            {
                AssetDatabase.SaveAssets();
                SetStatus("Settings saved.", Color.green);
            }
        }

        private void DrawWeightSlider(string label, ref float value, Color col, float total)
        {
            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.color = col;
                GUILayout.Label(label, GUILayout.Width(80));
                GUI.color = Color.white;
                value     = EditorGUILayout.Slider(value, 0f, 100f);
                float pct = total > 0 ? value / total * 100f : 0f;
                GUI.color = Color.gray;
                GUILayout.Label($"{pct:0.0}%", GUILayout.Width(48));
                GUI.color = Color.white;
            }
        }

        private void DrawProbabilityBar(float c, float r, float e, float l)
        {
            Rect rect = GUILayoutUtility.GetRect(0, 18, GUILayout.ExpandWidth(true));
            float x = rect.x;

            void DrawSegment(float pct, Color col)
            {
                float w = rect.width * pct;
                EditorGUI.DrawRect(new Rect(x, rect.y, w, rect.height), col);
                x += w;
            }

            DrawSegment(c, ColCommon);
            DrawSegment(r, ColRare);
            DrawSegment(e, ColEpic);
            DrawSegment(l, ColLegendary);
        }

        private void DrawLegendItem(string label, Color col, float pct)
        {
            GUI.color = col;
            GUILayout.Label($"■ {label} {pct*100:0.0}%", EditorStyles.miniLabel);
            GUI.color = Color.white;
        }

        // ══════════════════════════════════════════════════════════════════════
        // TAB 3 — SIMULATOR
        // ══════════════════════════════════════════════════════════════════════

        private void DrawSimulator()
        {
            GUILayout.Label("DRAFT SIMULATOR", _headerStyle);

            using (new EditorGUILayout.VerticalScope(_cardStyle))
            {
                using (new EditorGUILayout.HorizontalScope())
                {
                    GUILayout.Label("Rolls to simulate:", GUILayout.Width(130));
                    _simulatorRolls = EditorGUILayout.IntSlider(_simulatorRolls, 1, 20);
                }

                using (new EditorGUILayout.HorizontalScope())
                {
                    _useSeed = EditorGUILayout.Toggle("Use Seed", _useSeed, GUILayout.Width(130));
                    GUI.enabled = _useSeed;
                    _seedText = EditorGUILayout.TextField(_seedText, GUILayout.Width(80));
                    GUI.enabled = true;
                    GUI.color = Color.gray;
                    GUILayout.Label("(same seed = same results every time)", EditorStyles.miniLabel);
                    GUI.color = Color.white;
                }

                EditorGUILayout.Space(4);

                if (_lootSettings == null)
                    EditorGUILayout.HelpBox("Assign a LootSettingsSO first (Settings tab).", MessageType.Warning);
                else if (_items.Count == 0)
                    EditorGUILayout.HelpBox("No items loaded. Check Items tab.", MessageType.Warning);
                else if (GUILayout.Button("▶  Run Simulation", GUILayout.Height(32)))
                    RunSimulation();
            }

            if (_simulatorHistory.Count > 0)
            {
                EditorGUILayout.Space(4);

                // Stats bar
                using (new EditorGUILayout.HorizontalScope(_cardStyle))
                {
                    int total = _simulatorHistory.Sum(d => d.Count);
                    GUILayout.Label($"Total items rolled: {total}", EditorStyles.boldLabel);
                    GUILayout.FlexibleSpace();
                    foreach (var kvp in _rarityCount)
                    {
                        Rarity r = (Rarity)Enum.Parse(typeof(Rarity), kvp.Key);
                        GUI.color = GetRarityColor(r);
                        GUILayout.Label($"{kvp.Key}: {kvp.Value} ({kvp.Value * 100f / total:0.0}%)",
                            EditorStyles.miniLabel, GUILayout.Width(130));
                        GUI.color = Color.white;
                    }
                }

                EditorGUILayout.Space(2);
                GUILayout.Label("Results:", EditorStyles.boldLabel);

                _simScroll = EditorGUILayout.BeginScrollView(_simScroll);

                for (int roll = 0; roll < _simulatorHistory.Count; roll++)
                {
                    using (new EditorGUILayout.VerticalScope(_cardStyle))
                    {
                        GUI.color = Color.gray;
                        GUILayout.Label($"DRAFT #{roll + 1}", EditorStyles.miniLabel);
                        GUI.color = Color.white;

                        using (new EditorGUILayout.HorizontalScope())
                        {
                            foreach (var item in _simulatorHistory[roll])
                            {
                                GUI.backgroundColor = GetRarityBg(item.rarityEnum);
                                using (new EditorGUILayout.VerticalScope(_cardStyle, GUILayout.Width(190)))
                                {
                                    GUI.backgroundColor = Color.white;
                                    GUI.color = GetRarityColor(item.rarityEnum);
                                    GUILayout.Label(item.displayName, EditorStyles.boldLabel);
                                    GUI.color = Color.white;
                                    GUILayout.Label(item.description, EditorStyles.wordWrappedMiniLabel);
                                    GUI.color = GetRarityColor(item.rarityEnum);
                                    GUILayout.Label(item.rarityEnum.ToString().ToUpper(), EditorStyles.miniLabel);
                                    GUI.color = Color.white;
                                }
                            }
                        }
                    }
                }

                EditorGUILayout.EndScrollView();

                if (GUILayout.Button("🗑  Clear Results", GUILayout.Height(26)))
                {
                    _simulatorHistory.Clear();
                    _rarityCount.Clear();
                }
            }
        }

        private void RunSimulation()
        {
            _simulatorHistory.Clear();
            _rarityCount.Clear();

            int? seed = _useSeed && int.TryParse(_seedText, out int s) ? s : (int?)null;
            var service = new LootService(_lootSettings, seed);
            service.LoadItems(new List<ItemData>(_items));

            for (int i = 0; i < _simulatorRolls; i++)
            {
                var draft = service.GenerateDraft();
                _simulatorHistory.Add(draft);

                foreach (var item in draft)
                {
                    string key = item.rarityEnum.ToString();
                    if (!_rarityCount.ContainsKey(key)) _rarityCount[key] = 0;
                    _rarityCount[key]++;
                }
            }
        }

        // ══════════════════════════════════════════════════════════════════════
        // TAB 4 — RUN LOG
        // ══════════════════════════════════════════════════════════════════════

        private void DrawRunLog()
        {
            GUILayout.Label("RUN LOG", _headerStyle);

            using (new EditorGUILayout.HorizontalScope())
            {
                GUI.color = Color.gray;
                GUILayout.Label(_logPath.Length > 0 ? _logPath : "Path unknown", EditorStyles.miniLabel);
                GUI.color = Color.white;
            }

            EditorGUILayout.Space(4);

            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("🔄  Refresh", GUILayout.Height(28)))
                    LoadRunLog();
                if (GUILayout.Button("📂  Open Folder", GUILayout.Height(28)))
                    EditorUtility.RevealInFinder(_logPath);
                GUI.backgroundColor = new Color(1f, 0.4f, 0.4f);
                if (GUILayout.Button("🗑  Delete Log", GUILayout.Height(28)))
                {
                    if (File.Exists(_logPath) &&
                        EditorUtility.DisplayDialog("Delete Run Log", "Delete RunLog.json?", "Delete", "Cancel"))
                    {
                        File.Delete(_logPath);
                        _runLogText = "Log deleted.";
                    }
                }
                GUI.backgroundColor = Color.white;
            }

            EditorGUILayout.Space(4);

            if (string.IsNullOrEmpty(_runLogText))
                LoadRunLog();

            _logScroll = EditorGUILayout.BeginScrollView(_logScroll);
            EditorGUILayout.TextArea(_runLogText,
                GUILayout.ExpandHeight(true), GUILayout.ExpandWidth(true));
            EditorGUILayout.EndScrollView();
        }

        private void LoadRunLog()
        {
            _logPath = Path.Combine(Application.persistentDataPath, "RunLog.json");
            _runLogText = File.Exists(_logPath)
                ? File.ReadAllText(_logPath)
                : $"No RunLog.json found at:\n{_logPath}\n\nPress Play and pick some items to generate the log.";
        }

        // ══════════════════════════════════════════════════════════════════════
        // DATA — Load / Save
        // ══════════════════════════════════════════════════════════════════════

        private void LoadItemsFromJson()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "Items.json");
            if (!File.Exists(path))
            {
                SetStatus($"Items.json not found at: {path}", Color.red);
                return;
            }

            try
            {
                string json = File.ReadAllText(path);
                var db = JsonUtility.FromJson<ItemDatabase>(json);
                _items = db?.items ?? new List<ItemData>();
                foreach (var item in _items) item.InitializeRarity();
                _itemsDirty = false;
                SetStatus($"Loaded {_items.Count} items from Items.json", Color.green);
            }
            catch (Exception ex)
            {
                SetStatus($"Parse error: {ex.Message}", Color.red);
            }
        }

        private void SaveItemsToJson()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "Items.json");
            try
            {
                // Sync rarity string from enum before saving
                foreach (var item in _items) item.rarity = item.rarityEnum.ToString();

                var db   = new ItemDatabase { items = _items };
                string json = JsonUtility.ToJson(db, prettyPrint: true);
                File.WriteAllText(path, json);
                AssetDatabase.Refresh();
                _itemsDirty = false;
                SetStatus($"Saved {_items.Count} items to Items.json", Color.green);
            }
            catch (Exception ex)
            {
                SetStatus($"Save error: {ex.Message}", Color.red);
            }
        }

        private void SaveItemsToCsv()
        {
            string path = Path.Combine(Application.streamingAssetsPath, "Items.csv");
            try
            {
                var sb = new StringBuilder();
                sb.AppendLine("id,displayName,description,rarity");
                foreach (var item in _items)
                {
                    string desc = item.description.Contains(',')
                        ? $"\"{item.description.Replace("\"", "\"\"")}\"" : item.description;
                    sb.AppendLine($"{item.id},{item.displayName},{desc},{item.rarityEnum}");
                }
                File.WriteAllText(path, sb.ToString());
                AssetDatabase.Refresh();
                _itemsDirty = false;
                SetStatus($"Saved {_items.Count} items to Items.csv", Color.green);
            }
            catch (Exception ex)
            {
                SetStatus($"CSV save error: {ex.Message}", Color.red);
            }
        }

        private void FindLootSettings()
        {
            string[] guids = AssetDatabase.FindAssets("t:LootSettingsSO");
            if (guids.Length > 0)
            {
                string assetPath = AssetDatabase.GUIDToAssetPath(guids[0]);
                _lootSettings = AssetDatabase.LoadAssetAtPath<LootSettingsSO>(assetPath);
            }
        }

        private void CreateLootSettingsAsset()
        {
            string dir = "Assets/Resources/LootSettings";
            if (!Directory.Exists(dir)) Directory.CreateDirectory(dir);

            var so = CreateInstance<LootSettingsSO>();
            AssetDatabase.CreateAsset(so, $"{dir}/LootSettings.asset");
            AssetDatabase.SaveAssets();
            FindLootSettings();
            SetStatus("LootSettings.asset created.", Color.green);
        }

        // ══════════════════════════════════════════════════════════════════════
        // HELPERS
        // ══════════════════════════════════════════════════════════════════════

        private void SetStatus(string msg, Color color)
        {
            _itemsStatusMsg   = msg;
            _itemsStatusColor = color;
        }

        private Color GetRarityColor(Rarity r) => r switch
        {
            Rarity.Common    => ColCommon,
            Rarity.Rare      => ColRare,
            Rarity.Epic      => ColEpic,
            Rarity.Legendary => ColLegendary,
            _                => Color.white
        };

        private Color GetRarityBg(Rarity r) => r switch
        {
            Rarity.Common    => new Color(0.85f, 0.85f, 0.85f, 0.15f),
            Rarity.Rare      => new Color(0.30f, 0.55f, 0.95f, 0.15f),
            Rarity.Epic      => new Color(0.65f, 0.25f, 0.90f, 0.15f),
            Rarity.Legendary => new Color(1.00f, 0.70f, 0.10f, 0.20f),
            _                => Color.clear
        };
    }
}
