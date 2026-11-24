using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ThreeDUI.Editor
{
    /// <summary>
    /// Tutorial 2, interaction techniques: reach techniques (Go-Go, HOMER, Flashlight), steering travel, and a "reach range" in the T2 example.
    /// Self-contained on purpose, so it can ship in every update package.
    /// </summary>
    public static class TechniquesTools
    {
        const string T2Scene = "Assets/_Template/Examples/T2_Wayfinding.unity";
        const string Cube = "Assets/_Template/Prefabs/Interactions/Grabbable Cube.prefab";
        const string Anchor = "Assets/_Template/Prefabs/Interactions/Teleport Anchor.prefab";

        [MenuItem("3DUI/Create/Techniques/Go-Go Hand (G · headset: A)", priority = 133)]
        private static void MenuGoGo() => Select(Create("Go-Go Hand", go => go.AddComponent<GoGoHand>()),
            "Go-Go Hand added. Play, press G: aim with the mouse, X / Z (or wheel) = arm length, left button = grab. Headset: right controller + trigger.");

        [MenuItem("3DUI/Create/Techniques/HOMER Hand (G · headset: A)", priority = 134)]
        private static void MenuHomer() => Select(Create("HOMER Hand", go => go.AddComponent<HomerHand>()),
            "HOMER Hand added. Play, press G until HOMER: point at an object, hold the left button, then X / Z (or wheel) move it in and out, the mouse sweeps it around you. Headset: right controller + trigger.");

        [MenuItem("3DUI/Create/Techniques/Flashlight (cone) Pointer (G · headset: A)", priority = 135)]
        private static void MenuFlashlight() => Select(Create("Flashlight Pointer", go => go.AddComponent<FlashlightPointer>()),
            "Flashlight Pointer added. Play, press G until Flashlight: the object closest to the centre of the circle is chosen; left button grabs, wheel pushes / pulls.");

        [MenuItem("3DUI/Create/Techniques/All reach techniques (G cycles)", priority = 136)]
        private static void MenuAll() => Select(Create("Reach Techniques", go =>
            { go.AddComponent<GoGoHand>(); go.AddComponent<HomerHand>(); go.AddComponent<FlashlightPointer>(); }),
            "Go-Go, HOMER and Flashlight added. In Play, G cycles: ray → Go-Go → HOMER → Flashlight → ray. Remove the ones you do not use.");

        [MenuItem("3DUI/Create/Techniques/Steering Travel (hold T)", priority = 140)]
        private static void MenuSteering() => Select(Create("Steering Travel", go => go.AddComponent<SteeringTravel>()),
            "Steering Travel added. Hold T to move where the mouse points; Y switches Gaze / Pointing. Headset: left stick forward.");

        [MenuItem("3DUI/Examples/T2 Techniques (Go-Go, HOMER, Flashlight)", priority = 20)]
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
            AddReachTechniques();
            EditorSceneManager.SaveScene(scene);
        }

        /// <summary>
        /// Adds a Go-Go range (cubes at 1, 2.5 and 4.5 m from a viewpoint), a Go-Go hand and steering travel to the open T2 example.
        /// </summary>
        public static void AddToExample()
        {
            if (Object.FindAnyObjectByType<GoGoHand>() != null) return;
            var parent = new GameObject("Techniques (T2)").transform;
            parent.gameObject.AddComponent<GoGoHand>();
            parent.gameObject.AddComponent<SteeringTravel>();

            // The range runs along +z from a viewpoint in the south-west corner.
            var range = new GameObject("Go-Go range").transform;
            range.SetParent(parent, false);
            var stand = new Vector3(-6.8f, 0f, -7.2f);
            var vp = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Anchor), range);
            vp.name = "Viewpoint: Go-Go range";
            vp.transform.SetPositionAndRotation(stand, Quaternion.identity);
            var wood = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Template/Materials/Wood.mat");
            // Staggered sideways so the near cube does not hide the far ones.
            foreach (var (dist, side, label) in new[] { (1.0f, -0.55f, "1 m · arm's reach"), (2.5f, 0.45f, "2.5 m · Go-Go"), (4.5f, -0.15f, "4.5 m · Go-Go, far") })
            {
                var p = stand + new Vector3(side, 0f, dist);
                var stand3 = GameObject.CreatePrimitive(PrimitiveType.Cube);
                stand3.name = "Stand " + dist + " m";
                stand3.transform.SetParent(range, false);
                stand3.transform.position = p + Vector3.up * 0.5f;
                stand3.transform.localScale = new Vector3(0.35f, 1.0f, 0.35f);
                if (wood != null) stand3.GetComponent<Renderer>().sharedMaterial = wood;
                GameObjectUtility.SetStaticEditorFlags(stand3, StaticEditorFlags.BatchingStatic);

                var cube = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Cube), range);
                cube.name = "Cube at " + dist + " m";
                cube.transform.position = p + Vector3.up * 1.1f;
                var g = cube.GetComponent<Interactable>();
                if (g != null) { g.DisplayName = label; EditorUtility.SetDirty(g); }
            }

            // A note beside the range, facing the viewpoint.
            var group = new GameObject("Note: 7. Go-Go range").transform;
            group.SetParent(parent, false);
            group.position = stand + new Vector3(1.4f, 1.8f, 2.2f);
            var look = group.position - (stand + Vector3.up * 1.6f); look.y = 0f;
            group.rotation = Quaternion.LookRotation(look, Vector3.up);
            var note = WorldPanel.Create(group, "Panel", new Vector2(1.4f, 0.75f), "7. Go-Go and steering",
                "G: switch ray / Go-Go hand. X / Z (or wheel) = arm length.\nGrab the cubes at 1, 2.5 and 4.5 m.\nHold T: steer where you point (Y: gaze / pointing).");
            note.TitleText.enableAutoSizing = false; note.TitleText.fontSize = 72;
            note.BodyText.enableAutoSizing = false; note.BodyText.fontSize = 44;
            EditorUtility.SetDirty(note.TitleText); EditorUtility.SetDirty(note.BodyText);
            EditorSceneManager.MarkSceneDirty(parent.gameObject.scene);
        }

        /// <summary>
        /// Adds HOMER and the Flashlight next to the Go-Go hand (G cycles through them), a shelf of small blocks for the flashlight,
        /// and rewrites the range's note. Safe to run again.
        /// </summary>
        public static void AddReachTechniques()
        {
            var gogo = Object.FindAnyObjectByType<GoGoHand>();
            if (gogo == null) { AddToExample(); gogo = Object.FindAnyObjectByType<GoGoHand>(); }
            if (Object.FindAnyObjectByType<HomerHand>() != null) { WriteReachNote(); return; }
            var parent = gogo.transform;
            parent.gameObject.AddComponent<HomerHand>();
            parent.gameObject.AddComponent<FlashlightPointer>();
            var stand = new Vector3(-6.8f, 0f, -7.2f);
            var range = parent.Find("Go-Go range");
            if (range != null) range.name = "Reach range";
            var far = GameObject.Find("Cube at 4.5 m")?.GetComponent<Interactable>();
            if (far != null) { far.DisplayName = "4.5 m · beyond Go-Go: HOMER"; EditorUtility.SetDirty(far); }

            // A shelf of small blocks: hard to hit with a thin ray, easy with a cone.
            var wood = AssetDatabase.LoadAssetAtPath<Material>("Assets/_Template/Materials/Wood.mat");
            var shelfPos = stand + new Vector3(1.3f, 0f, 4.2f);
            var shelf = GameObject.CreatePrimitive(PrimitiveType.Cube);
            shelf.name = "Shelf of small blocks";
            shelf.transform.SetParent(range != null ? range : parent, false);
            shelf.transform.position = shelfPos + Vector3.up * 0.5f;
            shelf.transform.localScale = new Vector3(0.6f, 1.0f, 0.3f);
            if (wood != null) shelf.GetComponent<Renderer>().sharedMaterial = wood;
            GameObjectUtility.SetStaticEditorFlags(shelf, StaticEditorFlags.BatchingStatic);
            var right = Vector3.Cross(Vector3.up, (shelfPos - stand).normalized);
            for (int i = 0; i < 5; i++)
            {
                var b = (GameObject)PrefabUtility.InstantiatePrefab(AssetDatabase.LoadAssetAtPath<GameObject>(Cube), shelf.transform.parent);
                b.name = "Small block " + (i + 1);
                b.transform.localScale = Vector3.one * 0.06f;
                b.transform.position = shelfPos + right * ((i - 2) * 0.1f) + Vector3.up * 1.035f;
                var g = b.GetComponent<Interactable>();
                if (g != null) { g.DisplayName = "small block · try the Flashlight"; EditorUtility.SetDirty(g); }
            }

            WriteReachNote();
            EditorSceneManager.MarkSceneDirty(parent.gameObject.scene);
        }

        static void WriteReachNote()
        {
            var note = GameObject.Find("Note: 7. Go-Go range") ?? GameObject.Find("Note: 7. Reach techniques");
            var panel = note != null ? note.GetComponentInChildren<WorldPanel>() : null;
            if (panel == null) return;
            note.name = "Note: 7. Reach techniques";
            panel.TitleText.text = "7. Reach: Go-Go · HOMER · Flashlight";
            panel.BodyText.text = "G (headset: A on the right controller): ray → Go-Go → HOMER → Flashlight.\n" +
                                  "Same task, three mappings: grab the cubes at 1, 2.5 and 4.5 m.\n" +
                                  "Small blocks on the shelf: ray, then Flashlight. X / Z (headset: right stick) = arm or cone.";
            panel.TitleText.fontSize = 60;
            panel.BodyText.fontSize = 36;
            EditorUtility.SetDirty(panel.TitleText); EditorUtility.SetDirty(panel.BodyText);
            EditorSceneManager.MarkSceneDirty(note.scene);
        }

        /// <summary>Batch mode: -executeMethod ThreeDUI.Editor.TechniquesTools.UpdateExampleBatch</summary>
        public static void UpdateExampleBatch()
        {
            var scene = EditorSceneManager.OpenScene(T2Scene);
            AddReachTechniques();
            EditorSceneManager.SaveScene(scene);
            EditorApplication.Exit(0);
        }

        static GameObject Create(string name, System.Action<GameObject> setup)
        {
            var go = new GameObject(name);
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            setup(go);
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
