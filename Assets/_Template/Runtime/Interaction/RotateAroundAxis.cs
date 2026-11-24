using System.Collections;
using UnityEngine;
using UnityEngine.Events;

namespace ThreeDUI
{
    /// <summary>
    /// Turns around one axis between two angles while the visitor drags it: a door on its hinge, a lever,
    /// a dial, a tap. Put it on the pivot (the hinge); the visible parts can be children with colliders.
    ///
    /// How the drag becomes an angle: the pointer (mouse ray or controller) is projected onto the plane the
    /// object turns in, and the angle between where you grabbed and where you are now is added.
    /// It reports 0..1 between the two limits, so the same component drives a light, a sound or a panel.
    /// </summary>
    public class RotateAroundAxis : Interactable
    {
        [Header("Rotate")]
        [Tooltip("Hinge axis, in the object's own space at start (Y = up: doors, table dials; X = levers; Z = wall dials).")]
        [SerializeField] private Vector3 axis = Vector3.up;
        [SerializeField] private float minAngle = 0f;
        [SerializeField] private float maxAngle = 90f;
        [Tooltip("Angle at start.")]
        [SerializeField] private float startAngle = 0f;
        [Tooltip("Return to the start angle when released (a spring-loaded lever).")]
        [SerializeField] private bool springBack = false;

        [Header("Rotate events")]
        [Tooltip("0 at Min Angle, 1 at Max Angle.")]
        public UnityEvent<float> onValueChanged = new UnityEvent<float>();
        public UnityEvent onReachedMax = new UnityEvent();
        public UnityEvent onReachedMin = new UnityEvent();

        public float Angle { get; private set; }
        public float Value01 => Mathf.Approximately(maxAngle, minAngle) ? 0f : Mathf.InverseLerp(minAngle, maxAngle, Angle);

        private Quaternion baseLocalRotation;
        private Vector3 grabVector, planePoint;
        private float grabAngle;
        private bool atMax, atMin;

        protected virtual void Awake()
        {
            baseLocalRotation = transform.localRotation;
            SetAngle(startAngle, false);
            atMin = Value01 < 0.02f; atMax = Value01 > 0.98f;
        }

        private Vector3 AxisWorld
        {
            get
            {
                var parentRot = transform.parent != null ? transform.parent.rotation : Quaternion.identity;
                return (parentRot * baseLocalRotation * axis).normalized;
            }
        }

        /// <summary>Where the pointer is, on the plane the object turns in.</summary>
        private Vector3 PointerPoint(InteractionContext ctx)
        {
            if (ctx.IsNear) return ctx.Pointer.AttachTransform.position;
            var ray = ctx.Pointer.GetRay();
            var plane = new Plane(AxisWorld, planePoint);
            return plane.Raycast(ray, out float d) && d < 30f ? ray.GetPoint(d) : ctx.HitPoint;
        }

        protected override void OnSelectEnter(InteractionContext ctx)
        {
            planePoint = ctx.IsNear ? ctx.Pointer.AttachTransform.position : ctx.HitPoint;
            grabVector = Vector3.ProjectOnPlane(PointerPoint(ctx) - transform.position, AxisWorld);
            grabAngle = Angle;
            ctx.Pointer.Haptic(0.3f, 30f);
        }

        protected override void OnSelectUpdate(InteractionContext ctx)
        {
            var v = Vector3.ProjectOnPlane(PointerPoint(ctx) - transform.position, AxisWorld);
            if (v.sqrMagnitude < 1e-6f || grabVector.sqrMagnitude < 1e-6f) return;
            SetAngle(grabAngle + Vector3.SignedAngle(grabVector, v, AxisWorld), true, ctx.Pointer);
        }

        protected override void OnSelectExit(InteractionContext ctx, bool cancelled)
        {
            if (springBack) StartCoroutine(SpringBack());
        }

        /// <summary>Set it from code, e.g. open the door from a button (degrees).</summary>
        public void SetAngle(float degrees) => SetAngle(degrees, true);

        /// <summary>Set it from code as 0..1 between the limits.</summary>
        public void SetValue01(float value) => SetAngle(Mathf.Lerp(minAngle, maxAngle, Mathf.Clamp01(value)), true);

        private void SetAngle(float degrees, bool notify, Pointer pointer = null)
        {
            Angle = Mathf.Clamp(degrees, Mathf.Min(minAngle, maxAngle), Mathf.Max(minAngle, maxAngle));
            transform.localRotation = baseLocalRotation * Quaternion.AngleAxis(Angle, axis);
            if (!notify) return;
            float v = Value01;
            onValueChanged.Invoke(v);
            if (v > 0.98f && !atMax) { onReachedMax.Invoke(); pointer?.Haptic(0.7f, 40f); }
            if (v < 0.02f && !atMin) { onReachedMin.Invoke(); pointer?.Haptic(0.7f, 40f); }
            atMax = v > 0.98f; atMin = v < 0.02f;
        }

        private IEnumerator SpringBack()
        {
            while (!IsSelected && Mathf.Abs(Angle - startAngle) > 0.5f)
            {
                SetAngle(Mathf.MoveTowards(Angle, startAngle, 240f * Time.deltaTime));
                yield return null;
            }
        }
    }
}
