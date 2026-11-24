using System.Collections.Generic;
using TMPro;
using UnityEngine;

namespace ThreeDUI
{
    /// <summary>
    /// Base class for selection / manipulation techniques that replace the normal ray of one hand (Go-Go, HOMER, Flashlight, or your own).
    /// It is a Pointer, so every Interactable (Grabbable, drawers, doors, buttons) works with every technique unchanged.
    ///
    /// It provides the body model every technique is written in:
    ///     Torso        where the arm is measured from (desktop: the eye; headset: ≈ 25 cm below the head)
    ///     RealHand     the real hand (desktop: a simulated arm along the mouse ray, X / Z or wheel = length)
    ///     RealDistance |RealHand − Torso|
    ///     HandRotation the real hand's rotation
    ///     Aim          the ray the real hand points along (desktop: the mouse ray; headset: the right controller)
    /// A technique overrides UpdateTechnique() and answers the Pointer questions (GetRay, NearPoint, AttachTransform).
    ///
    /// Switching: G (headset: A on the right controller) cycles  ray → every technique in the scene (in CycleOrder) → ray. Only one is active at a time;
    /// while one is active the mouse ray (desktop) or the right controller's ray (headset) is off.
    /// Headset: while a technique is active the right thumbstick belongs to it (up / down = arm length, or the cone for the Flashlight);
    /// teleport and snap turn on that stick work again with the ray.
    /// </summary>
    public abstract class ReachTechnique : Pointer
    {
        [Header("Switching")]
        [SerializeField] private bool startActive = false;
        [Tooltip("Cycles ray → techniques → ray. None = this technique is switched only from code (TechniqueActive).")]
        [SerializeField] private KeyCode toggleKey = KeyCode.G;
        [Tooltip("Headset: this button of the right controller cycles the techniques. The thumbstick press works too where the browser reports it " +
                 "(the Vive XR Elite browser does not). While a technique uses A, the travel menu opens with X on the left controller.")]
        [SerializeField] private XRButton xrCycleButton = XRButton.ButtonA;

        [Header("Body")]
        [Tooltip("Body origin below the head (m): the shoulder/chest the arm is measured from (headset).")]
        [SerializeField] private float bodyBelowHead = 0.25f;

        [Header("Desktop (simulated arm)")]
        [SerializeField] private float simulatedArm = 0.35f;
        [SerializeField] private float maxArm = 0.75f;
        [Tooltip("Arm length change per mouse-wheel notch (m).")]
        [SerializeField] private float wheelStep = 0.04f;
        [SerializeField] private KeyCode stretchKey = KeyCode.X, pullKey = KeyCode.Z;
        [Tooltip("Arm speed while X / Z is held (m/s).")]
        [SerializeField] private float armSpeed = 0.4f;

        [Header("Look")]
        [Tooltip("Optional. Defaults to the rig's ray material.")]
        [SerializeField] protected Material handMaterial;

        // ---- the body model, updated every frame while active ----
        protected Vector3 Torso { get; private set; }
        protected Vector3 RealHand { get; private set; }
        protected Quaternion HandRotation { get; private set; }
        protected Ray Aim { get; private set; }
        public float RealDistance { get; private set; }

        public float SimulatedArm { get => simulatedArm; set => simulatedArm = Mathf.Clamp(value, 0.05f, maxArm); }

        /// <summary>Shown when the technique is switched on.</summary>
        public abstract string TechniqueName { get; }
        /// <summary>Position in the G cycle (lower first).</summary>
        public virtual int CycleOrder => 100;
        /// <summary>False for techniques that do not use the arm length (X / Z and the wheel stay free).</summary>
        protected virtual bool UsesArm => true;

        /// <summary>Place the virtual hand / choose the target. Runs every frame while active, before the Pointer loop.</summary>
        protected abstract void UpdateTechnique(PlayerRig rig);
        /// <summary>Show or hide the technique's visuals.</summary>
        protected virtual void OnTechniqueChanged(bool on) { }

        private bool active;
        private float xrArmOffset;                                 // headset: extra arm from the right thumbstick (m)
        protected const float StickDeadZone = 0.2f;

        /// <summary>True while a technique uses this hand's thumbstick (headset: the right one, for the arm or the cone),
        /// so locomotion (teleport, snap turn) leaves it alone. With the ray, the stick moves the visitor as usual.</summary>
        public static bool UsesThumbstick(HandSide side) => side == HandSide.Right && XRModeManager.IsXR && AnyActive;
        private static readonly List<ReachTechnique> all = new List<ReachTechnique>();
        private static int cycledFrame = -1;

