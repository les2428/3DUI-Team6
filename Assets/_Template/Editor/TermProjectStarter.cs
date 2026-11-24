using System.IO;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;
using Object = UnityEngine.Object;

namespace ThreeDUI.Editor
{
    /// <summary>
    /// Builds the starting scene for a term project. Every requirement in the brief is already
    /// here as a working placeholder, labelled with what it is for, so a team replaces things
    /// rather than wiring infrastructure from scratch - which the brief says is not the point.
    ///
    /// The scene is laid out along the user journey the brief asks you to design:
    ///     ENTRY -> EXPLORE -> CORE (your signature interaction) -> EXIT
    /// Those are marker objects, not walls. Move them, rename them, build your space around them.
    /// </summary>
    public static class TermProjectStarter
    {
        [MenuItem("3DUI/Term Project/New Term Project Scene...", priority = 40)]
        public static void CreateMenu()
        {
            string path = EditorUtility.SaveFilePanelInProject(
                "New term project scene", "TermProject", "unity",
                "Where to save it? Use your team name, e.g. Assets/Teams/Team07/Team07.unity", "Assets/Teams");
            if (string.IsNullOrEmpty(path)) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            Build(path);
            EditorSceneManager.OpenScene(path);
            Debug.Log("[3DUI] Term project scene created at " + path +
                      ". Start with the objects marked TODO in the Hierarchy.");
        }

        /// <summary>Command line: -buildScene &lt;path&gt;</summary>
        public static void CreateFromCommandLine()
        {
            string path = "Assets/Teams/TermProject.unity";
            var argv = System.Environment.GetCommandLineArgs();
            for (int i = 0; i < argv.Length - 1; i++) if (argv[i] == "-buildScene") path = argv[i + 1];
            try { Build(path); EditorApplication.Exit(0); }
            catch (System.Exception e) { Debug.LogException(e); EditorApplication.Exit(1); }
        }

