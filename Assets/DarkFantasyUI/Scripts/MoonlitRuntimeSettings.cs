using UnityEngine;

namespace Moonlit.UI
{
    public static class MoonlitRuntimeSettings
    {
        // SubsystemRegistration also runs when Enter Play Mode skips domain reload.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetSession()
        {
            ForgeState.Current=new ForgeState();
            CollectionProgression.Reset();
            DungeonProgression.Reset();
            RewardState.Current=new RewardState();
            ForgeScreenModule.ResetSession();
            ProgressionScreenModule.ResetSession();
            SocialScreenModule.ResetSession();
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        public static void Apply()
        {
            QualitySettings.vSyncCount = 0;
            Application.targetFrameRate = 60;
            Application.runInBackground = true;
        }
    }
}
