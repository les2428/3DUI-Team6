using System;
using UnityEngine;
using UnityEngine.Events;

namespace ThreeDUI
{
    /// <summary>
    /// An object that can be picked up and moved (Manipulation).
    ///   Desktop: hold left mouse on it, move the mouse, scroll to push/pull.
    ///   XR ray : hold trigger/grip while pointing, thumbstick Y to push/pull.
    ///   XR near: touch it with the controller and hold trigger/grip; it follows your hand.
    ///
    /// Extend it by subclassing and overriding OnGrabbed / OnReleased / ComputeTargetPose,
    /// e.g. to constrain movement to an axis (a slider or a drawer).
    /// </summary>
    public class Grabbable : Interactable
    {
        public enum RotationMode
        {
            /// <summary>Rotates with the hand/ray (natural in XR).</summary>
            FollowPointer,
            /// <summary>Keeps its world rotation (good for desktop, furniture, pictures).</summary>
            KeepWorldRotation
        }

        [Header("Grab")]
        [SerializeField] private RotationMode xrRotation = RotationMode.FollowPointer;
        [SerializeField] private RotationMode desktopRotation = RotationMode.KeepWorldRotation;
        [Tooltip("Meters per second of push/pull while holding (scroll wheel / thumbstick).")]
        [SerializeField] private float pushPullSpeed = 3f;
        [SerializeField] private float minHoldDistance = 0.3f;
        [SerializeField] private float maxHoldDistance = 10f;
        [Tooltip("0 = snap instantly, higher = smoother but laggier following.")]
        [SerializeField, Range(0f, 30f)] private float followSmoothing = 0f;

        [Header("Physics")]
        [Tooltip("Throw the object with the hand velocity on release (requires Rigidbody).")]
        [SerializeField] private bool throwOnRelease = true;
        [SerializeField] private float throwVelocityScale = 1f;
        [Tooltip("Snap into a nearby SnapZone when released.")]
        [SerializeField] private bool canSnap = true;

        [Header("Reset")]
        [Tooltip("If the object falls below this height it returns to its start pose.")]
        [SerializeField] private float resetBelowY = -10f;

        [Header("Grab events")]
        public UnityEvent onGrabbed = new UnityEvent();
        public UnityEvent onReleased = new UnityEvent();

        public event Action<Grabbable> Grabbed;
        public event Action<Grabbable> Released;

        public bool IsGrabbed => IsSelected;
        public Rigidbody Body { get; private set; }

        private Vector3 startPosition;
        private Quaternion startRotation;
        private bool wasKinematic;
        private float holdDistance;
        private Vector3 localGrabOffset;        // hit point offset in object space
        private Quaternion rotationOffset;      // object rotation relative to the pointer
        private Vector3 nearPositionOffset;     // for near grabs, in attach-transform space
        private Quaternion nearRotationOffset;
        private Vector3 lastPosition;
        private Vector3 velocity;

        protected virtual void Reset()
        {
            SelectButtonMode = SelectButton.Either;
        }

        protected virtual void Awake()
        {
            Body = GetComponent<Rigidbody>();
            if (Body != null) wasKinematic = Body.isKinematic;
            startPosition = transform.position;
            startRotation = transform.rotation;
        }

        protected virtual void FixedUpdate()
        {
            if (!IsGrabbed && transform.position.y < resetBelowY) ResetToStart();
        }

        /// <summary>Put the object back where it was when the scene started.</summary>
        public void ResetToStart()
        {
            if (IsGrabbed) SelectingPointer.ReleaseSelection(true);
            if (Body != null && !Body.isKinematic)
            {
                Body.linearVelocity = Vector3.zero;
                Body.angularVelocity = Vector3.zero;
            }
            transform.SetPositionAndRotation(startPosition, startRotation);
        }

        /// <summary>Override where the object considers "home" (e.g. after being placed in a SnapZone).</summary>
        public void SetStartPose(Vector3 position, Quaternion rotation)
        {
            startPosition = position;
            startRotation = rotation;
        }

