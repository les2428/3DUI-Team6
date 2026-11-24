using UnityEngine;

namespace ThreeDUI
{
    public enum HandSide
    {
        None,
        Left,
        Right,
        Mouse
    }

    /// <summary>Abstract buttons shared by mouse and XR controllers.</summary>
    public enum PointerButton
    {
        /// <summary>Left mouse / XR trigger.</summary>
        Primary,
        /// <summary>F key / XR grip.</summary>
        Secondary
    }

    /// <summary>Which button(s) an Interactable responds to.</summary>
    public enum SelectButton
    {
        Primary,
        Secondary,
        Either
    }

    /// <summary>
    /// Everything an Interactable needs to know about the interaction that is happening to it.
    /// </summary>
    public class InteractionContext
    {
        public Pointer Pointer;
        public PointerButton Button;
        /// <summary>World-space point where the ray hits (or the hand position for near interaction).</summary>
        public Vector3 HitPoint;
        public Vector3 HitNormal;
        public float Distance;
        /// <summary>True when the XR hand is touching the object (direct/near interaction) instead of pointing at it.</summary>
        public bool IsNear;

        public HandSide Hand => Pointer != null ? Pointer.Side : HandSide.None;
        public ExperienceMode Mode => XRModeManager.Mode;
    }

    /// <summary>
    /// Base class for anything that can point at and select Interactables (mouse, XR controllers,
    /// or your own custom input such as gaze). It runs the full hover / select / activate loop;
    /// subclasses only provide a ray and button states.
    /// </summary>
    [DefaultExecutionOrder(10)]
    public abstract class Pointer : MonoBehaviour
    {
        [Header("Raycast")]
        [SerializeField] protected float maxRayDistance = 15f;
        [SerializeField] protected LayerMask raycastLayers = ~0;

        [Header("Near (direct) interaction")]
        [Tooltip("Radius around the hand used to touch/grab objects directly. 0 disables near interaction.")]
        [SerializeField] protected float nearRadius = 0f;

        // ---- Implemented by subclasses ----
        public abstract HandSide Side { get; }
        /// <summary>False while the device is not tracked/usable (e.g. controller off, or wrong mode).</summary>
        public abstract bool IsActive { get; }
        public abstract Ray GetRay();
        public abstract bool GetButton(PointerButton button);
        /// <summary>True on the frame the button went down. Override when the device reports presses
        /// as events, so a press+release inside one frame (fast click) is not lost.</summary>
        public virtual bool GetButtonDown(PointerButton button) => false;
        /// <summary>Push/pull input while holding something (scroll wheel / thumbstick Y).</summary>
        public virtual float PushPullInput => 0f;
        /// <summary>Transform that grabbed objects follow for near grabs.</summary>
        public virtual Transform AttachTransform => transform;
        public virtual Vector3 NearPoint => transform.position;
        public virtual void Haptic(float intensity, float durationMs) { }

        // ---- State ----
        /// <summary>When true, the pointer stops interacting (e.g. while aiming a teleport).</summary>
        public bool Suspended { get; set; }
        public Interactable Hovered { get; private set; }
        public Interactable Selected { get; private set; }
        public bool IsSelecting => Selected != null;
        public bool HasHit { get; private set; }
        public RaycastHit LastHit => lastHit;
        public float MaxRayDistance => maxRayDistance;

        protected readonly InteractionContext context = new InteractionContext();
        private RaycastHit lastHit;
        private bool hoverIsNear;
        private readonly bool[] prevButtons = new bool[2];
        private static readonly Collider[] overlapBuffer = new Collider[16];

        protected virtual void OnDisable()
        {
            ClearAll();
            prevButtons[0] = prevButtons[1] = false;
        }

        protected virtual void Update()
        {
            if (!IsActive || Suspended)
            {
                ClearAll();
                prevButtons[0] = prevButtons[1] = false;
                return;
            }

            bool primary = GetButton(PointerButton.Primary);
            bool secondary = GetButton(PointerButton.Secondary);
            bool primaryDown = (primary && !prevButtons[0]) || GetButtonDown(PointerButton.Primary);
            bool secondaryDown = (secondary && !prevButtons[1]) || GetButtonDown(PointerButton.Secondary);
            prevButtons[0] = primary;
            prevButtons[1] = secondary;

            if (Selected != null)
            {
                UpdateSelection();
                return;
            }

            FindTarget(out var target, out bool near);
            SetHovered(target, near);

            if (Hovered != null)
            {
                if (primaryDown && Hovered.Accepts(PointerButton.Primary) && Hovered.CanSelect(this))
                    BeginSelect(PointerButton.Primary);
                else if (secondaryDown && Hovered.Accepts(PointerButton.Secondary) && Hovered.CanSelect(this))
                    BeginSelect(PointerButton.Secondary);
            }
        }

