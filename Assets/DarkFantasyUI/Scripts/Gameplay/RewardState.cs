using System;
using UnityEngine;

namespace Moonlit.UI
{
    [Serializable]
    public sealed class RewardState
    {
        public long accruedSeconds;
        public long lastTickUtc;
        public long goldClaimed;
        public long hammersClaimed;
        public bool[] passClaimed = new bool[100];
        public int arenaPoints;
        public int arenaChallenges;
        public int arenaWins;
        public string arenaDay;
        public int arenaAttemptsUsed;
        public long lastClaimAccruedSeconds;
        public string dailyDiamondClaimDay;
        public bool CanClaimDailyDiamonds => CanClaimDailyDiamondsOn(DateTime.UtcNow);
        public bool CanClaimDailyDiamondsOn(DateTime utc)
        {
            string day=utc.ToUniversalTime().AddHours(9).ToString("yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture);
            return String.CompareOrdinal(day,dailyDiamondClaimDay)>0;
        }
        public bool ClaimDailyDiamonds(MainScreen main,Vector3? origin=null) => ClaimDailyDiamondsOn(main,DateTime.UtcNow,origin);
        public bool ClaimDailyDiamondsOn(MainScreen main,DateTime utc,Vector3? origin=null)
        {
            if(!main || !CanClaimDailyDiamondsOn(utc) || main.gems>int.MaxValue-100)return false;
            dailyDiamondClaimDay=utc.ToUniversalTime().AddHours(9).ToString("yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture);
            main.gems+=100;main.Refresh();main.SaveGame();
            RewardVisuals.Absorb(main,RewardVisuals.Kind.Diamond,100,origin);
            return true;
        }
        public static RewardState Current = new RewardState();
        public long GoldAvailable => Math.Max(0, accruedSeconds - goldClaimed);
        public long HammersAvailable => Math.Max(0, accruedSeconds / 60 - hammersClaimed);
        public long CollectionSeconds => Math.Max(0,accruedSeconds-Math.Max(lastClaimAccruedSeconds,goldClaimed));
        public bool CanClaim => CollectionSeconds>=60 && (GoldAvailable>0 || HammersAvailable>0);
        public bool CanClaimPass(int clearedStage,int index)
            => index>=0 && index<100 && passClaimed!=null && index<passClaimed.Length && !passClaimed[index] && clearedStage>=(index+1)*5;
        public bool HasPassReward(int clearedStage) {
            for(int i=0;i<100;i++)if(CanClaimPass(clearedStage,i))return true;
            return false;
        }
        public int ArenaRemaining(DateTime utc)
        {
            string day=utc.ToUniversalTime().AddHours(9).ToString("yyyy-MM-dd",System.Globalization.CultureInfo.InvariantCulture);
            if(String.CompareOrdinal(day,arenaDay)>0){arenaDay=day;arenaAttemptsUsed=0;}
            return Math.Max(0,5-Math.Max(0,arenaAttemptsUsed));
        }
        public bool TryUseArenaAttempt(DateTime utc)
        {
            if(ArenaRemaining(utc)<=0)return false;
            arenaAttemptsUsed++;return true;
        }
        public void Advance(long utcSeconds)
        {
            if (lastTickUtc == 0) { lastTickUtc = utcSeconds; return; }
            if (utcSeconds <= lastTickUtc) return;
            accruedSeconds += utcSeconds - lastTickUtc;
            lastTickUtc = utcSeconds;
        }
        public bool Claim(MainScreen main)
        {
            if (!main) return false;
            Advance(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            if(!CanClaim)return false;
            long g = Math.Min(GoldAvailable, int.MaxValue - (long)main.gold);
            long h = Math.Min(HammersAvailable, int.MaxValue - (long)main.ore);
            if (g == 0 && h == 0) return false;
            main.gold += (int)g; main.ore += (int)h;
            goldClaimed += g; hammersClaimed += h;lastClaimAccruedSeconds=accruedSeconds;
            main.Refresh(); main.SaveGame(); return true;
        }
        public bool ClaimPass(MainScreen main, int index)
        {
            if (!main || !CanClaimPass(main.highestClearedStage,index)) return false;
            passClaimed[index] = true;
            main.ore = RewardRules.Add(main.ore,100);
            if(index%3==0) main.skillTickets=RewardRules.Add(main.skillTickets,10);
            if(index%3==1) main.petTickets=RewardRules.Add(main.petTickets,10);
            if(index%3==2) main.mountTickets=RewardRules.Add(main.mountTickets,10);
            main.Refresh(); main.SaveGame(); return true;
        }
    }

    public static class RewardRules
    {
        public static int Add(int value,int amount) => (int)Math.Min(int.MaxValue,Math.Max(0L,(long)value+amount));
        public static readonly string[] ArenaTiers = { "아이언", "실버", "골드", "에메랄드", "루비", "사파이어", "마스터", "그랜드 마스터", "챌린저" };
        public static int TierIndex(int points) => Math.Min(44,Math.Max(0,(points-1)/100));
        public static string TierName(int points) => TierLabel(TierIndex(points));
        public static string TierLabel(int tier) => ArenaTiers[Mathf.Clamp(tier,0,44)/5]+" "+(Mathf.Clamp(tier,0,44)%5+1);
        public static string TierRange(int tier) => tier==44 ? "4401+" : (tier==0 ? "0" : (tier*100+1).ToString())+"~"+((tier+1)*100);
        // Deterministic local standings; ties favor the dummy opponent.
        public static int ArenaRank(int points) { int rank=1; for(int i=0;i<99;i++) if(4900-i*50>=points) rank++; return rank; }
        public static int RewardFactor(int points,int rank) => rank>=1 && rank<=5 ? 51-rank : TierIndex(points)+1;
        public static string ArenaRewardText(int points,int rank)
        {
            int factor=RewardFactor(points,rank);
            return "망치 "+(factor*10)+" · 다이아 "+(factor*5)+" · 골드 "+(factor*100);
        }
        public static void FinishArena(MainScreen main,bool won)
        {
            var state=RewardState.Current;
            state.arenaChallenges++;
            if(won) { state.arenaWins++; state.arenaPoints=Add(state.arenaPoints,25); }
            else state.arenaPoints=Math.Max(0,state.arenaPoints-10);
            int factor=RewardFactor(state.arenaPoints,ArenaRank(state.arenaPoints));
            main.ore=Add(main.ore,factor*10); main.gems=Add(main.gems,factor*5); main.gold=Add(main.gold,factor*100);
            main.Refresh(); main.SaveGame();
            RewardVisuals.Absorb(main,RewardVisuals.Kind.Hammer,factor*10);
            RewardVisuals.Absorb(main,RewardVisuals.Kind.Diamond,factor*5);
            RewardVisuals.Absorb(main,RewardVisuals.Kind.Gold,factor*100);
            main.Toast((won?"승리":"패배")+" · 도전 보상 "+ArenaRewardText(state.arenaPoints,ArenaRank(state.arenaPoints)));
        }
    }

    public sealed class LiveUiRefresh : MonoBehaviour
    {
        public Action RefreshView;
        float next;
        void Update() { if(Time.unscaledTime<next)return; next=Time.unscaledTime+.2f; RefreshView?.Invoke(); }
    }
}
