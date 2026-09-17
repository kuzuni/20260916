using System;
using System.Collections.Generic;
using UnityEngine;

namespace Moonlit.UI
{
    public sealed class CombatAppearance : MonoBehaviour
    {
        readonly Dictionary<string, SpriteRenderer> parts = new Dictionary<string, SpriteRenderer>();
        readonly Dictionary<string, Sprite> originals = new Dictionary<string, Sprite>();
        BattleAssetCatalog catalog;
        int lastSignature = int.MinValue;
        public void Initialize(BattleAssetCatalog assets)
        {
            catalog = assets;
            foreach (var renderer in GetComponentsInChildren<SpriteRenderer>(true))
            {
                string key = renderer.sprite ? renderer.sprite.name : renderer.name;
                if (parts.ContainsKey(key)) continue;
                parts[key] = renderer;
                originals[key] = renderer.sprite;
            }
        }
        public void Refresh(EquipmentRoll[] equipped)
        {
            if (!catalog || equipped == null) return;
            int signature = 17;
            foreach (var item in equipped) unchecked { signature = signature * 31 + (item == null ? 0 : item.id); }
            if (signature == lastSignature) return;
            lastSignature = signature;
            foreach (var pair in parts) pair.Value.sprite = originals[pair.Key];
            foreach (var item in equipped)
            {
                if (item == null) continue;
                var set = catalog.Find(item.tier, item.variant);
                if (set == null) continue;
                switch (item.part)
                {
                    case EquipmentPart.Armor:
                        Swap("몸통", set.body); Swap("팔1", set.arm1); Swap("팔2", set.arm2);
                        Swap("다리1", set.leg1); Swap("다리2", set.leg2);
                        break;
                    case EquipmentPart.Hat: Swap("머리", set.head); break;
                    case EquipmentPart.Weapon: Swap("무기", set.weapon); break;
                }
            }
        }
        void Swap(string key, Sprite sprite)
        {
            if (!sprite || !parts.TryGetValue(key, out var renderer)) return;
            renderer.sprite = sprite;
            // PSD parts share the reference skeleton and bone names. Enable SpriteSkin's supported
            // automatic name rebinding without making the entire UI assembly depend on 2D Animation.
            var skin = renderer.GetComponent("SpriteSkin");
            var property = skin ? skin.GetType().GetProperty("autoRebind") : null;
            if (property != null && property.CanWrite) property.SetValue(skin, true);
        }
    }
}
