using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ThreeDUI.Editor
{
    /// <summary>
    /// Tutorial 2 (with the System Control lecture): a "Go to…" menu that lists the viewpoints.
    /// Self-contained on purpose, so it can ship in every update package.
    /// </summary>
    public static class TravelMenuTools
    {
        const string T2Scene = "Assets/_Template/Examples/T2_Wayfinding.unity";

        [MenuItem("3DUI/Create/Travel Menu (system control)", priority = 128)]
        private static void MenuCreate()
        {
            var go = Create(null);
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
            Debug.Log("[3DUI] Travel Menu added: press M (desktop) or A / X (headset) in Play. It lists every viewpoint; " +
                      "rename the viewpoints (e.g. 'Viewpoint: Record shelf') to name the items.");
        }

        [MenuItem("3DUI/Examples/T2 Travel menu (system control)", priority = 20)]
        private static void MenuExample()
        {
            if (!System.IO.File.Exists(T2Scene))
            {
                EditorUtility.DisplayDialog("3DUI", "The T2 Wayfinding example is missing. Import the latest update package first.", "OK");
                return;
            }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            var scene = EditorSceneManager.OpenScene(T2Scene);
            AddToExample();
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>Adds the travel menu and its note to the open T2 example scene (used by the menu above and by tests).</summary>
        public static void AddToExample()
        {
            if (Object.FindAnyObjectByType<TravelMenu>() != null) return;
            var parent = GameObject.Find("Wayfinding ideas")?.transform;
            Create(parent);

            // A plain group carries the position: a world-space canvas placed from code and saved right away can lose
            // its x/y in batch mode (see TermProjectStarter.CommitPanelPositions).
            var group = new GameObject("Note: 5. Travel menu").transform;
            group.SetParent(parent, false);
            // Beside the entrance viewpoint (-2, 0, -6), turned towards a visitor standing there.
            group.position = new Vector3(-3.8f, 1.9f, -2.9f);
            var look = group.position - new Vector3(-2f, 1.6f, -6f); look.y = 0f;
            group.rotation = Quaternion.LookRotation(look, Vector3.up);
            var note = WorldPanel.Create(group, "Panel", new Vector2(1.5f, 0.75f), "5. Travel menu (system control)",
                "Press M (desktop) or A / X (headset).\nOne item per viewpoint, in front of you, just below eye level,\nsized by visual angle. Pick one to jump there.");
            note.TitleText.enableAutoSizing = false; note.TitleText.fontSize = 72;
            note.BodyText.enableAutoSizing = false; note.BodyText.fontSize = 46;
            foreach (var p in new[] { note.TitleText, note.BodyText }) EditorUtility.SetDirty(p);
            EditorSceneManager.MarkSceneDirty(note.gameObject.scene);
        }

        private static GameObject Create(Transform parent)
        {
            var go = new GameObject("Travel Menu");
            Undo.RegisterCreatedObjectUndo(go, "Create Travel Menu");
            if (parent != null) go.transform.SetParent(parent, false);
            go.AddComponent<TravelMenu>();
            if (SceneView.lastActiveSceneView != null && parent == null)
                go.transform.position = SceneView.lastActiveSceneView.pivot;
            EditorSceneManager.MarkSceneDirty(go.scene);
            return go;
        }
    }
}
