using System.Collections.Generic;
using System.Linq;
using TMPro;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;

namespace ThreeDUI
{
    /// <summary>
    /// System control for travel: a "Go to…" menu with one item per viewpoint (TeleportAnchor).
    ///   Desktop: press M        XR: press A / X on either controller (only X while a reach technique cycles with A)
    /// Built with the rules from the System Control lecture:
    ///   frame     where the menu lives: the world, in front of the head, following the head, or on the left hand
    ///   placement flatten the head's forward, place at a distance, turn to face the visitor, (follow smoothly)
    ///   size      by visual angle: item height = 2·d·tan(θ/2), so it looks the same size at any distance
    ///   breadth   Hick–Hyman: choice time grows with log2(n + 1), so keep it to one level of at most 8 items
    /// </summary>
    public class TravelMenu : MonoBehaviour
    {
        public enum MenuFrame
        {
            [Tooltip("Stays where you put it in the scene (world-fixed).")] World,
            [Tooltip("Appears in front of the visitor when opened, then stays put.")] InFrontOfHead,
            [Tooltip("Follows the head lazily (smoothed), like a HUD that does not shake.")] FollowHead,
            [Tooltip("Rides on the left controller (XR). On desktop it falls back to InFrontOfHead.")] OnLeftHand
        }

        [Header("Frame and placement")]
        [SerializeField] private MenuFrame frame = MenuFrame.InFrontOfHead;
        [Tooltip("Distance from the head when opened in front of it (m). Comfort zone from the lecture: 1.25–5 m.")]
        [SerializeField] private float distance = 1.5f;
        [Tooltip("How far below eye level the menu sits (degrees). Comfortable: 0–35.")]
        [Range(0f, 35f)] [SerializeField] private float degreesBelowEye = 12f;
        [Tooltip("FollowHead: fraction of the remaining gap closed per frame at 60 fps (α).")]
        [Range(0.01f, 1f)] [SerializeField] private float followAlpha = 0.1f;
        [Tooltip("OnLeftHand: offset in the controller's own frame (p = R·o + t).")]
        [SerializeField] private Vector3 handOffset = new Vector3(0f, 0.08f, 0.06f);

        [Header("Size (visual angle)")]
        [Tooltip("How tall one item looks, in degrees of view. Ray selection needs at least ~1°; 3–4° is comfortable to read.")]
        [Range(1f, 8f)] [SerializeField] private float itemDegrees = 3.5f;

        [Header("Items")]
        [SerializeField] private string title = "Go to…";
        [Tooltip("Leave empty to list every TeleportAnchor in the scene (sorted by name).")]
        [SerializeField] private TeleportAnchor[] destinations = new TeleportAnchor[0];
        [SerializeField] private bool closeAfterTravel = true;

        [Header("Open / close")]
        [SerializeField] private bool startOpen = false;
        [SerializeField] private KeyCode desktopKey = KeyCode.M;
        [SerializeField] private XRButton xrButton = XRButton.ButtonA;

        public UnityEvent onOpened = new UnityEvent();
        public UnityEvent onClosed = new UnityEvent();
        public UnityEvent<string> onTravelled = new UnityEvent<string>();

        public const int RecommendedMaxItems = 8;
        static readonly Color ItemColor = new Color(0.12f, 0.16f, 0.23f, 0.92f), HoverColor = new Color(0.20f, 0.45f, 0.75f, 0.95f);

        private Transform root;
        private readonly List<(string label, Interactable item, Image bg)> items = new List<(string, Interactable, Image)>();

        public bool IsOpen => root != null && root.gameObject.activeSelf;
        public MenuFrame Frame { get => frame; set => frame = value; }
        public IReadOnlyList<Interactable> Items => items.Select(i => i.item).ToList();

        /// <summary>The item with this label (e.g. "Exhibit"), or null.</summary>
        public Interactable ItemFor(string label) => items.FirstOrDefault(i => i.label == label).item;

        /// <summary>Hick–Hyman estimate of the choice time for this menu (a = 0.2 s, b = 0.15 s as in the lecture).</summary>
        public float EstimatedChoiceSeconds => 0.2f + 0.15f * Mathf.Log(items.Count + 1, 2f);

