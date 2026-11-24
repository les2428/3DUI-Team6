using TMPro;
using UnityEditor;
using UnityEditor.Events;
using UnityEditor.SceneManagement;
using UnityEngine;
using UnityEngine.Events;

namespace ThreeDUI.Editor
{
    /// <summary>Tutorial 4 · Make it understandable: readable panels, proximity, feedback, and the example scene.</summary>
    public static partial class TemplateSceneBuilder
    {
        public const string T4Path = ExampleDir + "/T4_SpatialUI.unity";

        [MenuItem("3DUI/Create/Spatial UI/Sign (readable from 3 m)", priority = 160)]
        private static void MenuSign() => PlacePanel(Sign(null, "Sign", "TODO: title", "TODO: one or two short lines.", 1.2f, 0.7f, 110, 64));

        [MenuItem("3DUI/Create/Spatial UI/Label (readable from 1 m)", priority = 161)]
        private static void MenuLabel() => PlacePanel(Sign(null, "Label", "TODO: name", "TODO: a hint", 0.5f, 0.22f, 48, 30));

        [MenuItem("3DUI/Create/Spatial UI/Proximity panel (appears when you come close)", priority = 162)]
        private static void MenuProximity()
        {
            var root = new GameObject("Proximity panel");
            root.AddComponent<ProximityReveal>();
            var p = Sign(root.transform, "Panel", "TODO: title", "Appears when the visitor is within 2.5 m.", 1.0f, 0.55f, 90, 56);
            p.gameObject.AddComponent<Billboard>();
            PlacePanel(root.GetComponent<Transform>(), 1.6f);
        }

        [MenuItem("3DUI/Examples/T4 Spatial UI and feedback", priority = 22)] private static void OpenT4() => OpenExample(T4Path);

        /// <summary>A world panel with fixed (not auto-shrinking) text sizes, so its readability is predictable.</summary>
        internal static WorldPanel Sign(Transform parent, string name, string title, string body, float w, float h, float titlePx, float bodyPx)
        {
            var p = WorldPanel.Create(parent, name, new Vector2(w, h), title, body);
            foreach (var (t, px) in new[] { (p.TitleText, titlePx), (p.BodyText, bodyPx) })
            {
                t.enableAutoSizing = false;
                t.fontSize = px;
                t.overflowMode = TextOverflowModes.Ellipsis;
            }
            return p;
        }

        private static void PlacePanel(Component c, float height = 1.6f)
        {
            c.transform.position = ScenePivot() + Vector3.up * height;
            Undo.RegisterCreatedObjectUndo(c.gameObject, "Create " + c.name);
            Selection.activeGameObject = c.gameObject;
        }