        /// <summary>Force-release whatever this pointer is holding.</summary>
        public void ReleaseSelection(bool cancelled = true)
        {
            if (Selected == null) return;
            var s = Selected;
            Selected = null;
            s.InternalSelectExit(this, context, cancelled);
        }

        private void ClearAll()
        {
            ReleaseSelection(true);
            SetHovered(null, false);
            HasHit = false;
        }

        private void BeginSelect(PointerButton button)
        {
            context.Button = button;
            Selected = Hovered;
            Selected.InternalSelectEnter(this, context);
        }

        private void UpdateSelection()
        {
            // Keep the ray hit info fresh for visuals and for "released while still pointing at it".
            FindTarget(out var target, out bool near);
            bool stillOnTarget = target == Selected;
            if (stillOnTarget) FillContextFromHit(near);

            Selected.InternalSelectUpdate(this, context);
            if (Selected == null) return; // interactable released itself

            if (!GetButton(context.Button))
            {
                var s = Selected;
                Selected = null;
                s.InternalSelectExit(this, context, false);
                if (stillOnTarget) s.InternalActivate(this, context);
            }
        }

        private void FindTarget(out Interactable target, out bool near)
        {
            target = null;
            near = false;

            if (nearRadius > 0f)
            {
                var center = NearPoint;
                int count = Physics.OverlapSphereNonAlloc(center, nearRadius, overlapBuffer, raycastLayers, QueryTriggerInteraction.Ignore);
                float best = float.MaxValue;
                for (int i = 0; i < count; i++)
                {
                    var it = overlapBuffer[i].GetComponentInParent<Interactable>();
                    if (it == null || !it.AllowNear || !it.isActiveAndEnabled || !it.IsInteractable) continue;
                    float d = (overlapBuffer[i].ClosestPoint(center) - center).sqrMagnitude;
                    if (d < best)
                    {
                        best = d;
                        target = it;
                        lastHit = default;
                        context.HitPoint = overlapBuffer[i].ClosestPoint(center);
                        context.HitNormal = (center - context.HitPoint).normalized;
                        context.Distance = Mathf.Sqrt(d);
                    }
                }
                if (target != null)
                {
                    near = true;
                    HasHit = false;
                    return;
                }
            }

            var ray = GetRay();
            HasHit = Physics.Raycast(ray, out lastHit, maxRayDistance, raycastLayers, QueryTriggerInteraction.Ignore);
            if (!HasHit) return;

            var hitInteractable = lastHit.collider.GetComponentInParent<Interactable>();
            if (hitInteractable != null && hitInteractable.AllowRay && hitInteractable.isActiveAndEnabled &&
                hitInteractable.IsInteractable && lastHit.distance <= hitInteractable.GetMaxRayDistance(maxRayDistance))
            {
                target = hitInteractable;
                FillContextFromHit(false);
            }
        }

        private void FillContextFromHit(bool near)
        {
            context.Pointer = this;
            context.IsNear = near;
            if (!near && HasHit)
            {
                context.HitPoint = lastHit.point;
                context.HitNormal = lastHit.normal;
                context.Distance = lastHit.distance;
            }
        }

        private void SetHovered(Interactable target, bool near)
        {
            context.Pointer = this;
            context.IsNear = near;
            if (target == Hovered)
            {
                hoverIsNear = near;
                return;
            }
            if (Hovered != null) Hovered.InternalHoverExit(this, context);
            Hovered = target;
            hoverIsNear = near;
            if (Hovered != null) Hovered.InternalHoverEnter(this, context);
        }

        /// <summary>True if the current hover/selection comes from touching (near) instead of the ray.</summary>
        public bool IsNearInteraction => hoverIsNear;

        /// <summary>Point where the ray visual should end.</summary>
        public Vector3 GetRayEndPoint()
        {
            var ray = GetRay();
            if (Selected != null && !context.IsNear) return context.HitPoint;
            return HasHit ? lastHit.point : ray.origin + ray.direction * maxRayDistance;
        }
    }
}