        /// <summary>Item height in metres that looks 'degrees' tall from 'metres' away: s = 2·d·tan(θ/2).</summary>
        public static float SizeForVisualAngle(float metres, float degrees) => 2f * metres * Mathf.Tan(degrees * 0.5f * Mathf.Deg2Rad);

        private void Start()
        {
            Build();
            if (frame == MenuFrame.World || startOpen) Open(); else root.gameObject.SetActive(false);
        }

        private void Update()
        {
            var rig = PlayerRig.Instance;
            if (rig == null || root == null) return;

            bool pressed = XRModeManager.IsXR
                ? (rig.LeftController != null && rig.LeftController.GetButtonDown(xrButton)) ||
                  (rig.RightController != null && !ReachTechnique.UsesXRButton(xrButton) && rig.RightController.GetButtonDown(xrButton))
                : DesktopInput.GetKeyDown(desktopKey);
            if (pressed) { if (IsOpen) Close(); else Open(); }
            if (!IsOpen) return;

            if (frame == MenuFrame.FollowHead)
            {
                // p ← p + α(p_target − p), made independent of the frame rate.
                float a = 1f - Mathf.Pow(1f - followAlpha, Time.deltaTime * 60f);
                Target(rig, out var p, out var r);
                root.position = Vector3.Lerp(root.position, p, a);
                root.rotation = Quaternion.Slerp(root.rotation, r, a);
            }
            else if (frame == MenuFrame.OnLeftHand && XRModeManager.IsXR && rig.LeftController != null)
            {
                Target(rig, out var p, out var r);
                root.SetPositionAndRotation(p, r);
                Resize(Vector3.Distance(rig.Head.position, p));
            }
        }

        public void Open()
        {
            if (root == null) Build();
            var rig = PlayerRig.Instance;
            if (rig != null && frame != MenuFrame.World)
            {
                Target(rig, out var p, out var r);
                root.SetPositionAndRotation(p, r);
            }
            // A world menu is sized for the distance it is meant to be read from; the others for where they are now.
            Resize(frame == MenuFrame.World || rig == null ? distance : Vector3.Distance(rig.Head.position, root.position));
            root.gameObject.SetActive(true);
            onOpened.Invoke();
        }

        public void Close()
        {
            if (root == null || !root.gameObject.activeSelf) return;
            if (frame == MenuFrame.World) return;            // a world menu is part of the room
            root.gameObject.SetActive(false);
            onClosed.Invoke();
        }

        public void Toggle() { if (IsOpen) Close(); else Open(); }

        public void TravelTo(TeleportAnchor anchor)
        {
            if (anchor == null) return;
            anchor.TeleportHere();
            ToneLibrary.Play(ToneLibrary.Blip(880f, 0.08f), anchor.transform.position, 0.4f);
            onTravelled.Invoke(Label(anchor));
            if (closeAfterTravel) Close();
        }

        // ------------------------------------------------------------------ placement (the lecture's "flatten, place, face")
        private void Target(PlayerRig rig, out Vector3 position, out Quaternion rotation)
        {
            var head = rig.Head;
            if (frame == MenuFrame.OnLeftHand && XRModeManager.IsXR && rig.LeftController != null)
            {
                var hand = rig.LeftController.transform;
                position = hand.TransformPoint(handOffset);                      // p = R·o + t
            }
            else
            {
                var fFlat = rig.HeadForwardFlat;                                 // forward without its up-down part
                float drop = distance * Mathf.Tan(degreesBelowEye * Mathf.Deg2Rad);
                position = head.position + fFlat * distance + Vector3.down * drop;
            }
            var toMenu = position - head.position;                               // turn to face the head
            rotation = Quaternion.LookRotation(toMenu.sqrMagnitude > 1e-6f ? toMenu.normalized : head.forward, Vector3.up);
        }

        // ------------------------------------------------------------------ size (visual angle)
        private void Resize(float metres)
        {
            // The menu is built 1 unit = the item height that looks itemDegrees tall at 1 m; scaling the whole menu by the
            // distance keeps every item at the same visual angle.
            root.localScale = Vector3.one * Mathf.Max(metres, 0.2f) / transform.lossyScale.x;
        }

