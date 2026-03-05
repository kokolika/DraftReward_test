// RewardCardUI.cs
// Controls a single reward card panel in the draft UI.
// Displays item name, description, rarity, and a Pick button.
// Fires OnPicked event when the player selects this item.
// No loot logic here — this is purely presentation.

using System;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RewardDraft.Core;

namespace RewardDraft.UI
{
    public class RewardCardUI : MonoBehaviour
    {
        [Header("Text Fields (TextMeshPro)")]
        [SerializeField] private TextMeshProUGUI _nameText;
        [SerializeField] private TextMeshProUGUI _descriptionText;
        [SerializeField] private TextMeshProUGUI _rarityText;

        [Header("Interaction")]
        [SerializeField] private Button _pickButton;
        [SerializeField] private Image  _cardBackground;

        [Header("Rarity Colors")]
        [SerializeField] private Color _colorCommon    = new Color(0.85f, 0.85f, 0.85f);
        [SerializeField] private Color _colorRare      = new Color(0.30f, 0.55f, 0.95f);
        [SerializeField] private Color _colorEpic      = new Color(0.65f, 0.25f, 0.90f);
        [SerializeField] private Color _colorLegendary = new Color(1.00f, 0.70f, 0.10f);

        // ── State ─────────────────────────────────────────────────────────────
        private ItemData _currentItem;

        /// <summary>Raised when the player clicks Pick on this card. Passes the item.</summary>
        public event Action<ItemData> OnPicked;

        // ── Lifecycle ─────────────────────────────────────────────────────────

        private void Awake()
        {
            _pickButton.onClick.AddListener(HandlePickClicked);
        }

        // ── Public API ────────────────────────────────────────────────────────

        /// <summary>
        /// Populates the card with item data and enables interaction.
        /// </summary>
        public void Populate(ItemData item)
        {
            _currentItem = item ?? throw new ArgumentNullException(nameof(item));

            _nameText.text        = item.displayName;
            _descriptionText.text = item.description;
            _rarityText.text      = item.rarityEnum.ToString().ToUpper();

            // Apply rarity color to background and rarity label
            Color c = GetRarityColor(item.rarityEnum);
            if (_cardBackground != null)
                _cardBackground.color = new Color(c.r, c.g, c.b, 0.18f); // subtle tint
            _rarityText.color = c;

            gameObject.SetActive(true);
            SetInteractable(true);
        }

        /// <summary>Hides the card and clears its content.</summary>
        public void Hide()
        {
            gameObject.SetActive(false);
            _currentItem = null;
        }

        /// <summary>Enables or disables the Pick button (called after a pick is made).</summary>
        public void SetInteractable(bool interactable)
        {
            _pickButton.interactable = interactable;
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        private void HandlePickClicked()
        {
            if (_currentItem == null) return;
            OnPicked?.Invoke(_currentItem);
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private Color GetRarityColor(Rarity rarity) => rarity switch
        {
            Rarity.Common    => _colorCommon,
            Rarity.Rare      => _colorRare,
            Rarity.Epic      => _colorEpic,
            Rarity.Legendary => _colorLegendary,
            _                => Color.white
        };
    }
}
