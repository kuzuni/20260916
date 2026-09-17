using System;
using System.Collections.Generic;
namespace Moonlit.UI
{
    [Serializable] public sealed class ForgeState
    {
        public static ForgeState Current = new ForgeState();
        public int level=1, filledSegments, nextId, freeSkipsUsed;
        public long upgradeEndsUtcTicks;
        public string freeSkipDay="";
        public int[] draws=new int[10];
        public EquipmentRoll[] equipped=new EquipmentRoll[6];
        public List<EquipmentRoll> pending=new List<EquipmentRoll>();
        public bool autoEnabled, continueAfterMatch=true, filterEnabled;
        public int batchSize=1, affixMask=511;
        public bool[] keepTiers={true,true,true,true,true,true,true,true,true,true};
        public int Segments => 3+(level-1)%4;
        public int SegmentCost => 20*level*level;
        public double UpgradeSeconds => 10*Math.Pow(1.3,level-1);
        public int UpgradePhase(DateTime now) => level>=35 ? 4 : upgradeEndsUtcTicks>0 ? (now.Ticks>=upgradeEndsUtcTicks ? 3 : 2) : filledSegments>=Segments ? 1 : 0;
        public double RemainingSeconds(DateTime now) => Math.Max(0,(upgradeEndsUtcTicks-now.Ticks)/(double)TimeSpan.TicksPerSecond);
        public int DiamondSkipCost(DateTime now) => (int)Math.Ceiling(RemainingSeconds(now)/6.0);
        public bool FillSegment(ref int gold)
        {
            if(UpgradePhase(DateTime.UtcNow)!=0 || gold<SegmentCost) return false;
            gold-=SegmentCost; filledSegments++; return true;
        }
        public bool StartUpgrade(DateTime now)
        {
            if(UpgradePhase(now)!=1) return false;
            upgradeEndsUtcTicks=now.AddSeconds(UpgradeSeconds).Ticks; return true;
        }
        public bool ClaimUpgrade(DateTime now)
        {
            if(UpgradePhase(now)!=3) return false;
            level++; filledSegments=0; upgradeEndsUtcTicks=0; return true;
        }
        public bool DiamondSkip(DateTime now,ref int diamonds)
        {
            if(UpgradePhase(now)!=2) return false;
            int price=DiamondSkipCost(now);if(diamonds<price)return false;
            diamonds-=price;upgradeEndsUtcTicks=now.Ticks;return true;
        }
        public void ResetDaily(DateTime now)
        {
            string day=now.ToUniversalTime().AddHours(9).ToString("yyyy-MM-dd");
            if(freeSkipDay==day)return;freeSkipDay=day;freeSkipsUsed=0;
        }
        public bool FreeSkip(DateTime now)
        {
            ResetDaily(now);
            if(UpgradePhase(now)!=2 || freeSkipsUsed>=4)return false;
            freeSkipsUsed++;upgradeEndsUtcTicks=Math.Max(now.Ticks,upgradeEndsUtcTicks-TimeSpan.TicksPerMinute*30);return true;
        }
        public EquipmentRoll Draw(System.Random random)
        {
            int tier=EquipmentRules.SelectTier(EquipmentRules.TierProbabilities(level),random.NextDouble());
            return DrawTier(tier,random);
        }
        public EquipmentRoll DrawTier(int tier,System.Random random)
        {
            draws[tier]=Math.Min(100,draws[tier]+1);
            return new EquipmentRoll { id=++nextId,tier=tier,level=draws[tier],variant=random.Next(3),part=(EquipmentPart)random.Next(6),affixes=EquipmentRules.RollAffixes(tier,random) };
        }
        public bool Matches(EquipmentRoll item)
        {
            if(keepTiers==null || item.tier>=keepTiers.Length || !keepTiers[item.tier])return false;
            if(!filterEnabled)return true;
            foreach(var affix in item.affixes)if((affixMask&(1<<(int)affix.kind))!=0)return true;
            return false;
        }
        public int CompleteAutoBatch(IReadOnlyList<EquipmentRoll> batch)
        {
            int gold=0;
            foreach(var item in batch) {
                // Settlement is idempotent and never sells an already equipped item.
                if(item==null || !pending.Contains(item) || Array.Exists(equipped,e=>e!=null && e.id==item.id))continue;
                if(!Matches(item) && pending.Remove(item))gold+=EquipmentRules.SaleGold(item);
            }
            return gold;
        }
        public void NormalizeAfterLoad()
        {
            level=Math.Max(1,Math.Min(35,level));
            filledSegments=Math.Max(0,Math.Min(Segments,filledSegments));
            upgradeEndsUtcTicks=Math.Max(0,upgradeEndsUtcTicks);
            freeSkipsUsed=Math.Max(0,Math.Min(4,freeSkipsUsed));
            batchSize=Math.Max(1,Math.Min(99,batchSize));affixMask&=511;
            autoEnabled=false; // A restored queue is reviewed before new hammers are spent.
            if(draws==null)draws=new int[10];else if(draws.Length!=10)Array.Resize(ref draws,10);
            for(int i=0;i<10;i++)draws[i]=Math.Max(0,Math.Min(100,draws[i]));
            if(keepTiers==null)keepTiers=new[]{true,true,true,true,true,true,true,true,true,true};
            else if(keepTiers.Length!=10)Array.Resize(ref keepTiers,10);
            var oldEquipment=equipped;equipped=new EquipmentRoll[6];
            var identities=new HashSet<int>();nextId=Math.Max(0,nextId);
            if(oldEquipment!=null)foreach(var item in oldEquipment) {
                if(!ValidSavedItem(item) || equipped[(int)item.part]!=null || !identities.Add(item.id))continue;
                NormalizeItem(item);equipped[(int)item.part]=item;
            }
            var oldPending=pending;pending=new List<EquipmentRoll>();
            if(oldPending!=null)foreach(var item in oldPending) {
                if(!ValidSavedItem(item) || !identities.Add(item.id))continue;
                NormalizeItem(item);pending.Add(item);
            }
        }
        static bool ValidSavedItem(EquipmentRoll item)
            => item!=null && item.id>0 && item.tier>=0 && item.tier<10 && (int)item.part>=0 && (int)item.part<6;
        void NormalizeItem(EquipmentRoll item)
        {
            item.level=Math.Max(1,Math.Min(100,item.level));item.variant=Math.Max(0,Math.Min(2,item.variant));
            nextId=Math.Max(nextId,item.id);draws[item.tier]=Math.Max(draws[item.tier],item.level);
            var affixes=new List<EquipmentAffix>();var kinds=new HashSet<EquipmentAffixKind>();
            if(item.affixes!=null)foreach(var a in item.affixes) {
                if(a==null || (int)a.kind<0 || (int)a.kind>=9 || !kinds.Add(a.kind))continue;
                if(affixes.Count>=EquipmentRules.AffixCount(item.tier))break;
                a.percent=Math.Max(1,Math.Min(EquipmentRules.AffixMaximums[(int)a.kind],a.percent));affixes.Add(a);
            }
            item.affixes=affixes.ToArray();
        }
        public bool ShouldCompare => pending.Count>0 && (!continueAfterMatch || pending.Count>=25);
        public EquipmentRoll Pending => pending.Count>0 ? pending[0] : null;
        // The two compared cards swap ownership each click; selling always resolves the lower unequipped card.
        public bool ToggleEquip(int id)
        {
            var item=Pending;if(item==null || item.id!=id)return false;
            int index=(int)item.part;var previous=equipped[index];equipped[index]=item;
            if(previous==null)pending.RemoveAt(0);else pending[0]=previous;
            return true;
        }
        public bool SellPending(int id,out int gold)
        {
            gold=0;var item=Pending;
            if(item==null || item.id!=id || equipped[(int)item.part]==null)return false;
            gold=EquipmentRules.SaleGold(item);pending.RemoveAt(0);return true;
        }
        public double AffixTotal(EquipmentAffixKind kind)
        {
            double sum=0;
            foreach(var item in equipped)if(item!=null)foreach(var affix in item.affixes)if(affix.kind==kind)sum+=affix.percent;
            return sum;
        }
        public EquipmentStats TotalStats {
            get {
                var result=new EquipmentStats();
                foreach(var item in equipped)if(item!=null) {var s=item.Stats;result.health+=s.health;result.attack+=s.attack;result.speed+=s.speed;}
                result.health*=1+AffixTotal(EquipmentAffixKind.HealthIncrease)/100;
                result.attack*=1+AffixTotal(EquipmentAffixKind.AttackIncrease)/100;return result;
            }
        }
    }
}
