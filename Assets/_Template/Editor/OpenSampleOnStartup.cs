using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace ThreeDUI.Editor
{
    /// <summary>
    /// The first time the project is opened (empty "Untitled" scene), open the Sample Playground so
    /// students land in a working scene. Later sessions keep whatever scene was last open.
    /// Also: 3DUI > Open Sample Playground.
    /// </summary>
    [InitializeOnLoad]
    internal static class OpenSampleOnStartup
    {
        private const string Key = "ThreeDUI.OpenedSampleOnce";
        internal const string SamplePath = "Assets/_Template/Scenes/SamplePlayground.unity";

        static OpenSampleOnStartup()
        {
            if (Application.isBatchMode || SessionState.GetBool(Key, false)) return;
            SessionState.SetBool(Key, true);
            EditorApplication.delayCall += () =>
            {
                if (EditorApplication.isPlayingOrWillChangePlaymode) return;
                Scene active = SceneManager.GetActiveScene();
                bool untitled = string.IsNullOrEmpty(active.path) && !active.isDirty;
                if (untitled && File.Exists(SamplePath)) EditorSceneManager.OpenScene(SamplePath);
            };
        }

        [MenuItem("3DUI/Open Sample Playground", priority = 0)]
        private static void OpenSample()
        {
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            EditorSceneManager.OpenScene(SamplePath);
        }
    }
}
