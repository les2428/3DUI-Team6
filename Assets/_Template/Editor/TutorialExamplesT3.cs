using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering;

namespace ThreeDUI.Editor
{
    /// <summary>Tutorial 3 · Make it interactive: drawer, lever, dial, door prefabs and the example scene.</summary>
    public static partial class TemplateSceneBuilder
    {
        public const string T3Path = ExampleDir + "/T3_Interactions.unity";

        [MenuItem("3DUI/Create/Interactive/Drawer (cabinet)", priority = 150)] private static void MenuDrawer() => PlaceInScene("Drawer Cabinet");
        [MenuItem("3DUI/Create/Interactive/Lever", priority = 151)] private static void MenuLever() => PlaceInScene("Lever");
        [MenuItem("3DUI/Create/Interactive/Dial", priority = 152)] private static void MenuDial() => PlaceInScene("Dial");
        [MenuItem("3DUI/Create/Interactive/Door (hinged)", priority = 153)] private static void MenuDoor() => PlaceInScene("Hinged Door");
        [MenuItem("3DUI/Examples/T3 Interactions (drawer, lever, dial, door)", priority = 21)] private static void OpenT3() => OpenExample(T3Path);

        static partial void BuildT3()
        {
            BuildT3Prefabs();
            BuildT3StarterPrefabs();
            BuildT3Scene();
        }

        public const string StarterPrefabDir = PrefabDir + "/Starter";

        /// <summary>
        /// The same drawer, lever and door, plus a key and lock, with no behaviour (no scripts at all): shipped alone as 3DUI_T3_Starter.unitypackage,
        /// so students can import them on the Tutorial 2 template and write the constraints themselves (MyDrawer, MyHinge, MySnap).
        /// Pivots are already where the hinge is. Add the script to: Drawer · Lever/Pivot · Door/Hinge · Key (Socket = Socket).
        /// </summary>
        public static void BuildT3StarterPrefabs()
        {
            TemplateProjectSetup.EnsureFolder(StarterPrefabDir);
            {
                var root = new GameObject("Bare Drawer Cabinet");
                Prim(PrimitiveType.Cube, "Body", root.transform, new Vector3(0f, 0.375f, 0f), new Vector3(0.8f, 0.75f, 0.5f), "Wood");
                var drawer = Prim(PrimitiveType.Cube, "Drawer", root.transform, new Vector3(0f, 0.55f, 0.02f), new Vector3(0.7f, 0.18f, 0.48f), "Pedestal");
                Prim(PrimitiveType.Cube, "Handle", drawer.transform, new Vector3(0f, 0f, 0.56f), new Vector3(0.3f, 0.2f, 0.12f), "Dark", collider: false);
                Prim(PrimitiveType.Sphere, "Inside (shown when open)", drawer.transform, new Vector3(0f, 0.6f, -0.1f), new Vector3(0.18f, 0.5f, 0.25f), "Yellow", collider: false);
                SavePrefab(root, $"{StarterPrefabDir}/Bare Drawer Cabinet.prefab");
            }
            {
                var root = new GameObject("Bare Lever");
                Prim(PrimitiveType.Cube, "Base", root.transform, new Vector3(0f, 0.45f, 0f), new Vector3(0.35f, 0.9f, 0.35f), "Dark");
                var pivot = Empty("Pivot", root.transform, new Vector3(0f, 0.92f, 0f));
                Prim(PrimitiveType.Cylinder, "Stick", pivot.transform, new Vector3(0f, 0.2f, 0f), new Vector3(0.04f, 0.2f, 0.04f), "Pedestal");
                Prim(PrimitiveType.Sphere, "Knob", pivot.transform, new Vector3(0f, 0.42f, 0f), Vector3.one * 0.1f, "Red");
                SavePrefab(root, $"{StarterPrefabDir}/Bare Lever.prefab");
            }
            {
                var root = new GameObject("Bare Door");
                Prim(PrimitiveType.Cube, "Frame left", root.transform, new Vector3(-0.55f, 1.075f, 0f), new Vector3(0.1f, 2.15f, 0.12f), "Dark").isStatic = true;
                Prim(PrimitiveType.Cube, "Frame right", root.transform, new Vector3(0.55f, 1.075f, 0f), new Vector3(0.1f, 2.15f, 0.12f), "Dark").isStatic = true;
                Prim(PrimitiveType.Cube, "Frame top", root.transform, new Vector3(0f, 2.2f, 0f), new Vector3(1.2f, 0.1f, 0.12f), "Dark").isStatic = true;
                var hinge = Empty("Hinge", root.transform, new Vector3(-0.48f, 0f, 0f));
                Prim(PrimitiveType.Cube, "Door", hinge.transform, new Vector3(0.48f, 1.05f, 0f), new Vector3(0.95f, 2.08f, 0.05f), "Wood");
                Prim(PrimitiveType.Sphere, "Handle", hinge.transform, new Vector3(0.85f, 1.0f, 0.06f), Vector3.one * 0.07f, "Yellow", collider: false);
                SavePrefab(root, $"{StarterPrefabDir}/Bare Door.prefab");
            }
            {
                // A key on one stand, a lock on the other. "Socket" is turned 45°, so a snapped key visibly takes its rotation too.
                var root = new GameObject("Bare Key and Lock");
                Prim(PrimitiveType.Cube, "Key stand", root.transform, new Vector3(-0.35f, 0.45f, 0f), new Vector3(0.3f, 0.9f, 0.3f), "Pedestal");
                var key = Prim(PrimitiveType.Cube, "Key", root.transform, new Vector3(-0.35f, 0.92f, 0f), new Vector3(0.06f, 0.03f, 0.18f), "Yellow");
                var rb = key.AddComponent<Rigidbody>();
                rb.mass = 0.2f;
                rb.interpolation = RigidbodyInterpolation.Interpolate;
                Prim(PrimitiveType.Cube, "Lock", root.transform, new Vector3(0.35f, 0.45f, 0f), new Vector3(0.3f, 0.9f, 0.3f), "Dark");
                var socket = Empty("Socket", root.transform, new Vector3(0.35f, 0.92f, 0f));
                socket.transform.localRotation = Quaternion.Euler(0f, 45f, 0f);
                var slot = Prim(PrimitiveType.Cube, "Slot (shows where)", socket.transform, new Vector3(0f, -0.014f, 0f), new Vector3(0.08f, 0.006f, 0.2f), "Ghost", collider: false);
                slot.GetComponent<Renderer>().shadowCastingMode = ShadowCastingMode.Off;
                SavePrefab(root, $"{StarterPrefabDir}/Bare Key and Lock.prefab");
            }
        }

