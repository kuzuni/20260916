using System;
using System.Collections.Generic;

namespace Moonlit.UI
{
    [Serializable]
    public sealed class CollectionEntry
    {
        public int category, grade, variant, ascension, level = 1, fragments;
        public bool unlocked;
        public int Id => grade * 3 + variant;
        public bool CanUpgrade => unlocked && level < 100 && fragments >= Required;
        public int Required => Math.Min(level + 1, 20);
        public int Cooldown => variant == 0 ? 3 : variant == 1 ? 2 : 5;
        public string Name => category == 0 ? SkillCatalog.Name(grade, variant) :
            EquipmentRules.TierNames[grade] + " " + CollectionProgression.CategoryNames[category] + " " + (variant + 1);
        public double OwnedHealth => EquipmentRules.FullSetStats(grade, level, ascension).health / 12d;
        public double OwnedAttack => EquipmentRules.FullSetStats(grade, level, ascension).attack / 12d;
        public double EquippedHealth => category == 1 ? EquipmentRules.FullSetStats(grade, level, ascension).health / 6d :
            category == 2 ? EquipmentRules.FullSetStats(grade, level, ascension).health / 2d : 0d;
        public double EquippedAttack => category == 1 ? EquipmentRules.FullSetStats(grade, level, ascension).attack / 6d :
            category == 2 ? EquipmentRules.FullSetStats(grade, level, ascension).attack / 2d : 0d;
        public double FixedHeal => category == 0 && variant == 0 ? EquipmentRules.FullSetStats(grade, level, ascension).health / 4d : 0d;
        public double FixedAttackBoost => category == 0 && variant == 0 ? EquipmentRules.FullSetStats(grade, level, ascension).attack / 5d : 0d;
        public double FixedDamage => category != 0 || variant == 0 ? 0d :
            EquipmentRules.FullSetStats(grade, level, ascension).attack * (variant == 1 ? .5d : 1.5d);
        public bool Upgrade()
        {
            if (!CanUpgrade) return false;
            fragments -= Required; level++; return true;
        }
    }
    [Serializable]
    public sealed class CollectionCategory
    {
        public int summonLevel = 1, experience, ascension;
        public int[] equipped = { -1, -1, -1 };
        public CollectionEntry[] entries;
        public int ExperienceRequired => RequiredAt(summonLevel);
        static int RequiredAt(int level) => level >= 31 ? 100 : 10 + (Math.Max(1, level) - 1) * 3;
        public bool CanSummon => summonLevel < 100;
        public bool CanAscend => summonLevel >= 100 && ascension < 3;
        public int RemainingSummons
        {
            get {
                if(!CanSummon)return 0;
                int count=-Math.Max(0,experience);
                for(int level=Math.Max(1,summonLevel);level<100;level++)count+=RequiredAt(level);
                return Math.Max(0,count);
            }
        }
        public void AddExperience()
        {
            if(!CanSummon){experience=0;return;}
            experience++;
            while (summonLevel<100 && experience >= ExperienceRequired)
            {
                experience -= ExperienceRequired;summonLevel++;
            }
            if(summonLevel>=100)experience=0;
        }
    }
    [Serializable]
    public sealed class CollectionSave
    {
        public CollectionCategory[] categories;
    }
    public static class CollectionProgression
    {
        public static readonly string[] CategoryNames = { "스킬", "펫", "탈것" };
        public static CollectionSave Data = Create();
        public static CollectionSave Create()
        {
            var save = new CollectionSave { categories = new CollectionCategory[3] };
            for (int category = 0; category < 3; category++)save.categories[category]=CreateCategory(category,0);
            return save;
        }
        static CollectionCategory CreateCategory(int category,int ascension)
        {
            var data=new CollectionCategory{ascension=ascension,entries=new CollectionEntry[EquipmentRules.TierNames.Length*3]};
            for(int id=0;id<data.entries.Length;id++)
                data.entries[id]=new CollectionEntry{category=category,grade=id/3,variant=id%3,ascension=ascension};
            return data;
        }
        public static void NormalizeAfterLoad()
        {
            if(Data==null || Data.categories==null || Data.categories.Length!=3){Data=Create();return;}
            for(int category=0;category<3;category++){
                var data=Data.categories[category];
                if(data==null){Data.categories[category]=CreateCategory(category,0);continue;}
                data.ascension=Math.Max(0,Math.Min(3,data.ascension));
                data.summonLevel=Math.Max(1,Math.Min(100,data.summonLevel));
                data.experience=data.summonLevel==100?0:Math.Max(0,Math.Min(data.ExperienceRequired-1,data.experience));
                if(data.entries==null || data.entries.Length!=EquipmentRules.TierNames.Length*3){
                    Data.categories[category]=CreateCategory(category,data.ascension);continue;
                }
                for(int id=0;id<data.entries.Length;id++){
                    if(data.entries[id]==null)data.entries[id]=new CollectionEntry();
                    var entry=data.entries[id];entry.category=category;entry.grade=id/3;entry.variant=id%3;
                    entry.ascension=data.ascension;entry.level=Math.Max(1,Math.Min(100,entry.level));entry.fragments=Math.Max(0,entry.fragments);
                }
                if(data.equipped==null || data.equipped.Length!=3)data.equipped=new[]{-1,-1,-1};
                for(int slot=0;slot<3;slot++){
                    int id=data.equipped[slot];
                    if(slot>=Capacity(category)||id<0||id>=data.entries.Length||!data.entries[id].unlocked)data.equipped[slot]=-1;
                }
            }
        }
        public static bool Ascend(int category)
        {
            if(category<0 || category>=3 || !Data.categories[category].CanAscend)return false;
            Data.categories[category]=CreateCategory(category,Data.categories[category].ascension+1);
            return true;
        }
        public static bool HasUpgrade(int category)
        {
            foreach(var entry in Data.categories[category].entries)if(entry.CanUpgrade)return true;
            return false;
        }
        public static bool HasBetterEquip(int category)
        {
            var owned=SortedOwned(category);var equipped=Equipped(category);
            double current=0,best=0;
            foreach(var entry in equipped)current+=entry.OwnedAttack+entry.OwnedHealth;
            for(int i=0;i<Math.Min(Capacity(category),owned.Count);i++)best+=owned[i].OwnedAttack+owned[i].OwnedHealth;
            return best>current;
        }
        public static bool CanSummonWithTickets(MainScreen main,int category)
        {
            if(!main || !Data.categories[category].CanSummon)return false;
            return (category==0?main.skillTickets:category==1?main.petTickets:main.mountTickets)>0;
        }
        public static int SummonQuantity(int category,int requested) =>
            category<0 || category>=3 || requested<1 || requested>500?0:Math.Min(requested,Data.categories[category].RemainingSummons);
        public static bool TrySummon(int category,int quantity,ref int tickets,ref int diamonds,Random random,out CollectionEntry[] results)
        {
            results=Array.Empty<CollectionEntry>();
            if(random==null || quantity<1 || SummonQuantity(category,quantity)!=quantity)return false;
            if(!PaySummon(quantity,ref tickets,ref diamonds))return false;
            results=Summon(category,quantity,random);return true;
        }
        public static void Reset() { Data = Create(); }
        public static int Capacity(int category) => category == 2 ? 1 : 3;
        public static double[] Probabilities(int summonLevel) =>
            EquipmentRules.TierProbabilities(1 + Math.Min(99, Math.Max(0, summonLevel - 1)) * 34 / 99);
        public static bool PaySummon(int quantity, ref int tickets, ref int diamonds)
        {
            if (quantity < 1 || quantity > 500 || tickets < 0 || diamonds < 0) return false;
            int usedTickets = Math.Min(tickets, quantity), cost = (quantity - usedTickets) * 100;
            if (diamonds < cost) return false;
            tickets -= usedTickets; diamonds -= cost; return true;
        }
        public static CollectionEntry[] Summon(int category, int quantity, Random random)
        {
            quantity=SummonQuantity(category,quantity);
            if(quantity==0 || random==null)return Array.Empty<CollectionEntry>();
            var data = Data.categories[category];
            var results = new CollectionEntry[quantity];
            for (int i = 0; i < quantity; i++)
            {
                double roll = random.NextDouble(); var chances = Probabilities(data.summonLevel); int grade = 0;
                for (; grade < chances.Length - 1; grade++) { roll -= chances[grade]; if (roll < 0) break; }
                var entry = data.entries[grade * 3 + random.Next(3)];
                entry.unlocked = true; entry.fragments++; results[i] = entry;
                data.AddExperience();
            }
            return results;
        }
        public static bool Equip(CollectionEntry entry, int slot)
        {
            if (entry == null || !entry.unlocked || slot < 0 || slot >= Capacity(entry.category)) return false;
            if(!ReferenceEquals(Data.categories[entry.category].entries[entry.Id],entry))return false;
            var equipped = Data.categories[entry.category].equipped;
            int previous = Array.IndexOf(equipped, entry.Id);
            if (previous >= 0) equipped[previous] = -1;
            equipped[slot] = entry.Id; return true;
        }
        public static bool IsEquipped(CollectionEntry entry) => entry!=null && ReferenceEquals(Data.categories[entry.category].entries[entry.Id],entry) && Array.IndexOf(Data.categories[entry.category].equipped, entry.Id) >= 0;
        public static List<CollectionEntry> Equipped(int category)
        {
            var result = new List<CollectionEntry>(); var data = Data.categories[category];
            for (int i = 0; i < Capacity(category); i++)
            {
                int id = data.equipped[i];
                if (id >= 0 && id < data.entries.Length && data.entries[id].unlocked && !result.Contains(data.entries[id]))
                    result.Add(data.entries[id]);
            }
            return result;
        }
        public static List<CollectionEntry> EquippedSkills => Equipped(0);
        public static double OwnedHealth => Sum(false, true);
        public static double OwnedAttack => Sum(false, false);
        public static double EquippedHealth => Sum(true, true);
        public static double EquippedAttack => Sum(true, false);
        static double Sum(bool equippedOnly, bool health)
        {
            double total = 0;
            for (int category = 0; category < 3; category++)
                foreach (var entry in equippedOnly ? Equipped(category) : new List<CollectionEntry>(Data.categories[category].entries))
                    if (entry.unlocked) total += equippedOnly ?
                        (health ? entry.EquippedHealth : entry.EquippedAttack) : (health ? entry.OwnedHealth : entry.OwnedAttack);
            return total;
        }
        static List<CollectionEntry> SortedOwned(int category)
        {
            var owned = new List<CollectionEntry>();
            foreach (var entry in Data.categories[category].entries) if (entry.unlocked) owned.Add(entry);
            owned.Sort((a, b) => (b.OwnedAttack + b.OwnedHealth).CompareTo(a.OwnedAttack + a.OwnedHealth));
            return owned;
        }
        public static void QuickEquip(int category)
        {
            var owned=SortedOwned(category);
            var equipped = Data.categories[category].equipped;
            for (int i = 0; i < equipped.Length; i++) equipped[i] = -1;
            for (int i = 0; i < Math.Min(Capacity(category), owned.Count); i++) equipped[i] = owned[i].Id;
        }
    }
}
