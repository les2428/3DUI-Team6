using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.EventSystems;
using UnityEngine.Rendering;
using UnityEngine.SceneManagement;
using TMPro;
using UnityEngine.UI;
using WebXR;
using Object = UnityEngine.Object;

namespace ThreeDUI.Editor
{
    /// <summary>
    /// Generates the template's prefabs and scenes from code (menu: 3DUI > Create / 3DUI > Rebuild).
    /// Everything here can also be done by hand; this just guarantees a working starting point.
    /// </summary>
    public static partial class TemplateSceneBuilder
    {
        private const string Root = "Assets/_Template";
        private const string MatDir = Root + "/Materials";
        private const string PrefabDir = Root + "/Prefabs";
        private const string InteractionPrefabDir = PrefabDir + "/Interactions";
        private const string SceneDir = Root + "/Scenes";
        private const string SampleScenePath = SceneDir + "/SamplePlayground.unity";
        private const string TeamScenePath = "Assets/Teams/_TeamTemplate/Scenes/TeamScene.unity";
        private const string RigPrefabPath = PrefabDir + "/PlayerRig.prefab";

        // ================================================================== Menu

        [MenuItem("3DUI/Rebuild Template (prefabs + scenes)", priority = 1)]
        public static void RebuildAll()
        {
            BuildMaterials();
            BuildRigPrefab();
            BuildInteractionPrefabs();
            BuildSampleScene();
            BuildTeamScene();
            BuildTutorialExamples();
            SetBuildScenes(SampleScenePath, TeamScenePath);
            AssetDatabase.SaveAssets();
            Debug.Log("[3DUI] Template prefabs and scenes rebuilt.");
        }

        [MenuItem("3DUI/Create/New Team Scene (with PlayerRig)", priority = 100)]
        public static void CreateNewTeamSceneMenu()
        {
            string path = EditorUtility.SaveFilePanelInProject("New team scene", "MyTeamScene", "unity", "Where to save the scene?", "Assets/Teams");
            if (string.IsNullOrEmpty(path)) return;
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;
            BuildBasicScene(path, "My WebXR Space", addTeamSignature: false);
            EditorSceneManager.OpenScene(path);
        }

        [MenuItem("3DUI/Create/Grabbable Object", priority = 120)] private static void MenuGrabbable() => PlaceInScene("Grabbable Cube");
        [MenuItem("3DUI/Create/Push Button", priority = 121)] private static void MenuButton() => PlaceInScene("Push Button");
        [MenuItem("3DUI/Create/Info Point + Panel", priority = 122)] private static void MenuInfo() => PlaceInScene("Info Point");
        [MenuItem("3DUI/Create/Teleport Anchor (viewpoint)", priority = 123)] private static void MenuAnchor() => PlaceInScene("Teleport Anchor");
        [MenuItem("3DUI/Create/Snap Zone (pedestal)", priority = 124)] private static void MenuSnap() => PlaceInScene("Snap Pedestal");
        [MenuItem("3DUI/Create/Lamp (bulb starts off)", priority = 126)] private static void MenuLamp() => PlaceInScene("Lamp");
        [MenuItem("3DUI/Create/Signpost (points at a target)", priority = 127)] private static void MenuSignpost() => PlaceInScene("Signpost");

        // ---- greybox blocks: real-world sizes, so a layout sketch can be blocked out in minutes
        [MenuItem("3DUI/Create/Greybox/Wall (4 m x 3 m)", priority = 140)]
        private static void MenuWall() => PlaceBlock("Wall", new Vector3(4f, 3f, 0.2f), "Wall");
        [MenuItem("3DUI/Create/Greybox/Short wall (2 m x 3 m)", priority = 141)]
        private static void MenuShortWall() => PlaceBlock("Short wall", new Vector3(2f, 3f, 0.2f), "Wall");
        [MenuItem("3DUI/Create/Greybox/Doorway wall (4 m, 1 m door)", priority = 141)]
        private static void MenuDoorway()
        {
            var p = ScenePivot();
            var root = new GameObject("Doorway wall");
            root.transform.position = p;
            root.isStatic = true;
            // two 1.5 m sides and a lintel above a 1 m x 2.1 m opening
            Prim(PrimitiveType.Cube, "Left", root.transform, new Vector3(-1.25f, 1.5f, 0f), new Vector3(1.5f, 3f, 0.2f), "Wall").isStatic = true;
            Prim(PrimitiveType.Cube, "Right", root.transform, new Vector3(1.25f, 1.5f, 0f), new Vector3(1.5f, 3f, 0.2f), "Wall").isStatic = true;
            Prim(PrimitiveType.Cube, "Lintel", root.transform, new Vector3(0f, 2.55f, 0f), new Vector3(1f, 0.9f, 0.2f), "Wall").isStatic = true;
            Undo.RegisterCreatedObjectUndo(root, "Create Doorway wall");
            Selection.activeGameObject = root;
        }
        [MenuItem("3DUI/Create/Greybox/Table (1.2 m x 0.75 m high)", priority = 142)]
        private static void MenuTable() => PlaceBlock("Table", new Vector3(1.2f, 0.75f, 0.7f), "Wood");
        [MenuItem("3DUI/Create/Greybox/Block (1 m cube)", priority = 143)]
        private static void MenuBlock() => PlaceBlock("Block", Vector3.one, "Pedestal");

        /// <summary>A static box standing on the floor at the Scene view's pivot.</summary>
        private static void PlaceBlock(string name, Vector3 size, string mat)
        {
            var p = ScenePivot();
            var go = Prim(PrimitiveType.Cube, name, null, new Vector3(p.x, p.y + size.y / 2f, p.z), size, mat);
            go.isStatic = true;
            Undo.RegisterCreatedObjectUndo(go, "Create " + name);
            Selection.activeGameObject = go;
        }
        [MenuItem("3DUI/Create/World Panel (text)", priority = 125)]
        private static void MenuPanel()
        {
            var panel = WorldPanel.Create(null, "World Panel", new Vector2(0.8f, 0.5f), "Title", "Body text goes here.");
            panel.transform.position = ScenePivot() + Vector3.up * 1.4f;
            Undo.RegisterCreatedObjectUndo(panel.gameObject, "Create World Panel");
            Selection.activeGameObject = panel.gameObject;
        }

