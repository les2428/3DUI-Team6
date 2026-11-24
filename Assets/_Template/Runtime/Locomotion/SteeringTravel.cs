using UnityEngine;

namespace ThreeDUI
{
    /// <summary>
    /// Steering travel: continuous motion in a direction chosen by the body. The one design decision is where the
    /// direction comes from (Bowman, Koller &amp; Hodges, VRAIS 1997):
    ///   Gaze     — move where you look. Simple, but you cannot look around while travelling.
    ///   Pointing — move where the hand points. Looking and moving are independent; better for moving relative to objects.
    /// Velocity:  v = speed · throttle · d̂   (d̂ flattened onto the floor unless Fly is on)
    ///   Desktop: hold T (direction: Gaze = view, Pointing = mouse ray).   Headset: left thumbstick forward.
    /// Smooth motion can cause sickness; pair it with a ComfortVignette.
    /// </summary>
    public class SteeringTravel : MonoBehaviour
    {
        public enum DirectionSource { Gaze, Pointing }

        [SerializeField] private DirectionSource direction = DirectionSource.Pointing;
        [Tooltip("Metres per second at full throttle.")]
        [SerializeField] private float speed = 2f;
        [Tooltip("Keep the height: move only over the floor.")]
        [SerializeField] private bool fly = false;
        [SerializeField] private KeyCode desktopKey = KeyCode.T;
        [SerializeField] private KeyCode switchKey = KeyCode.Y;

        public DirectionSource Direction { get => direction; set => direction = value; }
        public float Speed { get => speed; set => speed = value; }
        public Vector3 LastVelocity { get; private set; }

        /// <summary>0..1: how hard the visitor is asking to move.</summary>
        protected virtual float Throttle
        {
            get
            {
                var rig = PlayerRig.Instance;
                if (XRModeManager.IsXR) return rig.LeftController != null ? Mathf.Max(0f, rig.LeftController.Thumbstick.y) : 0f;
                return DesktopInput.GetKey(desktopKey) ? 1f : 0f;
            }
        }

        /// <summary>The direction the hand points (desktop: the mouse ray).</summary>
        protected virtual Vector3 PointingDirection
        {
            get
            {
                var rig = PlayerRig.Instance;
                if (XRModeManager.IsXR && rig.LeftController != null) return rig.LeftController.transform.forward;
                return rig.DesktopPointer != null ? rig.DesktopPointer.GetRay().direction : rig.Head.forward;
            }
        }

        private void Update()
        {
            var rig = PlayerRig.Instance;
            if (rig == null) return;
            if (switchKey != KeyCode.None && DesktopInput.GetKeyDown(switchKey))
                direction = direction == DirectionSource.Gaze ? DirectionSource.Pointing : DirectionSource.Gaze;

            float throttle = Throttle;
            if (throttle <= 0.01f) { LastVelocity = Vector3.zero; return; }

            Vector3 d = direction == DirectionSource.Gaze ? rig.Head.forward : PointingDirection;
            if (!fly) d = Vector3.ProjectOnPlane(d, Vector3.up);
            if (d.sqrMagnitude < 1e-4f) return;
            LastVelocity = d.normalized * speed * throttle;          // v = speed · throttle · d̂
            rig.Move(LastVelocity * Time.deltaTime);
        }
    }
}