        // ---------------------------------------------------------------- Interactable hooks

        protected override void OnSelectEnter(InteractionContext ctx)
        {
            var pointer = ctx.Pointer;
            if (Body != null) Body.isKinematic = true;

            if (ctx.IsNear)
            {
                var attach = pointer.AttachTransform;
                nearPositionOffset = attach.InverseTransformPoint(transform.position);
                nearRotationOffset = Quaternion.Inverse(attach.rotation) * transform.rotation;
            }
            else
            {
                var ray = pointer.GetRay();
                holdDistance = Mathf.Clamp(ctx.Distance, minHoldDistance, maxHoldDistance);
                localGrabOffset = transform.InverseTransformPoint(ctx.HitPoint);
                rotationOffset = Quaternion.Inverse(Quaternion.LookRotation(ray.direction)) * transform.rotation;
            }

            lastPosition = transform.position;
            velocity = Vector3.zero;
            pointer.Haptic(0.3f, 40f);
            OnGrabbed(ctx);
            onGrabbed.Invoke();
            Grabbed?.Invoke(this);
        }

        protected override void OnSelectUpdate(InteractionContext ctx)
        {
            ComputeTargetPose(ctx, out var targetPos, out var targetRot);

            if (followSmoothing > 0f)
            {
                float t = 1f - Mathf.Exp(-followSmoothing * Time.deltaTime);
                targetPos = Vector3.Lerp(transform.position, targetPos, t);
                targetRot = Quaternion.Slerp(transform.rotation, targetRot, t);
            }

            transform.SetPositionAndRotation(targetPos, targetRot);

            if (Time.deltaTime > 0f)
            {
                var v = (transform.position - lastPosition) / Time.deltaTime;
                velocity = Vector3.Lerp(velocity, v, 0.5f);
            }
            lastPosition = transform.position;

            // Let the ray visual end on the grab point.
            if (!ctx.IsNear) ctx.HitPoint = transform.TransformPoint(localGrabOffset);
        }

        protected override void OnSelectExit(InteractionContext ctx, bool cancelled)
        {
            if (Body != null)
            {
                Body.isKinematic = wasKinematic;
                if (!wasKinematic && throwOnRelease)
                {
                    Body.linearVelocity = velocity * throwVelocityScale;
                }
            }
            OnReleased(ctx);
            onReleased.Invoke();
            Released?.Invoke(this);
            if (!cancelled && canSnap) SnapZone.TryCapture(this);
        }

        /// <summary>
        /// Where the object should be this frame while held. Override to add constraints,
        /// e.g. lock to an axis:  base.ComputeTargetPose(...); pos = Vector3.Project(pos - origin, axis) + origin;
        /// </summary>
        protected virtual void ComputeTargetPose(InteractionContext ctx, out Vector3 position, out Quaternion rotation)
        {
            var pointer = ctx.Pointer;
            if (ctx.IsNear)
            {
                var attach = pointer.AttachTransform;
                position = attach.TransformPoint(nearPositionOffset);
                rotation = attach.rotation * nearRotationOffset;
                return;
            }

            holdDistance = Mathf.Clamp(holdDistance + pointer.PushPullInput * pushPullSpeed * Time.deltaTime,
                minHoldDistance, maxHoldDistance);

            var ray = pointer.GetRay();
            var mode = XRModeManager.IsXR ? xrRotation : desktopRotation;
            rotation = mode == RotationMode.FollowPointer
                ? Quaternion.LookRotation(ray.direction) * rotationOffset
                : transform.rotation;

            // Place the object so that the original grab point sits on the ray.
            var grabPointWorld = ray.origin + ray.direction * holdDistance;
            position = grabPointWorld - rotation * Vector3.Scale(localGrabOffset, transform.lossyScale);
        }

        protected virtual void OnGrabbed(InteractionContext ctx) { }
        protected virtual void OnReleased(InteractionContext ctx) { }
    }
}
