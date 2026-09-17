using System;
using UnityEngine;
using UnityEngine.UI;

namespace Moonlit.UI
{
    /// <summary>Reusable view: frame, icon, labels and badges are independent children.</summary>
    [RequireComponent(typeof(Button))]
    public sealed class EquipmentSlot : MonoBehaviour
    {
        [Header("Independent visual layers")]
        public Image frame;
        public Image icon;
        public Sprite equipmentFrame;
        public Sprite companionFrame;
        public Image categoryBadge;
        public Text levelLabel;
        public Text starLabel;
        public GameObject lockedBadge;
        public GameObject notificationBadge;
        public GameObject selection;
        [Header("Optional initial binding (leave empty for an empty slot)")]
        public ItemDefinition item;
        [NonSerialized] public EquipmentRoll roll;
        public int level;
        public bool isLocked;
        public bool hasNotification;
        public event Action<EquipmentSlot> Clicked;
        public Button Button => GetComponent<Button>();

        void Awake() { Button.onClick.AddListener(HandleClick); Refresh(); }
        void OnDestroy() { Button.onClick.RemoveListener(HandleClick); }
        void HandleClick() { Clicked?.Invoke(this); }

        public void Bind(ItemDefinition definition, int itemLevel = -1, bool locked = false, bool notify = false)
        {
            item = definition;
            if (definition == null) roll = null;
            level = definition == null ? 0 : (itemLevel < 0 ? definition.startingLevel : itemLevel);
            isLocked = definition != null && locked;
            hasNotification = definition != null && notify;
            SetSelected(false);
            Refresh();
        }

        public void SetSelected(bool selected) { if (selection) selection.SetActive(selected); }
        public void SetLocked(bool value) { isLocked = item != null && value; Refresh(); }
        public void SetNotification(bool value) { hasNotification = item != null && value; Refresh(); }
        public void Refresh()
        {
            bool occupied = item != null;
            icon.sprite = occupied ? item.icon : null;
            icon.enabled = occupied && item.icon != null;
            levelLabel.text = occupied ? "Lv." + level : "";
            starLabel.gameObject.SetActive(occupied);
            lockedBadge.SetActive(occupied && isLocked);
            notificationBadge.SetActive(occupied && hasNotification);
            if(categoryBadge) { categoryBadge.sprite=occupied ? item.categoryBadgeIcon : null; categoryBadge.transform.parent.gameObject.SetActive(occupied && !isLocked && item.categoryBadgeIcon!=null); }
            if(equipmentFrame) frame.sprite=occupied && item.rarity==ItemRarity.Companion && companionFrame ? companionFrame : equipmentFrame;
            frame.color = roll != null ? EquipmentRules.TierColor(roll.tier) : !occupied ? new Color(.45f,.48f,.51f) : item.rarity==ItemRarity.Companion && companionFrame ? Color.white : RarityTint(item.rarity);
        }

        public static Color RarityTint(ItemRarity rarity)
        {
            switch (rarity)
            {
                case ItemRarity.Common: return new Color(.66f,.70f,.76f);
                case ItemRarity.Rare: return new Color(.48f,.77f,1f);
                case ItemRarity.Epic: return new Color(.84f,.48f,1f);
                case ItemRarity.Companion: return new Color(.35f,1f,.78f);
                default: return Color.white;
            }
        }
    }
}
