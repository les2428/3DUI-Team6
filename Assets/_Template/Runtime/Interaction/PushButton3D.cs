using UnityEngine;
using UnityEngine.Events;

namespace ThreeDUI
{
    /// <summary>
    /// A physical 3D button (System Control). Click it with the mouse, point + trigger in XR,
    /// or poke it with the controller. The "cap" child moves down while pressed.
    /// Wire onPressed (or onToggledOn/Off) in the Inspector to make things happen.
    /// </summary>
    public class PushButton3D : Interactable
    {
        [Header("Button")]
        [Tooltip("Moving part of the button (optional).")]
        [SerializeField] private Transform cap;
        [SerializeField] private float pressDepth = 0.015f;
        [Tooltip("Toggle mode: each press flips between on and off.")]
        [SerializeField] private bool toggle = false;
        [SerializeField] private bool startsOn = false;

        [Header("Button events")]
        public UnityEvent onPressed = new UnityEvent();
        public UnityEvent onToggledOn = new UnityEvent();
        public UnityEvent onToggledOff = new UnityEvent();

        public bool IsOn { get; private set; }

        private Vector3 capRestLocalPos;

        protected virtual void Awake()
        {
            IsOn = startsOn;
            if (cap != null) capRestLocalPos = cap.localPosition;
        }

        protected override void OnSelectEnter(InteractionContext ctx)
        {
            SetCapPressed(true);
            ctx.Pointer.Haptic(0.5f, 30f);
        }

        protected override void OnSelectExit(InteractionContext ctx, bool cancelled)
        {
            SetCapPressed(toggle && IsOn);
        }

        protected override void OnActivate(InteractionContext ctx)
        {
            Press();
        }

        /// <summary>Press the button from code.</summary>
        public void Press()
        {
            onPressed.Invoke();
            if (!toggle) return;
            IsOn = !IsOn;
            if (IsOn) onToggledOn.Invoke(); else onToggledOff.Invoke();
            SetCapPressed(IsOn || IsSelected);
        }

        private void SetCapPressed(bool pressed)
        {
            if (cap == null) return;
            cap.localPosition = capRestLocalPos + (pressed ? Vector3.down * pressDepth / Mathf.Max(0.0001f, cap.parent.lossyScale.y) : Vector3.zero);
        }
    }
}
