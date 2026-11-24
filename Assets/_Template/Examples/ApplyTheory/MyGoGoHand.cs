using UnityEngine;

namespace ThreeDUI.ApplyTheory
{
    // Tutorial 2 · applying the theory. Same code as Tutorials/Undergrad_T2/ApplyTheory/MyGoGoHand.cs.
    // The Go-Go technique (Poupyrev et al. 1996) as a Pointer:
    //   Rv = Rr                 if Rr < D     (1:1 near the body)
    //   Rv = Rr + k (Rr - D)^2  otherwise     (the arm grows beyond D)
    // Desktop: the mouse gives the direction, X / Z stretch and pull the simulated real arm, left button grabs.
    public class MyGoGoHand : Pointer
    {
        public float D = 0.45f, k = 30f;           // the two Go-Go parameters
        public float realDistance = 0.3f;          // Rr: simulated real arm (m)
        public float virtualDistance;              // Rv: watch it in the Inspector
        public Transform virtualHand;              // a small sphere, collider removed

        // ---- 1. the mapping ----
        float GoGo(float rr) => rr < D ? rr : rr + k * (rr - D) * (rr - D);

        // ---- 2. what every Pointer must say ----
        public override HandSide Side => HandSide.Right;
        public override bool IsActive => PlayerRig.Instance != null && virtualHand != null;
        public override Ray GetRay() => new Ray(virtualHand.position, Vector3.forward);    // never used: no ray
        public override bool GetButton(PointerButton b) =>                                 // confirmation
            b == PointerButton.Primary && DesktopInput.GetMouseButton(0);
        public override Vector3 NearPoint => virtualHand.position;                         // indication: touch here
        public override Transform AttachTransform => virtualHand;                          // grabbed objects follow

        void Awake() { maxRayDistance = 0.001f; nearRadius = 0.1f; }                      // touch only

        // The Go-Go hand replaces the mouse ray while it is on.
        void Start() => PlayerRig.Instance.DesktopPointer.enabled = false;
        protected override void OnDisable()
        {
            base.OnDisable();
            if (PlayerRig.Instance != null) PlayerRig.Instance.DesktopPointer.enabled = true;
        }

        // ---- 3. every frame: place the virtual hand, then run the interaction loop ----
        protected override void Update()
        {
            var rig = PlayerRig.Instance;
            if (rig == null || virtualHand == null) return;

            if (DesktopInput.GetKey(KeyCode.X)) realDistance += 0.4f * Time.deltaTime;  // real hand
            if (DesktopInput.GetKey(KeyCode.Z)) realDistance -= 0.4f * Time.deltaTime;
            realDistance = Mathf.Clamp(realDistance, 0.05f, 0.75f);

            Ray aim = rig.DesktopPointer.GetRay();                                     // direction of the real hand
            virtualDistance = GoGo(realDistance);
            virtualHand.position = aim.origin + aim.direction * virtualDistance;       // virtual hand
            virtualHand.rotation = Quaternion.LookRotation(aim.direction);             // rotation stays 1:1

            base.Update();                                                             // hover · grab · release
        }
    }
}
