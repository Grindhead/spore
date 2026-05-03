using UnityEngine;

namespace RoobsWorld.Core
{
    /// <summary>
    /// Rarity tier for items; used for UI coloring and sorting.
    /// </summary>
    public enum ItemRarity
    {
        Common,
        Uncommon,
        Rare,
        Epic,
        Legendary
    }

    /// <summary>
    /// ScriptableObject defining shared item data: icon, name, rarity, and description.
    /// </summary>
    [CreateAssetMenu(fileName = "NewItem", menuName = "RoobsWorld/Item Data")]
    public class ItemData : ScriptableObject
    {
        [Header("Display")]
        [Tooltip("Icon shown in inventory, tooltips, and UI.")]
        public Sprite Icon;

        [Tooltip("Display name of the item.")]
        public string ItemName;

        [Tooltip("Rarity tier (affects UI treatment and sorting).")]
        public ItemRarity Rarity = ItemRarity.Common;

        [TextArea(2, 6)]
        [Tooltip("Short description for tooltips and item details.")]
        public string Description;
    }
}
