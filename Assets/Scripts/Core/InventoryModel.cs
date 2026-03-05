// InventoryModel.cs
// Pure C# class. Tracks items the player has selected across all draft phases.
// No MonoBehaviour — the UI layer reads from this model and updates its display.

using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;

namespace RewardDraft.Core
{
    public class InventoryModel
    {
        private readonly List<ItemData> _items = new List<ItemData>();

        /// <summary>Raised whenever an item is added to the inventory.</summary>
        public event Action<ItemData> OnItemAdded;

        /// <summary>All items currently in the inventory (read-only view).</summary>
        public ReadOnlyCollection<ItemData> Items => _items.AsReadOnly();

        /// <summary>Number of items in inventory.</summary>
        public int Count => _items.Count;

        /// <summary>
        /// Adds an item to the inventory and raises OnItemAdded.
        /// </summary>
        public void Add(ItemData item)
        {
            if (item == null) throw new ArgumentNullException(nameof(item));
            _items.Add(item);
            OnItemAdded?.Invoke(item);
        }

        /// <summary>Clears all items (used for new run).</summary>
        public void Clear() => _items.Clear();
    }
}
