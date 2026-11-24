using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ThreeDUI.Editor
{
    /// <summary>
    /// 3DUI > Check My Scene: the term-project requirements, checked against the open scene for your team size
    /// (set on ★ MY SPACE). Each section names the tutorial that covers it. Nothing is changed; use the
    /// Select buttons to find what needs work.
    /// </summary>
    public class SceneCheck : EditorWindow
    {
        public enum Level { Ok, Warn, Fail, Info }

        public class Item
        {
            public string Section, Text;
            public Level Level;
            public Object[] Objects = new Object[0];
        }

        // Requirements per team size: Solo, Pair, Trio
        static readonly int[] Viewpoints = { 2, 3, 4 }, InteractiveObjects = { 3, 5, 8 }, Panels = { 2, 3, 4 };
        const float MinTextDegrees = 0.7f;      // smallest comfortable text height in the headset, in degrees
        const int MaxRealtimeLights = 2, MaxTexture = 1024, MaxTriangles = 300000;

        List<Item> items = new List<Item>();
        Vector2 scroll;

        [MenuItem("3DUI/Check My Scene", priority = 30)]
        public static void Open()
        {
            var w = GetWindow<SceneCheck>("Check My Scene");
            w.minSize = new Vector2(420, 360);
            w.Run();
        }

        void OnFocus() => Run();
        void OnHierarchyChange() { Run(); Repaint(); }

        void Run() => items = Check();

        void OnGUI()
        {
            var scene = EditorSceneManager.GetActiveScene();
            var my = FindAnyObjectByType<MySpace>();
            EditorGUILayout.LabelField(scene.name, EditorStyles.boldLabel);
            EditorGUILayout.LabelField(my != null ? $"{my.TeamName} · {my.Size} — requirements for a {my.Size.ToString().ToLower()} team"
                                                  : "No ★ MY SPACE in this scene (pair requirements shown)", EditorStyles.miniLabel);
            if (GUILayout.Button("Check again")) Run();
            int fail = items.Count(i => i.Level == Level.Fail), warn = items.Count(i => i.Level == Level.Warn);
            EditorGUILayout.HelpBox(fail == 0 && warn == 0 ? "Everything checked here is fine." : $"{fail} to fix · {warn} to look at",
                                    fail > 0 ? MessageType.Error : warn > 0 ? MessageType.Warning : MessageType.Info);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            string section = null;
            foreach (var it in items)
            {
                if (it.Section != section)
                {
                    section = it.Section;
                    EditorGUILayout.Space(6);
                    EditorGUILayout.LabelField(section, EditorStyles.boldLabel);
                }
                EditorGUILayout.BeginHorizontal();
                string icon = it.Level == Level.Ok ? "✔" : it.Level == Level.Warn ? "⚠" : it.Level == Level.Fail ? "✖" : "•";
                var style = new GUIStyle(EditorStyles.wordWrappedLabel) { richText = true };
                string col = it.Level == Level.Ok ? "#3c9a4a" : it.Level == Level.Warn ? "#c08400" : it.Level == Level.Fail ? "#d04030" : "#808080";
                GUILayout.Label($"<color={col}>{icon}</color> {it.Text}", style);
                if (it.Objects.Length > 0 && GUILayout.Button("Select", GUILayout.Width(56)))
                {
                    Selection.objects = it.Objects;
                    EditorGUIUtility.PingObject(it.Objects[0]);
                }
                EditorGUILayout.EndHorizontal();
            }
            EditorGUILayout.EndScrollView();
        }

        // ------------------------------------------------------------------ the checks

        public static List<Item> Check()
        {
            var list = new List<Item>();
            void Add(string sec, Level lv, string text, IEnumerable<Object> objs = null) =>
                list.Add(new Item { Section = sec, Level = lv, Text = text, Objects = objs?.Where(o => o != null).ToArray() ?? new Object[0] });

            var scene = EditorSceneManager.GetActiveScene();
            var my = FindAnyObjectByType<MySpace>();
            int size = my != null ? (int)my.Size - 1 : 1;
            var rig = FindAnyObjectByType<PlayerRig>();
            bool InRig(Component c) => rig != null && c.transform.IsChildOf(rig.transform);
            bool Todo(Object o) => o.name.StartsWith("TODO");

            // ---- Tutorial 1 · your scene
            const string s1 = "Tutorial 1 · Your scene";
            Add(s1, scene.path.StartsWith("Assets/Teams/") && !scene.path.Contains("_TeamTemplate") ? Level.Ok : Level.Warn,
                string.IsNullOrEmpty(scene.path) ? "Scene not saved yet (Ctrl+S, into Assets/Teams/<YourTeam>/)" : "Saved in " + scene.path);
            if (my == null) Add(s1, Level.Fail, "No ★ MY SPACE: make your scene with 3DUI > Term Project > New Term Project Scene…");
            else
            {
                bool filled = !my.TeamName.Contains("??") && !my.SpaceTitle.StartsWith("TODO") && !my.OneSentence.StartsWith("TODO");
                Add(s1, filled ? Level.Ok : Level.Warn, filled ? "★ MY SPACE filled in" : "★ MY SPACE still has TODO text", new Object[] { my.gameObject });
            }

            // ---- Tutorial 2 · navigation
            const string s2 = "Tutorial 2 · Navigation";
            var anchors = FindObjectsByType<TeleportAnchor>(FindObjectsSortMode.None);
            Add(s2, anchors.Length >= Viewpoints[size] ? Level.Ok : Level.Fail,
                $"Viewpoints: {anchors.Length} (need {Viewpoints[size]})", anchors.Select(a => a.gameObject));
            var floors = FindObjectsByType<TeleportArea>(FindObjectsSortMode.None);
            Add(s2, floors.Length > 0 ? Level.Ok : Level.Fail, floors.Length > 0 ? $"Walkable floors: {floors.Length}" :
                "No walkable floor (select the floor: 3DUI > Models > Make walkable)", floors.Select(f => f.gameObject));
            Physics.SyncTransforms();
            bool OnFloor(Vector3 p) => Physics.RaycastAll(p + Vector3.up * 0.5f, Vector3.down, 3f)
                                              .Any(h => h.collider.GetComponentInParent<TeleportArea>() != null);
            if (rig != null)
                Add(s2, OnFloor(rig.transform.position) ? Level.Ok : Level.Fail,
                    OnFloor(rig.transform.position) ? "The visitor starts on a walkable floor" : "The PlayerRig is not standing on a walkable floor",
                    new Object[] { rig.gameObject });
            var floating = anchors.Where(a => !OnFloor(a.transform.position)).ToArray();
            if (floating.Length > 0) Add(s2, Level.Warn, $"{floating.Length} viewpoint(s) not on a walkable floor", floating.Select(a => a.gameObject));
            var signs = FindObjectsByType<Signpost>(FindObjectsSortMode.None);
            var noTarget = signs.Where(s => s.Target == null).ToArray();
            if (noTarget.Length > 0) Add(s2, Level.Warn, $"{noTarget.Length} signpost(s) without a target", noTarget.Select(s => s.gameObject));
            if (size == 2) Add(s2, signs.Length > 0 ? Level.Ok : Level.Info, $"Trio: wayfinding between zones — signposts in the scene: {signs.Length}",
                               signs.Select(s => s.gameObject));

            // ---- Tutorial 3 · interaction
            const string s3 = "Tutorial 3 · Interactive objects";
            var interactables = FindObjectsByType<Interactable>(FindObjectsSortMode.None)
                .Where(i => !(i is TeleportAnchor) && !(i is InfoPoint) && !InRig(i)).ToArray();
            var real = interactables.Where(i => !Todo(i) && !Todo(i.transform.root) && !(i.transform.parent != null && Todo(i.transform.parent))).ToArray();
            Add(s3, real.Length >= InteractiveObjects[size] ? Level.Ok : Level.Fail,
                $"Interactive objects: {real.Length} (need {InteractiveObjects[size]}; placeholders named TODO not counted)",
                real.Select(i => i.gameObject));
            var returnHome = System.Type.GetType("ThreeDUI.ReturnHome, ThreeDUI.Template");   // added in tutorial 3
            var lost = FindObjectsByType<Grabbable>(FindObjectsSortMode.None)
                .Where(g => g.GetComponent<Rigidbody>() != null && (returnHome == null || g.GetComponent(returnHome) == null)).ToArray();
            if (returnHome != null && lost.Length > 0) Add(s3, Level.Info, $"{lost.Length} throwable object(s) without ReturnHome — make sure they cannot get lost",
                                     lost.Select(g => g.gameObject));

            // ---- Tutorial 4 · spatial UI and feedback
            const string s4 = "Tutorial 4 · Spatial UI and feedback";
            var panels = FindObjectsByType<WorldPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None)
                .Where(p => !InRig(p) && !p.name.StartsWith("Checklist") && p.GetComponentInParent<Signpost>() == null).ToArray();
            Add(s4, panels.Length >= Panels[size] ? Level.Ok : Level.Fail, $"Information panels: {panels.Length} (need {Panels[size]})",
                panels.Select(p => p.gameObject));
            var silent = interactables.Where(i => i.GetComponent<InteractableFeedback>() == null).ToArray();
            Add(s4, silent.Length == 0 ? Level.Ok : Level.Warn,
                silent.Length == 0 ? "Every interactive object has InteractableFeedback" : $"{silent.Length} interactive object(s) without InteractableFeedback",
                silent.Select(i => i.gameObject));
            var viewpoints = anchors.Select(a => a.transform.position + Vector3.up * 1.6f).ToList();
            if (rig != null) viewpoints.Add(rig.transform.position + Vector3.up * 1.6f);
            var small = new List<WorldPanel>();
            foreach (var p in panels)
            {
                if (p.BodyText == null || viewpoints.Count == 0) continue;
                float px = p.BodyText.enableAutoSizing ? p.BodyText.fontSizeMax : p.BodyText.fontSize;
                float h = px * p.transform.lossyScale.y * 0.7f;   // cap height ≈ 0.7 em
                float d = viewpoints.Min(v => Vector3.Distance(v, p.transform.position));
                if (Mathf.Atan2(h, Mathf.Max(d, 0.1f)) * Mathf.Rad2Deg < MinTextDegrees) small.Add(p);
            }
            Add(s4, small.Count == 0 ? Level.Ok : Level.Warn,
                small.Count == 0 ? "Panel text is large enough from the nearest viewpoint" : $"{small.Count} panel(s) probably too small to read in the headset from the nearest viewpoint — move closer or enlarge",
                small.Select(p => p.gameObject));

            // ---- Tutorial 5 · signature and performance
            const string s5 = "Tutorial 5 · Signature and performance";
            var sigs = FindObjectsByType<SignatureInteraction>(FindObjectsSortMode.None);
            if (sigs.Length == 0) Add(s5, Level.Fail, "No SignatureInteraction in the scene");
            foreach (var sig in sigs)
            {
                bool named = !sig.Title.StartsWith("TODO");
                bool notes = !string.IsNullOrWhiteSpace(sig.Discoverability) && !sig.Discoverability.StartsWith("TODO")
                             && !string.IsNullOrWhiteSpace(sig.Action) && !string.IsNullOrWhiteSpace(sig.Feedback);
                Add(s5, named && notes ? Level.Ok : Level.Warn,
                    $"Signature '{sig.Title}': " + (named ? "" : "still named TODO; ") + (notes ? "DISCOVER / ACT / FEEDBACK filled" : "fill in DISCOVER / ACT / FEEDBACK"),
                    new Object[] { sig.gameObject });
            }
            if (size == 2 && sigs.Length < 2) Add(s5, Level.Warn, "Trio: a secondary signature interaction in another zone");
            var realtime = FindObjectsByType<Light>(FindObjectsSortMode.None).Where(l => l.enabled && l.lightmapBakeType != LightmapBakeType.Baked && !InRig(l)).ToArray();
            Add(s5, realtime.Length <= MaxRealtimeLights ? Level.Ok : Level.Warn, $"Realtime lights: {realtime.Length} (budget {MaxRealtimeLights})",
                realtime.Select(l => l.gameObject));
            var bigTex = new HashSet<Texture>();
            var bigUsers = new List<Object>();
            int tris = 0;
            foreach (var r in FindObjectsByType<Renderer>(FindObjectsSortMode.None))
            {
                if (InRig(r)) continue;
                var mf = r.GetComponent<MeshFilter>();
                if (mf != null && mf.sharedMesh != null && mf.sharedMesh.isReadable) tris += mf.sharedMesh.triangles.Length / 3;
                else if (mf != null && mf.sharedMesh != null) for (int i = 0; i < mf.sharedMesh.subMeshCount; i++) tris += (int)mf.sharedMesh.GetIndexCount(i) / 3;
                foreach (var m in r.sharedMaterials)
                {
                    if (m == null) continue;
                    foreach (var prop in m.GetTexturePropertyNames())
                    {
                        var t = m.GetTexture(prop);
                        if (t != null && Mathf.Max(t.width, t.height) > MaxTexture && bigTex.Add(t)) bigUsers.Add(r.gameObject);
                    }
                }
            }
            Add(s5, bigTex.Count == 0 ? Level.Ok : Level.Warn, $"Textures over {MaxTexture} px: {bigTex.Count} (set Max Size to 1024 in the texture's import settings)",
                bigTex.Cast<Object>());
            Add(s5, tris <= MaxTriangles ? Level.Ok : Level.Warn, $"Triangles in the scene: {tris:N0} (keep under {MaxTriangles:N0} for the headset)");
            float mb = WebGLBuilder.LastBuildMB();
            Add(s5, mb < 0 ? Level.Info : mb <= 60f ? Level.Ok : Level.Fail, mb < 0 ? "Web build size: build once to measure" : $"Last web build: {mb:0.0} MB (budget 60 MB)");
            var checklist = FindObjectsByType<WorldPanel>(FindObjectsInactive.Include, FindObjectsSortMode.None).Where(p => p.name.StartsWith("Checklist")).ToArray();
            Add(s5, checklist.Length == 0 ? Level.Ok : Level.Info, checklist.Length == 0 ? "Checklist panel deleted" : "Delete the Checklist panel before you submit",
                checklist.Select(c => c.gameObject));
            return list;
        }

        /// <summary>Command line: prints the report for the scene given with -scene.</summary>
        public static void PrintFromCommandLine()
        {
            var argv = System.Environment.GetCommandLineArgs();
            int i = System.Array.IndexOf(argv, "-scene");
            if (i >= 0) EditorSceneManager.OpenScene(argv[i + 1]);
            foreach (var it in Check()) Debug.Log($"[CHECK] {it.Level,-4} {it.Section} | {it.Text}");
            EditorApplication.Exit(0);
        }
    }
}
