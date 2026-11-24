using System;
using System.IO;
using System.Linq;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEngine;

namespace ThreeDUI.Editor
{
    /// <summary>
    /// WebGL build helpers (menu: 3DUI > Build) and command-line entry points.
    ///
    /// Command line (e.g. for CI / the course build server):
    ///   Unity.exe -batchmode -quit -projectPath . -executeMethod ThreeDUI.Editor.WebGLBuilder.BuildFromCommandLine -buildPath Builds/WebGL
    ///   Unity.exe -batchmode -quit -projectPath . -executeMethod ThreeDUI.Editor.WebGLBuilder.SetupFromCommandLine
    /// </summary>
    public static class WebGLBuilder
    {
        private const string DefaultBuildPath = "Builds/WebGL";

        [MenuItem("3DUI/Build/Build WebGL (Builds/WebGL)", priority = 200)]
        public static void BuildMenu()
        {
            var report = Build(DefaultBuildPath);
            if (report != null && report.summary.result == BuildResult.Succeeded)
                EditorUtility.RevealInFinder(Path.GetFullPath(DefaultBuildPath));
        }

        [MenuItem("3DUI/Build/Build WebGL (current scene only)", priority = 201)]
        public static void BuildCurrentSceneMenu()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(scene.path))
            {
                EditorUtility.DisplayDialog("3DUI", "Save the scene first.", "OK");
                return;
            }
            Build(DefaultBuildPath, new[] { scene.path });
        }

        /// <summary>
        /// Builds the open scene and lets Unity serve it on http://localhost and open your browser.
        /// localhost counts as a secure context, so WebXR works here without HTTPS.
        /// </summary>
        [MenuItem("3DUI/Build/Build and Run locally (current scene)", priority = 202)]
        public static void BuildAndRunCurrentSceneMenu()
        {
            var scene = UnityEditor.SceneManagement.EditorSceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(scene.path))
            {
                EditorUtility.DisplayDialog("3DUI", "Save the scene first.", "OK");
                return;
            }
            if (!UnityEditor.SceneManagement.EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build(DefaultBuildPath, new[] { scene.path }, BuildOptions.AutoRunPlayer);
        }

        public static BuildReport Build(string outputPath, string[] scenes = null, BuildOptions buildOptions = BuildOptions.None)
        {
            if (!BuildPipeline.IsBuildTargetSupported(BuildTargetGroup.WebGL, BuildTarget.WebGL))
            {
                Debug.LogError("[3DUI] WebGL Build Support is not installed. Add it in Unity Hub > Installs > (this version) > Add modules.");
                return null;
            }

            scenes ??= EditorBuildSettings.scenes.Where(s => s.enabled).Select(s => s.path).ToArray();
            if (scenes.Length == 0)
            {
                Debug.LogError("[3DUI] No scenes in Build Settings.");
                return null;
            }

            if (EditorUserBuildSettings.activeBuildTarget != BuildTarget.WebGL)
                EditorUserBuildSettings.SwitchActiveBuildTarget(BuildTargetGroup.WebGL, BuildTarget.WebGL);

            var options = new BuildPlayerOptions
            {
                scenes = scenes,
                locationPathName = outputPath,
                target = BuildTarget.WebGL,
                targetGroup = BuildTargetGroup.WebGL,
                options = buildOptions
            };
            var report = BuildPipeline.BuildPlayer(options);
            var s = report.summary;
            if (s.result == BuildResult.Succeeded)
                EditorPrefs.SetFloat(BuildSizeKey, FolderMB(Path.Combine(outputPath, "Build")));
            Debug.Log($"[3DUI] WebGL build {s.result}: {s.outputPath}  download size={FolderMB(Path.Combine(outputPath, "Build")):F1} MB  time={s.totalTime}");
            return report;
        }

        private static string BuildSizeKey => "ThreeDUI.LastBuildMB." + Application.dataPath;

        /// <summary>Size of the last successful web build's files in MB (what visitors download), or -1.</summary>
        public static float LastBuildMB() => EditorPrefs.GetFloat(BuildSizeKey, -1f);

        public static float FolderMB(string dir)
        {
            if (!Directory.Exists(dir)) return -1f;
            long bytes = 0;
            foreach (var f in Directory.GetFiles(dir, "*", SearchOption.AllDirectories)) bytes += new FileInfo(f).Length;
            return bytes / (1024f * 1024f);
        }

        // ------------------------------------------------------------------ Command line

        public static void SetupFromCommandLine()
        {
            try
            {
                TemplateProjectSetup.ApplyAll();
                TemplateSceneBuilder.RebuildAll();
                EditorApplication.Exit(0);
            }
            catch (Exception e)
            {
                Debug.LogException(e);
                EditorApplication.Exit(1);
            }
        }

        public static void BuildFromCommandLine()
        {
            string path = DefaultBuildPath;
            var args = Environment.GetCommandLineArgs();
            int i = Array.IndexOf(args, "-buildPath");
            if (i >= 0 && i + 1 < args.Length) path = args[i + 1];

            // Optional: -scenes Assets/a.unity;Assets/b.unity (default: the enabled scenes in Build Settings)
            string[] scenes = null;
            int j = Array.IndexOf(args, "-scenes");
            if (j >= 0 && j + 1 < args.Length) scenes = args[j + 1].Split(';');

            var report = Build(path, scenes);
            EditorApplication.Exit(report != null && report.summary.result == BuildResult.Succeeded ? 0 : 1);
        }
    }
}
