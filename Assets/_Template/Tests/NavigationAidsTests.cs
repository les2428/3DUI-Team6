using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ThreeDUI.Tests
{
    /// <summary>Tutorial 2, advanced navigation: map-based travel, breadcrumbs, and the comfort vignette.</summary>
    public class NavigationAidsTests
    {
        private IEnumerator Load()
        {
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/_Template/Examples/T2_Wayfinding.unity", new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return null;
#endif
            yield return null;
            foreach (var c in Object.FindObjectsByType<OverviewMap>(FindObjectsSortMode.None)) Object.Destroy(c.gameObject);
            foreach (var c in Object.FindObjectsByType<Breadcrumbs>(FindObjectsSortMode.None)) Object.Destroy(c);
            foreach (var c in Object.FindObjectsByType<ComfortVignette>(FindObjectsSortMode.None)) Object.Destroy(c);
            yield return null;
        }

        static Vector2 UV(OverviewMap map, Vector3 world) { var v = map.MapCamera.WorldToViewportPoint(world); return new Vector2(v.x, v.y); }

        [UnityTest]
        public IEnumerator T2_MapClickTravelsToThatSpot()
        {
            yield return Load();
            var map = new GameObject("Map (test)").AddComponent<OverviewMap>();
            yield return null; yield return null;
            map.Open();
            yield return null;
            var rig = PlayerRig.Instance;
            var exhibit = Object.FindObjectsByType<TeleportAnchor>(FindObjectsSortMode.None).First(a => a.name.Contains("Exhibit")).transform.position;

            var p = map.WorldPointAt(UV(map, exhibit));
            Assert.IsTrue(p.HasValue, "the floor at the exhibit is walkable on the map");
            Assert.Less(Vector2.Distance(new Vector2(p.Value.x, p.Value.z), new Vector2(exhibit.x, exhibit.z)), 0.1f, "map point = world point");

            Assert.IsTrue(map.TravelToMapPoint(UV(map, exhibit)));
            yield return null;
            var feet = rig.FeetPosition;
            Assert.Less(Vector2.Distance(new Vector2(feet.x, feet.z), new Vector2(exhibit.x, exhibit.z)), 0.1f, "travelled there");
        }

        [UnityTest]
        public IEnumerator T2_MapRefusesWallsEvenWhenCutAway()
        {
            yield return Load();
            var map = new GameObject("Map (test)").AddComponent<OverviewMap>();
            yield return null; yield return null;
            map.Open();
            yield return null;
            // The dividing wall runs along z = 0 and is taller than the cutaway, so the map ray starts inside it.
            Assert.IsFalse(map.WorldPointAt(UV(map, new Vector3(-4f, 0f, 0f))).HasValue, "no standing inside a wall");
        }

        [UnityTest]
        public IEnumerator T2_TrackUpMapTurnsWithTheVisitor()
        {
            yield return Load();
            var map = new GameObject("Map (test)").AddComponent<OverviewMap>();
            map.MapOrientation = OverviewMap.Orientation.TrackUp;
            yield return null; yield return null;
            map.Open();
            yield return null;
            var rig = PlayerRig.Instance;
            // "Up" on a track-up map is always the way the visitor faces.
            var ahead = map.MapCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.8f, 1f)) - rig.FeetPosition; ahead.y = 0f;
            Assert.Greater(Vector3.Dot(ahead.normalized, rig.HeadForwardFlat), 0.99f);
            rig.RotateAroundHead(90f);
            yield return null; yield return null;
            ahead = map.MapCamera.ViewportToWorldPoint(new Vector3(0.5f, 0.8f, 1f)) - rig.FeetPosition; ahead.y = 0f;
            Assert.Greater(Vector3.Dot(ahead.normalized, rig.HeadForwardFlat), 0.99f, "still up after turning");
        }

        [UnityTest]
        public IEnumerator T2_BreadcrumbsMarkWalkingAndBothEndsOfATeleport()
        {
            yield return Load();
            var rig = PlayerRig.Instance;
            var crumbs = new GameObject("Crumbs (test)").AddComponent<Breadcrumbs>();
            yield return null;
            int start = crumbs.Count;
            for (int i = 0; i < 40; i++) { rig.Move(new Vector3(0f, 0f, 0.1f)); yield return null; }   // 4 m
            Assert.GreaterOrEqual(crumbs.Count - start, 3, "about one crumb per metre");
            int before = crumbs.Count;
            rig.TeleportTo(rig.FeetPosition + new Vector3(3f, 0f, 3f));
            yield return null;
            Assert.AreEqual(before + 2, crumbs.Count, "a crumb where the jump started and where it ended");
        }

        [UnityTest]
        public IEnumerator T2_VignetteClosesWhileMovingNotForTeleports()
        {
            yield return Load();
            var rig = PlayerRig.Instance;
            var v = new GameObject("Vignette (test)").AddComponent<ComfortVignette>();
            yield return null; yield return null;
            float t = 0f;
            while (t < 1f) { rig.Move(rig.HeadForwardFlat * 2f * Time.deltaTime); t += Time.deltaTime; yield return null; }
            Assert.Greater(v.Amount, 0.5f, "walking at 2 m/s narrows the view");
            t = 0f;
            while (t < 1.5f) { t += Time.deltaTime; yield return null; }
            Assert.Less(v.Amount, 0.05f, "it opens again at rest");
            rig.TeleportTo(rig.FeetPosition + new Vector3(4f, 0f, 0f));
            yield return null; yield return null;
            Assert.Less(v.Amount, 0.05f, "a teleport is not visual flow");
        }
    }
}
