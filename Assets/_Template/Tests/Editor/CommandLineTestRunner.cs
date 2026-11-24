using System.IO;
using System.Text;
using UnityEditor;
using UnityEditor.TestTools.TestRunner.Api;
using UnityEngine;

namespace ThreeDUI.Tests
{
    /// <summary>
    /// Runs the PlayMode tests from the command line via -executeMethod (works where -runTests is unavailable):
    ///   Unity.exe -batchmode -projectPath . -executeMethod ThreeDUI.Tests.CommandLineTestRunner.RunPlayMode
    /// Writes a summary to Logs/test-summary.txt and exits with 0 (all passed) or 1.
    /// </summary>
    [InitializeOnLoad]
    public static class CommandLineTestRunner
    {
        private const string ActiveKey = "ThreeDUI.CommandLineTestRunner.Active";

        static CommandLineTestRunner()
        {
            // Re-register after the domain reload that happens when entering play mode.
            if (SessionState.GetBool(ActiveKey, false))
                ScriptableObject.CreateInstance<TestRunnerApi>().RegisterCallbacks(new Callbacks());
        }

        public static void RunPlayMode()
        {
            SessionState.SetBool(ActiveKey, true);
            var api = ScriptableObject.CreateInstance<TestRunnerApi>();
            api.RegisterCallbacks(new Callbacks());
            api.Execute(new ExecutionSettings(new Filter { testMode = TestMode.PlayMode }));
        }

        private class Callbacks : ICallbacks
        {
            private readonly StringBuilder log = new StringBuilder();

            public void RunStarted(ITestAdaptor testsToRun) { }

            public void TestStarted(ITestAdaptor test) { }

            public void TestFinished(ITestResultAdaptor result)
            {
                if (result.HasChildren) return;
                string line = $"{result.TestStatus,-8} {result.FullName}";
                if (result.TestStatus == TestStatus.Failed) line += "\n         " + result.Message?.Trim().Replace("\n", "\n         ");
                Debug.Log("[TEST] " + line);
                log.AppendLine(line);
            }

            public void RunFinished(ITestResultAdaptor result)
            {
                string summary = $"passed={result.PassCount} failed={result.FailCount} skipped={result.SkipCount}";
                Debug.Log("[TEST] " + summary);
                Directory.CreateDirectory("Logs");
                File.WriteAllText("Logs/test-summary.txt", log + summary + "\n");
                SessionState.SetBool(ActiveKey, false);
                EditorApplication.Exit(result.FailCount > 0 ? 1 : 0);
            }
        }
    }
}