        private static void PlaceInScene(string prefabName)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>($"{InteractionPrefabDir}/{prefabName}.prefab");
            if (prefab == null)
            {
                EditorUtility.DisplayDialog("3DUI", "Template prefabs not found. Run 3DUI > Rebuild Template first.", "OK");
                return;
            }
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            go.transform.position = ScenePivot();
            Undo.RegisterCreatedObjectUndo(go, "Create " + prefabName);
            Selection.activeGameObject = go;
        }

        private static Vector3 ScenePivot()
        {
            var sv = SceneView.lastActiveSceneView;
            if (sv == null) return Vector3.zero;
            var p = sv.pivot;
            if (Physics.Raycast(p + Vector3.up * 5f, Vector3.down, out var hit, 20f)) p = hit.point;
            return p;
        }

        // ================================================================== Materials

        private static readonly Dictionary<string, Material> mats = new Dictionary<string, Material>();

        private static void BuildMaterials()
        {
            TemplateProjectSetup.EnsureFolder(MatDir);
            Lit("Floor", new Color(0.42f, 0.44f, 0.47f));
            Lit("Wall", new Color(0.82f, 0.82f, 0.8f));
            Lit("Wood", new Color(0.55f, 0.38f, 0.24f));
            Lit("Pedestal", new Color(0.9f, 0.9f, 0.92f));
            Lit("Red", new Color(0.9f, 0.25f, 0.2f));
            Lit("Green", new Color(0.3f, 0.8f, 0.35f));
            Lit("Blue", new Color(0.25f, 0.45f, 0.95f));
            Lit("Yellow", new Color(1f, 0.8f, 0.2f));
            Lit("Dark", new Color(0.15f, 0.16f, 0.18f));
            Lit("Controller", new Color(0.2f, 0.22f, 0.25f));
            Lit("Glow", new Color(1f, 0.85f, 0.4f), emission: new Color(2f, 1.6f, 0.6f));
            Lit("Info", new Color(0.2f, 0.7f, 1f), emission: new Color(0.1f, 0.4f, 0.8f));
            Lit("Anchor", new Color(0.3f, 0.9f, 1f, 0.6f), transparent: true);
            Lit("Ghost", new Color(0.3f, 1f, 0.6f, 0.35f), transparent: true);

            // Line / reticle material (vertex colors, unlit).
            Save("Line", new Material(Shader.Find("Sprites/Default")) { name = "Line" });
            var reticle = new Material(Shader.Find("Universal Render Pipeline/Unlit"));
            reticle.SetColor("_BaseColor", Color.white);
            Save("Reticle", reticle);
        }

        private static Material Lit(string name, Color color, Color? emission = null, bool transparent = false)
        {
            var m = new Material(Shader.Find("Universal Render Pipeline/Lit"));
            m.SetColor("_BaseColor", color);
            m.SetFloat("_Smoothness", 0.3f);
            if (emission.HasValue)
            {
                m.EnableKeyword("_EMISSION");
                m.SetColor("_EmissionColor", emission.Value);
                m.globalIlluminationFlags = MaterialGlobalIlluminationFlags.RealtimeEmissive;
            }
            if (transparent)
            {
                m.SetFloat("_Surface", 1f);
                m.SetFloat("_Blend", 0f);
                m.SetOverrideTag("RenderType", "Transparent");
                m.SetInt("_SrcBlend", (int)BlendMode.SrcAlpha);
                m.SetInt("_DstBlend", (int)BlendMode.OneMinusSrcAlpha);
                m.SetInt("_ZWrite", 0);
                m.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
                m.renderQueue = (int)RenderQueue.Transparent;
            }
            return Save(name, m);
        }

        private static Material Save(string name, Material m)
        {
            string path = $"{MatDir}/{name}.mat";
            var existing = AssetDatabase.LoadAssetAtPath<Material>(path);
            if (existing != null)
            {
                existing.shader = m.shader;
                existing.CopyPropertiesFromMaterial(m);
                existing.shaderKeywords = m.shaderKeywords;
                existing.renderQueue = m.renderQueue;
                existing.SetOverrideTag("RenderType", m.GetTag("RenderType", false));
                EditorUtility.SetDirty(existing);
                mats[name] = existing;
                return existing;
            }
            AssetDatabase.CreateAsset(m, path);
            mats[name] = m;
            return m;
        }

        private static Material M(string name)
        {
            if (!mats.TryGetValue(name, out var m) || m == null)
            {
                m = AssetDatabase.LoadAssetAtPath<Material>($"{MatDir}/{name}.mat");
                mats[name] = m;
            }
            return m;
        }

        // ================================================================== Helpers

        // ---------------------------------------------------------------- term project
        // The starter scene is built from the same pieces as everything else here; these are the
        // doors into them, so TermProjectStarter does not have to duplicate any of it.

        public static UnityEngine.SceneManagement.Scene NewSceneForTermProject(string title, out Transform env)
            => NewEnvironmentScene(title, out env);

        public static GameObject AddRigAt(Vector3 position, float yaw) => AddRig(position, yaw);

        public static void RoomAround(Transform env, float half, float height) => WallsAround(env, half, height);

        public static GameObject Spawn(string prefabName, Transform parent, Vector3 pos, float yaw = 0f)
            => Interaction(prefabName, parent, pos, yaw);

        public static void FaceThePanel(Component panel, Vector3 viewer) => FacePanel(panel, viewer);

        public static WorldPanel Panel(Transform parent, string name, Vector2 size, string title, string body)
            => WorldPanel.Create(parent, name, size, title, body);

        public static GameObject Block(Transform parent, string name, Vector3 pos, Vector3 scale)
            => Prim(PrimitiveType.Cube, name, parent, pos, scale, "Pedestal");

