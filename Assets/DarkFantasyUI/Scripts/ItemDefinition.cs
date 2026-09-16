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
        [Header("Local UI demo stats at starting level")]
        public double baseHealth = 1000;
        public string firstBonusName = "치명타 확률";
        public float firstBonusPercent = 1;
        public string secondBonusName = "공격 속도";
        public float secondBonusPercent = 2;

        // Deterministic UI preview progression; not a combat/backend stat calculation.
        public double HealthAtLevel(int level)
            => System.Math.Max(0, baseHealth) * System.Math.Pow(1.02, System.Math.Max(1, level) - System.Math.Max(1, startingLevel));
    }
}
