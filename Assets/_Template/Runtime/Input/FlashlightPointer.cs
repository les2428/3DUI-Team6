using System.Collections.Generic;
using UnityEngine;

namespace ThreeDUI
{
    /// <summary>
    /// Flashlight (cone) selection (Liang &amp; Green 1994; see also Forsberg et al. 1996, aperture selection).
    /// A thin ray must hit the object exactly, which is hard for small or far objects and for a shaky hand. The flashlight
    /// selects with a cone instead: every object inside the cone is a candidate, and the one closest to the cone's axis wins
    /// (when two are about as close, the nearer one).
    ///
    ///     candidate  ⇔  angle(axis, object) ≤ half-angle   and   distance ≤ range
    ///     score      =  angle + distanceWeight · distance        (lowest score wins)
    ///
    /// Then it aims an exact ray at the winner, so the rest of the Pointer loop (hover, grab, push/pull) is ordinary ray-casting.
    ///   Desktop: G switches techniques. The circle around the cursor is the cone; left button grabs, wheel pushes / pulls.
    ///   Headset: press A (right controller) to cycle techniques; the right controller is the flashlight; thumbstick up / down sizes the cone; trigger grabs, thumbstick pushes / pulls.
    /// </summary>
    public class FlashlightPointer : ReachTechnique
    {
        [Header("Flashlight")]
        [Tooltip("Half the opening angle of the cone (degrees). Liang & Green: a few degrees is enough to absorb hand jitter.")]
        [SerializeField] private float halfAngle = 8f;
        [SerializeField] private float range = 15f;
        [Tooltip("Degrees of score added per metre of distance: breaks ties in favour of the nearer object.")]
        [SerializeField] private float distanceWeight = 0.3f;
        [Tooltip("Headset: degrees per second the right thumbstick widens or narrows the cone.")]
        [SerializeField] private float coneSpeed = 10f;

        private readonly List<(Interactable it, Collider col)> candidates = new List<(Interactable, Collider)>();
        private float refreshAt;
        private LineRenderer ring, snap;
        private Vector3 candidatePoint;
        private const int RingPoints = 40;

        /// <summary>The object the cone currently chooses (null if none).</summary>
        public Interactable Candidate { get; private set; }
        public float HalfAngle { get => halfAngle; set => halfAngle = Mathf.Clamp(value, 0.5f, 45f); }

        public override string TechniqueName => "Flashlight (cone)";
        public override int CycleOrder => 3;
        protected override bool UsesArm => false;

        /// <summary>Score of a point for a cone (lower is better); float.MaxValue if it is outside the cone.</summary>
        public static float Score(Ray axis, Vector3 point, float halfAngle, float range, float distanceWeight)
        {
            Vector3 to = point - axis.origin;
            float distance = to.magnitude;
            if (distance < 1e-4f || distance > range) return float.MaxValue;
            float angle = Vector3.Angle(axis.direction, to);
            return angle <= halfAngle ? angle + distanceWeight * distance : float.MaxValue;
        }

        // ---- Pointer: an ordinary ray, aimed at the chosen candidate ----
        public override Ray GetRay()
        {
            if (Selected != null || Candidate == null) return Aim;                          // holding: the hand's own ray
            return new Ray(Aim.origin, (candidatePoint - Aim.origin).normalized);          // snapped onto the winner
        }

        public override float PushPullInput
        {
            get
            {
                if (!IsSelecting) return 0f;
                var rig = PlayerRig.Instance;
                if (XRModeManager.IsXR) return rig != null && rig.RightController != null ? rig.RightController.Thumbstick.y : 0f;
                return Mathf.Clamp(DesktopInput.Scroll, -1f, 1f);
            }
        }

        private void Awake()
        {
            maxRayDistance = range;
            ring = MakeLine("Flashlight cone", new Color(1f, 0.9f, 0.5f, 0.7f), 0.003f, RingPoints);
            ring.loop = true;
            snap = MakeLine("Flashlight snap", new Color(1f, 0.9f, 0.5f, 0.9f), 0.003f);
        }

        protected override void OnTechniqueChanged(bool on)
        {
            if (ring == null) return;
            ring.enabled = on;
            if (!on) { snap.enabled = false; Candidate = null; }
        }

        protected override void UpdateTechnique(PlayerRig rig)
        {
            if (Selected == null)
            {
                // Headset: right thumbstick up / down = a wider / narrower cone (while holding, it pushes / pulls instead).
                float y = XRModeManager.IsXR && rig.RightController != null ? rig.RightController.Thumbstick.y : 0f;
                if (Mathf.Abs(y) > StickDeadZone) HalfAngle = halfAngle + y * coneSpeed * Time.deltaTime;
                if (Time.time >= refreshAt) Refresh();
                Candidate = null;
                float best = float.MaxValue;
                foreach (var (it, col) in candidates)
                {
                    if (it == null || col == null || !it.isActiveAndEnabled || !it.IsInteractable || !it.AllowRay) continue;
                    Vector3 p = col.bounds.center;
                    float score = Score(Aim, p, halfAngle, range, distanceWeight);
                    if (score < best) { best = score; Candidate = it; candidatePoint = p; }
                }
            }

            // The cone: a circle one metre along the axis (on the desktop it surrounds the cursor).
            Vector3 c = Aim.origin + Aim.direction * 1f;
            Vector3 u = Vector3.Cross(Aim.direction, Mathf.Abs(Aim.direction.y) < 0.95f ? Vector3.up : Vector3.right).normalized;
            Vector3 v = Vector3.Cross(Aim.direction, u);
            float r = Mathf.Tan(halfAngle * Mathf.Deg2Rad);
            for (int i = 0; i < RingPoints; i++)
            {
                float a = i * Mathf.PI * 2f / RingPoints;
                ring.SetPosition(i, c + (u * Mathf.Cos(a) + v * Mathf.Sin(a)) * r);
            }
            ring.enabled = Selected == null;

            // From the axis to the winner: shows that the cone, not the cursor, chose it.
            snap.enabled = Selected == null && Candidate != null;
            if (snap.enabled) { snap.SetPosition(0, c); snap.SetPosition(1, candidatePoint); }
        }

        private void Refresh()
        {
            refreshAt = Time.time + 0.5f;
            candidates.Clear();
            foreach (var it in FindObjectsByType<Interactable>(FindObjectsSortMode.None))
            {
                Collider col = null;
                foreach (var cc in it.GetComponentsInChildren<Collider>())
                    if (!cc.isTrigger) { col = cc; break; }
                if (col != null) candidates.Add((it, col));
            }
        }
    }
}