        // ------------------------------------------------------------------ build
        private void Build()
        {
            if (root != null) Destroy(root.gameObject);
            items.Clear();
            root = new GameObject("Menu").transform;
            root.SetParent(transform, false);

            var list = destinations.Where(d => d != null).ToList();
            if (list.Count == 0)
                list = FindObjectsByType<TeleportAnchor>(FindObjectsSortMode.None).OrderBy(a => a.name).ToList();
            if (list.Count > RecommendedMaxItems)
                Debug.LogWarning($"[TravelMenu] {list.Count} items: choice time ≈ {0.2f + 0.15f * Mathf.Log(list.Count + 1, 2f):0.00} s (Hick–Hyman). " +
                                 $"Keep one level of at most {RecommendedMaxItems}, or list only the important places in 'Destinations'.", this);

            float h = SizeForVisualAngle(1f, itemDegrees), w = h * 6f, gap = h * 0.25f;
            int rows = list.Count + 1;
            float top = (rows - 1) * (h + gap) * 0.5f + h * 0.5f;
            Item(null, title, new Vector3(0f, top, 0f), w, h * 0.8f, header: true);
            for (int i = 0; i < list.Count; i++)
            {
                var anchor = list[i];
                Item(anchor, Label(anchor), new Vector3(0f, top - (i + 1) * (h + gap), 0f), w, h, header: false);
            }
        }

        private static string Label(TeleportAnchor a)
        {
            // "Viewpoint: Record shelf" → "Record shelf". The object name is what students rename; every anchor's
            // hover name is "Viewpoint" unless changed, so that is only the fallback.
            var s = a.name.Replace("Viewpoint", "").Replace("Teleport Anchor", "").Trim(' ', '-', ':', '·', '(', ')');
            if (s.Length > 0) return s;
            return !string.IsNullOrEmpty(a.DisplayName) && a.DisplayName != "Viewpoint" ? a.DisplayName : a.name;
        }

        private void Item(TeleportAnchor anchor, string label, Vector3 localPos, float w, float h, bool header)
        {
            const float px = 1000f;                                              // canvas pixels per unit
            var go = new GameObject(header ? "Title" : "Item " + label, typeof(RectTransform));
            go.transform.SetParent(root, false);
            go.transform.localPosition = localPos;
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = new Vector2(w, h) * px;
            rt.localScale = Vector3.one / px;

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            bg.transform.SetParent(go.transform, false);
            var brt = (RectTransform)bg.transform; brt.anchorMin = Vector2.zero; brt.anchorMax = Vector2.one; brt.offsetMin = brt.offsetMax = Vector2.zero;
            bg.color = header ? new Color(0f, 0f, 0f, 0f) : ItemColor;
            bg.raycastTarget = false;

            var text = new GameObject("Label", typeof(RectTransform), typeof(TextMeshProUGUI)).GetComponent<TextMeshProUGUI>();
            text.transform.SetParent(go.transform, false);
            var trt = (RectTransform)text.transform; trt.anchorMin = Vector2.zero; trt.anchorMax = Vector2.one;
            trt.offsetMin = new Vector2(h * px * 0.3f, 0f); trt.offsetMax = new Vector2(-h * px * 0.3f, 0f);
            text.text = label;
            text.fontSize = h * px * (header ? 0.62f : 0.5f);
            text.fontStyle = header ? FontStyles.Bold : FontStyles.Normal;
            text.color = Color.white;
            text.alignment = header ? TextAlignmentOptions.Left : TextAlignmentOptions.MidlineLeft;
            text.textWrappingMode = TextWrappingModes.NoWrap;
            text.overflowMode = TextOverflowModes.Ellipsis;
            text.raycastTarget = false;
            if (header) return;

            var col = go.AddComponent<BoxCollider>();
            col.size = new Vector3(w * px, h * px, 10f);
            var item = go.AddComponent<Interactable>();
            // No DisplayName: the item shows its own text, so the rig's hover label would only cover it.
            item.HoverEntered += (_, __) => bg.color = HoverColor;
            item.HoverExited += (_, __) => bg.color = ItemColor;
            item.Activated += (_, __) => TravelTo(anchor);
            items.Add((label, item, bg));
        }
    }
}