        public bool TechniqueActive
        {
            get => active;
            set
            {
                if (value) foreach (var t in all) if (t != this && t.active) t.TechniqueActive = false;
                if (value && !active) xrArmOffset = 0f;            // every technique starts with the real arm
                active = value;
                var rig = PlayerRig.Instance;
                if (rig != null)
                {
                    // The technique replaces the ray of the hand it uses.
                    if (rig.DesktopPointer != null) rig.DesktopPointer.enabled = !AnyActive || XRModeManager.IsXR;
                    if (rig.RightController != null) rig.RightController.enabled = !AnyActive || !XRModeManager.IsXR;
                }
                OnTechniqueChanged(value);
            }
        }

        public static bool AnyActive { get { foreach (var t in all) if (t.active) return true; return false; } }
        public static ReachTechnique Current { get { foreach (var t in all) if (t.active) return t; return null; } }

        // ---- Pointer ----
        public override HandSide Side => HandSide.Right;
        public override bool IsActive => active && PlayerRig.Instance != null;
        public override bool GetButton(PointerButton button) => button == PointerButton.Primary && PrimaryPressed;
        public override void Haptic(float intensity, float durationMs) => XRModeManager.Haptic(HandSide.Right, intensity, durationMs);

        /// <summary>Where the real hand points from (desktop: the mouse ray). Virtual so tests and demos can script it.</summary>
        protected virtual Ray AimRay()
        {
            var rig = PlayerRig.Instance;
            if (XRModeManager.IsXR && rig.RightController != null)
                return new Ray(rig.RightController.transform.position, rig.RightController.transform.forward);
            return rig.DesktopPointer != null ? rig.DesktopPointer.GetRay() : new Ray(rig.Head.position, rig.Head.forward);
        }

        protected virtual bool PrimaryPressed
        {
            get
            {
                var rig = PlayerRig.Instance;
                if (rig == null) return false;
                return XRModeManager.IsXR
                    ? rig.RightController != null && rig.RightController.GetButton(XRButton.Trigger)
                    : DesktopInput.GetMouseButton(DesktopInput.LeftMouse);
            }
        }

        protected virtual void OnEnable() { if (!all.Contains(this)) all.Add(this); }

        protected override void OnDisable()
        {
            if (active) TechniqueActive = false;
            all.Remove(this);
            base.OnDisable();
        }

        protected virtual void Start() => TechniqueActive = startActive || active;

        protected override void Update()
        {
            var rig = PlayerRig.Instance;
            if (rig == null) return;
            bool xrPress = XRTogglePressed(rig, xrCycleButton);
            if (toggleKey != KeyCode.None && cycledFrame != Time.frameCount && (DesktopInput.GetKeyDown(toggleKey) || xrPress))
            {
                cycledFrame = Time.frameCount;                    // every technique sees the key; only the first one cycles
                Cycle();
            }
            if (active)
            {
                UpdateBody(rig);
                UpdateTechnique(rig);
            }
            Announcer.Tick(rig);
            base.Update();
        }

        // WebXR keeps a button's "down" flag until the next controller frame, which can span several Unity frames,
        // so one press could cycle more than once. Detect the press here instead: once per frame, edge + a short cooldown.
        private static int xrPolledFrame = -1;
        private static bool xrHeld, xrPressed;
        private static float xrNextCycle;

        private static bool XRTogglePressed(PlayerRig rig, XRButton button)
        {
            if (xrPolledFrame == Time.frameCount) return xrPressed;
            xrPolledFrame = Time.frameCount;
            var c = rig.RightController;
            bool held = XRModeManager.IsXR && c != null && (c.GetButton(button) || c.GetButton(XRButton.Thumbstick));
            xrPressed = held && !xrHeld && Time.unscaledTime >= xrNextCycle;
            if (xrPressed) xrNextCycle = Time.unscaledTime + 0.3f;
            xrHeld = held;
            return xrPressed;
        }

        /// <summary>True while a technique in the scene cycles with this button of the right controller (other features then leave it alone).</summary>
        public static bool UsesXRButton(XRButton button)
        {
            foreach (var t in all) if (t.isActiveAndEnabled && t.toggleKey != KeyCode.None && t.xrCycleButton == button) return true;
            return false;
        }

        /// <summary>ray → first technique → … → last technique → ray.</summary>
        public static void Cycle()
        {
            var list = new List<ReachTechnique>();
            foreach (var t in all) if (t.isActiveAndEnabled && t.toggleKey != KeyCode.None) list.Add(t);
            list.Sort((a, b) => a.CycleOrder.CompareTo(b.CycleOrder));
            int next = list.FindIndex(t => t.active) + 1;
            if (next < list.Count) list[next].TechniqueActive = true;
            else foreach (var t in list) t.TechniqueActive = false;
            Announcer.Show(next < list.Count ? list[next].TechniqueName : "Ray-casting");
        }