        private static void BuildT3Prefabs()
        {
            // Drawer cabinet: the drawer slides out along its own Z, and something is inside.
            {
                var root = new GameObject("Drawer Cabinet");
                Prim(PrimitiveType.Cube, "Body", root.transform, new Vector3(0f, 0.375f, 0f), new Vector3(0.8f, 0.75f, 0.5f), "Wood");
                var drawer = Prim(PrimitiveType.Cube, "Drawer", root.transform, new Vector3(0f, 0.55f, 0.02f), new Vector3(0.7f, 0.18f, 0.48f), "Pedestal");
                Prim(PrimitiveType.Cube, "Handle", drawer.transform, new Vector3(0f, 0f, 0.56f), new Vector3(0.3f, 0.2f, 0.12f), "Dark", collider: false);
                Prim(PrimitiveType.Sphere, "Inside (shown when open)", drawer.transform, new Vector3(0f, 0.6f, -0.1f), new Vector3(0.18f, 0.5f, 0.25f), "Yellow", collider: false);
                var slide = drawer.AddComponent<SlideAlongAxis>();
                SetValue(slide, "travel", p => p.floatValue = 0.38f);
                SetValue(slide, "canSnap", p => p.boolValue = false);
                SetLabel(slide, "Drawer", "Grab and pull");
                drawer.AddComponent<InteractableFeedback>();
                SavePrefab(root, $"{InteractionPrefabDir}/Drawer Cabinet.prefab");
            }
            // Lever: a box with a handle that tilts forward and back (spring-free), 0..1 between the stops.
            {
                var root = new GameObject("Lever");
                Prim(PrimitiveType.Cube, "Base", root.transform, new Vector3(0f, 0.45f, 0f), new Vector3(0.35f, 0.9f, 0.35f), "Dark");
                var pivot = Empty("Pivot", root.transform, new Vector3(0f, 0.92f, 0f));
                Prim(PrimitiveType.Cylinder, "Stick", pivot.transform, new Vector3(0f, 0.2f, 0f), new Vector3(0.04f, 0.2f, 0.04f), "Pedestal");
                Prim(PrimitiveType.Sphere, "Knob", pivot.transform, new Vector3(0f, 0.42f, 0f), Vector3.one * 0.1f, "Red");
                var rot = pivot.AddComponent<RotateAroundAxis>();
                SetValue(rot, "axis", p => p.vector3Value = Vector3.right);
                SetValue(rot, "minAngle", p => p.floatValue = -40f);
                SetValue(rot, "maxAngle", p => p.floatValue = 40f);
                SetValue(rot, "startAngle", p => p.floatValue = -40f);
                SetLabel(rot, "Lever", "Drag it forward");
                pivot.AddComponent<InteractableFeedback>();
                SavePrefab(root, $"{InteractionPrefabDir}/Lever.prefab");
            }
            // Dial: a disc on a stand that turns around Y, 0..270 degrees.
            {
                var root = new GameObject("Dial");
                Prim(PrimitiveType.Cylinder, "Stand", root.transform, new Vector3(0f, 0.45f, 0f), new Vector3(0.3f, 0.45f, 0.3f), "Pedestal");
                var pivot = Empty("Pivot", root.transform, new Vector3(0f, 0.93f, 0f));
                Prim(PrimitiveType.Cylinder, "Disc", pivot.transform, Vector3.zero, new Vector3(0.26f, 0.025f, 0.26f), "Dark");
                Prim(PrimitiveType.Cube, "Pointer", pivot.transform, new Vector3(0f, 0.03f, 0.08f), new Vector3(0.03f, 0.02f, 0.1f), "Yellow", collider: false);
                var rot = pivot.AddComponent<RotateAroundAxis>();
                SetValue(rot, "axis", p => p.vector3Value = Vector3.up);
                SetValue(rot, "minAngle", p => p.floatValue = 0f);
                SetValue(rot, "maxAngle", p => p.floatValue = 270f);
                SetLabel(rot, "Dial", "Drag around it");
                pivot.AddComponent<InteractableFeedback>();
                SavePrefab(root, $"{InteractionPrefabDir}/Dial.prefab");
            }
            // Hinged door in a frame: the pivot is the hinge at the left edge, the door swings 0..100 degrees.
            {
                var root = new GameObject("Hinged Door");
                Prim(PrimitiveType.Cube, "Frame left", root.transform, new Vector3(-0.55f, 1.075f, 0f), new Vector3(0.1f, 2.15f, 0.12f), "Dark").isStatic = true;
                Prim(PrimitiveType.Cube, "Frame right", root.transform, new Vector3(0.55f, 1.075f, 0f), new Vector3(0.1f, 2.15f, 0.12f), "Dark").isStatic = true;
                Prim(PrimitiveType.Cube, "Frame top", root.transform, new Vector3(0f, 2.2f, 0f), new Vector3(1.2f, 0.1f, 0.12f), "Dark").isStatic = true;
                var hinge = Empty("Hinge", root.transform, new Vector3(-0.48f, 0f, 0f));
                Prim(PrimitiveType.Cube, "Door", hinge.transform, new Vector3(0.48f, 1.05f, 0f), new Vector3(0.95f, 2.08f, 0.05f), "Wood");
                Prim(PrimitiveType.Sphere, "Handle", hinge.transform, new Vector3(0.85f, 1.0f, 0.06f), Vector3.one * 0.07f, "Yellow", collider: false);
                var rot = hinge.AddComponent<RotateAroundAxis>();
                SetValue(rot, "axis", p => p.vector3Value = Vector3.up);
                SetValue(rot, "minAngle", p => p.floatValue = -100f);
                SetValue(rot, "maxAngle", p => p.floatValue = 0f);
                SetValue(rot, "startAngle", p => p.floatValue = 0f);
                SetLabel(rot, "Door", "Drag to open");
                hinge.AddComponent<InteractableFeedback>();
                SavePrefab(root, $"{InteractionPrefabDir}/Hinged Door.prefab");
            }
        }

