using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Moonlit.Editor
{
    /// <summary>Batch entry point used only by the graphics-capable CI job.</summary>
    [InitializeOnLoad]
    public static class CloudVerification
    {
        const string Scene = "Assets/DarkFantasyUI/Scenes/MoonlitMain.unity";
        const string ActiveKey = "Moonlit.CloudCapture.Active";
        const string StartedKey = "Moonlit.CloudCapture.Started";

        static CloudVerification()
        {
            EditorApplication.update += Tick;
        }

        public static void Run()
        {
            CombatAssetBuilder.Build();
            CompanionAssetBuilder.Build();
            CelestialThumbnailBuilder.Build();
            Directory.CreateDirectory("Artifacts");
            if (File.Exists("Artifacts/Verification.txt")) File.Delete("Artifacts/Verification.txt");
            SessionState.SetBool(ActiveKey, true);
            SessionState.SetBool(StartedKey, false);
            EditorSceneManager.OpenScene(Scene);
            MainScreenBuilder.Capture();
        }

        static void Tick()
        {
            if (!SessionState.GetBool(ActiveKey, false)) return;
            if (EditorApplication.isPlaying) { SessionState.SetBool(StartedKey, true); return; }
            if (!SessionState.GetBool(StartedKey, false) || EditorApplication.isPlayingOrWillChangePlaymode) return;
            SessionState.SetBool(ActiveKey, false);
            EditorApplication.update -= Tick;
            string report = File.Exists("Artifacts/Verification.txt") ? File.ReadAllText("Artifacts/Verification.txt") : "";
            bool passed = report.StartsWith("PASS");
            if (!passed) Debug.LogError("Moonlit runtime capture verification failed. See the uploaded report and log.");
            EditorApplication.Exit(passed ? 0 : 1);
        }
    }
}
