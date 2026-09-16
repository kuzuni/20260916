using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace Moonlit.Editor
{
    /// <summary>Batch entry point used only by the graphics-capable CI job.</summary>
    public static class CloudVerification
    {
        const string Scene = "Assets/DarkFantasyUI/Scenes/MoonlitMain.unity";
        static bool started;

        public static void Run()
        {
            EditorSceneManager.OpenScene(Scene);
            EditorApplication.update += Tick;
            MainScreenBuilder.Capture();
        }

        static void Tick()
        {
            if (EditorApplication.isPlaying) { started = true; return; }
            if (!started) return;
            EditorApplication.update -= Tick;
            string report = File.Exists("Artifacts/Verification.txt") ? File.ReadAllText("Artifacts/Verification.txt") : "";
            bool passed = report.StartsWith("PASS");
            if (!passed) Debug.LogError("Moonlit runtime capture verification failed. See the uploaded report and log.");
            EditorApplication.Exit(passed ? 0 : 1);
        }
    }
}
