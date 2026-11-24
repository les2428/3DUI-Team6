using UnityEngine;

namespace ThreeDUI.ApplyTheory
{
    // Same code as Tutorials/Undergrad_T3/ApplyTheory/MyHinge.cs (own namespace, so a typed copy does not clash).
    // Tutorial 3 · live coding 2: a hinge (door, lever, tap). The hand offers 6 DOF; a hinge keeps 1: the angle θ around its axis.
    //   ray ∩ plane      t = n·(q − e) / n·d          the plane the handle turns in (n = the hinge axis)
    //   flatten          v = (p − c) − ((p − c)·n) n  the hand seen from the hinge, without the part along the axis
    //   angle            θ = θ₀ + ∠(v₀, v)            how far the hand has turned since the grab (signed, around n)
    //   limits           min ≤ θ ≤ max
    // Door: add it to "Hinge" inside "Bare Door" (defaults). Lever: add it to "Pivot" inside "Bare Lever", Axis (1, 0, 0), Min −40, Max 40.
    // Compare with RotateAroundAxis in the Tutorial 3 package.
    public class MyHinge : Interactable
    {
        public Vector3 axis = Vector3.up;                // the hinge, in the object's own space (door: Y, lever: X)
        public float minAngle = -100f, maxAngle = 0f;
        Quaternion baseRotation;
        Vector3 grabVector, planePoint;
        float angle, grabAngle;

        void Awake() => baseRotation = transform.localRotation;

        Vector3 HandPoint(InteractionContext ctx)
        {
            if (ctx.IsNear) return ctx.Pointer.AttachTransform.position;            // a hand: its position
            var ray = ctx.Pointer.GetRay();                                          // ray: e + t·d
            var plane = new Plane(transform.TransformDirection(axis), planePoint);   // the plane the handle turns in
            return plane.Raycast(ray, out float t) ? ray.GetPoint(t) : ctx.HitPoint;   // t = n·(q − e) / n·d
        }

        protected override void OnSelectEnter(InteractionContext ctx)
        {
            planePoint = ctx.IsNear ? ctx.Pointer.AttachTransform.position : ctx.HitPoint;
            grabVector = Vector3.ProjectOnPlane(HandPoint(ctx) - transform.position, transform.TransformDirection(axis));   // v₀
            grabAngle = angle;                                                                                          // θ₀
        }

        protected override void OnSelectUpdate(InteractionContext ctx)
        {
            Vector3 n = transform.TransformDirection(axis);                          // the hinge axis in the world
            Vector3 v = Vector3.ProjectOnPlane(HandPoint(ctx) - transform.position, n);   // v = flatten(p − c)
            angle = grabAngle + Vector3.SignedAngle(grabVector, v, n);               // θ = θ₀ + ∠(v₀, v)
            angle = Mathf.Clamp(angle, minAngle, maxAngle);                          // min ≤ θ ≤ max
            transform.localRotation = baseRotation * Quaternion.AngleAxis(angle, axis);   // turn only around the hinge
        }
    }
}