        public static void Build(string path)
        {
            var scene = TemplateSceneBuilder.NewSceneForTermProject("My WebXR Space", out var env);

            TemplateSceneBuilder.AddRigAt(new Vector3(0f, 0f, -6f), 0f);
            TemplateSceneBuilder.RoomAround(env, 7f, 3.2f);

            // ---- 1. ENTRY -------------------------------------------------------------------
            var entry = Group("1. ENTRY - what a visitor meets first", env, new Vector3(0f, 0f, -6f));
            var welcome = TemplateSceneBuilder.Panel(entry, "Welcome panel", new Vector2(1.6f, 0.8f),
                "TODO: name your space",
                "TODO: one or two sentences telling a visitor what this place is and what they can do here.\n\n" +
                "This is your Spatial UI requirement. Keep it short - it is read standing up.");
            // Off to the left, so it does not hide the rest of the space from the start point.
            welcome.transform.position = new Vector3(-1.3f, 1.9f, -2.6f);
            TemplateSceneBuilder.FaceThePanel(welcome, new Vector3(0f, 1.6f, -6f));
            TemplateSceneBuilder.Spawn("Teleport Anchor", entry, new Vector3(0f, 0f, -6f)).name = "Viewpoint: Entrance";

            // ★ MY SPACE: team name, title and one sentence, edited in one Inspector; the welcome panel follows.
            var mySpaceGo = new GameObject("★ MY SPACE");
            mySpaceGo.transform.SetSiblingIndex(0);
            var mySpace = mySpaceGo.AddComponent<ThreeDUI.MySpace>();
            var mySo = new SerializedObject(mySpace);
            mySo.FindProperty("welcomePanel").objectReferenceValue = welcome;
            mySo.ApplyModifiedPropertiesWithoutUndo();
            mySpace.Apply();

            // ---- 2. EXPLORE -----------------------------------------------------------------
            var explore = Group("2. EXPLORE - things worth walking over to", env, Vector3.zero);
            TemplateSceneBuilder.Spawn("Teleport Anchor", explore, new Vector3(-3.5f, 0f, -1f)).name = "Viewpoint: Left";
            TemplateSceneBuilder.Spawn("Teleport Anchor", explore, new Vector3(3.5f, 0f, -1f)).name = "Viewpoint: Right";

            var grab = TemplateSceneBuilder.Spawn("Grabbable Cube", explore, new Vector3(-2.2f, 0.9f, -1f));
            grab.name = "TODO: interactive object (replace me)";

            var info = TemplateSceneBuilder.Spawn("Info Point", explore, new Vector3(2.2f, 0f, -1f));
            info.name = "TODO: information point (replace my text)";

            // ---- 3. CORE --------------------------------------------------------------------
            var core = Group("3. CORE - your signature interaction", env, new Vector3(0f, 0f, 2.5f));
            TemplateSceneBuilder.Block(core, "Pedestal", new Vector3(0f, 0.45f, 2.5f), new Vector3(0.9f, 0.9f, 0.9f));

            var button = TemplateSceneBuilder.Spawn("Push Button", core, new Vector3(0.9f, 0f, 1.7f));
            button.name = "TODO: what triggers your signature interaction";

            var sigPanel = TemplateSceneBuilder.Panel(core, "Signature panel", new Vector2(1.6f, 0.7f),
                "TODO: what just happened?",
                "This panel is the FEEDBACK half of your signature interaction. Say what changed, " +
                "in the visitor's words, not the system's.");
            sigPanel.transform.position = new Vector3(0f, 2.0f, 4.0f);
            TemplateSceneBuilder.FaceThePanel(sigPanel, new Vector3(0f, 1.6f, 2.5f));

            // Something that appears when the interaction is completed: the placeholder reward.
            var reveal = TemplateSceneBuilder.Block(core, "TODO: what appears when it is done", new Vector3(0f, 1.25f, 2.5f),
                                                    new Vector3(0.35f, 0.35f, 0.35f));
            reveal.GetComponent<Renderer>().sharedMaterial =
                AssetDatabase.LoadAssetAtPath<Material>("Assets/_Template/Materials/Glow.mat");
            Object.DestroyImmediate(reveal.GetComponent<Collider>());

            // The signature component lives in the team folder (Assembly-CSharp), so it is found by name.
            var sigGo = new GameObject("SIGNATURE INTERACTION - fill this in");
            sigGo.transform.SetParent(core, false);
            sigGo.transform.position = new Vector3(0f, 1.15f, 2.5f);
            var sigType = System.Type.GetType("TeamTemplate.TeamSignatureInteraction, Assembly-CSharp");
            if (sigType == null)
            {
                Debug.LogWarning("[3DUI] TeamSignatureInteraction is not compiled yet; add it to '" + sigGo.name + "' by hand.");
            }
            else
            {
                var sig = sigGo.AddComponent(sigType);
                var so = new SerializedObject(sig);
                so.FindProperty("title").stringValue = "TODO: name your signature interaction";
                so.FindProperty("description").stringValue = "Press the button to try the placeholder. Then replace it with yours.";
                so.FindProperty("discoverability").stringValue = "TODO: what tells a visitor this can be used, before they touch it?";
                var steps = so.FindProperty("steps");
                steps.arraySize = 1;
                steps.GetArrayElementAtIndex(0).objectReferenceValue = button.GetComponentInChildren<ThreeDUI.Interactable>();
                so.FindProperty("feedbackPanel").objectReferenceValue = sigPanel;
                so.FindProperty("revealOnComplete").objectReferenceValue = reveal;
                so.ApplyModifiedPropertiesWithoutUndo();
            }

            // ---- 4. EXIT --------------------------------------------------------------------
            var exit = Group("4. EXIT - how a visit ends", env, new Vector3(0f, 0f, 5.5f));
            var bye = TemplateSceneBuilder.Panel(exit, "Closing panel", new Vector2(1.6f, 0.6f),
                "TODO: closing",
                "TODO: what should a visitor leave knowing, or be invited to do next?");
            bye.transform.position = new Vector3(-4.5f, 1.7f, 5.4f);
            TemplateSceneBuilder.FaceThePanel(bye, new Vector3(-3.5f, 1.6f, 4.0f));

            // ---- the checklist, in the scene where it will be read --------------------------
            // Positioned by its own group: the canvas, created last, had its own position reset on save.
            var checklistGroup = Group("Checklist (delete before submitting)", env, new Vector3(2.0f, 1.9f, -2.4f));
            var checklist = TemplateSceneBuilder.Panel(checklistGroup, "Checklist panel",
                new Vector2(2.0f, 1.3f), "Minimum requirements",
                "1. Runs in a desktop browser AND in the headset\n" +
                "2. Basic navigation - the rig and the viewpoints are already here\n" +
                "3. Interactive 3D objects\n" +
                "4. Spatial UI / information presentation\n" +
                "5. Clear feedback for every action\n" +
                "6. One signature interaction tied to your purpose\n" +
                "7. Light enough to run smoothly on the web\n\n" +
                "Delete this panel when you submit.");
            checklist.transform.localPosition = Vector3.zero;
            TemplateSceneBuilder.FaceThePanel(checklist, new Vector3(0f, 1.9f, -6f));

            TemplateProjectSetup.EnsureFolder(Path.GetDirectoryName(path).Replace('\\', '/'));
            CommitPanelPositions();
            EditorSceneManager.SaveScene(scene, path);

            // Your scene goes first in the build, so the web build opens straight into it.
            var scenes = new System.Collections.Generic.List<EditorBuildSettingsScene> { new EditorBuildSettingsScene(path, true) };
            foreach (var s in EditorBuildSettings.scenes)
                if (s.path != path) scenes.Add(new EditorBuildSettingsScene(s.path, false));
            EditorBuildSettings.scenes = scenes.ToArray();
        }

        /// <summary>
        /// A world-space panel placed from code and saved in the same step could be written with its x/y
        /// at 0 (seen in batch mode). Reading and re-writing each panel's position before saving avoids it.
        /// </summary>
        public static void CommitPanelPositions()
        {
            foreach (var p in Object.FindObjectsByType<ThreeDUI.WorldPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                var rt = (RectTransform)p.transform;
                rt.anchoredPosition3D = rt.anchoredPosition3D;
            }
        }

        private static Transform Group(string name, Transform parent, Vector3 pos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.position = pos;
            return go.transform;
        }
    }
}
