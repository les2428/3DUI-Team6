using UnityEngine;
using WebXR;

namespace ThreeDUI
{
    /// <summary>Raw XR controller buttons, for custom interactions that need more than Primary/Secondary.</summary>
    public enum XRButton
    {
        Trigger,
        Grip,
        Thumbstick,
        Touchpad,
        ButtonA,
        ButtonB
    }

    /// <summary>
    /// One XR controller (or tracked hand). Put it on the object that has the WebXRController component.
    ///   Trigger = Primary (select / grab / click)
    ///   Grip    = Secondary
    ///   Thumbstick Y while holding = push / pull
    ///
    /// Also exposes raw buttons and thumbstick so team code can build custom XR input
    /// without touching the WebXR namespace:
    ///   PlayerRig.Instance.RightController.GetButtonDown(XRButton.ButtonA)
    /// </summary>
    [RequireComponent(typeof(WebXRController))]
    public class XRControllerPointer : Pointer
    {
        [SerializeField] private WebXRController controller;
        [Tooltip("Optional visual model shown while the controller is tracked.")]
        [SerializeField] private GameObject model;

        public override HandSide Side =>
            controller != null && controller.hand == WebXRControllerHand.LEFT ? HandSide.Left : HandSide.Right;

        public override bool IsActive => XRModeManager.IsXR && IsTracked;

        public bool IsTracked => controller != null && (controller.isControllerActive || controller.isHandActive);
        public bool IsHandTracking => controller != null && controller.isHandActive;

        /// <summary>Thumbstick (x = right, y = forward). Falls back to touchpad.</summary>
        public Vector2 Thumbstick
        {
            get
            {
                if (controller == null) return Vector2.zero;
                var v = controller.GetAxis2D(WebXRController.Axis2DTypes.Thumbstick);
                if (v.sqrMagnitude < 1e-4f) v = controller.GetAxis2D(WebXRController.Axis2DTypes.Touchpad);
                return v;
            }
        }

        public float TriggerValue => controller != null ? controller.GetAxis(WebXRController.AxisTypes.Trigger) : 0f;
        public float GripValue => controller != null ? controller.GetAxis(WebXRController.AxisTypes.Grip) : 0f;

        public bool GetButton(XRButton b) => controller != null && IsTracked &&
            (controller.GetButton(Map(b)) ||
             (b != XRButton.Trigger && b != XRButton.Grip && XRRawButtons.Pressed(Side, XRRawButtons.IndexOf(b)) == 1));   // clicks the package can miss
        public bool GetButtonDown(XRButton b) { Poll(b); return downNow[(int)b]; }
        public bool GetButtonUp(XRButton b) { Poll(b); return upNow[(int)b]; }

        // WebXR keeps "down" / "up" until the next controller frame, which can span several Unity frames (one press, two toggles).
        // So the edges are found here, once per Unity frame, from the held state.
        private readonly bool[] held = new bool[6], downNow = new bool[6], upNow = new bool[6];
        private readonly int[] polledFrame = { -1, -1, -1, -1, -1, -1 };

        private void Poll(XRButton b)
        {
            int i = (int)b;
            if (polledFrame[i] == Time.frameCount) return;
            polledFrame[i] = Time.frameCount;
            bool h = GetButton(b);
            downNow[i] = h && !held[i];
            upNow[i] = !h && held[i];
            held[i] = h;
        }

        public override float PushPullInput => IsSelecting ? Thumbstick.y : 0f;

        private void Awake()
        {
            if (controller == null) controller = GetComponent<WebXRController>();
            if (nearRadius <= 0f) nearRadius = 0.07f;
        }

        protected override void Update()
        {
            if (model != null)
            {
                bool show = XRModeManager.IsXR && IsTracked && !IsHandTracking;
                if (model.activeSelf != show) model.SetActive(show);
            }
            base.Update();
        }

        public override Ray GetRay() => new Ray(transform.position, transform.forward);

        public override bool GetButton(PointerButton button) =>
            button == PointerButton.Primary ? GetButton(XRButton.Trigger) : GetButton(XRButton.Grip);

        public override void Haptic(float intensity, float durationMs)
        {
            if (controller != null) controller.Pulse(intensity, durationMs);
        }

        private static WebXRController.ButtonTypes Map(XRButton b) => b switch
        {
            XRButton.Trigger => WebXRController.ButtonTypes.Trigger,
            XRButton.Grip => WebXRController.ButtonTypes.Grip,
            XRButton.Thumbstick => WebXRController.ButtonTypes.Thumbstick,
            XRButton.Touchpad => WebXRController.ButtonTypes.Touchpad,
            XRButton.ButtonA => WebXRController.ButtonTypes.ButtonA,
            _ => WebXRController.ButtonTypes.ButtonB
        };
    }
}
