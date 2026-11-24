using UnityEngine;

namespace ThreeDUI.ApplyTheory
{
    // Same code as Tutorials/Undergrad_T3/ApplyTheory/MyDrawer.cs (own namespace, so a typed copy does not clash).
    // Tutorial 3 · live coding 1: a drawer. The hand offers 6 DOF; a drawer keeps 1 (along its own forward axis), with stops.
    //   ray ∩ plane      t = n·(q − e) / n·d          desktop: the cursor drags it across a level plane
    //   projection       x = (p − o)·a                how far along the axis (dot product)
    //   stops            0 ≤ x ≤ travel               joint limits
    //   back on the rail p′ = o + x·a                 rotation never changes
    // Add it to "Drawer" inside "Bare Drawer Cabinet" (3DUI_T3_Starter). Compare with SlideAlongAxis in the Tutorial 3 package.
    public class MyDrawer : Grabbable
    {
        public float travel = 0.38f;                     // how far it opens, in metres
        Vector3 closed, axis, grabOffset;
        Quaternion startRotation;

        protected override void Awake()
        {
            base.Awake();
            closed = transform.position;
            startRotation = transform.rotation;
            axis = transform.forward;
        }

        protected override void OnGrabbed(InteractionContext ctx) =>
            grabOffset = transform.position - (ctx.IsNear ? ctx.Pointer.AttachTransform.position : ctx.HitPoint);

        protected override void ComputeTargetPose(InteractionContext ctx, out Vector3 position, out Quaternion rotation)
        {
            base.ComputeTargetPose(ctx, out position, out rotation);                  // what the hand asks for (6 DOF)
            if (!ctx.IsNear)
            {
                var ray = ctx.Pointer.GetRay();                                      // ray: e + t·d
                var plane = new Plane(Vector3.up, transform.position - grabOffset);  // a level plane through the grab point
                if (plane.Raycast(ray, out float t)) position = ray.GetPoint(t) + grabOffset;   // t = n·(q − e) / n·d
            }
            float x = Vector3.Dot(position - closed, axis);                          // x = (p − o)·a
            x = Mathf.Clamp(x, 0f, travel);                                          // 0 ≤ x ≤ travel
            position = closed + axis * x;                                            // p′ = o + x·a
            rotation = startRotation;                                                // never tilts
        }
    }
}