        private static GameObject Prim(PrimitiveType type, string name, Transform parent, Vector3 localPos, Vector3 scale, string mat, bool collider = true)
        {
            var go = GameObject.CreatePrimitive(type);
            go.name = name;
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            go.transform.localScale = scale;
            var r = go.GetComponent<Renderer>();
            if (mat != null) r.sharedMaterial = M(mat);
            if (!collider) Object.DestroyImmediate(go.GetComponent<Collider>());
            return go;
        }

        private static GameObject Empty(string name, Transform parent, Vector3 localPos)
        {
            var go = new GameObject(name);
            go.transform.SetParent(parent, false);
            go.transform.localPosition = localPos;
            return go;
        }

        private static void Set(Object target, string field, Object value)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) throw new ArgumentException($"Field '{field}' not found on {target.GetType().Name}");
            p.objectReferenceValue = value;
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetValue(Object target, string field, Action<SerializedProperty> apply)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            if (p == null) throw new ArgumentException($"Field '{field}' not found on {target.GetType().Name}");
            apply(p);
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetArray(Object target, string field, Object[] values)
        {
            var so = new SerializedObject(target);
            var p = so.FindProperty(field);
            p.arraySize = values.Length;
            for (int i = 0; i < values.Length; i++) p.GetArrayElementAtIndex(i).objectReferenceValue = values[i];
            so.ApplyModifiedPropertiesWithoutUndo();
        }

        private static void SetLabel(Interactable i, string displayName, string hint)
        {
            i.DisplayName = displayName;
            i.Hint = hint;
            EditorUtility.SetDirty(i);
        }

        private static LineRenderer AddLine(GameObject go, float width)
        {
            var lr = go.AddComponent<LineRenderer>();
            lr.sharedMaterial = M("Line");
            lr.widthMultiplier = width;
            lr.numCapVertices = 2;
            lr.shadowCastingMode = ShadowCastingMode.Off;
            lr.receiveShadows = false;
            lr.useWorldSpace = true;
            lr.positionCount = 2;
            return lr;
        }

        private static GameObject SavePrefab(GameObject go, string path)
        {
            TemplateProjectSetup.EnsureFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
            var prefab = PrefabUtility.SaveAsPrefabAsset(go, path);
            Object.DestroyImmediate(go);
            return prefab;
        }

        // ================================================================== Player rig

        private static void BuildRigPrefab()
        {
            var rigGo = new GameObject("PlayerRig");
            var modeManager = rigGo.AddComponent<XRModeManager>();
            var rig = rigGo.AddComponent<PlayerRig>();

            // Camera
            var camGo = new GameObject("Main Camera");
            camGo.tag = "MainCamera";
            camGo.transform.SetParent(rigGo.transform, false);
            camGo.transform.localPosition = new Vector3(0f, 1.6f, 0f);
            var cam = camGo.AddComponent<Camera>();
            cam.nearClipPlane = 0.05f;
            cam.farClipPlane = 200f;
            cam.clearFlags = CameraClearFlags.Skybox;
            camGo.AddComponent<AudioListener>();
            var headDriver = camGo.AddComponent<XRHeadPoseDriver>();
            headDriver.enabled = false;

            // Controllers
            var left = BuildController(rigGo.transform, "LeftController", WebXRControllerHand.LEFT);
            var right = BuildController(rigGo.transform, "RightController", WebXRControllerHand.RIGHT);

            // Desktop pointer
            var dpGo = Empty("DesktopPointer", rigGo.transform, Vector3.zero);
            var desktopPointer = dpGo.AddComponent<DesktopPointer>();
            Set(desktopPointer, "viewCamera", cam);

            Set(rig, "headCamera", cam);
            Set(rig, "headPoseDriver", headDriver);
            Set(rig, "desktopPointer", desktopPointer);
            Set(rig, "leftController", left);
            Set(rig, "rightController", right);

            // Locomotion
            rigGo.AddComponent<DesktopLocomotion>();
            var xrLoco = rigGo.AddComponent<XRLocomotion>();
            var arcGo = Empty("TeleportArc", rigGo.transform, Vector3.zero);
            var arc = AddLine(arcGo, 0.015f);
            arc.enabled = false;
            var marker = Prim(PrimitiveType.Cylinder, "TeleportMarker", rigGo.transform, Vector3.zero, new Vector3(0.6f, 0.005f, 0.6f), "Anchor", collider: false);
            var markerArrow = Prim(PrimitiveType.Cube, "Arrow", marker.transform, new Vector3(0f, 1f, 0.35f), new Vector3(0.15f, 1f, 0.3f), "Anchor", collider: false);
            markerArrow.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            marker.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            marker.SetActive(false);
            Set(xrLoco, "arcLine", arc);
            Set(xrLoco, "teleportMarker", marker.transform);

            // Hover label
            var hoverGo = Empty("HoverLabel", rigGo.transform, Vector3.zero);
            var hover = hoverGo.AddComponent<HoverLabel>();
            var labelPanel = WorldPanel.Create(hoverGo.transform, "Label", new Vector2(0.6f, 0.2f), "Name", "Hint",
                backgroundColor: new Color(0.05f, 0.07f, 0.1f, 0.85f));
            // Fixed sizes: the panels' auto-size floor (34) is taller than each line of this small label,
            // which truncated the name to nothing. Long names end in "..." instead.
            foreach (var t in new[] { labelPanel.TitleText, labelPanel.BodyText })
            {
                t.enableAutoSizing = false;
                t.overflowMode = TextOverflowModes.Ellipsis;
            }
            labelPanel.TitleText.fontSize = 52;
            labelPanel.BodyText.fontSize = 38;
            labelPanel.TitleText.alignment = TextAlignmentOptions.Center;
            labelPanel.BodyText.alignment = TextAlignmentOptions.Center;
            var contentLayout = labelPanel.transform.Find("Content").GetComponent<VerticalLayoutGroup>();
            contentLayout.padding = new RectOffset(0, 0, 0, 0);
            contentLayout.spacing = 4f;
            ((RectTransform)contentLayout.transform).offsetMin = new Vector2(12f, 8f);
            ((RectTransform)contentLayout.transform).offsetMax = new Vector2(-12f, -8f);
            Set(hover, "label", labelPanel);

            // Desktop HUD + EventSystem
            BuildHud(rigGo.transform);
            var es = Empty("EventSystem", rigGo.transform, Vector3.zero);
            es.AddComponent<EventSystem>();
            es.AddComponent<StandaloneInputModule>();

            SavePrefab(rigGo, RigPrefabPath);
        }

        private static XRControllerPointer BuildController(Transform parent, string name, WebXRControllerHand hand)
        {
            var go = Empty(name, parent, new Vector3(hand == WebXRControllerHand.LEFT ? -0.2f : 0.2f, 1.1f, 0.3f));
            var webxr = go.AddComponent<WebXRController>();
            webxr.hand = hand;
            var pointer = go.AddComponent<XRControllerPointer>();
            Set(pointer, "controller", webxr);

            var model = Empty("Model", go.transform, Vector3.zero);
            Prim(PrimitiveType.Cube, "Body", model.transform, new Vector3(0f, -0.015f, -0.05f), new Vector3(0.035f, 0.03f, 0.12f), "Controller", collider: false);
            Prim(PrimitiveType.Sphere, "Tip", model.transform, new Vector3(0f, -0.005f, 0.01f), new Vector3(0.02f, 0.02f, 0.02f), "Info", collider: false);
            Set(pointer, "model", model);

            var rayGo = Empty("Ray", go.transform, Vector3.zero);
            AddLine(rayGo, 0.004f);
            var visual = rayGo.AddComponent<PointerRayVisual>();
            Set(visual, "pointer", pointer);
            var reticle = Prim(PrimitiveType.Sphere, "Reticle", rayGo.transform, Vector3.zero, Vector3.one * 0.02f, "Reticle", collider: false);
            reticle.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
            Set(visual, "reticle", reticle.transform);
            return pointer;
        }

        private static void BuildHud(Transform parent)
        {
            var hudGo = new GameObject("DesktopHUD", typeof(RectTransform));
            hudGo.transform.SetParent(parent, false);
            var canvas = hudGo.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.ScreenSpaceOverlay;
            canvas.sortingOrder = 100;
            var scaler = hudGo.AddComponent<CanvasScaler>();
            scaler.uiScaleMode = CanvasScaler.ScaleMode.ScaleWithScreenSize;
            scaler.referenceResolution = new Vector2(1920, 1080);
            scaler.matchWidthOrHeight = 0.5f;
            hudGo.AddComponent<GraphicRaycaster>();
            var hud = hudGo.AddComponent<DesktopHUD>();

            // Help panel (bottom-left)
            var help = new GameObject("Help", typeof(RectTransform), typeof(Image));
            help.transform.SetParent(hudGo.transform, false);
            var hrt = (RectTransform)help.transform;
            hrt.anchorMin = hrt.anchorMax = hrt.pivot = Vector2.zero;
            hrt.anchoredPosition = new Vector2(24, 24);
            hrt.sizeDelta = new Vector2(760, 330);
            help.GetComponent<Image>().color = new Color(0f, 0f, 0f, 0.55f);

            var title = HudText(help.transform, "Title", 34, FontStyles.Bold, new Vector2(20, -16), new Vector2(720, 44));
            var controls = HudText(help.transform, "Controls", 24, FontStyles.Normal, new Vector2(20, -66), new Vector2(720, 250));

            // Enter VR button (top-right)
            var btnGo = new GameObject("EnterVRButton", typeof(RectTransform), typeof(Image), typeof(Button));
            btnGo.transform.SetParent(hudGo.transform, false);
            var brt = (RectTransform)btnGo.transform;
            brt.anchorMin = brt.anchorMax = brt.pivot = new Vector2(1f, 1f);
            brt.anchoredPosition = new Vector2(-24, -24);
            brt.sizeDelta = new Vector2(220, 64);
            btnGo.GetComponent<Image>().color = new Color(0.15f, 0.55f, 0.95f, 0.95f);
            var btnText = HudText(btnGo.transform, "Label", 28, FontStyles.Bold, Vector2.zero, new Vector2(220, 64));
            btnText.alignment = TextAlignmentOptions.Center;
            btnText.text = "Enter VR";
            var bt = (RectTransform)btnText.transform;
            bt.anchorMin = bt.anchorMax = bt.pivot = new Vector2(0.5f, 0.5f);
            bt.anchoredPosition = Vector2.zero;

            // Crosshair-like dot at the cursor is not needed (the OS cursor is visible).

            Set(hud, "canvas", canvas);
            Set(hud, "helpPanel", help);
            Set(hud, "enterVRButton", btnGo.GetComponent<Button>());
            Set(hud, "titleText", title);
            Set(hud, "controlsText", controls);
            // Both languages live on the component; it writes the one the visitor picked.
            SetValue(hud, "spaceTitleKo", p => p.stringValue = "나의 WebXR 공간");
            SetValue(hud, "controlsEn", p => p.stringValue =
                "WASD / arrows : move      Shift : run\n" +
                "Drag (empty space) or right-drag : look\n" +
                "Q / E : turn\n" +
                "Click : select / press      Hold + move : grab\n" +
                "Scroll while holding : push / pull\n" +
                "H : hide this help");
            SetValue(hud, "controlsKo", p => p.stringValue =
                "WASD / 방향키 : 이동      Shift : 달리기\n" +
                "빈 곳 드래그 또는 오른쪽 버튼 드래그 : 시점\n" +
                "Q / E : 회전\n" +
                "클릭 : 선택 / 누르기      누른 채 이동 : 잡기\n" +
                "잡은 채 휠 : 밀고 당기기\n" +
                "H : 도움말 숨기기");
        }

        private static TextMeshProUGUI HudText(Transform parent, string name, float size, FontStyles style, Vector2 pos, Vector2 sizeDelta)
        {
            var t = WorldPanel.CreateText(parent, name, size, style, Color.white);
            var rt = (RectTransform)t.transform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(0f, 1f);
            rt.anchoredPosition = pos;
            rt.sizeDelta = sizeDelta;
            t.overflowMode = TextOverflowModes.Overflow;
            return t;
        }

        // ================================================================== Interaction prefabs

        private static void BuildInteractionPrefabs()
        {
            TemplateProjectSetup.EnsureFolder(InteractionPrefabDir);

            // Grabbable cube
            {
                var go = Prim(PrimitiveType.Cube, "Grabbable Cube", null, Vector3.zero, Vector3.one * 0.15f, "Yellow");
                var rb = go.AddComponent<Rigidbody>();
                rb.mass = 0.5f;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                var g = go.AddComponent<Grabbable>();
                g.SelectButtonMode = SelectButton.Either;
                SetLabel(g, "Cube", "Grab and move me");
                go.AddComponent<InteractableFeedback>();
                go.AddComponent<ReturnHome>();   // thrown over a wall or through the floor: it comes back
                SavePrefab(go, $"{InteractionPrefabDir}/Grabbable Cube.prefab");
            }

            // Push button (toggle)
            {
                var root = new GameObject("Push Button");
                Prim(PrimitiveType.Cylinder, "Stand", root.transform, new Vector3(0f, 0.5f, 0f), new Vector3(0.25f, 0.5f, 0.25f), "Pedestal");
                var housing = Prim(PrimitiveType.Cylinder, "Housing", root.transform, new Vector3(0f, 1.02f, 0f), new Vector3(0.2f, 0.02f, 0.2f), "Dark");
                var cap = Prim(PrimitiveType.Cylinder, "Cap", root.transform, new Vector3(0f, 1.06f, 0f), new Vector3(0.14f, 0.025f, 0.14f), "Red");
                var btn = cap.AddComponent<PushButton3D>();
                Set(btn, "cap", cap.transform);
                SetValue(btn, "toggle", p => p.boolValue = true);
                SetLabel(btn, "Button", "Press to toggle the lamp");
                btn.gameObject.AddComponent<InteractableFeedback>();
                Object.DestroyImmediate(housing.GetComponent<Collider>());
                SavePrefab(root, $"{InteractionPrefabDir}/Push Button.prefab");
            }

            // Info point + panel
            {
                var root = new GameObject("Info Point");
                Prim(PrimitiveType.Cylinder, "Stand", root.transform, new Vector3(0f, 0.6f, 0f), new Vector3(0.05f, 0.6f, 0.05f), "Dark", collider: false);
                var marker = Prim(PrimitiveType.Sphere, "Marker (i)", root.transform, new Vector3(0f, 1.3f, 0f), Vector3.one * 0.16f, "Info");
                var info = marker.AddComponent<InfoPoint>();
                SetLabel(info, "Info", "Click to read");
                marker.AddComponent<InteractableFeedback>();
                var panel = WorldPanel.Create(root.transform, "Info Panel", new Vector2(0.9f, 0.6f), "About this place",
                    "Replace this text with information about your space.\n\nPanels are world-space UI (Spatial UI) and are readable on desktop and in XR.");
                panel.transform.localPosition = new Vector3(0f, 1.75f, 0f);
                panel.gameObject.AddComponent<Billboard>();
                Set(info, "panel", panel);
                SavePrefab(root, $"{InteractionPrefabDir}/Info Point.prefab");
            }

            // Teleport anchor
            {
                var go = Prim(PrimitiveType.Cylinder, "Teleport Anchor", null, Vector3.zero, new Vector3(0.7f, 0.01f, 0.7f), "Anchor");
                go.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                var arrow = Prim(PrimitiveType.Cube, "Arrow (forward)", go.transform, new Vector3(0f, 1f, 0.3f), new Vector3(0.12f, 1f, 0.25f), "Anchor", collider: false);
                arrow.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                var anchor = go.AddComponent<TeleportAnchor>();
                SetLabel(anchor, "Viewpoint", "Click to go here");
                go.AddComponent<InteractableFeedback>();
                SavePrefab(go, $"{InteractionPrefabDir}/Teleport Anchor.prefab");
            }

            // Snap pedestal
            {
                var root = new GameObject("Snap Pedestal");
                Prim(PrimitiveType.Cube, "Pedestal", root.transform, new Vector3(0f, 0.45f, 0f), new Vector3(0.4f, 0.9f, 0.4f), "Pedestal");
                var attach = Empty("Attach Point", root.transform, new Vector3(0f, 1.0f, 0f));
                var ghost = Prim(PrimitiveType.Sphere, "Preview", attach.transform, Vector3.zero, Vector3.one * 0.2f, "Ghost", collider: false);
                ghost.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                ghost.SetActive(false);
                var zone = root.AddComponent<SnapZone>();
                Set(zone, "attachPoint", attach.transform);
                Set(zone, "previewIndicator", ghost);
                SetValue(zone, "matchRotation", p => p.boolValue = true);
                SavePrefab(root, $"{InteractionPrefabDir}/Snap Pedestal.prefab");
            }

            // Lamp: a pole and a bulb. "Bulb (on)" (glowing sphere + light) starts switched off,
            // so a button only has to SetActive it - the no-code lab in tutorial 1.
            {
                var root = new GameObject("Lamp");
                Prim(PrimitiveType.Cylinder, "Pole", root.transform, new Vector3(0f, 0.9f, 0f), new Vector3(0.05f, 0.9f, 0.05f), "Dark");
                Prim(PrimitiveType.Sphere, "Bulb (off)", root.transform, new Vector3(0f, 1.9f, 0f), Vector3.one * 0.24f, "Pedestal", collider: false);
                var on = Empty("Bulb (on)", root.transform, new Vector3(0f, 1.9f, 0f));
                Prim(PrimitiveType.Sphere, "Glow", on.transform, Vector3.zero, Vector3.one * 0.26f, "Glow", collider: false)
                    .GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                var light = on.AddComponent<Light>();
                light.type = LightType.Point;
                light.range = 5f;
                light.intensity = 3f;
                light.color = new Color(1f, 0.85f, 0.5f);
                on.SetActive(false);
                SavePrefab(root, $"{InteractionPrefabDir}/Lamp.prefab");
            }

            // Signpost (tutorial 2, wayfinding): an arrow that turns to face its target, and a label.
            {
                var root = new GameObject("Signpost");
                Prim(PrimitiveType.Cylinder, "Pole", root.transform, new Vector3(0f, 1.0f, 0f), new Vector3(0.06f, 1.0f, 0.06f), "Dark");
                var arrow = Empty("Arrow", root.transform, new Vector3(0f, 1.75f, 0f));
                Prim(PrimitiveType.Cube, "Board", arrow.transform, new Vector3(0f, 0f, 0.28f), new Vector3(0.12f, 0.12f, 0.5f), "Yellow", collider: false);
                var tip = Prim(PrimitiveType.Cube, "Tip", arrow.transform, new Vector3(0f, 0f, 0.55f), new Vector3(0.2f, 0.12f, 0.2f), "Yellow", collider: false);
                tip.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
                var label = WorldPanel.Create(root.transform, "Label", new Vector2(0.9f, 0.22f), "TODO: where does this lead?", "");
                label.transform.localPosition = new Vector3(0f, 2.15f, 0f);
                label.TitleText.enableAutoSizing = false;
                label.TitleText.fontSize = 80;
                label.TitleText.alignment = TextAlignmentOptions.Center;
                label.gameObject.AddComponent<Billboard>();
                var sign = root.AddComponent<Signpost>();
                Set(sign, "arrow", arrow.transform);
                Set(sign, "labelPanel", label);
                SavePrefab(root, $"{InteractionPrefabDir}/Signpost.prefab");
            }
        }

        private static GameObject InstantiatePrefab(string path, Transform parent, Vector3 position, float yaw = 0f)
        {
            var prefab = AssetDatabase.LoadAssetAtPath<GameObject>(path);
            var go = (GameObject)PrefabUtility.InstantiatePrefab(prefab);
            if (parent != null) go.transform.SetParent(parent, false);
            go.transform.position = position;
            go.transform.rotation = Quaternion.Euler(0f, yaw, 0f);
            return go;
        }

        private static GameObject Interaction(string name, Transform parent, Vector3 pos, float yaw = 0f) =>
            InstantiatePrefab($"{InteractionPrefabDir}/{name}.prefab", parent, pos, yaw);

        // ================================================================== Scenes

        private static Scene NewEnvironmentScene(string welcomeTitle, out Transform env)
        {
            var scene = EditorSceneManager.NewScene(NewSceneSetup.EmptyScene, NewSceneMode.Single);

            RenderSettings.skybox = AssetDatabase.GetBuiltinExtraResource<Material>("Default-Skybox.mat");
            RenderSettings.ambientMode = AmbientMode.Trilight;
            RenderSettings.ambientSkyColor = new Color(0.75f, 0.8f, 0.9f);
            RenderSettings.ambientEquatorColor = new Color(0.55f, 0.55f, 0.55f);
            RenderSettings.ambientGroundColor = new Color(0.3f, 0.28f, 0.26f);
            RenderSettings.fog = false;

            var lightGo = new GameObject("Sun");
            var light = lightGo.AddComponent<Light>();
            light.type = LightType.Directional;
            light.intensity = 1.4f;
            light.shadows = LightShadows.Hard;
            light.color = new Color(1f, 0.96f, 0.9f);
            lightGo.transform.rotation = Quaternion.Euler(50f, -30f, 0f);

            env = new GameObject("Environment").transform;
            var floor = Prim(PrimitiveType.Cube, "Floor", env, new Vector3(0f, -0.05f, 0f), new Vector3(20f, 0.1f, 20f), "Floor");
            floor.AddComponent<TeleportArea>();
            floor.isStatic = true;

            return scene;
        }

        private static GameObject AddRig(Vector3 position, float yaw)
        {
            return InstantiatePrefab(RigPrefabPath, null, position, yaw);
        }

        private static void BuildSampleScene()
        {
            TemplateProjectSetup.EnsureFolder(SceneDir);
            var scene = NewEnvironmentScene("Sample Playground", out var env);

            WallsAround(env, 8f, 3f);

            AddRig(new Vector3(0f, 0f, -5f), 0f);

            // Welcome panels, different for desktop and XR (ModeVisibility example)
            var welcome = new GameObject("Welcome (per mode)").transform;
            welcome.position = new Vector3(0f, 0f, -3f);
            var desktopGroup = new GameObject("Desktop only").transform;
            desktopGroup.SetParent(welcome, false);
            desktopGroup.gameObject.AddComponent<ModeVisibility>();
            SetValue(desktopGroup.GetComponent<ModeVisibility>(), "showInVR", p => p.boolValue = false);
            SetValue(desktopGroup.GetComponent<ModeVisibility>(), "showInAR", p => p.boolValue = false);
            var dp = WorldPanel.Create(desktopGroup, "Welcome Desktop", new Vector2(1.6f, 0.8f), "Welcome to the 3DUI Sample Playground",
                "Desktop: WASD to move, drag to look, click objects to interact.\nPress 'Enter VR' (top right) on a headset browser.\n\nVisit the four stations: Grab - Button - Info - Signature.");
            dp.transform.localPosition = new Vector3(0f, 1.9f, 0f);
            var xrGroup = new GameObject("XR only").transform;
            xrGroup.SetParent(welcome, false);
            xrGroup.gameObject.AddComponent<ModeVisibility>();
            SetValue(xrGroup.GetComponent<ModeVisibility>(), "showInDesktop", p => p.boolValue = false);
            var xp = WorldPanel.Create(xrGroup, "Welcome XR", new Vector2(1.6f, 0.8f), "Welcome (VR)",
                "Right stick forward: teleport arc.  Right stick left/right: snap turn.\nTrigger: select / press.  Trigger or grip: grab.\nTouch objects directly or point with the ray.");
            xp.transform.localPosition = new Vector3(0f, 1.9f, 0f);

            // ---- Station 1: Grab (left)
            var grab = new GameObject("Station 1 - Grab").transform;
            grab.position = new Vector3(-4f, 0f, 0f);
            Prim(PrimitiveType.Cube, "Table", grab, new Vector3(0f, 0.375f, 0f), new Vector3(1.4f, 0.75f, 0.7f), "Wood");
            var redItem = Interaction("Grabbable Cube", grab, grab.position + new Vector3(-0.45f, 0.83f, 0f));
            redItem.name = "Item Red (cube)";
            ColorItem(redItem, "Red", "Red cube", "red");
            var greenItem = Interaction("Grabbable Cube", grab, grab.position + new Vector3(0f, 0.83f, 0f));
            greenItem.name = "Item Green (sphere)";
            ReplaceMesh(greenItem, PrimitiveType.Sphere);
            ColorItem(greenItem, "Green", "Green sphere", "green");
            var blueItem = Interaction("Grabbable Cube", grab, grab.position + new Vector3(0.45f, 0.83f, 0f));
            blueItem.name = "Item Blue (cylinder)";
            ReplaceMesh(blueItem, PrimitiveType.Cylinder);
            blueItem.transform.localScale = new Vector3(0.12f, 0.08f, 0.12f);
            ColorItem(blueItem, "Blue", "Blue cylinder", "blue");
            StationSign(grab, "1. Grab", "Pick up the items (hold click / trigger).\nScroll or thumbstick to push/pull.\nThey belong on the pedestals of station 4.");

            // ---- Station 2: Button (front-left)
            var btn = new GameObject("Station 2 - Button").transform;
            btn.position = new Vector3(-1.5f, 0f, 3f);
            var buttonGo = Interaction("Push Button", btn, btn.position);
            var lamp = new GameObject("Lamp");
            lamp.transform.SetParent(btn, false);
            lamp.transform.localPosition = new Vector3(0.9f, 0f, 0f);
            Prim(PrimitiveType.Cylinder, "Pole", lamp.transform, new Vector3(0f, 0.9f, 0f), new Vector3(0.05f, 0.9f, 0.05f), "Dark");
            var bulb = Prim(PrimitiveType.Sphere, "Bulb", lamp.transform, new Vector3(0f, 1.9f, 0f), Vector3.one * 0.25f, "Glow", collider: false);
            var lightOn = new GameObject("Light (on)");
            lightOn.transform.SetParent(lamp.transform, false);
            lightOn.transform.localPosition = new Vector3(0f, 1.9f, 0f);
            var pl = lightOn.AddComponent<Light>();
            pl.type = LightType.Point;
            pl.range = 5f;
            pl.intensity = 3f;
            pl.color = new Color(1f, 0.85f, 0.5f);
            bulb.SetActive(false);
            lightOn.SetActive(false);
            var push = buttonGo.GetComponentInChildren<PushButton3D>();
            UnityEventTools.AddBoolPersistentListener(push.onToggledOn, bulb.SetActive, true);
            UnityEventTools.AddBoolPersistentListener(push.onToggledOn, lightOn.SetActive, true);
            UnityEventTools.AddBoolPersistentListener(push.onToggledOff, bulb.SetActive, false);
            UnityEventTools.AddBoolPersistentListener(push.onToggledOff, lightOn.SetActive, false);
            EditorUtility.SetDirty(push);
            StationSign(btn, "2. Button", "Click / trigger / poke the red button.\nNo code: wired with UnityEvents\nin the Inspector (onToggledOn/Off).");

            // ---- Station 3: Info (front-right)
            var infoSt = new GameObject("Station 3 - Info").transform;
            infoSt.position = new Vector3(1.5f, 0f, 3f);
            var infoGo = Interaction("Info Point", infoSt, infoSt.position);
            var panel = infoGo.GetComponentInChildren<WorldPanel>(true);
            panel.SetText("Spatial UI",
                "This panel is placed in the world, faces you (Billboard), and hides when you walk away.\n\nUse it to present information where it matters, instead of on a flat 2D page.");
            EditorUtility.SetDirty(panel);
            StationSign(infoSt, "3. Info", "Click the blue sphere to open\nor close its information panel.");

            // ---- Station 4: Signature example (right)
            var sig = new GameObject("Station 4 - Signature (example)").transform;
            sig.position = new Vector3(4f, 0f, 0f);
            sig.rotation = Quaternion.Euler(0f, -90f, 0f);
            var zones = new List<SnapZone>();
            string[] keys = { "red", "green", "blue" };
            string[] colors = { "Red", "Green", "Blue" };
            for (int i = 0; i < 3; i++)
            {
                var ped = Interaction("Snap Pedestal", sig, sig.TransformPoint(new Vector3((i - 1) * 0.8f, 0f, 0.6f)), -90f);
                ped.name = $"Pedestal {colors[i]}";
                var z = ped.GetComponent<SnapZone>();
                SetValue(z, "acceptedKey", p => p.stringValue = keys[i]);
                var stripe = Prim(PrimitiveType.Cube, "Color Stripe", ped.transform, new Vector3(0f, 0.905f, 0f), new Vector3(0.42f, 0.01f, 0.42f), colors[i], collider: false);
                stripe.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                zones.Add(z);
            }
            var reward = Prim(PrimitiveType.Sphere, "Reward", sig, Vector3.zero, Vector3.one * 0.5f, "Glow", collider: false);
            reward.transform.localPosition = new Vector3(0f, 2.2f, 0.6f);
            var rewardLight = reward.AddComponent<Light>();
            rewardLight.type = LightType.Point;
            rewardLight.range = 6f;
            rewardLight.intensity = 4f;
            rewardLight.color = new Color(1f, 0.85f, 0.4f);
            var statusPanel = WorldPanel.Create(sig, "Signature Status", new Vector2(1.2f, 0.55f), "", "");
            statusPanel.transform.localPosition = new Vector3(0f, 1.6f, -0.2f);
            statusPanel.transform.localRotation = Quaternion.Euler(0f, 180f, 0f); // UI reads along its forward axis
            var sigComp = sig.gameObject.AddComponent<FillSnapZonesSignature>();
            SetValue(sigComp, "title", p => p.stringValue = "Signature example: Restore the exhibit");
            SetValue(sigComp, "description", p => p.stringValue = "Bring each item from station 1 to the pedestal of the same color.");
            SetArray(sigComp, "zones", zones.Cast<Object>().ToArray());
            Set(sigComp, "reward", reward);
            Set(sigComp, "statusPanel", statusPanel);
            StationSign(sig, "4. Signature (example)", "Your team designs ONE core interaction.\nThis example: restore the exhibit by placing\neach item on its matching pedestal.", 180f);

            // ---- Teleport anchors (wayfinding)
            var anchors = new GameObject("Viewpoints").transform;
            Anchor(anchors, new Vector3(-4f, 0f, -1.4f), 0f, "Viewpoint: Grab");
            Anchor(anchors, new Vector3(0f, 0f, 1.4f), 0f, "Viewpoint: Button & Info");
            Anchor(anchors, new Vector3(2.4f, 0f, 0f), 90f, "Viewpoint: Signature");
            Anchor(anchors, new Vector3(0f, 0f, -5.8f), 0f, "Viewpoint: Entrance");

            // ---- Team extension examples (no code references from here; see Assets/Teams/_TeamTemplate)
            EditorSceneManager.SaveScene(scene, SampleScenePath);
        }

        /// <summary>
        /// Turns a world-space panel so a visitor standing at 'viewer' reads it the right way round.
        /// </summary>
        private static void FacePanel(Component panel, Vector3 viewer)
        {
            var t = panel.transform;
            Vector3 away = t.position - viewer;
            away.y = 0f;
            if (away.sqrMagnitude < 0.0001f) return;
            t.rotation = Quaternion.LookRotation(away.normalized, Vector3.up);
        }

        private static void BuildTeamScene()
        {
            BuildBasicScene(TeamScenePath, "Team Space (template)", addTeamSignature: true);
        }

        private static void BuildBasicScene(string path, string title, bool addTeamSignature)
        {
            var scene = NewEnvironmentScene(title, out var env);
            AddRig(new Vector3(0f, 0f, -3f), 0f);
            var p = WorldPanel.Create(null, "Start Here", new Vector2(1.4f, 0.7f), title,
                "This is your team's scene.\n\n1. Build your space in 'Environment'.\n2. Add interactions from the 3DUI > Create menu.\n3. Design your Signature Interaction (TeamSignatureInteraction).");
            p.transform.position = new Vector3(0f, 1.8f, 0f);
            Interaction("Teleport Anchor", null, new Vector3(0f, 0f, -3f)).name = "Viewpoint: Entrance";

            if (addTeamSignature)
            {
                var type = Type.GetType("TeamTemplate.TeamSignatureInteraction, Assembly-CSharp");
                var sigGo = new GameObject("Team Signature Interaction");
                if (type != null) sigGo.AddComponent(type);
                else Debug.LogWarning("[3DUI] TeamSignatureInteraction not compiled yet; add it to 'Team Signature Interaction' manually.");
            }

            TemplateProjectSetup.EnsureFolder(System.IO.Path.GetDirectoryName(path).Replace('\\', '/'));
            EditorSceneManager.SaveScene(scene, path);
        }

        private static void WallsAround(Transform env, float half, float height)
        {
            var walls = new GameObject("Walls").transform;
            walls.SetParent(env, false);
            Prim(PrimitiveType.Cube, "Wall N", walls, new Vector3(0f, height / 2f, half), new Vector3(half * 2f, height, 0.2f), "Wall").isStatic = true;
            Prim(PrimitiveType.Cube, "Wall S", walls, new Vector3(0f, height / 2f, -half), new Vector3(half * 2f, height, 0.2f), "Wall").isStatic = true;
            Prim(PrimitiveType.Cube, "Wall E", walls, new Vector3(half, height / 2f, 0f), new Vector3(0.2f, height, half * 2f), "Wall").isStatic = true;
            Prim(PrimitiveType.Cube, "Wall W", walls, new Vector3(-half, height / 2f, 0f), new Vector3(0.2f, height, half * 2f), "Wall").isStatic = true;
        }

        private static void StationSign(Transform station, string title, string body, float yaw = 0f)
        {
            var p = WorldPanel.Create(station, "Sign", new Vector2(1.0f, 0.5f), title, body);
            p.transform.localPosition = new Vector3(0f, 2.3f, 0.2f);
            p.transform.localRotation = Quaternion.Euler(0f, yaw, 0f);
        }

        private static void Anchor(Transform parent, Vector3 pos, float yaw, string label)
        {
            var go = Interaction("Teleport Anchor", parent, pos, yaw);
            go.name = label;
            var a = go.GetComponent<TeleportAnchor>();
            SetLabel(a, label, "Click to go here");
        }

        private static void ColorItem(GameObject item, string mat, string displayName, string key)
        {
            item.GetComponent<Renderer>().sharedMaterial = M(mat);
            var g = item.GetComponent<Grabbable>();
            SetLabel(g, displayName, "Put me on the " + key + " pedestal");
            var k = item.AddComponent<SnapKey>();
            k.Key = key;
            EditorUtility.SetDirty(k);
        }

        private static void ReplaceMesh(GameObject go, PrimitiveType type)
        {
            var tmp = GameObject.CreatePrimitive(type);
            go.GetComponent<MeshFilter>().sharedMesh = tmp.GetComponent<MeshFilter>().sharedMesh;
            Object.DestroyImmediate(go.GetComponent<BoxCollider>());
            if (type == PrimitiveType.Sphere) go.AddComponent<SphereCollider>();
            else go.AddComponent<CapsuleCollider>();
            Object.DestroyImmediate(tmp);
        }

        private static void SetBuildScenes(params string[] paths)
        {
            EditorBuildSettings.scenes = paths.Select(p => new EditorBuildSettingsScene(p, true)).ToArray();
        }
    }
}
