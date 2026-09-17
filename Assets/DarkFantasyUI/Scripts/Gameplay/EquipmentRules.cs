using System;
using UnityEngine;
namespace Moonlit.UI
{
    public enum EquipmentPart { Armor, Earring, Hat, Necklace, Ring, Weapon }
    public enum EquipmentAffixKind { CriticalChance, CriticalDamage, Dodge, LifeSteal, Regeneration, DoubleChance, AttackIncrease, HealthIncrease, SkillDamage }
    [Serializable] public struct EquipmentStats { public double health, attack, speed; }
    [Serializable] public sealed class EquipmentAffix { public EquipmentAffixKind kind; public int percent; }
    [Serializable] public sealed class EquipmentRoll
    {
        public int id, tier, level = 1, variant;
        public EquipmentPart part;
        public EquipmentAffix[] affixes = Array.Empty<EquipmentAffix>();
        public EquipmentStats Stats => EquipmentRules.BaseStats(tier, level, part);
        public string Name => "[" + EquipmentRules.TierNames[tier] + "] " + EquipmentRules.VariantNames[variant] + " " + EquipmentRules.PartNames[(int)part];
    }
    public static class EquipmentRules
    {
        public const int TierCount = 10, MaxEquipmentLevel = 100, MaxForgeLevel = 35;
        public static double baseHealth = 80, baseAttack = 10;
        public static readonly string[] TierNames = { "원시적", "중세의", "근대 초기", "현대의", "우주", "항성간", "다중 우주", "양자", "지하 세계", "천상" };
        public static readonly string[] PartNames = { "갑옷", "귀걸이", "모자", "목걸이", "반지", "무기" };
        public static readonly string[] VariantNames = { "도적", "전사", "암살자" };
        public static readonly string[] ThemeFolders = { "01_Primitive", "02_Medieval", "03_EarlyModern", "04_Modern", "05_Cyber", "06_Future", "07_Space", "08_Immortal", "09_Infinite", "10_Holy" };
        public static readonly string[] VariantFolders = { "Thief", "Warrior", "Assassin" };
        public static readonly string[] PartFiles = { "armor", "earring", "hat", "necklace", "ring", "weapon" };
        public static readonly string[] AffixNames = { "치명타확률", "치명타피해", "회피확률", "생명력흡수", "체력재생", "더블찬스", "공격력증가", "체력증가", "스킬데미지증가" };
        public static readonly int[] AffixMaximums = { 10, 40, 3, 5, 3, 10, 10, 10, 15 };
        public static readonly Color[] TierColors = {
            new Color(.64f,.58f,.48f), new Color(.22f,.61f,1), new Color(.27f,.82f,.23f),
            new Color(1,.83f,.18f), new Color(1,.19f,.20f), new Color(.70f,.24f,1),
            new Color(.05f,.88f,.89f), new Color(.28f,.30f,1), new Color(.72f,.38f,.30f), new Color(1,.53f,.10f)
        };
        public static Color TierColor(int tier) => TierColors[Mathf.Clamp(tier,0,9)];
        public static bool IsHealthPart(EquipmentPart part) => part == EquipmentPart.Armor || part == EquipmentPart.Hat || part == EquipmentPart.Necklace;
        public static EquipmentStats BaseStats(int tier, int level, EquipmentPart part)
        {
            tier = Math.Max(0, Math.Min(9,tier)); level = Math.Max(1, Math.Min(100,level));
            // "10% per level" is multiplicative; every tier shares this single tuning point.
            double scale = Math.Pow(2 * Math.Pow(1.1,99), tier) * Math.Pow(1.1,level-1);
            double speedBase = 1;
            for(int i=0;i<tier;i++) speedBase = (speedBase+99)*2;
            return IsHealthPart(part) ? new EquipmentStats { health=baseHealth*scale, speed=speedBase+level-1 }
                : new EquipmentStats { attack=baseAttack*scale };
        }
        public static EquipmentStats FullSetStats(int tier, int level)
        {
            var hp=BaseStats(tier,level,EquipmentPart.Armor); var atk=BaseStats(tier,level,EquipmentPart.Weapon);
            return new EquipmentStats { health=hp.health*3, attack=atk.attack*3, speed=hp.speed*3 };
        }
        public static int AffixCount(int tier) => tier < 2 ? 0 : tier < 4 ? 1 : 2;
        public static EquipmentAffix[] RollAffixes(int tier, System.Random random)
        {
            var pool = new int[9]; for(int i=0;i<pool.Length;i++) pool[i]=i;
            var result = new EquipmentAffix[AffixCount(tier)];
            for(int i=0;i<result.Length;i++) {
                int j=random.Next(i,pool.Length); int swap=pool[i];pool[i]=pool[j];pool[j]=swap;
                result[i]=new EquipmentAffix {kind=(EquipmentAffixKind)pool[i],percent=random.Next(1,AffixMaximums[pool[i]]+1)};
            }
            return result;
        }
        public static double[] TierProbabilities(int forgeLevel)
        {
            int level=Math.Max(1,Math.Min(35,forgeLevel));
            var rates=new double[10];
            if(level==1) { rates[0]=1; return rates; }
            // Moving four-grade window: unavailable grades have exactly zero weight.
            if(level>=33) {
                double t=(level-33)/2.0;
                rates[6]=.28*(1-t)+.06*t; rates[7]=.58*(1-t)+.55*t;
                rates[8]=.13*(1-t)+.35*t; rates[9]=.01*(1-t)+.04*t; return rates;
            }
            int top=Math.Min(8,1+(level-2)/4);
            int low=Math.Max(0,top-3);
            double progress=((level-2)%4)/4.0, sum=0;
            for(int i=low;i<=top;i++) {
                rates[i]=i==top ? .04+.10*progress : Math.Pow(2.8,i-low);
                sum+=rates[i];
            }
            for(int i=0;i<10;i++) rates[i]/=sum;
            return rates;
        }
        public static int SelectTier(double[] rates,double value)
        {
            double cumulative=0;
            for(int i=0;i<rates.Length;i++) { cumulative+=rates[i];if(value<cumulative)return i; }
            for(int i=rates.Length-1;i>=0;i--) if(rates[i]>0) return i;
            return 0;
        }
        public static int SaleGold(EquipmentRoll item) => Math.Max(1, (item.tier+1)*10+item.level*2);
        public static string Number(double value) => value>=1e12 ? value.ToString("0.##E+0") : value>=1e9 ? (value/1e9).ToString("0.##")+"b" : value>=1e6 ? (value/1e6).ToString("0.##")+"m" : value>=1e3 ? (value/1e3).ToString("0.##")+"k" : value.ToString("0.##");
    }
}
