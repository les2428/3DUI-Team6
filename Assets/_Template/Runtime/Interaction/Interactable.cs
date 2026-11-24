using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ThreeDUI
{
    /// <summary>
    /// Base class for everything the visitor can point at, touch, or select.
    /// Works identically with the mouse (desktop) and XR controllers.
    ///
    /// Lifecycle:
    ///   HoverEnter  -> pointer starts pointing at / touching the object
    ///   SelectEnter -> button pressed on the object (trigger / left click)
    ///   SelectUpdate-> every frame while the button is held
    ///   SelectExit  -> button released (or cancelled)
    ///   Activate    -> button released while still on the object ("click")
    ///   HoverExit   -> pointer leaves
    ///
    /// Three ways to add behavior (pick whatever suits your team):
    ///   1. No code: wire the UnityEvents below in the Inspector.
    ///   2. Subclass and override OnHoverEnter / OnSelectEnter / OnActivate / ...
    ///   3. Subscribe to the C# events (HoverEntered, Activated, ...) from another script.
    /// </summary>
    [DisallowMultipleComponent]
    public class Interactable : MonoBehaviour
    {
        [Header("Interactable")]
        [Tooltip("Name shown in the hover label. Leave empty to hide the label.")]
        [SerializeField] private string displayName = "";
        [Tooltip("Short hint shown under the name, e.g. \"Click to open\".")]
        [SerializeField] private string hint = "";
        [SerializeField] private bool isInteractable = true;
        [SerializeField] private SelectButton selectButton = SelectButton.Primary;
        [Tooltip("Can be pointed at with the mouse / XR ray.")]
        [SerializeField] private bool allowRay = true;
        [Tooltip("Can be touched directly with an XR hand/controller.")]
        [SerializeField] private bool allowNear = true;
        [Tooltip("Max ray distance for this object (0 = pointer default).")]
        [SerializeField] private float maxRayDistance = 0f;

        [Header("Events (no-code)")]
        public UnityEvent onHoverEnter = new UnityEvent();
        public UnityEvent onHoverExit = new UnityEvent();
        public UnityEvent onSelectEnter = new UnityEvent();
        public UnityEvent onSelectExit = new UnityEvent();
        public UnityEvent onActivate = new UnityEvent();

        // C# events for other scripts.
        public event Action<Interactable, InteractionContext> HoverEntered;
        public event Action<Interactable, InteractionContext> HoverExited;
        public event Action<Interactable, InteractionContext> SelectEntered;
        public event Action<Interactable, InteractionContext> SelectExited;
        public event Action<Interactable, InteractionContext> Activated;

        private readonly List<Pointer> hoveringPointers = new List<Pointer>();

        public string DisplayName { get => displayName; set => displayName = value; }
        public string Hint { get => hint; set => hint = value; }
        public bool IsInteractable
        {
            get => isInteractable;
            set
            {
                isInteractable = value;
                if (!value && SelectingPointer != null) SelectingPointer.ReleaseSelection(true);
            }
        }
        public SelectButton SelectButtonMode { get => selectButton; set => selectButton = value; }
        public bool AllowRay => allowRay;
        public bool AllowNear => allowNear;
        public bool IsHovered => hoveringPointers.Count > 0;
        public bool IsSelected => SelectingPointer != null;
        public Pointer SelectingPointer { get; private set; }
        public IReadOnlyList<Pointer> HoveringPointers => hoveringPointers;

        public float GetMaxRayDistance(float pointerDefault) => maxRayDistance > 0f ? maxRayDistance : pointerDefault;

        public bool Accepts(PointerButton button) =>
            selectButton == SelectButton.Either ||
            (selectButton == SelectButton.Primary && button == PointerButton.Primary) ||
            (selectButton == SelectButton.Secondary && button == PointerButton.Secondary);

        /// <summary>Override to add conditions (e.g. "only when the puzzle is unlocked").</summary>
        public virtual bool CanSelect(Pointer pointer) => isInteractable && SelectingPointer == null;

        // ---------------------------------------------------------------- Overridable hooks

        protected virtual void OnHoverEnter(InteractionContext ctx) { }
        protected virtual void OnHoverExit(InteractionContext ctx) { }
        protected virtual void OnSelectEnter(InteractionContext ctx) { }
        protected virtual void OnSelectUpdate(InteractionContext ctx) { }
        protected virtual void OnSelectExit(InteractionContext ctx, bool cancelled) { }
        protected virtual void OnActivate(InteractionContext ctx) { }

        /// <summary>Trigger this object's Activate from code (e.g. from a keyboard shortcut or another object).</summary>
        public void ActivateFromCode()
        {
            OnActivate(null);
            onActivate.Invoke();
            Activated?.Invoke(this, null);
        }

        // ---------------------------------------------------------------- Called by Pointer

        internal void InternalHoverEnter(Pointer p, InteractionContext ctx)
        {
            if (hoveringPointers.Contains(p)) return;
            hoveringPointers.Add(p);
            if (hoveringPointers.Count > 1) return; // only the first pointer triggers hover
            OnHoverEnter(ctx);
            onHoverEnter.Invoke();
            HoverEntered?.Invoke(this, ctx);
        }

        internal void InternalHoverExit(Pointer p, InteractionContext ctx)
        {
            if (!hoveringPointers.Remove(p)) return;
            if (hoveringPointers.Count > 0) return;
            OnHoverExit(ctx);
            onHoverExit.Invoke();
            HoverExited?.Invoke(this, ctx);
        }

        internal void InternalSelectEnter(Pointer p, InteractionContext ctx)
        {
            SelectingPointer = p;
            OnSelectEnter(ctx);
            onSelectEnter.Invoke();
            SelectEntered?.Invoke(this, ctx);
        }

        internal void InternalSelectUpdate(Pointer p, InteractionContext ctx)
        {
            OnSelectUpdate(ctx);
        }

        internal void InternalSelectExit(Pointer p, InteractionContext ctx, bool cancelled)
        {
            if (SelectingPointer != p) return;
            SelectingPointer = null;
            OnSelectExit(ctx, cancelled);
            onSelectExit.Invoke();
            SelectExited?.Invoke(this, ctx);
        }

        internal void InternalActivate(Pointer p, InteractionContext ctx)
        {
            if (!isInteractable) return;
            OnActivate(ctx);
            onActivate.Invoke();
            Activated?.Invoke(this, ctx);
        }

        protected virtual void OnDisable()
        {
            if (SelectingPointer != null) SelectingPointer.ReleaseSelection(true);
        }
    }
}
