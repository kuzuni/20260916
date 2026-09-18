using System;
namespace Moonlit.UI
{
    [Serializable]
    public sealed class DungeonSave
    {
        public int[] keys = { 2, 2, 2, 2 };
        public int[] highestCleared = { 0, 0, 0, 0 };
        public string refillDay = "";
        public int pendingIndex = -1, pendingDifficulty;
        public bool pendingClaim;
    }
    public static class DungeonProgression
    {
        public static readonly string[] Names = { "망치 도둑", "유령 마을", "침략", "좀비 러시" };
        public static readonly string[] Rewards = { "망치", "스킬소환권", "펫소환권", "탈것소환권" };
        public static DungeonSave Data = new DungeonSave();
        static int activeIndex = -1, activeDifficulty;
        public static void Reset() { Data = new DungeonSave(); activeIndex = -1; }
        // Calendar boundary is fixed to Korea rather than the player's device time zone.
        public static void RefreshDay(DateTime utc)
        {
            string day = utc.ToUniversalTime().AddHours(9).ToString("yyyy-MM-dd");
            if (String.CompareOrdinal(day, Data.refillDay) <= 0) return;
            for (int i = 0; i < 4; i++) Data.keys[i] = Math.Max(2, Data.keys[i]);
            Data.refillDay = day;
        }
        public static void AddKeys(int count) { if (count <= 0) return; RefreshDay(DateTime.UtcNow); for (int i = 0; i < 4; i++) Data.keys[i] += count; }
        public static int Waves(int index) => index == 0 ? 1 : index == 1 ? 2 : 3;
        public static int NormalStage(int difficulty) => checked(difficulty * 5);
        public static int Reward(int index, int difficulty) => index == 0 ? 100 + 3 * (Math.Max(1, difficulty) - 1) : 3 + Math.Max(1, difficulty) - 1;
        public static int NextDifficulty(int index) => Data.highestCleared[index] + 1;
        public static int SweepDifficulty(int index) => Math.Max(1, Data.highestCleared[index] - 1);
        public static bool BeginEntry(int index, int difficulty)
        {
            RefreshDay(DateTime.UtcNow);
            if (activeIndex >= 0 || Data.pendingClaim || index < 0 || index >= 4 || difficulty < 1 ||
                difficulty > NextDifficulty(index) || Data.keys[index] < 1) return false;
            activeIndex = index; activeDifficulty = difficulty; return true;
        }
        public static void CancelEntry() { activeIndex = -1; }
        public static bool CompleteEntry(bool won, out int index, out int amount)
        {
            index = activeIndex; amount = 0;
            if (activeIndex < 0) return false;
            activeIndex = -1;
            if (!won) return false;
            Data.pendingIndex=index;Data.pendingDifficulty=activeDifficulty;Data.pendingClaim=true;
            amount = Reward(index, activeDifficulty); return true;
        }
        public static bool ClaimEntry(out int index,out int amount)
        {
            index=Data.pendingIndex;amount=0;
            if(!Data.pendingClaim || index<0 || index>=4 || Data.keys[index]<1)return false;
            Data.keys[index]--;
            Data.highestCleared[index]=Math.Max(Data.highestCleared[index],Data.pendingDifficulty);
            amount=Reward(index,Data.pendingDifficulty);
            Data.pendingClaim=false;Data.pendingIndex=-1;Data.pendingDifficulty=0;
            return true;
        }
        public static bool TrySweep(int index, out int amount)
        {
            amount = 0; RefreshDay(DateTime.UtcNow);
            if (index < 0 || index >= 4 || activeIndex >= 0 || Data.pendingClaim || Data.highestCleared[index] < 1 || Data.keys[index] < 1) return false;
            Data.keys[index]--; amount = Reward(index, SweepDifficulty(index)); return true;
        }
    }
}
