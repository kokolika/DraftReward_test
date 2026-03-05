// RewardDraftController.cs
// The central MonoBehaviour that wires everything together.
// Owns: LootService, InventoryModel, RunLogger.
// Drives: JsonItemLoader (data), RewardCardUI × N (view), InventoryUI (view).
//
// Lifecycle:
//   Awake  → create models
//   Start  → load JSON, bind UI
//   User presses Roll → GenerateDraft → populate cards
//   User picks card   → add to inventory → log → re-enable Roll button

using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RewardDraft.Core;
using RewardDraft.UI;

namespace RewardDraft
{
    public class RewardDraftController : MonoBehaviour
    {
        // ── Inspector References ──────────────────────────────────────────────

        [Header("ScriptableObject Configuration")]
        [SerializeField] private LootSettingsSO _lootSettings;

        [Header("Data")]
        [SerializeField] private JsonItemLoader _itemLoader;

        [Header("UI — Draft")]
        [SerializeField] private GameObject _draftPanel;    // parent panel to show/hide
        [SerializeField] private Button _rollButton;
        [SerializeField] private TextMeshProUGUI _rollButtonText;
        [SerializeField] private List<RewardCardUI> _rewardCards;  // exactly 3

        [Header("UI — Inventory")]
        [SerializeField] private InventoryUI _inventoryUI;

        [Header("UI — Status")]
        [SerializeField] private TextMeshProUGUI _statusText;
        [SerializeField] private TextMeshProUGUI _pityText;

        // ── Internal state ────────────────────────────────────────────────────

        private LootService _lootService;
        private InventoryModel _inventory;
        private RunLogger _runLogger;

        private List<ItemData> _currentDraft = new List<ItemData>();
        private int _phaseIndex = 0;
        private bool _isReady = false;

        // ── Unity Lifecycle ───────────────────────────────────────────────────

        private void Awake()
        {
            ValidateReferences();

            // Create pure-logic objects
            _inventory = new InventoryModel();
            _runLogger = new RunLogger();

            // Disable roll button until items are loaded
            SetRollInteractable(false);
        }

        private void Start()
        {
            // Bind inventory model to its UI
            _inventoryUI.BindModel(_inventory);

            // Subscribe to card events
            foreach (var card in _rewardCards)
                card.OnPicked += HandleCardPicked;

            // Start JSON load
            SetStatus("Loading items...");
            _itemLoader.OnItemsLoaded += HandleItemsLoaded;
            _itemLoader.OnLoadError += HandleLoadError;
            _itemLoader.Load();
        }

        // ── Load Handlers ─────────────────────────────────────────────────────

        private void HandleItemsLoaded(List<ItemData> items)
        {
            _lootService = new LootService(_lootSettings);
            _lootService.LoadItems(items);

            _isReady = true;
            SetRollInteractable(true);
            SetStatus($"Ready. {items.Count} items loaded.");
            SetDraftPanelVisible(false);
            Debug.Log($"[RewardDraftController] Items loaded. Run log will be saved to: {_runLogger.OutputPath}");
        }

        private void HandleLoadError(string error)
        {
            SetStatus($"ERROR: {error}");
            Debug.LogError($"[RewardDraftController] Load error: {error}");
        }

        // ── Roll Button ───────────────────────────────────────────────────────

        public void OnRollButtonClicked()
        {
            if (!_isReady) return;

            _phaseIndex++;
            SetStatus($"Phase {_phaseIndex} — Pick one reward.");

            try
            {
                _currentDraft = _lootService.GenerateDraft();
            }
            catch (System.Exception ex)
            {
                SetStatus($"Draft error: {ex.Message}");
                Debug.LogError($"[RewardDraftController] GenerateDraft failed: {ex}");
                return;
            }

            // Populate cards
            for (int i = 0; i < _rewardCards.Count; i++)
            {
                if (i < _currentDraft.Count)
                    _rewardCards[i].Populate(_currentDraft[i]);
                else
                    _rewardCards[i].Hide();
            }

            // Show the draft panel
            SetDraftPanelVisible(true);

            // Disable roll button until player picks
            SetRollInteractable(false);
            UpdatePityDisplay();
        }

        // ── Card Pick Handler ─────────────────────────────────────────────────

        private void HandleCardPicked(ItemData item)
        {
            // Add to inventory
            _inventory.Add(item);

            // Log the phase
            _runLogger.RecordPhase(_phaseIndex, _currentDraft, item, _inventory.Items);

            // Update status
            SetStatus($"Picked: {item.displayName} [{item.rarityEnum}]. Roll again!");

            // Hide the draft panel
            SetDraftPanelVisible(false);

            // Re-enable roll button for next phase
            SetRollInteractable(true);
            UpdatePityDisplay();
        }

        // ── UI Helpers ────────────────────────────────────────────────────────

        private void SetDraftPanelVisible(bool visible)
        {
            if (_draftPanel != null)
                _draftPanel.SetActive(visible);
        }

        private void SetRollInteractable(bool interactable)
        {
            _rollButton.interactable = interactable;
            if (_rollButtonText != null)
                _rollButtonText.text = interactable ? "Roll Rewards" : "Pick a Reward...";
        }

        private void SetStatus(string message)
        {
            if (_statusText != null)
                _statusText.text = message;
        }

        private void HideAllCards()
        {
            foreach (var card in _rewardCards)
                card.Hide();
        }

        private void UpdatePityDisplay()
        {
            if (_pityText == null || _lootService == null) return;

            if (!_lootSettings.usePitySystem)
            {
                _pityText.text = "";
                return;
            }

            float bonus = _lootService.CurrentPityBonus;
            int streak = _lootService.PityStreak;

            if (bonus > 0f)
                _pityText.text = $"Pity: streak {streak}  /  +{bonus:0}% Rare+ bonus";
            else if (streak > 0)
                _pityText.text = $"Pity: streak {streak}  (bonus starts at {_lootSettings.pityThreshold})";
            else
                _pityText.text = "Pity: inactive";
        }

        // ── Validation ────────────────────────────────────────────────────────

        private void ValidateReferences()
        {
            if (_lootSettings == null)
                Debug.LogError("[RewardDraftController] LootSettingsSO not assigned!");
            if (_itemLoader == null)
                Debug.LogError("[RewardDraftController] JsonItemLoader not assigned!");
            if (_rewardCards == null || _rewardCards.Count == 0)
                Debug.LogError("[RewardDraftController] No RewardCardUI references assigned!");
            if (_inventoryUI == null)
                Debug.LogError("[RewardDraftController] InventoryUI not assigned!");
            if (_rollButton == null)
                Debug.LogError("[RewardDraftController] Roll Button not assigned!");
            if (_draftPanel == null)
                Debug.LogWarning("[RewardDraftController] DraftPanel not assigned — panel show/hide will be skipped.");
        }
    }
}