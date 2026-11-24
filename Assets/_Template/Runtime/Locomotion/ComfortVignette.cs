using UnityEngine;
using UnityEngine.UI;

namespace ThreeDUI
{
    /// <summary>
    /// Dynamic field-of-view restriction ("tunnelling"): while the view moves without the body moving
    /// (thumbstick walking, smooth turning, WASD), the edges of the view darken.
    /// Why: in sensory-conflict theory, sickness comes from the eyes reporting motion that the inner ear does not feel;
    /// the periphery is where most of that visual motion (vection) is picked up. Narrowing it only while moving
    /// reduces sickness while keeping the full view at rest (Fernandes &amp; Feiner 2016).
    /// Teleports and snap turns are ignored: they are instant, so there is no visual flow to hide.
    /// </summary>
    public class ComfortVignette : MonoBehaviour
    {
        [Tooltip("0 = off, 1 = strongest restriction.")]
        [Range(0f, 1f)] [SerializeField] private float strength = 0.8f;
        [Tooltip("Field of view left open at full strength (degrees).")]
        [Range(30f, 110f)] [SerializeField] private float minimumFieldOfView = 60f;
        [Tooltip("Walking speed that gives full restriction (m/s).")]
        [SerializeField] private float fullAtSpeed = 2f;
        [Tooltip("Turning speed that gives full restriction (degrees/s).")]
        [SerializeField] private float fullAtTurnSpeed = 90f;
        [Tooltip("How fast it closes and opens (per second).")]
        [SerializeField] private float closeRate = 4f, openRate = 2f;
        [SerializeField] private bool onDesktopToo = true;

        private const float Distance = 0.3f;
        private const float InnerRadius = 0.5f;           // the sprite is clear inside half its half-width
        private RectTransform ring;
        private Image image;
        private CanvasGroup group;
        private Vector3 lastPos;
        private float lastYaw, amount;
        private PlayerRig rig;

        /// <summary>Current restriction, 0 (full view) .. 1 (minimumFieldOfView).</summary>
        public float Amount => amount;
        public float Strength { get => strength; set => strength = Mathf.Clamp01(value); }

        private void Update()
        {
            if (rig == null) { rig = PlayerRig.Instance; if (rig == null) return; Build(); Sample(out lastPos, out lastYaw); }

            Sample(out var pos, out var yaw);
            float dt = Mathf.Max(Time.deltaTime, 1e-4f);
            float moved = Vector3.Distance(pos, lastPos), turned = Mathf.Abs(Mathf.DeltaAngle(lastYaw, yaw));
            // Jumps (teleport, snap turn) are not visual flow.
            float speed = moved > 0.75f ? 0f : moved / dt;
            // On desktop, looking around with the mouse is the visitor's own action, not imposed motion: only walking counts.
            float turnSpeed = turned > 20f || !XRModeManager.IsXR ? 0f : turned / dt;
            lastPos = pos; lastYaw = yaw;

            float target = (XRModeManager.IsXR || onDesktopToo)
                ? strength * Mathf.Clamp01(Mathf.Max(speed / fullAtSpeed, turnSpeed / fullAtTurnSpeed))
                : 0f;
            amount = Mathf.MoveTowards(amount, target, (target > amount ? closeRate : openRate) * dt);

            ring.gameObject.SetActive(amount > 0.01f);
            if (!ring.gameObject.activeSelf) return;
            // Aperture: at amount 1 the clear disc spans minimumFieldOfView; at 0 it is far outside the view.
            float openHalf = Mathf.Lerp(80f, minimumFieldOfView * 0.5f, amount) * Mathf.Deg2Rad;
            float clearRadius = Distance * Mathf.Tan(Mathf.Min(openHalf, 85f * Mathf.Deg2Rad));
            float size = clearRadius / (InnerRadius * 0.5f);   // the clear disc is InnerRadius of the half-width
            ring.sizeDelta = Vector2.one * size * 1000f;
            group.alpha = Mathf.SmoothStep(0f, 1f, amount * 3f);
        }

        // Body motion: the rig carries the visitor; in room-scale XR the head also moves with the real body, which
        // causes no conflict — so the rig's movement is what counts, plus turning of the rig.
        private void Sample(out Vector3 pos, out float yaw)
        {
            pos = rig.transform.position;
            yaw = rig.transform.eulerAngles.y;
        }

        private void Build()
        {
            var cam = rig.HeadCamera != null ? rig.HeadCamera.transform : rig.Head;
            var go = new GameObject("Comfort vignette", typeof(RectTransform));
            go.layer = OverviewMap.UILayer;
            go.transform.SetParent(cam, false);
            go.transform.localPosition = new Vector3(0f, 0f, Distance);
            go.transform.localRotation = Quaternion.identity;
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            canvas.sortingOrder = 100;
            group = go.AddComponent<CanvasGroup>();
            group.blocksRaycasts = false; group.interactable = false;
            ring = (RectTransform)go.transform;
            ring.localScale = Vector3.one / 1000f;
            image = new GameObject("Ring", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
            image.gameObject.layer = OverviewMap.UILayer;
            image.transform.SetParent(go.transform, false);
            var ir = (RectTransform)image.transform;
            ir.anchorMin = Vector2.zero; ir.anchorMax = Vector2.one; ir.offsetMin = ir.offsetMax = Vector2.zero;
            image.sprite = ProceduralSprites.Vignette(InnerRadius, 0.95f);
            image.raycastTarget = false;
            // A sprite only covers its square; beyond it, four black bars keep the far periphery dark.
            foreach (var (min, max) in new[] { (new Vector2(-4f, 1f), new Vector2(5f, 5f)), (new Vector2(-4f, -4f), new Vector2(5f, 0f)),
                                               (new Vector2(-4f, 0f), new Vector2(0f, 1f)), (new Vector2(1f, 0f), new Vector2(5f, 1f)) })
            {
                var bar = new GameObject("Bar", typeof(RectTransform), typeof(Image)).GetComponent<Image>();
                bar.gameObject.layer = OverviewMap.UILayer;
                bar.transform.SetParent(image.transform, false);
                var br = (RectTransform)bar.transform;
                br.anchorMin = min; br.anchorMax = max; br.offsetMin = br.offsetMax = Vector2.zero;
                bar.color = Color.black; bar.raycastTarget = false;
            }
            go.SetActive(false);
        }
    }
}
