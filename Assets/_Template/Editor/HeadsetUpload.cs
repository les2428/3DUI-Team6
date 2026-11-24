using System;
using System.IO;
using System.IO.Compression;
using UnityEditor;
using UnityEditor.Build.Reporting;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ThreeDUI.Editor
{
    /// <summary>
    /// 3DUI > Build > Build for headset (upload zip): builds the open scene for the web and packs it as
    /// Builds/&lt;TeamId&gt;.zip. You upload that zip; the course gallery puts it online at an HTTPS address,
    /// which is what the headset needs to enter VR (WebXR only runs on HTTPS or localhost).
    /// </summary>
    public static class HeadsetUpload
    {
        [Serializable]
        public class SpaceInfo
        {
            public string team, teamId, title, sentence, size, scene, builtAt;
            public float downloadMB;
            public string[] testQuestions = new string[0];
        }

        [MenuItem("3DUI/Build/Build for headset (upload zip)", priority = 203)]
        public static void BuildMenu()
        {
            var scene = EditorSceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(scene.path))
            {
                EditorUtility.DisplayDialog("3DUI", "Save your scene first (Ctrl+S).", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            string zip = Build(scene.path, out var info, out string error);
            if (zip == null) { EditorUtility.DisplayDialog("3DUI", "The build failed:\n" + error + "\n\nSee the Console.", "OK"); return; }
            string budget = info.downloadMB <= 60f ? "within the 60 MB budget" : "OVER the 60 MB budget — shrink textures before uploading";
            EditorUtility.DisplayDialog("3DUI · Ready to upload",
                $"{Path.GetFileName(zip)} ({info.downloadMB:0.0} MB, {budget}).\n\n" +
                "Upload this zip to the LMS assignment for the headset session.\n" +
                "Do not unzip it or rename it.", "Show the file");
            EditorUtility.RevealInFinder(zip);
        }

        /// <summary>Builds the scene to Builds/Upload/&lt;TeamId&gt;/ and zips it. Returns the zip path, or null.</summary>
        public static string Build(string scenePath, out SpaceInfo info, out string error)
        {
            EditorSceneManager.OpenScene(scenePath);
            var my = UnityEngine.Object.FindAnyObjectByType<MySpace>();
            info = new SpaceInfo
            {
                team = my != null ? my.TeamName : Path.GetFileNameWithoutExtension(scenePath),
                teamId = my != null ? my.TeamId : Path.GetFileNameWithoutExtension(scenePath).Replace(" ", ""),
                title = my != null ? my.SpaceTitle : "",
                sentence = my != null ? my.OneSentence : "",
                size = my != null ? my.Size.ToString() : "",
                scene = scenePath,
                builtAt = DateTime.Now.ToString("yyyy-MM-dd HH:mm"),
            };
            var questions = my != null ? my.GetType().GetProperty("TestQuestions") : null;   // added in tutorial 5
            if (questions != null) info.testQuestions = (string[])questions.GetValue(my);

            string dir = Path.Combine("Builds", "Upload", info.teamId);
            if (Directory.Exists(dir)) Directory.Delete(dir, true);
            var report = WebGLBuilder.Build(dir, new[] { scenePath });
            if (report == null || report.summary.result != BuildResult.Succeeded) { error = "WebGL build did not succeed."; return null; }

            info.downloadMB = WebGLBuilder.FolderMB(Path.Combine(dir, "Build"));
            File.WriteAllText(Path.Combine(dir, "space.json"), JsonUtility.ToJson(info, true));
            string zip = Path.Combine("Builds", info.teamId + ".zip");
            if (File.Exists(zip)) File.Delete(zip);
            ZipFile.CreateFromDirectory(dir, zip, System.IO.Compression.CompressionLevel.Optimal, false);
            error = null;
            Debug.Log($"[3DUI] Headset upload ready: {Path.GetFullPath(zip)}  ({info.downloadMB:0.0} MB download)");
            return Path.GetFullPath(zip);
        }

        /// <summary>Command line: -scene Assets/Teams/Team07/Team07.unity</summary>
        public static void BuildFromCommandLine()
        {
            var argv = Environment.GetCommandLineArgs();
            string scene = argv[Array.IndexOf(argv, "-scene") + 1];
            var zip = Build(scene, out _, out string error);
            if (zip == null) Debug.LogError("[3DUI] " + error);
            EditorApplication.Exit(zip != null ? 0 : 1);
        }
    }
}
