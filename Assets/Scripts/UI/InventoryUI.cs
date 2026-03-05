// InventoryUI.cs
// Displays the player's inventory as a scrollable list.
// Each row has: item name TMP + rarity TMP + rarity background Image.
// All three are colored based on item rarity.

using UnityEngine;
using UnityEngine.UI;
using TMPro;
using RewardDraft.Core;

namespace RewardDraft.UI
{
    public class InventoryUI : MonoBehaviour
    {
        [Header("Inventory Panel")]
        [SerializeField] private Transform _listContainer;
        [SerializeField] private GameObject _rowPrefab;
        [SerializeField] private TextMeshProUGUI _countText;
        [SerializeField] private ScrollRect _scrollRect;

        [Header("Row Child Names (must match prefab)")]
        [SerializeField] private string _nameFieldName = "ItemNameText";
        [SerializeField] private string _rarityFieldName = "RarityText";
        [SerializeField] private string _rarityBgImageName = "RarityBackground";

        [Header("Rarity Text Colors")]
        [SerializeField] private Color _colorCommon = new Color(0.20f, 0.20f, 0.20f);
        [SerializeField] private Color _colorRare = new Color(1.00f, 1.00f, 1.00f);
        [SerializeField] private Color _colorEpic = new Color(1.00f, 1.00f, 1.00f);
        [SerializeField] private Color _colorLegendary = new Color(0.10f, 0.05f, 0.00f);

        [Header("Rarity Row Background Colors (subtle tint)")]
        [SerializeField] private Color _bgCommon = new Color(0.85f, 0.85f, 0.85f, 0.20f);
        [SerializeField] private Color _bgRare = new Color(0.30f, 0.55f, 0.95f, 0.20f);
        [SerializeField] private Color _bgEpic = new Color(0.65f, 0.25f, 0.90f, 0.20f);
        [SerializeField] private Color _bgLegendary = new Color(1.00f, 0.70f, 0.10f, 0.20f);

        [Header("Rarity Badge Background Colors (solid — matches reward cards)")]
        [SerializeField] private Color _badgeCommon = new Color(0.65f, 0.65f, 0.65f, 1.00f);
        [SerializeField] private Color _badgeRare = new Color(0.30f, 0.55f, 0.95f, 1.00f);
        [SerializeField] private Color _badgeEpic = new Color(0.65f, 0.25f, 0.90f, 1.00f);
        [SerializeField] private Color _badgeLegendary = new Color(1.00f, 0.70f, 0.10f, 1.00f);

        // ── Public API ────────────────────────────────────────────────────────

        public void BindModel(InventoryModel model)
        {
            model.OnItemAdded += HandleItemAdded;
            foreach (var item in model.Items)
                AppendRow(item);
            RefreshCount(model.Count);
        }

        // ── Handlers ──────────────────────────────────────────────────────────

        private void HandleItemAdded(ItemData item) => AppendRow(item);

        // ── Row Builder ───────────────────────────────────────────────────────

        private void AppendRow(ItemData item)
        {
            if (_rowPrefab == null || _listContainer == null)
            {
                Debug.LogError("[InventoryUI] Row prefab or list container not assigned.");
                return;
            }

            GameObject row = Instantiate(_rowPrefab, _listContainer);

            // ── 1. Row background (full row subtle tint) ───────────────────
            Image rowBg = row.GetComponent<Image>();
            if (rowBg == null) rowBg = row.AddComponent<Image>();
            rowBg.color = GetRowBgColor(item.rarityEnum);

            // ── 2. Item name text ──────────────────────────────────────────
            TextMeshProUGUI nameLabel = FindTMP(row, _nameFieldName);
            if (nameLabel != null)
                nameLabel.text = item.displayName;
            else
                Debug.LogWarning($"[InventoryUI] Child '{_nameFieldName}' not found in row prefab.");

            // ── 3. Rarity badge background Image ──────────────────────────
            Image rarityBadgeBg = FindImage(row, _rarityBgImageName);
            if (rarityBadgeBg != null)
                rarityBadgeBg.color = GetBadgeColor(item.rarityEnum);
            else
                Debug.LogWarning($"[InventoryUI] Child '{_rarityBgImageName}' not found in row prefab.");

            // ── 4. Rarity text ─────────────────────────────────────────────
            TextMeshProUGUI rarityLabel = FindTMP(row, _rarityFieldName);
            if (rarityLabel != null)
            {
                rarityLabel.text = item.rarityEnum.ToString().ToUpper();
                rarityLabel.color = GetRarityTextColor(item.rarityEnum);
            }
            else
                Debug.LogWarning($"[InventoryUI] Child '{_rarityFieldName}' not found in row prefab.");

            // ── 5. Count + scroll ──────────────────────────────────────────
            RefreshCount(_listContainer.childCount);
            if (_scrollRect != null)
            {
                Canvas.ForceUpdateCanvases();
                _scrollRect.verticalNormalizedPosition = 0f;
            }
        }

        // ── Search Helpers ────────────────────────────────────────────────────

        private TextMeshProUGUI FindTMP(GameObject root, string childName)
        {
            foreach (var tmp in root.GetComponentsInChildren<TextMeshProUGUI>(true))
                if (tmp.gameObject.name == childName)
                    return tmp;
            return null;
        }

        private Image FindImage(GameObject root, string childName)
        {
            foreach (var img in root.GetComponentsInChildren<Image>(true))
                if (img.gameObject.name == childName)
                    return img;
            return null;
        }

        private void RefreshCount(int count)
        {
            if (_countText != null)
                _countText.text = $"{count}";
        }

        // ── Color Getters ─────────────────────────────────────────────────────

        // Text color on top of the badge
        private Color GetRarityTextColor(Rarity rarity) => rarity switch
        {
            Rarity.Common => _colorCommon,
            Rarity.Rare => _colorRare,
            Rarity.Epic => _colorEpic,
            Rarity.Legendary => _colorLegendary,
            _ => Color.white
        };

        // Subtle tint on the full row background
        private Color GetRowBgColor(Rarity rarity) => rarity switch
        {
            Rarity.Common => _bgCommon,
            Rarity.Rare => _bgRare,
            Rarity.Epic => _bgEpic,
            Rarity.Legendary => _bgLegendary,
            _ => Color.clear
        };

        // Solid color on the rarity badge Image — matches reward card colors
        private Color GetBadgeColor(Rarity rarity) => rarity switch
        {
            Rarity.Common => _badgeCommon,
            Rarity.Rare => _badgeRare,
            Rarity.Epic => _badgeEpic,
            Rarity.Legendary => _badgeLegendary,
            _ => Color.gray
        };
    }
}
