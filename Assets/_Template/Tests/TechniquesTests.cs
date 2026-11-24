using System.Collections;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ThreeDUI.Tests
{
    /// <summary>Tutorial 2, interaction techniques: Go-Go, HOMER and Flashlight (reach), switching between them, and steering travel.</summary>
    public class TechniquesTests
    {
        class ScriptedGoGo : GoGoHand
        {
            public Vector3 aim = Vector3.forward;
            public bool press;
            protected override Ray AimRay() => new Ray(PlayerRig.Instance.Head.position, aim);
            protected override bool PrimaryPressed => press;
        }

        class ScriptedHomer : HomerHand
        {
            public Vector3 aim = Vector3.forward;
            public bool press;
            protected override Ray AimRay() => new Ray(PlayerRig.Instance.Head.position, aim);
            protected override bool PrimaryPressed => press;
        }

        class ScriptedFlashlight : FlashlightPointer
        {
            public Vector3 aim = Vector3.forward;
            public bool press;
            protected override Ray AimRay() => new Ray(PlayerRig.Instance.Head.position, aim);
            protected override bool PrimaryPressed => press;
        }

        class ScriptedSteering : SteeringTravel
        {
            public Vector3 pointing = Vector3.right;
            protected override float Throttle => 1f;
            protected override Vector3 PointingDirection => pointing;
        }

        private IEnumerator Load()
        {
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/_Template/Examples/T2_Wayfinding.unity", new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return null;
#endif
            yield return null;
            foreach (var c in Object.FindObjectsByType<ReachTechnique>(FindObjectsSortMode.None)) Object.Destroy(c.gameObject);
            foreach (var c in Object.FindObjectsByType<SteeringTravel>(FindObjectsSortMode.None)) Object.Destroy(c);
            yield return null;
        }

        [Test]
        public void GoGoMappingIsLinearNearAndQuadraticFar()
        {
            Assert.AreEqual(0.30f, GoGoHand.Map(0.30f, 0.45f, 30f), 1e-5f, "1:1 below D");
            Assert.AreEqual(0.45f, GoGoHand.Map(0.45f, 0.45f, 30f), 1e-5f, "continuous at D");
            Assert.AreEqual(0.70f + 30f * 0.25f * 0.25f, GoGoHand.Map(0.70f, 0.45f, 30f), 1e-4f, "Rr + k(Rr - D)^2");
        }

        [UnityTest]
        public IEnumerator T2_GoGoGrabsACubeBeyondArmsReach()
        {
            yield return Load();
            var rig = PlayerRig.Instance;
            var gogo = new GameObject("Go-Go (test)").AddComponent<ScriptedGoGo>();
            yield return null;
            gogo.TechniqueActive = true;

            // A cube 2.2 m in front of the body: out of reach for a 1:1 hand.
            var body = rig.Head.position;                                  // desktop: measured from the eye
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.localScale = Vector3.one * 0.2f;
            cube.transform.position = body + rig.HeadForwardFlat * 2.2f;
            cube.AddComponent<Rigidbody>().isKinematic = true;
            var grab = cube.AddComponent<Grabbable>();
            gogo.aim = rig.HeadForwardFlat;

            gogo.SimulatedArm = 0.6f;                                         // Rv = 0.6 + 30·0.15² = 1.275 m: too short
            yield return null; yield return null;
            Assert.IsNull(gogo.Hovered, "not reachable yet");

            // Real distance whose virtual distance lands on the cube: solve Rr + k(Rr - D)² = 2.2.
            float rr = 0.45f + (-1f + Mathf.Sqrt(1f + 4f * 30f * (2.2f - 0.45f))) / (2f * 30f);
            gogo.SimulatedArm = rr;
            yield return null; yield return null;
            Assert.AreEqual(2.2f, gogo.VirtualDistance, 0.02f);
            Assert.AreEqual(grab, gogo.Hovered, "the virtual hand touches the far cube");

            gogo.press = true;
            yield return null; yield return null;
            Assert.IsTrue(grab.IsSelected, "grabbed by hand, not by ray");
            Vector3 before = cube.transform.position;
            gogo.SimulatedArm = rr - 0.1f;                                    // pull the real hand in: the cube comes closer, fast
            yield return null; yield return null;
            float moved = Vector3.Distance(before, cube.transform.position);
            Assert.Greater(moved, 0.3f, "10 cm of real hand moves the far cube much more than 10 cm");
            gogo.press = false;
            yield return null;
        }

        static Grabbable MakeCube(Vector3 position, float size)
        {
            var cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.localScale = Vector3.one * size;
            cube.transform.position = position;
            cube.AddComponent<Rigidbody>().isKinematic = true;
            return cube.AddComponent<Grabbable>();
        }

        [Test]
        public void FlashlightScoreIsTheAngleToTheAxisNearerBreaksTies()
        {
            var axis = new Ray(Vector3.zero, Vector3.forward);
            float onAxis = FlashlightPointer.Score(axis, new Vector3(0f, 0f, 3f), 8f, 15f, 0.3f);
            float off = FlashlightPointer.Score(axis, new Vector3(0.2f, 0f, 3f), 8f, 15f, 0.3f);      // ≈ 3.8°
            float outside = FlashlightPointer.Score(axis, new Vector3(1f, 0f, 3f), 8f, 15f, 0.3f);     // ≈ 18°
            float farOnAxis = FlashlightPointer.Score(axis, new Vector3(0f, 0f, 6f), 8f, 15f, 0.3f);
            Assert.Less(onAxis, off, "closer to the axis wins");
            Assert.AreEqual(float.MaxValue, outside, "outside the cone is not a candidate");
            Assert.Less(onAxis, farOnAxis, "same angle: the nearer one wins");
        }

        [UnityTest]
        public IEnumerator T2_FlashlightSelectsASmallObjectTheRayMisses()
        {
            yield return Load();
            var rig = PlayerRig.Instance;
            var fwd = rig.HeadForwardFlat;
            var right = Vector3.Cross(Vector3.up, fwd);
            var axis = Quaternion.AngleAxis(-35f, right) * fwd;           // up into empty air, away from the scene's objects
            var eye = rig.Head.position;
            // A 5 cm block 1.5 m away, 0.1 m beside the axis (≈ 3.8°): a thin ray along the axis misses it.
            var small = MakeCube(eye + axis * 1.5f + right * 0.1f, 0.05f);
            Assert.IsFalse(Physics.Raycast(new Ray(eye, axis), 15f), "the plain ray misses");
            var decoy = MakeCube(eye + axis * 1.5f + right * 0.5f, 0.05f);   // ≈ 18°: outside the cone

            var fl = new GameObject("Flashlight (test)").AddComponent<ScriptedFlashlight>();
            fl.aim = axis;
            yield return null;
            fl.TechniqueActive = true;
            yield return null; yield return null;
            Assert.AreEqual(small, fl.Candidate, "the cone chooses the block nearest its axis");
            Assert.AreEqual(small, fl.Hovered, "and the snapped ray hovers it");

            fl.press = true;
            yield return null; yield return null;
            Assert.IsTrue(small.IsSelected, "grabbed");
            Assert.IsFalse(decoy.IsSelected);
            fl.aim = Quaternion.AngleAxis(15f, Vector3.up) * axis;            // turn the hand: the block comes along
            Vector3 before = small.transform.position;
            yield return null; yield return null;
            Assert.Greater(Vector3.Distance(before, small.transform.position), 0.2f, "manipulated like a ray grab");
            fl.press = false;
            yield return null;
        }

        [UnityTest]
        public IEnumerator T2_HomerSelectsByRayAndScalesTheHand()
        {
            yield return Load();
            var rig = PlayerRig.Instance;
            var fwd = rig.HeadForwardFlat;
            var body = rig.Head.position;                                  // desktop: measured from the eye
            var grab = MakeCube(body + fwd * 3f, 0.2f);                    // far beyond the arm

            var homer = new GameObject("HOMER (test)").AddComponent<ScriptedHomer>();
            homer.aim = fwd;
            yield return null;
            homer.TechniqueActive = true;
            homer.SimulatedArm = 0.3f;
            yield return null; yield return null;
            Assert.IsTrue(homer.HasTarget, "the ray hits");
            Assert.AreEqual(grab, homer.Hovered, "selection by ray-casting: the hand waits at the hit");

            homer.press = true;
            yield return null; yield return null;
            Assert.IsTrue(grab.IsSelected, "grabbed");
            float expected = (3f - 0.1f - 0.01f) / 0.3f;                   // D_object / D_hand
            Assert.AreEqual(expected, homer.Scale, 0.3f, "s = D_object / D_hand at the grab");

            float d0 = Vector3.Distance(body, grab.transform.position);
            homer.SimulatedArm = 0.2f;                                     // 10 cm of real hand towards the body
            yield return null; yield return null;
            float d1 = Vector3.Distance(body, grab.transform.position);
            Assert.AreEqual(0.1f * homer.Scale, d0 - d1, 0.08f, "10 cm of hand moves the object 10·s cm");

            homer.aim = Quaternion.AngleAxis(20f, Vector3.up) * fwd;       // sweep the hand: the object orbits at its distance
            yield return null; yield return null;
            Assert.AreEqual(d1, Vector3.Distance(body, grab.transform.position), 0.1f, "sweeping keeps the distance");
            homer.press = false;
            yield return null;
        }

        [UnityTest]
        public IEnumerator T2_GCyclesRayGoGoHomerFlashlight()
        {
            yield return Load();
            var go = new GameObject("Reach techniques (test)");
            var fl = go.AddComponent<FlashlightPointer>();                // added out of order on purpose
            var gogo = go.AddComponent<GoGoHand>();
            var homer = go.AddComponent<HomerHand>();
            yield return null;
            var rig = PlayerRig.Instance;
            Assert.IsNull(ReachTechnique.Current, "starts with the ray");
            ReachTechnique.Cycle(); Assert.AreEqual(gogo, ReachTechnique.Current);
            Assert.IsFalse(rig.DesktopPointer.enabled, "the mouse ray is off while a technique is on");
            ReachTechnique.Cycle(); Assert.AreEqual(homer, ReachTechnique.Current);
            Assert.IsFalse(gogo.TechniqueActive, "one at a time");
            ReachTechnique.Cycle(); Assert.AreEqual(fl, ReachTechnique.Current);
            ReachTechnique.Cycle(); Assert.IsNull(ReachTechnique.Current, "back to the ray");
            Assert.IsTrue(rig.DesktopPointer.enabled, "the mouse ray is back");
            yield return null;
        }

        [UnityTest]
        public IEnumerator T2_SteeringFollowsThePointingOrTheGaze()
        {
            yield return Load();
            var rig = PlayerRig.Instance;
            var steer = new GameObject("Steering (test)").AddComponent<ScriptedSteering>();
            var right = Vector3.Cross(Vector3.up, rig.HeadForwardFlat);
            steer.pointing = right;
            steer.Direction = SteeringTravel.DirectionSource.Pointing;
            Vector3 start = rig.FeetPosition;
            for (int i = 0; i < 10; i++) yield return null;
            var moved = rig.FeetPosition - start; moved.y = 0f;
            Assert.Greater(Vector3.Dot(moved.normalized, right), 0.95f, "pointing-directed: moves where the hand points");

            steer.Direction = SteeringTravel.DirectionSource.Gaze;
            start = rig.FeetPosition;
            for (int i = 0; i < 10; i++) yield return null;
            moved = rig.FeetPosition - start; moved.y = 0f;
            Assert.Greater(Vector3.Dot(moved.normalized, rig.HeadForwardFlat), 0.95f, "gaze-directed: moves where you look");
        }
    }
}
