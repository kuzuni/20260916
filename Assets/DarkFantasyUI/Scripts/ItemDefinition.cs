using UnityEngine;

namespace Moonlit.UI
{
    public enum ItemRarity { Common, Rare, Epic, Legendary, Companion }

    [CreateAssetMenu(menuName = "Moonlit/Item Definition")]
    public sealed class ItemDefinition : ScriptableObject
    {
        public string displayName;
        public Sprite icon;
        public Sprite categoryBadgeIcon;
        public ItemRarity rarity = ItemRarity.Legendary;
        [Min(1)] public int startingLevel = 1;
        [TextArea] public string description;
    }
}
