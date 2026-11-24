using UnityEngine;

namespace ThreeDUI.ApplyTheory
{
    // Tutorial 2 · applying the theory. Same code as Tutorials/Undergrad_T2/ApplyTheory/MyHomerHand.cs.
    // HOMER (Bowman & Hodges 1997) as a Pointer: select with a ray, manipulate with the hand.
    //   1. select:      ray-casting; the virtual hand waits where the ray hits
    //   2. grab:        s = D_object / D_hand                       (fixed once)
    //   3. manipulate:  virtual hand = torso + direction * (D_hand * s)   rotation 1:1
    // Desktop: torso = the eye, the mouse gives the direction, X / Z move the simulated real hand in and out, left button grabs.
    public class MyHomerHand : Pointer
    {
        public float realDistance = 0.3f;          // D_hand: simulated real arm (m)
        public float scale = 1f;                   // s: watch it in the Inspector
        public Transform virtualHand;              // a small sphere, collider removed
        bool holding;

        // ---- what every Pointer must say (as for Go-Go: the virtual hand touches) ----
        public override HandSide Side => HandSide.Right;
        public override bool IsActive => PlayerRig.Instance != null && virtualHand != null;
        public override Ray GetRay() => new Ray(virtualHand.position, Vector3.forward);    // never used: our own ray selects
        public override bool GetButton(PointerButton b) =>                                 // confirmation
            b == PointerButton.Primary && DesktopInput.GetMouseButton(0);
        public override Vector3 NearPoint => virtualHand.position;                         // indication: where the hand is
        public override Transform AttachTransform => virtualHand;                          // grabbed objects follow

        void Awake() { maxRayDistance = 0.001f; nearRadius = 0.05f; }                     // touch only

        // HOMER replaces the mouse ray while it is on.
        void Start() => PlayerRig.Instance.DesktopPointer.enabled = false;
        protected override void OnDisable()
        {
            base.OnDisable();
            if (PlayerRig.Instance != null) PlayerRig.Instance.DesktopPointer.enabled = true;
        }

        // ---- every frame: place the virtual hand by HOMER, then run the interaction loop ----
        protected override void Update()
        {
            var rig = PlayerRig.Instance;
            if (rig == null || virtualHand == null) return;

            if (DesktopInput.GetKey(KeyCode.X)) realDistance += 0.4f * Time.deltaTime;  // real hand
            if (DesktopInput.GetKey(KeyCode.Z)) realDistance -= 0.4f * Time.deltaTime;
            realDistance = Mathf.Clamp(realDistance, 0.05f, 0.75f);

            Ray aim = rig.DesktopPointer.GetRay();
            Vector3 torso = aim.origin;                                                // desktop: the eye

            if (Selected == null)
            {
                holding = false;                                                       // 1. select by ray-casting
                if (Physics.Raycast(aim, out var hit, 15f))
                    virtualHand.position = hit.point - hit.normal * 0.01f;             //    the hand waits at the hit
            }
            else
            {
                if (!holding)                                                          // 2. the grab: fix the scale
                {
                    holding = true;
                    scale = Vector3.Distance(torso, virtualHand.position) / realDistance;
                }
                virtualHand.position = torso + aim.direction * (realDistance * scale); // 3. hand-centred, scaled
            }
            virtualHand.rotation = Quaternion.LookRotation(aim.direction);             // rotation stays 1:1

            base.Update();                                                             // hover · grab · release
        }
    }
}
