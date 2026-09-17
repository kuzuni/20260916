using System;
using System.Collections.Generic;

namespace Moonlit.UI
{
    [Serializable]
    public sealed class CollectionEntry
    {
        public int category, grade, variant, level = 1, fragments;
        public bool unlocked;
        public int Id => grade * 3 + variant;
        public int Required => Math.Min(level + 1, 20);
        public int Cooldown => variant == 0 ? 3 : variant == 1 ? 2 : 5;
        public string Name => EquipmentRules.TierNames[grade] + " " +
            (category == 0 ? new[] { "생명의 기원", "돌날 투척", "유성 강타" }[variant] :
             CollectionProgression.CategoryNames[category] + " " + (variant + 1));
        public double OwnedHealth => EquipmentRules.FullSetStats(grade, level).health / 12d;
        public double OwnedAttack => EquipmentRules.FullSetStats(grade, level).attack / 12d;
        public double EquippedHealth => category == 1 ? EquipmentRules.FullSetStats(grade, level).health / 6d :
            category == 2 ? EquipmentRules.FullSetStats(grade, level).health / 2d : 0d;
        public double EquippedAttack => category == 1 ? EquipmentRules.FullSetStats(grade, level).attack / 6d :
            category == 2 ? EquipmentRules.FullSetStats(grade, level).attack / 2d : 0d;
        public double FixedHeal => category == 0 && variant == 0 ? EquipmentRules.FullSetStats(grade, level).health / 4d : 0d;
        public double FixedAttackBoost => category == 0 && variant == 0 ? EquipmentRules.FullSetStats(grade, level).attack / 5d : 0d;
        public double FixedDamage => category != 0 || variant == 0 ? 0d :
            EquipmentRules.FullSetStats(grade, level).attack * (variant == 1 ? .5d : 1.5d);
        public bool Upgrade()
        {
            if (!unlocked || level >= 100 || fragments < Required) return false;
            fragments -= Required; level++; return true;
        }
    }
    [Serializable]
    public sealed class CollectionCategory
    {
        public int summonLevel = 1, experience;
        public int[] equipped = { -1, -1, -1 };
        public CollectionEntry[] entries;
        public int ExperienceRequired => Math.Min(10 + (summonLevel - 1) * 3, 100);
        public void AddExperience()
        {
            if (summonLevel >= 100) return;
            experience++;
            while (summonLevel < 100 && experience >= ExperienceRequired)
            { experience -= ExperienceRequired; summonLevel++; }
            if (summonLevel >= 100) experience = 0;
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
            for (int category = 0; category < 3; category++)
            {
                var data = save.categories[category] = new CollectionCategory();
                data.entries = new CollectionEntry[EquipmentRules.TierNames.Length * 3];
                for (int id = 0; id < data.entries.Length; id++)
                    data.entries[id] = new CollectionEntry { category = category, grade = id / 3, variant = id % 3 };
            }
            return save;
        }
        public static void Reset() { Data = Create(); }
        public static int Capacity(int category) => category == 2 ? 1 : 3;
        public static double[] Probabilities(int summonLevel) =>
            EquipmentRules.TierProbabilities(1 + Math.Min(99, Math.Max(0, summonLevel - 1)) * 34 / 99);
        public static bool PaySummon(int quantity, ref int tickets, ref int diamonds)
        {
            if (quantity < 1 || quantity > 100 || tickets < 0 || diamonds < 0) return false;
            int usedTickets = Math.Min(tickets, quantity), cost = (quantity - usedTickets) * 100;
            if (diamonds < cost) return false;
            tickets -= usedTickets; diamonds -= cost; return true;
        }
        public static CollectionEntry[] Summon(int category, int quantity, Random random)
        {
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
            var equipped = Data.categories[entry.category].equipped;
            int previous = Array.IndexOf(equipped, entry.Id);
            if (previous >= 0) equipped[previous] = -1;
            equipped[slot] = entry.Id; return true;
        }
        public static bool IsEquipped(CollectionEntry entry) => Array.IndexOf(Data.categories[entry.category].equipped, entry.Id) >= 0;
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
        public static void QuickEquip(int category)
        {
            var owned = new List<CollectionEntry>();
            foreach (var entry in Data.categories[category].entries) if (entry.unlocked) owned.Add(entry);
            owned.Sort((a, b) => (b.OwnedAttack + b.OwnedHealth).CompareTo(a.OwnedAttack + a.OwnedHealth));
            var equipped = Data.categories[category].equipped;
            for (int i = 0; i < equipped.Length; i++) equipped[i] = -1;
            for (int i = 0; i < Math.Min(Capacity(category), owned.Count); i++) equipped[i] = owned[i].Id;
        }
    }
}
