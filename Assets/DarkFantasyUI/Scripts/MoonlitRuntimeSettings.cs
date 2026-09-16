using UnityEngine;

namespace Moonlit.UI
{
    public static class MoonlitRuntimeSettings
    {
        // SubsystemRegistration also runs when Enter Play Mode skips domain reload.
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        public static void ResetSession()
        {
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
