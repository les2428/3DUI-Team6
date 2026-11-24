using TMPro;
using UnityEngine;

namespace ThreeDUI
{
    /// <summary>
    /// The Go-Go interaction technique (Poupyrev, Billinghurst, Weghorst &amp; Ichikawa, UIST 1996): a virtual hand that follows the
    /// real hand 1:1 close to the body, and grows non-linearly beyond a threshold D, so far objects can be grabbed "by hand".
    ///
    ///     Rv = Rr                      if Rr &lt; D        (isomorphic: precise near the body)
    ///     Rv = Rr + k (Rr − D)²        otherwise         (non-isomorphic: reach grows with the square)
    ///
    /// Rr = distance of the real hand from the body (desktop: from the eye), Rv = distance of the virtual hand. The direction is the real hand's.
    /// It touches objects with the virtual hand (near interaction), it never casts a ray.
    ///   Desktop: G switches techniques (ray → Go-Go → …). Aim with the mouse; X stretches and Z pulls in the
    ///            simulated arm (or the mouse wheel; 0.05–0.75 m); left button grabs.
    ///   Headset: press A (right controller) to cycle techniques; the right controller becomes the Go-Go hand, trigger grabs;
    ///            right thumbstick up / down lengthens / shortens the arm (teleport and turn come back with the ray).
    /// </summary>
    public class GoGoHand : ReachTechnique
    {
        [Header("Go-Go mapping")]
        [Tooltip("D: up to this real distance from the body the hand is 1:1 (m). Poupyrev et al.: about 2/3 of the arm.")]
        [SerializeField] private float threshold = 0.45f;
        [Tooltip("k: how fast the reach grows beyond D (1/m). With D = 0.45 and k = 30, a 0.7 m arm reaches 2.6 m.")]
        [SerializeField] private float k = 30f;

        [Header("Go-Go look")]
        [SerializeField] private float handSize = 0.09f;
        [SerializeField] private bool showDistances = true;

        private Transform hand, realHand;
        private LineRenderer stretch;
        private TextMeshPro label;
        private Vector3 virtualPos;

        public float VirtualDistance { get; private set; }
        public Vector3 VirtualHandPosition => virtualPos;
        public float Threshold { get => threshold; set => threshold = value; }
        public float K { get => k; set => k = value; }

        public override string TechniqueName => "Go-Go hand";
        public override int CycleOrder => 1;

        /// <summary>The Go-Go function: real distance from the body → virtual distance.</summary>
        public static float Map(float rr, float d, float k) => rr < d ? rr : rr + k * (rr - d) * (rr - d);

        // ---- Pointer: touch with the virtual hand, no ray ----
        public override Ray GetRay() => new Ray(virtualPos, Vector3.forward);       // never used: the ray is 1 mm long
        public override Transform AttachTransform => hand;                           // grabbed objects follow the virtual hand
        public override Vector3 NearPoint => virtualPos;                             // indication: what the virtual hand touches

        private void Awake()
        {
            maxRayDistance = 0.001f;                       // touch only
            if (nearRadius <= 0f) nearRadius = handSize * 1.2f;
            hand = MakeSphere("Go-Go hand (virtual)", handSize);
            realHand = MakeSphere("Go-Go hand (real, simulated)", handSize * 0.5f);
            stretch = MakeLine("Go-Go stretch", new Color(1f, 0.7f, 0.2f, 0.8f));
            label = MakeLabel("Go-Go distances");
        }

        protected override void OnTechniqueChanged(bool on)
        {
            if (hand == null) return;
            hand.gameObject.SetActive(on);
            realHand.gameObject.SetActive(on && !XRModeManager.IsXR);
            label.gameObject.SetActive(on && showDistances);
            if (!on) stretch.enabled = false;
        }

        protected override void UpdateTechnique(PlayerRig rig)
        {
            VirtualDistance = Map(RealDistance, threshold, k);
            var dir = RealDistance > 1e-4f ? (RealHand - Torso) / RealDistance : rig.Head.forward;
            virtualPos = Torso + dir * VirtualDistance;
            hand.SetPositionAndRotation(virtualPos, HandRotation);   // position is amplified, rotation stays 1:1

            realHand.position = RealHand;
            stretch.SetPosition(0, RealHand);
            stretch.SetPosition(1, virtualPos);
            stretch.enabled = VirtualDistance - RealDistance > 0.02f;
            label.gameObject.SetActive(showDistances);
            if (showDistances)
            {
                label.text = $"real {RealDistance:0.00} m → virtual {VirtualDistance:0.00} m";
                PlaceLabel(label, virtualPos, handSize * 0.5f, rig);
            }
        }
    }
}
