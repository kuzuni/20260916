using System;
using UnityEngine;

namespace Moonlit.UI
{
    [Serializable]
    public sealed class BattleAppearanceSet
    {
        public int tier, variant;
        public Sprite weapon, head, body, arm1, arm2, leg1, leg2;
    }

    public sealed class BattleAssetCatalog : ScriptableObject
    {
        public GameObject playerPrefab;
        public RuntimeAnimatorController controller;
        public Material effectMaterial;
        public Sprite buffSprite, weakSprite, strongSprite;
        public BattleAppearanceSet[] appearances;
        public BattleAppearanceSet Find(int tier, int variant)
            => Array.Find(appearances ?? Array.Empty<BattleAppearanceSet>(), x => x.tier == tier && x.variant == variant);
    }
}
