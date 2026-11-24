using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ThreeDUI.Tests
{
    /// <summary>
    /// Play-mode smoke tests for the sample scene. A scripted pointer stands in for the mouse / controller,
    /// so the full hover -> select -> activate pipeline is exercised exactly like a real visitor.
    /// Run: Window > General > Test Runner > PlayMode.
    /// </summary>
    public class SamplePlaygroundTests
    {
        private const string ScenePath = "Assets/_Template/Scenes/SamplePlayground.unity";

        /// <summary>A pointer driven by the test.</summary>
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

        [UnitySetUp]
        public IEnumerator LoadScene()
        {
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                ScenePath, new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return null;
#endif
            yield return null;
            pointer = new GameObject("ScriptedPointer").AddComponent<ScriptedPointer>();
            // Let physics settle the props on the table.
            for (int i = 0; i < 30; i++) yield return new WaitForFixedUpdate();
        }

        private static Ray Down(Vector3 target) => new Ray(target + Vector3.up * 1f, Vector3.down);

        private IEnumerator Click(Vector3 target)
        {
            pointer.ray = Down(target);
            yield return null;
            pointer.primary = true;
            yield return null;
            pointer.primary = false;
            yield return null;
        }

        [UnityTest]
        public IEnumerator SceneStartsInDesktopMode()
        {
            Assert.IsNotNull(PlayerRig.Instance, "PlayerRig missing");
            Assert.IsNotNull(XRModeManager.Instance, "XRModeManager missing");
            Assert.AreEqual(ExperienceMode.Desktop, XRModeManager.Mode);
            Assert.IsTrue(GameObject.Find("Welcome Desktop") != null, "desktop-only welcome panel should be visible");
            Assert.IsTrue(GameObject.Find("Welcome XR") == null, "XR-only welcome panel should be hidden");
            yield return null;
        }

        [UnityTest]
        public IEnumerator HoverShowsLabel()
        {
            var cap = Object.FindObjectsByType<PushButton3D>(FindObjectsSortMode.None).First();
            pointer.ray = Down(cap.transform.position);
            yield return null;
            yield return null;
            Assert.AreEqual(cap, pointer.Hovered);
            Assert.IsTrue(cap.IsHovered);
        }

        [UnityTest]
        public IEnumerator ButtonTogglesLamp()
        {
            var cap = Object.FindObjectsByType<PushButton3D>(FindObjectsSortMode.None).First();
            var bulb = GameObject.Find("Station 2 - Button").transform.Find("Lamp/Bulb").gameObject;
            Assert.IsFalse(bulb.activeSelf);

            yield return Click(cap.transform.position);
            Assert.IsTrue(cap.IsOn, "button should be on after one click");
            Assert.IsTrue(bulb.activeSelf, "lamp should turn on");

            yield return Click(cap.transform.position);
            Assert.IsFalse(bulb.activeSelf, "lamp should turn off again");
        }

        [UnityTest]
        public IEnumerator InfoPointTogglesPanel()
        {
            var info = Object.FindObjectsByType<InfoPoint>(FindObjectsSortMode.None).First();
            Assert.IsFalse(info.IsOpen);
            // Stand next to it so auto-hide does not close the panel.
            PlayerRig.Instance.TeleportTo(info.transform.position + new Vector3(0f, 0f, -1.5f));
            yield return Click(info.transform.position);
            Assert.IsTrue(info.IsOpen);
        }

        [UnityTest]
        public IEnumerator TeleportAnchorMovesRig()
        {
            var anchor = Object.FindObjectsByType<TeleportAnchor>(FindObjectsSortMode.None)
                .First(a => a.name == "Viewpoint: Signature");
            yield return Click(anchor.transform.position);
            yield return null;
            var feet = PlayerRig.Instance.FeetPosition;
            Assert.Less(Vector2.Distance(new Vector2(feet.x, feet.z), new Vector2(anchor.transform.position.x, anchor.transform.position.z)), 0.05f);
            Assert.Greater(Vector3.Dot(PlayerRig.Instance.HeadForwardFlat, anchor.transform.forward), 0.99f, "should face the anchor direction");
        }

        [UnityTest]
        public IEnumerator GrabIntoSnapZonesCompletesSignature()
        {
            var signature = Object.FindObjectsByType<FillSnapZonesSignature>(FindObjectsSortMode.None).Single();
            var zones = Object.FindObjectsByType<SnapZone>(FindObjectsSortMode.None);
            var reward = GameObject.Find("Station 4 - Signature (example)").transform.Find("Reward").gameObject;
            Assert.AreEqual(3, zones.Length);
            Assert.IsFalse(reward.activeSelf);

            foreach (var key in new[] { "red", "green", "blue" })
            {
                var item = Object.FindObjectsByType<SnapKey>(FindObjectsSortMode.None).Single(k => k.Key == key).GetComponent<Grabbable>();
                var zone = zones.Single(z => z.AcceptedKey == key);
                var attach = zone.transform.Find("Attach Point");

                // Point at the item and press.
                pointer.ray = Down(item.transform.position);
                yield return null;
                pointer.primary = true;
                yield return null;
                Assert.IsTrue(item.IsGrabbed, $"{key} item should be grabbed");

                // Carry it: same ray, moved above the pedestal (hold distance is preserved).
                var start = pointer.ray.origin;
                var end = attach.position + Vector3.up * 1f;
                for (int i = 1; i <= 10; i++)
                {
                    pointer.ray = new Ray(Vector3.Lerp(start, end, i / 10f), Vector3.down);
                    yield return null;
                }

                pointer.primary = false;
                yield return null;
                Assert.IsFalse(item.IsGrabbed);
                Assert.IsTrue(zone.IsOccupied, $"{key} item should snap into its pedestal (distance {(item.transform.position - attach.position).magnitude:F2} m)");
                Assert.AreEqual(item, zone.Current);
            }

            Assert.AreEqual(SignatureInteraction.State.Completed, signature.CurrentState);
            Assert.IsTrue(reward.activeSelf, "reward should appear");
        }

        [UnityTest]
        public IEnumerator WrongItemDoesNotSnap()
        {
            var red = Object.FindObjectsByType<SnapKey>(FindObjectsSortMode.None).Single(k => k.Key == "red").GetComponent<Grabbable>();
            var blueZone = Object.FindObjectsByType<SnapZone>(FindObjectsSortMode.None).Single(z => z.AcceptedKey == "blue");
            var attach = blueZone.transform.Find("Attach Point");

            pointer.ray = Down(red.transform.position);
            yield return null;
            pointer.primary = true;
            yield return null;
            Assert.IsTrue(red.IsGrabbed);
            pointer.ray = Down(attach.position);
            yield return null;
            pointer.primary = false;
            yield return null;
            Assert.IsFalse(blueZone.IsOccupied, "red item must not fit the blue pedestal");
        }
    }
}
