using System;
using UnityEngine;

namespace Moonlit.UI
{
    [Serializable]
    public sealed class GameplaySave
    {
        public int version=1;
        public int diamonds,gold,skillTickets,petTickets,mountTickets,hammers=1000,stage=1,highestClearedStage;
        public int successfulForges;
        public string forge,collections,dungeons;
        public RewardState rewards;
    }

    public sealed partial class MainScreen
    {
        public int skillTickets,petTickets,mountTickets,highestClearedStage;
        public static bool PersistenceEnabled=true;
        const string SaveKey="Moonlit.Gameplay.v1";
        bool gameplayInitialized;
        float nextSave;
        BattleRuntime battle;

        public void InitializeGameplay(MainScreenAssets assets)
        {
            if(gameplayInitialized)return;
            // Hosted tests/captures never load or overwrite a user's progression.
            if(PersistenceEnabled && !Application.isBatchMode && PlayerPrefs.HasKey(SaveKey))
            {
                try {
                    var save=JsonUtility.FromJson<GameplaySave>(PlayerPrefs.GetString(SaveKey));
                    if(save!=null && save.version==1) {
                        gems=Math.Max(0,save.diamonds);gold=Math.Max(0,save.gold);ore=Math.Max(0,save.hammers);
                        skillTickets=Math.Max(0,save.skillTickets);petTickets=Math.Max(0,save.petTickets);mountTickets=Math.Max(0,save.mountTickets);
                        stage=Math.Max(1,save.stage);highestClearedStage=Math.Max(0,save.highestClearedStage);
                        successfulForges=Math.Max(0,save.successfulForges);
                        if(!string.IsNullOrEmpty(save.forge))JsonUtility.FromJsonOverwrite(save.forge,ForgeState.Current);
                        if(!string.IsNullOrEmpty(save.collections))JsonUtility.FromJsonOverwrite(save.collections,CollectionProgression.Data);
                        if(!string.IsNullOrEmpty(save.dungeons))JsonUtility.FromJsonOverwrite(save.dungeons,DungeonProgression.Data);
                        if(save.rewards!=null)RewardState.Current=save.rewards;
                    }
                } catch(Exception e) { Debug.LogWarning("게임 저장을 불러오지 못했습니다: "+e.GetType().Name); }
            }
            ForgeState.Current.NormalizeAfterLoad();
            if(RewardState.Current.passClaimed==null || RewardState.Current.passClaimed.Length!=100)
                RewardState.Current.passClaimed=new bool[100];
            RewardState.Current.Advance(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            gameplayInitialized=true;
            ForgeRuntime.Ensure(this).SyncSlots();
            battle=gameObject.AddComponent<BattleRuntime>(); battle.Initialize(this,assets);
            SaveGame();
        }
        void TickGameplay()
        {
            if(!gameplayInitialized)return;
            RewardState.Current.Advance(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            if(Time.unscaledTime>=nextSave){nextSave=Time.unscaledTime+10;SaveGame();}
        }
        public void SaveGame()
        {
            if(!gameplayInitialized || !PersistenceEnabled || Application.isBatchMode)return;
            RewardState.Current.Advance(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            var save=new GameplaySave {
                diamonds=gems,gold=gold,hammers=ore,skillTickets=skillTickets,petTickets=petTickets,mountTickets=mountTickets,
                stage=stage,highestClearedStage=highestClearedStage,successfulForges=successfulForges,
                forge=JsonUtility.ToJson(ForgeState.Current),collections=JsonUtility.ToJson(CollectionProgression.Data),
                dungeons=JsonUtility.ToJson(DungeonProgression.Data),rewards=RewardState.Current
            };
            PlayerPrefs.SetString(SaveKey,JsonUtility.ToJson(save));PlayerPrefs.Save();
        }
        void OnApplicationPause(bool paused){if(paused)SaveGame();}
        void OnApplicationQuit(){SaveGame();}
        public bool StartDungeon(int index,int difficulty,int waves,Action<bool> completed)
        {
            if(!battle){Toast("전투 리소스를 준비 중입니다.");return false;}
            while(screens.ModalDepth>0)screens.CloseTop();screens.ShowMainPage();
            return battle.StartDungeon(index,difficulty,waves,completed);
        }
        public bool StartArena(int opponentRating,Action<bool> completed)
        {
            if(!battle){Toast("전투 리소스를 준비 중입니다.");return false;}
            while(screens.ModalDepth>0)screens.CloseTop();screens.ShowMainPage();
            return battle.StartArena(opponentRating,completed);
        }
        public void PreviewPrimitiveSkill(int variant)
        {
            if(!battle){Toast("전투 리소스를 준비 중입니다.");return;}
            while(screens.ModalDepth>0)screens.CloseTop();screens.ShowMainPage();
            battle.PreviewPrimitiveSkill(variant);
        }
        public void AddDungeonKeys(int count)
        {
            DungeonProgression.AddKeys(count);
        }
    }
}