        private static void BuildT3Scene()
        {
            var scene = NewEnvironmentScene("T3 Interactions", out var env);
            WallsAround(env, 7f, 3f);
            AddRig(new Vector3(0f, 0f, -4.5f), 0f);
            var stations = new GameObject("Interaction ideas").transform;
            Vector3 viewer = new Vector3(0f, 1.6f, -4.5f);

            // 1. drawer: Grabbable subclass, constrained to one axis
            Interaction("Drawer Cabinet", stations, new Vector3(-4.6f, 0f, 0.5f), 180f);
            Note(stations, "1. Drawer", "SlideAlongAxis: a Grabbable that moves only along one axis.\nPull it open: something is inside.",
                 new Vector3(-4.6f, 1.75f, 0.9f), viewer, 1.3f, 0.5f);

            // 2. lever drives a lamp through LightDimmer (no code: onValueChanged -> SetLevel)
            var lever = Interaction("Lever", stations, new Vector3(-1.6f, 0f, 0.5f), 180f);
            var lamp = Interaction("Lamp", stations, new Vector3(-0.8f, 0f, 1.2f));
            var bulb = lamp.transform.Find("Bulb (on)").gameObject;
            bulb.SetActive(true);
            var light = bulb.GetComponent<Light>();
            light.intensity = 0f; light.enabled = false;
            var dimmer = lamp.AddComponent<LightDimmer>();
            Set(dimmer, "target", light);
            Set(dimmer, "glow", bulb.transform.Find("Glow").gameObject);
            bulb.transform.Find("Glow").gameObject.SetActive(false);
            var leverRot = lever.GetComponentInChildren<RotateAroundAxis>();
            UnityEventTools.AddPersistentListener(leverRot.onValueChanged, new UnityAction<float>(dimmer.SetLevel));
            EditorUtility.SetDirty(leverRot);
            Note(stations, "2. Lever → light", "RotateAroundAxis reports 0..1.\nNo code: On Value Changed → LightDimmer.SetLevel.",
                 new Vector3(-1.4f, 1.75f, 0.9f), viewer, 1.3f, 0.5f);

            // 3. dial drives a value panel
            var dial = Interaction("Dial", stations, new Vector3(1.6f, 0f, 0.5f));
            var valueRoot = new GameObject("Dial value").transform;
            valueRoot.SetParent(stations, false);
            valueRoot.position = new Vector3(1.6f, 1.35f, 0.9f);
            var valuePanel = WorldPanel.Create(valueRoot, "Panel", new Vector2(0.6f, 0.3f), "Volume", "0 %");
            FacePanel(valuePanel, viewer);
            var display = valueRoot.gameObject.AddComponent<ValueDisplay>();
            Set(display, "panel", valuePanel);
            SetValue(display, "title", p => p.stringValue = "Volume");
            var dialRot = dial.GetComponentInChildren<RotateAroundAxis>();
            UnityEventTools.AddPersistentListener(dialRot.onValueChanged, new UnityAction<float>(display.Show));
            EditorUtility.SetDirty(dialRot);
            Note(stations, "3. Dial → panel", "Same component, different axis.\nOn Value Changed → ValueDisplay.Show.",
                 new Vector3(1.6f, 2.05f, 0.9f), viewer, 1.3f, 0.45f);

            // 4. hinged door
            Interaction("Hinged Door", stations, new Vector3(4.6f, 0f, 1.0f), 180f);
            Note(stations, "4. Door", "The pivot is the hinge, not the middle of the door.\nLimits: 0 to 100 degrees.",
                 new Vector3(4.6f, 2.6f, 1.0f), viewer, 1.3f, 0.45f);

            // 5. return home: throw it over the wall and it comes back
            Prim(PrimitiveType.Cube, "Table", stations, new Vector3(0f, 0.375f, -2.5f), new Vector3(1.0f, 0.75f, 0.6f), "Wood").isStatic = true;
            var ball = Interaction("Grabbable Cube", stations, new Vector3(0f, 0.85f, -2.5f));
            ball.name = "Throwable (ReturnHome)";
            SetLabel(ball.GetComponent<Grabbable>(), "Throw me", "I come back");
            Note(stations, "5. Cannot get lost", "ReturnHome: thrown too far or through the floor,\nit returns after 1.5 s with a sound.",
                 new Vector3(-1.3f, 1.3f, -2.5f), viewer, 1.2f, 0.45f);

            EditorSceneManager.SaveScene(scene, T3Path);
        }
    }
}
