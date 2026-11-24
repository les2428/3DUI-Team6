using TMPro;
using UnityEngine;

namespace ThreeDUI
{
    /// <summary>
    /// HOMER — Hand-centered Object Manipulation Extending Ray-casting (Bowman &amp; Hodges, I3D 1997).
    /// Ray-casting is good for selecting, a hand is good for manipulating: HOMER uses each for what it is good at.
    ///
    ///   1. Select with a ray from the hand. The virtual hand waits where the ray hits.
    ///   2. On grab the virtual hand is at the object. The scale is fixed:  s = D_object / D_hand   (both from the torso)
    ///   3. Manipulate with the hand:   virtual hand = torso + direction(torso → real hand) × (D_hand × s)
    ///      Moving the real hand 10 cm towards you brings the object 10·s cm closer; sweeping the hand sweeps the object
    ///      around you at its distance; the object turns with the hand 1:1.
    ///
    /// It touches the object with the virtual hand (near interaction), so every Interactable works with it unchanged.
    ///   Desktop: G switches techniques. Aim with the mouse; left button grabs; X / Z (or wheel) move the real hand in and out.
    ///   Headset: press A (right controller) to cycle techniques; the right controller casts the ray; trigger grabs;
    ///            right thumbstick up / down lengthens / shortens the arm (while holding: pushes / pulls the object × s).
    /// </summary>
    public class HomerHand : ReachTechnique
    {
        [Header("HOMER")]
        [Tooltip("How far the selection ray reaches (m).")]
        [SerializeField] private float rayLength = 15f;
        [Tooltip("The real hand is never treated as closer than this to the torso, so the scale stays finite (m).")]
        [SerializeField] private float minHandDistance = 0.1f;

        [Header("HOMER look")]
        [SerializeField] private float handSize = 0.07f;
        [SerializeField] private bool showScale = true;

        private Transform hand;
        private LineRenderer ray;
        private TextMeshPro label;
        private Vector3 virtualPos;
        private bool holding;
        private Quaternion grabOffset = Quaternion.identity;

        /// <summary>D_object / D_hand, fixed at the moment of the grab.</summary>
        public float Scale { get; private set; } = 1f;
        public bool HasTarget { get; private set; }
        public Vector3 VirtualHandPosition => virtualPos;

        public override string TechniqueName => "HOMER";
        public override int CycleOrder => 2;

        // ---- Pointer: the hand touches; the selection ray is our own (step 1) ----
        public override Ray GetRay() => new Ray(virtualPos, Vector3.forward);       // never used: the ray is 1 mm long
        public override Transform AttachTransform => hand;
        public override Vector3 NearPoint => virtualPos;

        private void Awake()
        {
            maxRayDistance = 0.001f;
            if (nearRadius <= 0f) nearRadius = 0.05f;
            hand = MakeSphere("HOMER hand (virtual)", handSize);
            ray = MakeLine("HOMER ray", new Color(0.4f, 0.8f, 1f, 0.8f), 0.004f);
            label = MakeLabel("HOMER scale");
        }

        protected override void OnTechniqueChanged(bool on)
        {
            if (hand == null) return;
            hand.gameObject.SetActive(on);
            label.gameObject.SetActive(on && showScale);
            if (!on) { ray.enabled = false; holding = false; }
        }

        protected override void UpdateTechnique(PlayerRig rig)
        {
            float dHand = Mathf.Max(RealDistance, minHandDistance);
            Vector3 dir = RealDistance > 1e-4f ? (RealHand - Torso) / RealDistance : Aim.direction;

            if (Selected == null)
            {
                // 1. Selection: ray-casting from the hand. The virtual hand waits just inside whatever the ray hits.
                holding = false;
                HasTarget = Physics.Raycast(Aim, out var hit, rayLength, raycastLayers, QueryTriggerInteraction.Ignore);
                virtualPos = HasTarget ? hit.point - hit.normal * 0.01f : RealHand;
                hand.gameObject.SetActive(HasTarget);
                ray.enabled = XRModeManager.IsXR;                    // desktop: the ray runs from the eye, the cursor shows it
                ray.SetPosition(0, Aim.origin);
                ray.SetPosition(1, HasTarget ? hit.point : Aim.origin + Aim.direction * rayLength);
            }
            else
            {
                if (!holding)
                {
                    // 2. The grab: the hand is at the object. Fix the scale  s = D_object / D_hand.
                    holding = true;
                    Vector3 toObject = virtualPos - Torso;
                    Scale = toObject.magnitude / dHand;
                    // Keep the object where it is: remember the angle between the torso→hand and torso→object directions.
                    grabOffset = Quaternion.FromToRotation(dir, toObject.normalized);
                }
                // 3. Manipulation: the hand's distance from the torso, scaled.
                virtualPos = Torso + grabOffset * dir * (dHand * Scale);
                hand.gameObject.SetActive(true);
                ray.enabled = false;
            }
            hand.SetPositionAndRotation(virtualPos, HandRotation);   // rotation stays 1:1

            label.gameObject.SetActive(showScale && (holding || HasTarget));
            if (label.gameObject.activeSelf)
            {
                label.text = holding ? $"HOMER × {Scale:0.0}" : $"HOMER: grab to scale × {Vector3.Distance(Torso, virtualPos) / dHand:0.0}";
                PlaceLabel(label, virtualPos, handSize * 0.5f, rig);
            }
        }
    }
}
