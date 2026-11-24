using UnityEngine;

namespace ThreeDUI.ApplyTheory
{
    // Tutorial 2 · applying the theory. Same code as Tutorials/Undergrad_T2/ApplyTheory/MyFlashlight.cs.
    // Flashlight (cone) selection (Liang & Green 1994) as a Pointer:
    //   candidate  if  angle(axis, object) <= halfAngle  and  distance <= range
    //   score      =   angle + 0.3 * distance            (lowest wins: closest to the axis, nearer breaks ties)
    // Then an exact ray at the winner: the template's loop does hover, grab and push/pull as for any ray.
    // Desktop: the mouse ray is the cone's axis, left button grabs, wheel pushes / pulls.
    public class MyFlashlight : Pointer
    {
        public float halfAngle = 8f;               // the cone (degrees)
        public float range = 15f;                  // m
        public Interactable candidate;             // the winner: watch it in the Inspector
        Vector3 candidatePoint;
        Ray aim;

        // ---- what every Pointer must say ----
        public override HandSide Side => HandSide.Right;
        public override bool IsActive => PlayerRig.Instance != null;
        public override bool GetButton(PointerButton b) =>                                 // confirmation
            b == PointerButton.Primary && DesktopInput.GetMouseButton(0);
        public override float PushPullInput => DesktopInput.Scroll;                        // manipulation: as a ray
        public override Ray GetRay() =>                                                    // indication: snapped onto the winner
            Selected != null || candidate == null ? aim : new Ray(aim.origin, (candidatePoint - aim.origin).normalized);

        // The flashlight replaces the mouse ray while it is on.
        void Start() => PlayerRig.Instance.DesktopPointer.enabled = false;
        protected override void OnDisable()
        {
            base.OnDisable();
            if (PlayerRig.Instance != null) PlayerRig.Instance.DesktopPointer.enabled = true;
        }

        // ---- every frame: choose the winner in the cone, then run the interaction loop ----
        protected override void Update()
        {
            var rig = PlayerRig.Instance;
            if (rig == null) return;
            aim = rig.DesktopPointer.GetRay();                                         // the cone's axis

            if (Selected == null)
            {
                candidate = null;
                float best = float.MaxValue;
                foreach (var it in FindObjectsByType<Interactable>(FindObjectsSortMode.None))   // 1. every object…
                {
                    var col = it.GetComponentInChildren<Collider>();
                    if (col == null) continue;
                    Vector3 to = col.bounds.center - aim.origin;
                    float angle = Vector3.Angle(aim.direction, to);
                    if (angle > halfAngle || to.magnitude > range) continue;            // …inside the cone is a candidate
                    float score = angle + 0.3f * to.magnitude;                          // 2. closest to the axis wins
                    if (score < best) { best = score; candidate = it; candidatePoint = col.bounds.center; }
                }
            }

            base.Update();                                                             // 3. hover · grab · release
        }
    }
}
