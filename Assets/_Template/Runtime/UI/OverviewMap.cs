using System.Linq;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ThreeDUI
{
    /// <summary>
    /// A live top-down map of the space: a wayfinding aid (survey knowledge, "you are here") and a travel technique
    /// (target-based travel: point at a spot on the map and you are taken there — the idea of the World-in-Miniature).
    ///   Desktop: press N        XR: press B / Y on either controller        (a World map board is always shown)
    /// Orientation (Darken &amp; Cevik 1999): NorthUp keeps the map fixed — good for learning the layout;
    /// TrackUp turns it with the visitor — good for "which way do I go from here".
    /// A cutaway height removes ceilings, so the map works indoors.
    /// </summary>
    public class OverviewMap : MonoBehaviour
    {
        public enum Orientation { NorthUp, TrackUp }

        [Header("Frame (same choices as the travel menu)")]
        [SerializeField] private TravelMenu.MenuFrame frame = TravelMenu.MenuFrame.InFrontOfHead;
        [Tooltip("Distance from the head when opened in front of it (m).")]
        [SerializeField] private float distance = 1.0f;
        [Range(0f, 45f)] [SerializeField] private float degreesBelowEye = 20f;
        [Range(0.01f, 1f)] [SerializeField] private float followAlpha = 0.1f;
        [Tooltip("OnLeftHand: offset in the controller's frame. A map in the hand is the classic World-in-Miniature.")]
        [SerializeField] private Vector3 handOffset = new Vector3(0f, 0.1f, 0.08f);
        [Tooltip("Width of the map in the world (m).")]
        [SerializeField] private float mapWidth = 0.6f;

        [Header("Map")]
        [SerializeField] private Orientation orientation = Orientation.NorthUp;
        [Tooltip("Metres shown around the visitor in TrackUp.")]
        [SerializeField] private float trackUpRadius = 6f;
        [Tooltip("Show everything below this height above the floor (cuts away ceilings).")]
        [SerializeField] private float cutawayHeight = 2.4f;
        [Tooltip("Leave at zero size to fit every walkable floor (TeleportArea).")]
        [SerializeField] private Bounds area = new Bounds(Vector3.zero, Vector3.zero);
        [SerializeField] private int resolution = 512;
        [Tooltip("Render the map every N frames only: it costs a second camera.")]
        [Range(1, 10)] [SerializeField] private int renderEveryNFrames = 3;
        [SerializeField] private Color background = new Color(0.07f, 0.09f, 0.12f, 1f);

        [Header("Travel")]
        [Tooltip("Pointing at the map and clicking takes you there (only onto walkable floors).")]
        [SerializeField] private bool clickToTravel = true;
        [SerializeField] private bool closeAfterTravel = true;

        [Header("Open / close")]
        [SerializeField] private KeyCode desktopKey = KeyCode.N;
        [SerializeField] private XRButton xrButton = XRButton.ButtonB;

        public UnityEvent<Vector3> onTravelled = new UnityEvent<Vector3>();

        public const int UILayer = 5;                       // the map's own canvas is hidden from the map camera
        private Transform root;
        private RectTransform canvasRect, marker;
        private Camera mapCam;
        private RenderTexture rt;
        private Bounds fitted;

        public Camera MapCamera => mapCam;
        public bool IsOpen => root != null && root.gameObject.activeSelf;
        public Orientation MapOrientation { get => orientation; set => orientation = value; }
        public TravelMenu.MenuFrame Frame { get => frame; set => frame = value; }

        private void Start()
        {
            Build();
            if (frame == TravelMenu.MenuFrame.World) Open(); else root.gameObject.SetActive(false);
        }

        private void OnDestroy()
        {
            if (rt != null) rt.Release();
        }

        private void Update()
        {
            var rig = PlayerRig.Instance;
            if (rig == null || root == null) return;

            bool pressed = XRModeManager.IsXR
                ? (rig.LeftController != null && rig.LeftController.GetButtonDown(xrButton)) ||
                  (rig.RightController != null && !ReachTechnique.UsesXRButton(xrButton) && rig.RightController.GetButtonDown(xrButton))
                : DesktopInput.GetKeyDown(desktopKey);
            if (pressed && frame != TravelMenu.MenuFrame.World) { if (IsOpen) Close(); else Open(); }

            mapCam.enabled = IsOpen && Time.frameCount % renderEveryNFrames == 0;
            if (!IsOpen) return;

            PlaceCamera(rig);
            UpdateMarker(rig);

            if (frame == TravelMenu.MenuFrame.FollowHead)
            {
                float a = 1f - Mathf.Pow(1f - followAlpha, Time.deltaTime * 60f);
                Target(rig, out var p, out var r);
                root.position = Vector3.Lerp(root.position, p, a);
                root.rotation = Quaternion.Slerp(root.rotation, r, a);
            }
            else if (frame == TravelMenu.MenuFrame.OnLeftHand && XRModeManager.IsXR && rig.LeftController != null)
            {
                Target(rig, out var p, out var r);
                root.SetPositionAndRotation(p, r);
            }
        }

        public void Open()
        {
            if (root == null) Build();
            var rig = PlayerRig.Instance;
            if (rig != null && frame != TravelMenu.MenuFrame.World)
            {
                Target(rig, out var p, out var r);
                root.SetPositionAndRotation(p, r);
            }
            root.gameObject.SetActive(true);
            if (rig != null) { PlaceCamera(rig); UpdateMarker(rig); }
            mapCam.enabled = true;                             // draw once straight away
        }

        public void Close()
        {
            if (root == null || frame == TravelMenu.MenuFrame.World) return;
            root.gameObject.SetActive(false);
            mapCam.enabled = false;
        }

        // ------------------------------------------------------------------ travel through the map
        /// <summary>The walkable point under map coordinates (0..1, 0..1), or null if there is no floor there.</summary>
        public Vector3? WorldPointAt(Vector2 uv)
        {
            var ray = mapCam.ViewportPointToRay(new Vector3(uv.x, uv.y, 0f));
            var hits = Physics.RaycastAll(ray, mapCam.farClipPlane, ~0, QueryTriggerInteraction.Ignore)
                              .Where(h => !h.collider.transform.IsChildOf(transform)).OrderBy(h => h.distance);
            var first = hits.FirstOrDefault();                 // the first thing seen from above decides
            if (first.collider == null) return null;
            // A viewpoint seen on the map is a target too: snap to it.
            var anchor = first.collider.GetComponentInParent<TeleportAnchor>();
            if (anchor != null) return anchor.transform.position;
            var area = first.collider.GetComponentInParent<TeleportArea>();
            if (area == null || !area.IsValidHit(first)) return null;
            // A ray that starts inside a wall (cut by the cutaway) does not see it, so also check there is room to stand.
            var rig = PlayerRig.Instance;
            var blockers = Physics.OverlapCapsule(first.point + Vector3.up * 0.4f, first.point + Vector3.up * 1.6f, 0.2f, ~0, QueryTriggerInteraction.Ignore)
                                  .Where(c => c != first.collider && !c.transform.IsChildOf(transform) && (rig == null || !c.transform.IsChildOf(rig.transform))
                                              && c.GetComponentInParent<TeleportAnchor>() == null);
            return blockers.Any() ? (Vector3?)null : first.point;
        }

        /// <summary>Travel to the place under map coordinates. Returns false (with a low tone) if it is not walkable.</summary>
        public bool TravelToMapPoint(Vector2 uv)
        {
            var rig = PlayerRig.Instance;
            var p = WorldPointAt(uv);
            if (rig == null || !p.HasValue)
            {
                ToneLibrary.Play(ToneLibrary.Blip(196f, 0.15f), root.position, 0.5f);
                return false;
            }
            rig.TeleportTo(p.Value);                           // keep the current heading: less disorientation after the jump
            ToneLibrary.Play(ToneLibrary.Blip(880f, 0.08f), root.position, 0.4f);
            onTravelled.Invoke(p.Value);
            if (closeAfterTravel) Close();
            return true;
        }

        private void OnMapActivated(Interactable _, InteractionContext ctx)
        {
            if (!clickToTravel || ctx == null) return;
            var local = canvasRect.InverseTransformPoint(ctx.HitPoint);
            var uv = new Vector2(local.x / canvasRect.sizeDelta.x + 0.5f, local.y / canvasRect.sizeDelta.y + 0.5f);
            TravelToMapPoint(uv);
        }

        // ------------------------------------------------------------------ camera and marker
        private void PlaceCamera(PlayerRig rig)
        {
            float floorY = rig.transform.position.y;
            if (orientation == Orientation.NorthUp)
            {
                var b = Fitted();
                mapCam.orthographicSize = Mathf.Max(b.extents.x, b.extents.z) * 1.05f + 0.5f;
                mapCam.transform.SetPositionAndRotation(new Vector3(b.center.x, floorY + cutawayHeight, b.center.z), Quaternion.Euler(90f, 0f, 0f));
            }
            else
            {
                var f = rig.HeadForwardFlat;
                float yaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
                var feet = rig.FeetPosition;
                mapCam.orthographicSize = trackUpRadius;
                mapCam.transform.SetPositionAndRotation(new Vector3(feet.x, floorY + cutawayHeight, feet.z), Quaternion.Euler(90f, yaw, 0f));
            }
            mapCam.farClipPlane = cutawayHeight + 30f;
        }

        private void UpdateMarker(PlayerRig rig)
        {
            var v = mapCam.WorldToViewportPoint(rig.FeetPosition);
            var size = canvasRect.sizeDelta;
            marker.anchoredPosition = new Vector2((v.x - 0.5f) * size.x, (v.y - 0.5f) * size.y);
            var f = rig.HeadForwardFlat;
            float yaw = Mathf.Atan2(f.x, f.z) * Mathf.Rad2Deg;
            marker.localRotation = Quaternion.Euler(0f, 0f, orientation == Orientation.NorthUp ? -yaw : 0f);
        }

        private Bounds Fitted()
        {
            if (area.size.sqrMagnitude > 0.01f) return area;
            if (fitted.size.sqrMagnitude > 0.01f) return fitted;
            var cols = FindObjectsByType<TeleportArea>(FindObjectsSortMode.None).Select(a => a.GetComponent<Collider>()).Where(c => c != null).ToList();
            if (cols.Count == 0) return fitted = new Bounds(Vector3.zero, new Vector3(20f, 1f, 20f));
            fitted = cols[0].bounds;
            foreach (var c in cols.Skip(1)) fitted.Encapsulate(c.bounds);
            return fitted;
        }

        // ------------------------------------------------------------------ placement (as in the travel menu)
        private void Target(PlayerRig rig, out Vector3 position, out Quaternion rotation)
        {
            var head = rig.Head;
            if (frame == TravelMenu.MenuFrame.OnLeftHand && XRModeManager.IsXR && rig.LeftController != null)
                position = rig.LeftController.transform.TransformPoint(handOffset);
            else
                position = head.position + rig.HeadForwardFlat * distance + Vector3.down * distance * Mathf.Tan(degreesBelowEye * Mathf.Deg2Rad);
            var toMap = position - head.position;
            rotation = Quaternion.LookRotation(toMap.sqrMagnitude > 1e-6f ? toMap.normalized : head.forward, Vector3.up);
        }

        // ------------------------------------------------------------------ build
        private void Build()
        {
            rt = new RenderTexture(resolution, resolution, 16) { name = "Overview map", antiAliasing = 2 };
            var camGo = new GameObject("Map Camera");
            camGo.transform.SetParent(transform, false);
            mapCam = camGo.AddComponent<Camera>();
            mapCam.orthographic = true;
            mapCam.clearFlags = CameraClearFlags.SolidColor;
            mapCam.backgroundColor = background;
            mapCam.cullingMask = ~(1 << UILayer);
            mapCam.targetTexture = rt;
            mapCam.nearClipPlane = 0.01f;
            mapCam.depth = -10f;
            mapCam.stereoTargetEye = StereoTargetEyeMask.None;
            mapCam.enabled = false;

            const float px = 1000f;
            root = new GameObject("Map", typeof(RectTransform)).transform;
            root.SetParent(transform, false);
            root.gameObject.layer = UILayer;
            var canvas = root.gameObject.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvasRect = (RectTransform)root;
            canvasRect.sizeDelta = new Vector2(mapWidth, mapWidth) * px;
            canvasRect.localScale = Vector3.one / px;

            var frameImg = Child<Image>("Frame", root, Vector2.zero, canvasRect.sizeDelta + Vector2.one * 24f);
            frameImg.color = new Color(0.9f, 0.93f, 0.97f, 1f);
            var img = Child<RawImage>("Map image", root, Vector2.zero, canvasRect.sizeDelta);
            img.texture = rt;

            marker = Child<Image>("You are here", root, Vector2.zero, Vector2.one * mapWidth * px * 0.07f).rectTransform;
            var arrow = marker.GetComponent<Image>();
            arrow.sprite = ProceduralSprites.Arrow;
            arrow.color = new Color(1f, 0.55f, 0.1f, 1f);

            var col = root.gameObject.AddComponent<BoxCollider>();
            col.size = new Vector3(canvasRect.sizeDelta.x, canvasRect.sizeDelta.y, 10f);
            var surface = root.gameObject.AddComponent<Interactable>();
            surface.Activated += OnMapActivated;
        }

        private static T Child<T>(string name, Transform parent, Vector2 pos, Vector2 size) where T : Graphic
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(T));
            go.layer = UILayer;
            go.transform.SetParent(parent, false);
            var r = (RectTransform)go.transform;
            r.anchoredPosition = pos; r.sizeDelta = size;
            var g = go.GetComponent<T>();
            g.raycastTarget = false;
            return g;
        }
    }
}