        private void UpdateBody(PlayerRig rig)
        {
            Aim = AimRay();
            if (XRModeManager.IsXR && rig.RightController != null)
            {
                Torso = rig.Head.position + Vector3.down * bodyBelowHead;
                Vector3 hand = rig.RightController.transform.position;
                if (UsesArm)
                {
                    // Right thumbstick up / down = a longer / shorter arm (like X / Z on the desktop). Locomotion leaves this stick alone meanwhile.
                    float y = rig.RightController.Thumbstick.y;
                    if (Mathf.Abs(y) > StickDeadZone) xrArmOffset = Mathf.Clamp(xrArmOffset + y * armSpeed * Time.deltaTime, -0.3f, maxArm);
                    Vector3 outward = hand - Torso;
                    float reach = outward.magnitude;
                    if (reach > 1e-4f) hand += outward / reach * Mathf.Max(xrArmOffset, 0.05f - reach);
                }
                RealHand = hand;
                HandRotation = rig.RightController.transform.rotation;
            }
            else
            {
                // Desktop: the arm reaches along the mouse ray from the eye, so the hand stays under the cursor.
                Torso = rig.Head.position;
                if (UsesArm)
                {
                    // Wheel deltas differ between browsers, so one notch per frame at most.
                    float keys = (DesktopInput.GetKey(stretchKey) ? 1f : 0f) - (DesktopInput.GetKey(pullKey) ? 1f : 0f);
                    SimulatedArm = simulatedArm + Mathf.Clamp(DesktopInput.Scroll, -1f, 1f) * wheelStep + keys * armSpeed * Time.deltaTime;
                }
                RealHand = Torso + Aim.direction.normalized * simulatedArm;
                HandRotation = Quaternion.LookRotation(Aim.direction);
            }
            RealDistance = Vector3.Distance(RealHand, Torso);
        }

        // ---- helpers for the visuals of a technique ----

        protected Material DefaultMaterial()
        {
            if (handMaterial != null) return handMaterial;
            var line = FindAnyObjectByType<PointerRayVisual>(FindObjectsInactive.Include)?.GetComponent<LineRenderer>();
            return line != null ? line.sharedMaterial : null;
        }

        protected Transform MakeSphere(string name, float size)
        {
            var go = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            go.name = name;
            Destroy(go.GetComponent<Collider>());                   // the hand must not touch itself
            go.transform.SetParent(transform, false);
            go.transform.localScale = Vector3.one * size;
            var mat = DefaultMaterial();
            if (mat != null) go.GetComponent<Renderer>().sharedMaterial = mat;
            go.SetActive(false);
            return go.transform;
        }

        protected LineRenderer MakeLine(string name, Color color, float width = 0.006f, int points = 2)
        {
            var line = new GameObject(name).AddComponent<LineRenderer>();
            line.transform.SetParent(transform, false);
            line.positionCount = points;
            line.widthMultiplier = width;
            line.sharedMaterial = DefaultMaterial();
            line.startColor = line.endColor = color;
            line.useWorldSpace = true;
            line.enabled = false;
            return line;
        }

        protected TextMeshPro MakeLabel(string name)
        {
            var label = new GameObject(name).AddComponent<TextMeshPro>();
            label.transform.SetParent(transform, false);
            label.fontSize = 0.35f;
            label.alignment = TextAlignmentOptions.Center;
            label.rectTransform.sizeDelta = new Vector2(1.2f, 0.15f);
            label.color = Color.white;
            label.gameObject.SetActive(false);
            return label;
        }

        /// <summary>Keeps a label above a world point at the same apparent size, facing the eye.</summary>
        protected static void PlaceLabel(TextMeshPro label, Vector3 above, float lift, PlayerRig rig)
        {
            float fromEye = Vector3.Distance(rig.Head.position, above);
            label.fontSize = 0.16f * fromEye;
            label.transform.position = above + Vector3.up * (lift + 0.04f * fromEye);
            label.transform.rotation = Quaternion.LookRotation(label.transform.position - rig.Head.position);
        }

        /// <summary>The name of the technique, for a moment, in front of the eye after G.</summary>
        private static class Announcer
        {
            static TextMeshPro text;
            static float until;
            static int tickedFrame = -1;

            public static void Show(string name)
            {
                if (text == null)
                {
                    text = new GameObject("Technique name").AddComponent<TextMeshPro>();
                    text.alignment = TextAlignmentOptions.Center;
                    text.rectTransform.sizeDelta = new Vector2(1.2f, 0.2f);
                    text.fontSize = 0.6f;
                    text.color = new Color(1f, 0.85f, 0.4f);
                }
                text.text = name;
                text.gameObject.SetActive(true);
                until = Time.time + 1.5f;
            }

            public static void Tick(PlayerRig rig)
            {
                if (text == null || tickedFrame == Time.frameCount) return;
                tickedFrame = Time.frameCount;
                if (Time.time > until) { text.gameObject.SetActive(false); return; }
                text.transform.position = rig.Head.position + rig.Head.forward * 1.2f + rig.Head.up * 0.3f;
                text.transform.rotation = Quaternion.LookRotation(rig.Head.forward, rig.Head.up);
            }
        }
    }
}
