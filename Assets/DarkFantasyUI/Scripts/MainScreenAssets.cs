using UnityEngine;

namespace Moonlit.UI
{
    [CreateAssetMenu(menuName="Moonlit/Main Screen Assets")]
    public sealed class MainScreenAssets : ScriptableObject
    {
        public Font font;
        public Sprite worldBackground, forgeBackground, anvil, circle;
        public Sprite[] equipmentIcons, interfaceIcons, panels;
        public ItemDefinition[] items;
        public EquipmentSlot equipmentSlotPrefab;
    }
}
