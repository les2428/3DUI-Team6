using UnityEngine;

namespace ThreeDUI
{
    /// <summary>
    /// Desktop navigation:
    ///   WASD / arrow keys  move          Shift   run
    ///   Drag with left mouse on empty space, or right mouse anywhere  look around
    ///   Q / E              turn
    /// Only runs in desktop mode.
    /// </summary>
    [RequireComponent(typeof(PlayerRig))]
    [DefaultExecutionOrder(15)] // after the pointers, so a click on an object is never mistaken for a look-drag
    public class DesktopLocomotion : MonoBehaviour
    {
        [SerializeField] private float walkSpeed = 2.5f;
        [SerializeField] private float runMultiplier = 2f;
        [SerializeField] private float lookSensitivity = 2.5f;
        [SerializeField] private float keyTurnSpeed = 90f;
        [SerializeField] private bool invertY = false;
        [Tooltip("Block walking through colliders (simple capsule check).")]
        [SerializeField] private bool collideWithWalls = true;
        [SerializeField] private float bodyRadius = 0.25f;
        [SerializeField] private LayerMask wallLayers = ~0;

        private PlayerRig rig;
        private bool dragging;

        private void Awake()
        {
            rig = GetComponent<PlayerRig>();
        }

        private void Update()
        {
            if (XRModeManager.IsXR)
            {
                dragging = false;
                return;
            }

            UpdateLook();
            UpdateMove();
        }

        private void UpdateLook()
        {
            var pointer = rig.DesktopPointer;

            // Start a look-drag: left mouse on empty space (not on an interactable) or right mouse anywhere.
            if (DesktopInput.GetMouseButtonDown(DesktopInput.RightMouse) ||
                (DesktopInput.GetMouseButtonDown(DesktopInput.LeftMouse) && (pointer == null || (pointer.Hovered == null && !pointer.IsSelecting)) && !DesktopPointer.IsOverScreenUI))
            {
                dragging = true;
            }
            if (dragging && !DesktopInput.GetMouseButton(DesktopInput.LeftMouse) && !DesktopInput.GetMouseButton(DesktopInput.RightMouse))
            {
                dragging = false;
            }
            if (pointer != null) pointer.IsLookDragging = dragging;

            if (dragging)
            {
                var d = DesktopInput.MouseDelta * lookSensitivity;
                rig.RotateAroundHead(d.x);
                rig.AddDesktopPitch(invertY ? -d.y : d.y);
            }

            float turn = DesktopInput.TurnAxis;
            if (Mathf.Abs(turn) > 0.01f) rig.RotateAroundHead(turn * keyTurnSpeed * Time.deltaTime);
        }

        private void UpdateMove()
        {
            var input = DesktopInput.MoveAxis;
            if (input.sqrMagnitude < 1e-4f) return;

            float speed = walkSpeed * (DesktopInput.GetKey(KeyCode.LeftShift) || DesktopInput.GetKey(KeyCode.RightShift) ? runMultiplier : 1f);
            var forward = rig.HeadForwardFlat;
            var right = Vector3.Cross(Vector3.up, forward);
            var delta = (forward * input.y + right * input.x) * speed * Time.deltaTime;

            if (collideWithWalls) delta = CollideAndSlide(delta);
            rig.Move(delta);
        }

        private Vector3 CollideAndSlide(Vector3 delta)
        {
            var feet = rig.FeetPosition;
            var p1 = feet + Vector3.up * (0.5f + bodyRadius);
            var p2 = feet + Vector3.up * 1.6f;
            float dist = delta.magnitude;
            if (Physics.CapsuleCast(p1, p2, bodyRadius, delta / dist, out var hit, dist + 0.02f, wallLayers, QueryTriggerInteraction.Ignore))
            {
                // Slide along the wall.
                var slide = Vector3.ProjectOnPlane(delta, hit.normal);
                slide.y = 0f;
                if (Physics.CapsuleCast(p1, p2, bodyRadius, slide.normalized, slide.magnitude + 0.02f, wallLayers, QueryTriggerInteraction.Ignore))
                    return Vector3.zero;
                return slide;
            }
            return delta;
        }
    }
}
