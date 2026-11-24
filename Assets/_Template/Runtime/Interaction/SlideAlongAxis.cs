using UnityEngine;
using UnityEngine.Events;

namespace ThreeDUI
{
    /// <summary>
    /// A Grabbable that can only move along one axis, between a closed and an open position:
    /// a drawer, a slider, a sliding door. Grab it as usual; it follows the hand only along the axis.
    ///
    /// This is the Grabbable subclass from the README, finished: it overrides ComputeTargetPose to
    /// project the pose the hand asks for onto the axis, and reports how far open it is (0..1).
    /// </summary>
    public class SlideAlongAxis : Grabbable
    {
        [Header("Slide")]
        [Tooltip("Direction it opens, in the object's own space at start (Z = forward).")]
        [SerializeField] private Vector3 axis = Vector3.forward;
        [Tooltip("How far it can travel from the closed position, in metres.")]
        [SerializeField] private float travel = 0.4f;

        [Header("Slide events")]
        [Tooltip("0 = closed, 1 = fully open. Drive a light, a sound or a panel from here.")]
        public UnityEvent<float> onValueChanged = new UnityEvent<float>();
        public UnityEvent onOpened = new UnityEvent();
        public UnityEvent onClosed = new UnityEvent();

        public float Value01 { get; private set; }

        private Vector3 closedPosition;
        private Quaternion closedRotation;
        private Vector3 axisWorld;
        private bool wasOpen, wasClosed = true;

        protected override void Awake()
        {
            base.Awake();
            closedPosition = transform.position;
            closedRotation = transform.rotation;
            axisWorld = (closedRotation * axis).normalized;
        }

        private Vector3 grabPoint, positionAtGrab;
        private float wheel;

        protected override void OnGrabbed(InteractionContext ctx)
        {
            grabPoint = ctx.IsNear ? ctx.Pointer.AttachTransform.position : ctx.HitPoint;
            positionAtGrab = transform.position;
            wheel = 0f;
        }

        protected override void ComputeTargetPose(InteractionContext ctx, out Vector3 position, out Quaternion rotation)
        {
            base.ComputeTargetPose(ctx, out position, out rotation);
            if (!ctx.IsNear)
            {
                // Ray: follow the point on the axis closest to the ray, so moving the mouse towards you pulls the
                // drawer even when it points at you. The wheel / thumbstick still pushes and pulls along the ray.
                var ray = ctx.Pointer.GetRay();
                Vector3 w0 = grabPoint - ray.origin;
                float b = Vector3.Dot(axisWorld, ray.direction);
                float s = 1f - b * b > 0.02f ? (b * Vector3.Dot(ray.direction, w0) - Vector3.Dot(axisWorld, w0)) / (1f - b * b) : 0f;
                wheel += ctx.Pointer.PushPullInput * 3f * Time.deltaTime * b;
                position = positionAtGrab + axisWorld * (s + wheel);
            }
            float t = Mathf.Clamp(Vector3.Dot(position - closedPosition, axisWorld), 0f, travel);   // only the part along the axis
            position = closedPosition + axisWorld * t;
            rotation = closedRotation;                                                               // never tilts
        }

        protected override void OnSelectUpdate(InteractionContext ctx)
        {
            base.OnSelectUpdate(ctx);
            Report();
        }

        /// <summary>Open it from code (0 = closed, 1 = open), e.g. from a button's UnityEvent.</summary>
        public void SetValue01(float value)
        {
            transform.SetPositionAndRotation(closedPosition + axisWorld * Mathf.Clamp01(value) * travel, closedRotation);
            Report();
        }

        private void Report()
        {
            float v = travel > 0f ? Vector3.Dot(transform.position - closedPosition, axisWorld) / travel : 0f;
            v = Mathf.Clamp01(v);
            if (Mathf.Abs(v - Value01) < 0.001f) return;
            Value01 = v;
            onValueChanged.Invoke(v);
            bool open = v > 0.95f, closed = v < 0.05f;
            if (open && !wasOpen) { onOpened.Invoke(); SelectingPointer?.Haptic(0.6f, 40f); }
            if (closed && !wasClosed) { onClosed.Invoke(); SelectingPointer?.Haptic(0.6f, 40f); }
            wasOpen = open; wasClosed = closed;
        }
    }
}
