using System.Collections;
using System.Linq;
using NUnit.Framework;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.TestTools;

namespace ThreeDUI.Tests
{
    /// <summary>
    /// Tutorial 2 + System Control lecture: the travel menu is placed in front of the head ("flatten, place, face"),
    /// sized by visual angle, lists every viewpoint, and travels there.
    /// </summary>
    public class TravelMenuTests
    {
        private IEnumerator LoadWithMenu(TravelMenu.MenuFrame frame)
        {
#if UNITY_EDITOR
            yield return UnityEditor.SceneManagement.EditorSceneManager.LoadSceneAsyncInPlayMode(
                "Assets/_Template/Examples/T2_Wayfinding.unity", new LoadSceneParameters(LoadSceneMode.Single));
#else
            yield return null;
#endif
            yield return null;
            foreach (var m in Object.FindObjectsByType<TravelMenu>(FindObjectsSortMode.None)) Object.Destroy(m.gameObject);
            var menu = new GameObject("Travel Menu (test)").AddComponent<TravelMenu>();
            menu.Frame = frame;
            for (int i = 0; i < 5; i++) yield return null;
        }

        static TravelMenu Menu => Object.FindAnyObjectByType<TravelMenu>();

        [UnityTest]
        public IEnumerator T2_TravelMenuOpensInFrontAtTheRightSize()
        {
            yield return LoadWithMenu(TravelMenu.MenuFrame.InFrontOfHead);
            var rig = PlayerRig.Instance;
            var menu = Menu;
            Assert.IsFalse(menu.IsOpen, "starts closed");
            menu.Open();
            yield return null;

            int anchors = Object.FindObjectsByType<TeleportAnchor>(FindObjectsSortMode.None).Length;
            Assert.AreEqual(anchors, menu.Items.Count, "one item per viewpoint");
            Assert.Less(menu.EstimatedChoiceSeconds, 0.2f + 0.15f * Mathf.Log(TravelMenu.RecommendedMaxItems + 1, 2f) + 1e-4f);

            var menuRoot = menu.transform.Find("Menu");
            var toMenu = menuRoot.position - rig.Head.position;
            var flat = Vector3.ProjectOnPlane(toMenu, Vector3.up);
            Assert.Greater(Vector3.Dot(flat.normalized, rig.HeadForwardFlat), 0.99f, "in front of the head (flattened forward)");
            Assert.AreEqual(1.5f, flat.magnitude, 0.02f, "at the set distance");
            Assert.Less(toMenu.y, 0f, "a little below eye level");
            Assert.Greater(Vector3.Dot(menuRoot.forward, toMenu.normalized), 0.99f, "turned to face the visitor");

            // Visual angle: the world height of an item matches s = 2·d·tan(θ/2) for its distance.
            var rt = (RectTransform)menu.Items[0].transform;
            var corners = new Vector3[4]; rt.GetWorldCorners(corners);
            float h = Vector3.Distance(corners[0], corners[1]);
            float d = Vector3.Distance(rig.Head.position, rt.position);
            float degrees = 2f * Mathf.Atan(h / (2f * d)) * Mathf.Rad2Deg;
            Assert.AreEqual(3.5f, degrees, 0.25f, "items look 3.5° tall");
        }

        [UnityTest]
        public IEnumerator T2_TravelMenuItemTakesYouThere()
        {
            yield return LoadWithMenu(TravelMenu.MenuFrame.InFrontOfHead);
            var rig = PlayerRig.Instance;
            var menu = Menu;
            menu.Open();
            yield return null;
            var item = menu.ItemFor("Exhibit");
            var anchor = Object.FindObjectsByType<TeleportAnchor>(FindObjectsSortMode.None).First(a => a.name.Contains("Exhibit"));
            item.ActivateFromCode();
            yield return null;
            var feet = rig.FeetPosition;
            Assert.Less(Vector2.Distance(new Vector2(feet.x, feet.z), new Vector2(anchor.transform.position.x, anchor.transform.position.z)), 0.05f);
            Assert.IsFalse(menu.IsOpen, "closes after travelling");
        }

        [UnityTest]
        public IEnumerator T2_FollowMenuCatchesUpWithTheHead()
        {
            yield return LoadWithMenu(TravelMenu.MenuFrame.FollowHead);
            var rig = PlayerRig.Instance;
            var menu = Menu;
            menu.Open();
            yield return null;
            var root = menu.transform.Find("Menu");
            rig.RotateAroundHead(90f);
            yield return null;
            float before = Vector3.Angle(Vector3.ProjectOnPlane(root.position - rig.Head.position, Vector3.up), rig.HeadForwardFlat);
            Assert.Greater(before, 60f, "right after the turn the menu is still behind");
            yield return new WaitForSeconds(1.5f);           // ≈ 90 frames at 60 fps: 0.9^90 of the gap is left
            float after = Vector3.Angle(Vector3.ProjectOnPlane(root.position - rig.Head.position, Vector3.up), rig.HeadForwardFlat);
            Assert.Less(after, 5f, "it has followed smoothly");
        }
    }
}
