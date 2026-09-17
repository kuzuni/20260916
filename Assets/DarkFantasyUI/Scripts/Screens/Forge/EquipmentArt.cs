using UnityEngine;
namespace Moonlit.UI
{
    public sealed class EquipmentArt : ScriptableObject
    {
        public Sprite[] thumbnails;
        static EquipmentArt catalog;
        public static Sprite Icon(EquipmentRoll item)
        {
            if(item==null)return null;
            if(!catalog)catalog=Resources.Load<EquipmentArt>("Moonlit/Forge/EquipmentArt");
            int index=(item.tier*3+item.variant)*6+(int)item.part;
            return catalog && catalog.thumbnails!=null && index<catalog.thumbnails.Length ? catalog.thumbnails[index] : null;
        }
    }
}
