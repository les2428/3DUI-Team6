using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Rendering;

namespace ThreeDUI.Editor
{
    /// <summary>
    /// Example scenes for the programming tutorials (menu: 3DUI > Examples). Each one shows the ideas of one
    /// tutorial in a small space, with a short panel next to each idea. They are rebuilt by
    /// 3DUI > Rebuild Template; your own scenes in Assets/Teams are never touched.
    /// </summary>
    public static partial class TemplateSceneBuilder
    {
        public const string ExampleDir = "Assets/_Template/Examples";
        public const string T2Path = ExampleDir + "/T2_Wayfinding.unity";

        // Each later tutorial adds its own file with one of these (an update package brings the file).
        static partial void BuildT3();
        static partial void BuildT4();
        static partial void BuildT5();

        private static void BuildTutorialExamples()
        {
            TemplateProjectSetup.EnsureFolder(ExampleDir);
            BuildT2Wayfinding();
            BuildT3();
            BuildT4();
            BuildT5();
        }

        [MenuItem("3DUI/Examples/T2 Wayfinding (two rooms, landmark, signpost)", priority = 20)]
        private static void OpenT2() => OpenExample(T2Path);

        internal static void OpenExample(string path)
        {
            if (!File.Exists(path))
            {
                EditorUtility.DisplayDialog("3DUI", "Example not found. Run 3DUI > Rebuild Template (prefabs + scenes) first.", "OK");
                return;
            }
            if (EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) EditorSceneManager.OpenScene(path);
        }

        /// <summary>A short label panel next to an idea, facing the given viewer position.</summary>
        internal static WorldPanel Note(Transform parent, string title, string body, Vector3 pos, Vector3 viewer, float width = 1.2f, float height = 0.55f)
        {
            var root = new GameObject("Note: " + title).transform;
            root.SetParent(parent, false);
            root.position = pos;          // the group carries the position (see TermProjectStarter.CommitPanelPositions)
            var p = WorldPanel.Create(root, "Panel", new Vector2(width, height), title, body);
            FacePanel(p, viewer);
            return p;
        }

        // ------------------------------------------------------------------ T2: wayfinding

        private static void BuildT2Wayfinding()
        {
            var scene = NewEnvironmentScene("T2 Wayfinding", out var env);
            WallsAround(env, 8f, 3f);

            // Room A (entry, z < 0) and room B (z > 0), joined by an off-centre doorway at x = 3
            var walls = new GameObject("Dividing wall (doorway at x = 3)").transform;
            walls.SetParent(env, false);
            Prim(PrimitiveType.Cube, "Wall left", walls, new Vector3(-2.75f, 1.5f, 0f), new Vector3(10.5f, 3f, 0.2f), "Wall").isStatic = true;
            Prim(PrimitiveType.Cube, "Wall right", walls, new Vector3(6.0f, 1.5f, 0f), new Vector3(4.0f, 3f, 0.2f), "Wall").isStatic = true;
            Prim(PrimitiveType.Cube, "Lintel", walls, new Vector3(3.5f, 2.55f, 0f), new Vector3(1.0f, 0.9f, 0.2f), "Wall").isStatic = true;
            Prim(PrimitiveType.Cube, "Door jamb", walls, new Vector3(2.75f, 1.05f, 0f), new Vector3(0.5f, 2.1f, 0.2f), "Wall").isStatic = true;

            AddRig(new Vector3(-2f, 0f, -6f), 0f);
            var ideas = new GameObject("Wayfinding ideas").transform;

            // 1. landmark: tall, bright, visible through the doorway from the entry
            var landmark = new GameObject("Landmark (glowing pillar)").transform;
            landmark.SetParent(ideas, false);
            landmark.position = new Vector3(4.5f, 0f, 5.5f);
            Prim(PrimitiveType.Cylinder, "Pillar", landmark, new Vector3(0f, 1.25f, 0f), new Vector3(0.5f, 1.25f, 0.5f), "Glow");
            var lamp = new GameObject("Light").AddComponent<Light>();
            lamp.transform.SetParent(landmark, false);
            lamp.transform.localPosition = new Vector3(0f, 2.2f, 0f);
            lamp.type = LightType.Point; lamp.range = 6f; lamp.intensity = 2.5f; lamp.color = new Color(1f, 0.85f, 0.5f);
            Note(ideas, "1. Landmark", "Something tall and bright at the place visitors should reach.\nYou can see it through the doorway from the entrance.",
                 new Vector3(4.5f, 1.9f, 4.4f), new Vector3(3f, 1.6f, 1.5f));

            // 2. signpost pointing at the landmark
            var sign = Interaction("Signpost", ideas, new Vector3(1.0f, 0f, -2.2f));
            var sp = sign.GetComponent<Signpost>();
            sp.Target = landmark;
            var so = new SerializedObject(sp);
            so.FindProperty("label").stringValue = "Exhibit";
            so.ApplyModifiedPropertiesWithoutUndo();
            Note(ideas, "2. Signpost", "3DUI > Create > Signpost. Drag the place into Target:\nthe arrow keeps pointing at it, even if you move things.",
                 new Vector3(-0.6f, 1.7f, -2.4f), new Vector3(-2f, 1.6f, -6f));

            // 3. viewpoints: where to stand, and which way to face
            Anchor(ideas, new Vector3(-2f, 0f, -6f), 0f, "Viewpoint: Entrance");
            Anchor(ideas, new Vector3(3.5f, 0f, -1.6f), 0f, "Viewpoint: Doorway");
            Anchor(ideas, new Vector3(2.5f, 0f, 3f), 40f, "Viewpoint: Exhibit");
            Note(ideas, "3. Viewpoints", "Blue circles: click (or point + trigger) to jump.\nThe arrow is the way you will face. Put them where the view matters.",
                 new Vector3(-4.6f, 1.7f, -4.6f), new Vector3(-2f, 1.6f, -6f));

            // 4. doorway sightline
            Note(ideas, "4. Sightline", "The doorway is off-centre, so the entrance does not show everything.\nThe landmark through it pulls the visitor on.",
                 new Vector3(3.5f, 2.35f, -0.3f), new Vector3(-2f, 1.6f, -6f), 1.3f, 0.5f);

            // room B content so there is something to find
            Prim(PrimitiveType.Cube, "Exhibit table", ideas, new Vector3(5.5f, 0.375f, 3.2f), new Vector3(1.4f, 0.75f, 0.7f), "Wood").isStatic = true;

            EditorSceneManager.SaveScene(scene, T2Path);
        }
    }
}