        static partial void BuildT4()
        {
            var scene = NewEnvironmentScene("T4 Spatial UI", out var env);
            WallsAround(env, 8f, 3f);
            AddRig(new Vector3(0f, 0f, -6f), 0f);
            var ideas = new GameObject("Spatial UI ideas").transform;
            Vector3 eye = new Vector3(0f, 1.6f, -6f);

            // 1. distance: the same small text at 1.5, 3 and 6 m (left side, in a row going away)
            for (int i = 0; i < 3; i++)
            {
                float d = new[] { 1.5f, 3f, 6f }[i];
                var g = new GameObject($"Same text at {d} m").transform;
                g.SetParent(ideas, false);
                g.position = new Vector3(-3.2f, 1.6f, -6f + d);
                var p = Sign(g, "Panel", $"{d} m away", "The same 40 px text on every panel.\nCan you still read this line?", 0.9f, 0.45f, 70, 40);
                FacePanel(p, new Vector3(-3.2f, 1.6f, -8f));
            }
            Note(ideas, "1. Distance", "Walk along the three panels. In the headset text needs to be\nnearer and bigger than feels right on a monitor.",
                 new Vector3(-5.2f, 2.3f, -3.5f), eye, 1.5f, 0.55f);

            // 2. proximity: a label appears only when you come close
            var statue = new GameObject("Statue (proximity label)").transform;
            statue.SetParent(ideas, false);
            statue.position = new Vector3(0f, 0f, 1.5f);
            Prim(PrimitiveType.Cube, "Pedestal", statue, new Vector3(0f, 0.45f, 0f), new Vector3(0.6f, 0.9f, 0.6f), "Pedestal").isStatic = true;
            Prim(PrimitiveType.Capsule, "Statue", statue, new Vector3(0f, 1.3f, 0f), new Vector3(0.35f, 0.4f, 0.35f), "Blue");
            var reveal = new GameObject("Label (ProximityReveal)").transform;
            reveal.SetParent(statue, false);
            reveal.localPosition = new Vector3(0f, 2.0f, 0f);
            reveal.gameObject.AddComponent<ProximityReveal>();
            var label = Sign(reveal, "Panel", "Blue figure, 2026", "Appears when you are within 2.5 m.", 0.8f, 0.36f, 64, 40);
            label.gameObject.AddComponent<Billboard>();
            Note(ideas, "2. Proximity", "Information where and when it is needed:\nwalk up to the statue.",
                 new Vector3(1.6f, 1.9f, 0.6f), eye, 1.2f, 0.45f);

            // 3. silent vs answered: two identical buttons and lamps; only the right one gives feedback
            for (int i = 0; i < 2; i++)
            {
                bool answered = i == 1;
                var st = new GameObject(answered ? "Answered button" : "Silent button").transform;
                st.SetParent(ideas, false);
                st.position = new Vector3(answered ? 4.4f : 2.8f, 0f, -2.6f);
                var b = Interaction("Push Button", st, st.position);
                var lamp = Interaction("Lamp", st, st.position + new Vector3(0.6f, 0f, 0.4f));
                var bulb = lamp.transform.Find("Bulb (on)").gameObject;
                var push = b.GetComponentInChildren<PushButton3D>();
                UnityEventTools.AddBoolPersistentListener(push.onToggledOn, bulb.SetActive, true);
                UnityEventTools.AddBoolPersistentListener(push.onToggledOff, bulb.SetActive, false);
                if (answered)
                {
                    var fx = lamp.AddComponent<FeedbackActions>();
                    UnityEventTools.AddPersistentListener(push.onPressed, new UnityAction(fx.Pulse));
                    UnityEventTools.AddPersistentListener(push.onPressed, new UnityAction(fx.Beep));
                }
                else
                {
                    Object.DestroyImmediate(push.GetComponent<InteractableFeedback>());
                    SetLabel(push, "", "");
                }
                EditorUtility.SetDirty(push);
            }
            Note(ideas, "3. Silent vs answered", "Left: no highlight, no sound, no label.\nRight: highlight, label, beep, the lamp pulses.",
                 new Vector3(3.6f, 2.15f, -2.3f), eye, 1.4f, 0.5f);

            // 4. one hint per platform
            var hints = new GameObject("4. Per-platform hints").transform;
            hints.SetParent(ideas, false);
            hints.position = new Vector3(0f, 2.2f, -3.2f);
            var desk = new GameObject("Desktop only").transform; desk.SetParent(hints, false);
            SetValue(desk.gameObject.AddComponent<ModeVisibility>(), "showInVR", p => p.boolValue = false);
            SetValue(desk.GetComponent<ModeVisibility>(), "showInAR", p => p.boolValue = false);
            FacePanel(Sign(desk, "Hint (desktop)", "4. Hints per platform", "Desktop: click the buttons on the right.\n(In the headset this panel says \"pull the trigger\".)", 1.4f, 0.5f, 70, 44), eye);
            var vr = new GameObject("XR only").transform; vr.SetParent(hints, false);
            SetValue(vr.gameObject.AddComponent<ModeVisibility>(), "showInDesktop", p => p.boolValue = false);
            FacePanel(Sign(vr, "Hint (XR)", "4. Hints per platform", "Headset: point at a button and pull the trigger,\nor press it with the controller.", 1.4f, 0.5f, 70, 44), eye);

            // 5. a guide agent with a body (lecture: avatars, embodiment and AI agents)
            AddGuideToT4(ideas);

            EditorSceneManager.SaveScene(scene, T4Path);
        }
    }
}
