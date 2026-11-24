using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace ThreeDUI
{
    /// <summary>
    /// Leaves a trail of small markers on the floor behind the visitor: "where have I been?"
    /// (the breadcrumb aid of Darken &amp; Sibert). Older crumbs fade, so the trail also shows the direction of travel.
    /// After a teleport a crumb is left at both ends, because nothing in between tells you how you got there.
    /// The crumbs are ordinary world objects, so they also show up on an OverviewMap.
    /// </summary>
    public class Breadcrumbs : MonoBehaviour
    {
        [Tooltip("Drop a crumb every this many metres walked.")]
        [SerializeField] private float spacing = 1.0f;
        [Tooltip("Oldest crumbs disappear beyond this number.")]
        [SerializeField] private int maxCrumbs = 40;
        [SerializeField] private float crumbSize = 0.18f;
        [SerializeField] private Color color = new Color(1f, 0.62f, 0.15f, 0.9f);
        [SerializeField] private bool markTeleports = true;
        [Tooltip("Desktop key that clears the trail (None = no key).")]
        [SerializeField] private KeyCode clearKey = KeyCode.None;

        private readonly Queue<Image> crumbs = new Queue<Image>();
        private Vector3 last, previousFeet;
        private bool hasLast;
        private PlayerRig rig;
        private Transform holder;

        public int Count => crumbs.Count;

        private void OnEnable()
        {
            rig = PlayerRig.Instance;
            if (rig != null) rig.Teleported += OnTeleported;
        }

        private void OnDisable()
        {
            if (rig != null) rig.Teleported -= OnTeleported;
        }

        private void Update()
        {
            if (rig == null) { rig = PlayerRig.Instance; if (rig != null) rig.Teleported += OnTeleported; else return; }
            if (clearKey != KeyCode.None && DesktopInput.GetKeyDown(clearKey)) Clear();
            var feet = rig.FeetPosition;
            if (!hasLast) { Drop(feet); }
            else if (Vector3.Distance(Flat(feet), Flat(last)) >= spacing) Drop(feet);
            previousFeet = feet;
        }

        private void OnTeleported(Vector3 destination)
        {
            if (!markTeleports) { last = destination; hasLast = true; return; }
            if (hasLast) Drop(previousFeet);                  // where the jump started
            Drop(destination);                                // and where it ended
            previousFeet = destination;
        }

        public void Clear()
        {
            while (crumbs.Count > 0) Destroy(crumbs.Dequeue().transform.parent.gameObject);
            hasLast = false;
        }

        private void Drop(Vector3 p)
        {
            if (holder == null) holder = new GameObject("Breadcrumbs (runtime)").transform;
            const float px = 1000f;
            var go = new GameObject("Crumb", typeof(RectTransform));
            go.transform.SetParent(holder, false);
            go.transform.SetPositionAndRotation(p + Vector3.up * 0.01f, Quaternion.Euler(90f, 0f, 0f));   // lying on the floor
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = Vector2.one * crumbSize * px;
            rt.localScale = Vector3.one / px;
            var img = new GameObject("Dot", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            img.transform.SetParent(go.transform, false);
            ((RectTransform)img.transform).sizeDelta = rt.sizeDelta;
            img.sprite = ProceduralSprites.Disc;
            img.color = color;
            img.raycastTarget = false;
            crumbs.Enqueue(img);
            while (crumbs.Count > maxCrumbs) Destroy(crumbs.Dequeue().transform.parent.gameObject);
            last = p; hasLast = true;
            Fade();
        }

        private void Fade()
        {
            int i = 0, n = crumbs.Count;
            foreach (var c in crumbs)
            {
                var col = color;
                col.a = color.a * Mathf.Lerp(0.25f, 1f, n <= 1 ? 1f : i / (float)(n - 1));   // oldest faintest
                c.color = col;
                i++;
            }
        }

        static Vector3 Flat(Vector3 v) => new Vector3(v.x, 0f, v.z);
    }
}
