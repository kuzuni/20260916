using System;
using System.IO;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;

namespace Moonlit.Editor
{
    // Use the same GameCI activation path as the verified Linux build.
    // Register again after PlayMode domain reload; never turn zero tests into a pass.
    [InitializeOnLoad]
    public static class CloudTests
    {
        const string Active = "Moonlit.CloudTests.Active";
        const string Deadline = "Moonlit.CloudTests.Deadline";
        const string ExitCode = "Moonlit.CloudTests.ExitCode";
        const string Results = "Artifacts/TestResults";

        static CloudTests()
        {
            TestRunnerApi.RegisterTestCallback(new ResultsCallback());
            EditorApplication.update += Tick;
        }

        public static void Run()
        {
            CombatAssetBuilder.Build();
            CelestialThumbnailBuilder.Build();
            Directory.CreateDirectory(Results);
            SessionState.SetBool(Active, true);
            SessionState.SetInt(ExitCode, -1);
            SessionState.SetString(Deadline, DateTime.UtcNow.AddMinutes(15).Ticks.ToString());
            var api = UnityEngine.ScriptableObject.CreateInstance<TestRunnerApi>();
            api.Execute(new ExecutionSettings(new Filter {
                testMode = TestMode.PlayMode,
                assemblyNames = new[] { "Moonlit.UI.PlayModeTests" }
            }));
        }

        static void Tick()
        {
            if (!SessionState.GetBool(Active, false)) return;
            var exit = SessionState.GetInt(ExitCode, -1);
            if (exit >= 0 && !EditorApplication.isPlayingOrWillChangePlaymode)
            {
                SessionState.SetBool(Active, false);
                EditorApplication.Exit(exit);
            }
            else if (long.TryParse(SessionState.GetString(Deadline, ""), out var deadline) && DateTime.UtcNow.Ticks > deadline)
            {
                File.WriteAllText(Results + "/failure.txt", "FAIL: PlayMode tests did not complete within 15 minutes.");
                SessionState.SetBool(Active, false);
                EditorApplication.Exit(1);
            }
        }

        sealed class ResultsCallback : ICallbacks
        {
            public void RunStarted(ITestAdaptor testsToRun) { }
            public void TestStarted(ITestAdaptor test) { }
            public void TestFinished(ITestResultAdaptor result) { }
            public void RunFinished(ITestResultAdaptor result)
            {
                if (!SessionState.GetBool(Active, false)) return;
                TestRunnerApi.SaveResultToFile(result, Results + "/playmode-results.xml");
                bool passed = result.PassCount > 0 && result.FailCount == 0 && result.SkipCount == 0 && result.InconclusiveCount == 0;
                File.WriteAllText(Results + "/summary.txt", $"{(passed ? "PASS" : "FAIL")} passed={result.PassCount} failed={result.FailCount} skipped={result.SkipCount} inconclusive={result.InconclusiveCount}\n");
                SessionState.SetInt(ExitCode, passed ? 0 : 1);
            }
        }
    }
}
