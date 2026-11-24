using UnityEngine;

namespace ThreeDUI.ApplyTheory
{
    // Tutorial 3 · applying the theory. Same code as Tutorials/Undergrad_T3/ApplyTheory/MyPlaneSlide.cs.
    // A 2-DOF constraint: the object slides on a plane (a piece on a board, a puck on a table), optionally on a grid
    // (snap-dragging, Bier 1990). Theory: a constraint removes degrees of freedom from the 6 the hand offers.
    //   ray:   intersect the ray with the plane     (2 DOF in, 2 DOF out: the natural desktop mapping)
    //   hand:  keep only the in-plane part           x = (p - o)·right,  z = (p - o)·forward
    //   board: clamp x, z to the board; grid: round x, z to the nearest step; rotation: never tilts
    public class MyPlaneSlide : Grabbable
    {
        public Vector2 halfSize = new Vector2(0.4f, 0.4f);   // the board, metres either side of the start position
        public float gridStep = 0f;                          // 0 = no snapping
        Vector3 origin, grabOffset;
        Quaternion startRotation;

        protected override void Awake()
        {
            base.Awake();
            origin = transform.position;
            startRotation = transform.rotation;
        }

        protected override void OnGrabbed(InteractionContext ctx) =>
            grabOffset = transform.position - (ctx.IsNear ? ctx.Pointer.AttachTransform.position : ctx.HitPoint);

        protected override void ComputeTargetPose(InteractionContext ctx, out Vector3 position, out Quaternion rotation)
        {
            base.ComputeTargetPose(ctx, out position, out rotation);                  // what the hand asks for (6 DOF)
            Vector3 up = startRotation * Vector3.up, right = startRotation * Vector3.right, fwd = startRotation * Vector3.forward;
            if (!ctx.IsNear)
            {
                var ray = ctx.Pointer.GetRay();                                      // 1. ray ∩ plane: t = n·(q − e) / n·d
                var plane = new Plane(up, transform.position - grabOffset);
                if (plane.Raycast(ray, out float d)) position = ray.GetPoint(d) + grabOffset;
            }
            float x = Vector3.Dot(position - origin, right);                         // 2. x = (p − o)·r
            float z = Vector3.Dot(position - origin, fwd);                           //    z = (p − o)·f
            x = Mathf.Clamp(x, -halfSize.x, halfSize.x);                              // 3. limits: −a ≤ x ≤ a
            z = Mathf.Clamp(z, -halfSize.y, halfSize.y);
            if (gridStep > 0f)                                                        // 4. snap: x′ = s·round(x / s)
            {
                x = Mathf.Round(x / gridStep) * gridStep;
                z = Mathf.Round(z / gridStep) * gridStep;
            }
            position = origin + right * x + fwd * z;                                  // p′ = o + x·r + z·f
            rotation = startRotation;                                                 // never tilts
        }
    }
}
