using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ThreeDUI.Editor
{
    /// <summary>
    /// Tutorial 2, advanced navigation: overview map (map-based travel), breadcrumbs, comfort vignette.
    /// Self-contained on purpose, so it can ship in every update package.
    /// </summary>
    public static class NavigationAidsTools
    {
        const string T2Scene = "Assets/_Template/Examples/T2_Wayfinding.unity";

        [MenuItem("3DUI/Create/Navigation/Overview Map (press N)", priority = 129)]
        private static void MenuMap() => Select(Create("Overview Map", go => go.AddComponent<OverviewMap>()),
            "Overview Map added: press N (desktop) or B / Y (headset). Click a floor on the map to travel there.");

        [MenuItem("3DUI/Create/Navigation/Map Board (fixed in the room)", priority = 130)]
        private static void MenuBoard() => Select(Create("Map Board", go => go.AddComponent<OverviewMap>().Frame = TravelMenu.MenuFrame.World),
            "Map Board added: a 'you are here' map that stays where you put it. Turn it to face the visitor.");

        [MenuItem("3DUI/Create/Navigation/Breadcrumbs", priority = 131)]
        private static void MenuCrumbs() => Select(Create("Breadcrumbs", go => go.AddComponent<Breadcrumbs>()),
            "Breadcrumbs added: a fading trail behind the visitor.");

        [MenuItem("3DUI/Create/Navigation/Comfort Vignette", priority = 132)]
        private static void MenuVignette() => Select(Create("Comfort Vignette", go => go.AddComponent<ComfortVignette>()),
            "Comfort Vignette added: the edges of the view darken while walking or turning smoothly.");

        [MenuItem("3DUI/Examples/T2 Navigation aids (map, breadcrumbs, vignette)", priority = 20)]
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

        /// <summary>Adds the travel menu, a map board, breadcrumbs and the vignette to the open T2 example scene.</summary>
        public static void AddToExample()
        {
            TravelMenuTools.AddToExample();
            if (Object.FindAnyObjectByType<OverviewMap>() != null) return;
            var parent = GameObject.Find("Wayfinding ideas")?.transform;
            var entrance = new Vector3(-2f, 1.6f, -6f);

            var board = new GameObject("Map Board (World frame)").transform;
            board.SetParent(parent, false);
            board.position = new Vector3(-0.3f, 1.35f, -4.6f);
            board.rotation = Facing(board.position, entrance);
            board.gameObject.AddComponent<OverviewMap>().Frame = TravelMenu.MenuFrame.World;
            EditorUtility.SetDirty(board.GetComponent<OverviewMap>());

            var aids = new GameObject("Navigation aids").transform;
            aids.SetParent(parent, false);
            aids.gameObject.AddComponent<Breadcrumbs>();
            aids.gameObject.AddComponent<ComfortVignette>();

            Note(parent, "6. Map, breadcrumbs, vignette",
                "Map board: you are here; click a floor to travel there.\nOrange dots: where you have been (older = fainter).\nWalk with WASD: the view's edges darken while moving.",
                new Vector3(1.9f, 1.85f, -4.2f), entrance);
            EditorSceneManager.MarkSceneDirty(board.gameObject.scene);
        }

        static Quaternion Facing(Vector3 at, Vector3 viewer)
        {
            var look = at - viewer; look.y = 0f;
            return Quaternion.LookRotation(look, Vector3.up);
        }

        static void Note(Transform parent, string title, string body, Vector3 pos, Vector3 viewer)
        {
            // A plain group carries the position (see TermProjectStarter.CommitPanelPositions).
            var group = new GameObject("Note: " + title).transform;
            group.SetParent(parent, false);
            group.SetPositionAndRotation(pos, Facing(pos, viewer));
            var note = WorldPanel.Create(group, "Panel", new Vector2(1.5f, 0.75f), title, body);
            note.TitleText.enableAutoSizing = false; note.TitleText.fontSize = 72;
            note.BodyText.enableAutoSizing = false; note.BodyText.fontSize = 46;
            EditorUtility.SetDirty(note.TitleText); EditorUtility.SetDirty(note.BodyText);
        }

        static GameObject Create(string name, System.Action<GameObject> setup)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            setup(go);
            if (SceneView.lastActiveSceneView != null) go.transform.position = SceneView.lastActiveSceneView.pivot;
            EditorSceneManager.MarkSceneDirty(go.scene);
            return go;
        }

        static void Select(GameObject go, string message)
        {
            Selection.activeGameObject = go;
            EditorGUIUtility.PingObject(go);
            Debug.Log("[3DUI] " + message);
        }
    }
}
