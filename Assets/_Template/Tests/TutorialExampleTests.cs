using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ThreeDUI.Tests
{
    /// <summary>
    /// Play-mode tests for the tutorial example scenes: a scripted pointer drags the drawer, lever, dial and door
    /// the way a visitor would, and checks what they report.
    /// </summary>
    public class TutorialExampleTests
    {
        private class ScriptedPointer : Pointer
        {
            public Ray ray;
            public bool primary;
            public override HandSide Side => HandSide.Right;
            public override bool IsActive => true;
            public override Ray GetRay() => ray;
            public override bool GetButton(PointerButton button) => button == PointerButton.Primary && primary;
        }

        private ScriptedPointer pointer;

        private IEnumerator Load(string path)
        {
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(path, new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return null;
#endif
            yield return null;
            // The rig's own desktop pointer would compete with the scripted one.
            foreach (var p in Object.FindObjectsByType<DesktopPointer>(FindObjectsSortMode.None)) p.enabled = false;
            pointer = new GameObject("ScriptedPointer").AddComponent<ScriptedPointer>();
            for (int i = 0; i < 20; i++) yield return new WaitForFixedUpdate();
        }

        /// <summary>Aim from 'from' at 'at', press, move the aim point along 'drag' in steps, release.</summary>
        private IEnumerator Drag(Vector3 from, Vector3 at, Vector3 drag, int steps = 20)
        {
            pointer.ray = new Ray(from, (at - from).normalized);
            yield return null; yield return null;
            pointer.primary = true;
            yield return null;
            for (int i = 1; i <= steps; i++)
            {
                var target = at + drag * (i / (float)steps);
                pointer.ray = new Ray(from, (target - from).normalized);
                yield return null;
            }
            pointer.primary = false;
            yield return null;
        }

        static T Find<T>(string name) where T : Component =>
            Object.FindObjectsByType<T>(FindObjectsSortMode.None).First(c => c.transform.root.name == name || c.name == name || c.transform.parent?.name == name);

        [UnityTest]
        public IEnumerator T2_SignpostPointsAtLandmark()
        {
            yield return Load("Assets/_Template/Examples/T2_Wayfinding.unity");
            var sign = Object.FindAnyObjectByType<Signpost>();
            var arrow = sign.transform.Find("Arrow");
            var to = sign.Target.position - arrow.position; to.y = 0;
            Assert.Greater(Vector3.Dot(arrow.forward, to.normalized), 0.99f, "the arrow should point at the landmark");
        }

        [UnityTest]
        public IEnumerator T3_DrawerOpensWhenPulled()
        {
            yield return Load("Assets/_Template/Examples/T3_Interactions.unity");
            var drawer = Object.FindAnyObjectByType<SlideAlongAxis>();
            Assert.Less(drawer.Value01, 0.05f);
            Vector3 front = drawer.transform.position + drawer.transform.forward * 0.25f;
            Vector3 eye = front + drawer.transform.forward * 1.3f + Vector3.up * 1.05f;   // standing in front of it
            // Pulling = the aim point moves towards the visitor, along the drawer's axis.
            yield return Drag(eye, front, drawer.transform.forward * 0.5f);
            Assert.Greater(drawer.Value01, 0.5f, "the drawer should be open after pulling");
        }

        [UnityTest]
        public IEnumerator T3_LeverDimsTheLamp()
        {
            yield return Load("Assets/_Template/Examples/T3_Interactions.unity");
            var lever = Object.FindObjectsByType<RotateAroundAxis>(FindObjectsSortMode.None).First(r => r.transform.parent.name == "Lever");
            var dimmer = Object.FindAnyObjectByType<LightDimmer>();
            var light = dimmer.GetComponentInChildren<Light>(true);
            float before = lever.Value01;
            Vector3 knob = lever.transform.Find("Knob").position;
            Vector3 eye = knob + new Vector3(1.2f, 0.2f, 0f);          // from the side, so the drag is across the ray
            yield return Drag(eye, knob, (lever.transform.parent.forward * 0.35f) + Vector3.down * 0.1f);
            float after = lever.Value01;
            if (Mathf.Abs(after - before) < 0.2f)                       // the other way round if the first went to the stop
            {
                yield return Drag(eye, lever.transform.Find("Knob").position, -(lever.transform.parent.forward * 0.35f) + Vector3.down * 0.1f);
                after = lever.Value01;
            }
            Assert.Greater(Mathf.Abs(after - before), 0.2f, "dragging the knob should move the lever");
            Assert.AreEqual(after * 3f, light.intensity, 0.05f, "LightDimmer should follow the lever (wired with a UnityEvent)");
        }

        [UnityTest]
        public IEnumerator T3_DialUpdatesItsPanel()
        {
            yield return Load("Assets/_Template/Examples/T3_Interactions.unity");
            var dial = Object.FindObjectsByType<RotateAroundAxis>(FindObjectsSortMode.None).First(r => r.transform.parent.name == "Dial");
            var display = Object.FindAnyObjectByType<ValueDisplay>();
            Vector3 c = dial.transform.position;
            Vector3 edge = c + dial.transform.forward * 0.09f;
            Vector3 eye = c + Vector3.up * 1.0f;
            // drag a quarter turn around the centre
            yield return Drag(eye, edge, (c + dial.transform.right * 0.09f) - edge);
            Assert.Greater(dial.Value01, 0.1f, "the dial should have turned");
            var text = display.GetComponentInChildren<WorldPanel>().BodyText.text;
            Assert.AreNotEqual("0 %", text, "the value panel should show the new value");
        }

        [UnityTest]
        public IEnumerator T3_DoorSwings()
        {
            yield return Load("Assets/_Template/Examples/T3_Interactions.unity");
            var door = Object.FindObjectsByType<RotateAroundAxis>(FindObjectsSortMode.None).First(r => r.transform.parent.name == "Hinged Door");
            Vector3 handle = door.transform.Find("Handle").position;
            Vector3 eye = handle + door.transform.parent.forward * 1.5f + Vector3.up * 0.4f;
            var sideways = door.transform.parent.right * -0.4f + door.transform.parent.forward * 0.3f;
            yield return Drag(eye, handle, sideways);
            if (Mathf.Abs(door.Angle) < 10f) yield return Drag(eye, door.transform.Find("Handle").position, -sideways);
            Assert.Greater(Mathf.Abs(door.Angle), 10f, "the door should swing when dragged at the handle");
            Assert.LessOrEqual(Mathf.Abs(door.Angle), 100.01f, "and stay within its limits");
        }

        [UnityTest]
        public IEnumerator T4_ProximityLabelAppearsWhenClose()
        {
            yield return Load("Assets/_Template/Examples/T4_SpatialUI.unity");
            var reveal = Object.FindAnyObjectByType<ProximityReveal>();
            var label = reveal.transform.GetChild(0).gameObject;
            Assert.IsFalse(label.activeSelf, "hidden while the visitor is far away");
            PlayerRig.Instance.TeleportTo(reveal.transform.position + new Vector3(0f, 0f, -1.8f) - Vector3.up * reveal.transform.position.y);
            yield return new WaitForSeconds(0.5f);
            Assert.IsTrue(label.activeSelf, "shown when the visitor comes close");
            PlayerRig.Instance.TeleportTo(new Vector3(0f, 0f, -6f));
            yield return new WaitForSeconds(0.5f);
            Assert.IsFalse(label.activeSelf, "hidden again when the visitor walks away");
        }

        [UnityTest]
        public IEnumerator T4_AnsweredButtonGivesFeedback()
        {
            yield return Load("Assets/_Template/Examples/T4_SpatialUI.unity");
            var answered = GameObject.Find("Answered button");
            var silent = GameObject.Find("Silent button");
            Assert.IsNull(silent.GetComponentInChildren<InteractableFeedback>(), "the silent button has no feedback component");
            var cap = answered.GetComponentInChildren<PushButton3D>();
            var fx = answered.GetComponentInChildren<FeedbackActions>();
            Vector3 baseScale = fx.transform.localScale;
            pointer.ray = new Ray(cap.transform.position + Vector3.up, Vector3.down);
            yield return null; yield return null;
            pointer.primary = true; yield return null;
            pointer.primary = false; yield return null;
            yield return new WaitForSeconds(0.1f);
            Assert.Greater(fx.transform.localScale.x, baseScale.x * 1.02f, "the lamp should pulse when the button is pressed");
            Assert.IsTrue(answered.transform.Find("Lamp/Bulb (on)").gameObject.activeSelf, "and switch on");
        }

        [UnityTest]
        public IEnumerator T3_LostObjectReturnsHome()
        {
            yield return Load("Assets/_Template/Examples/T3_Interactions.unity");
            var cube = Object.FindObjectsByType<ReturnHome>(FindObjectsSortMode.None).First();
            Vector3 home = cube.transform.position;
            var body = cube.GetComponent<Rigidbody>();
            body.isKinematic = true;
            cube.transform.position = home + new Vector3(0f, -6f, 0f);    // through the floor
            yield return new WaitForSeconds(2.2f);
            Assert.Less(Vector3.Distance(cube.transform.position, home), 0.3f, "it should be back where it started");
        }
    }
}
