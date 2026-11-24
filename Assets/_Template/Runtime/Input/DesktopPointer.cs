using UnityEngine;
using UnityEngine.EventSystems;

namespace ThreeDUI
{
    /// <summary>
    /// Mouse pointer for desktop mode. Casts a ray from the camera through the mouse cursor.
    ///   Left mouse  = Primary (select / grab / click)
    ///   F key       = Secondary
    ///   Scroll      = push / pull a grabbed object
    /// Disabled automatically in XR by PlayerRig.
    /// </summary>
    public class DesktopPointer : Pointer
    {
        [SerializeField] private Camera viewCamera;
        [Tooltip("When the mouse is locked (look mode), aim from the screen center instead of the cursor.")]
        [SerializeField] private bool useScreenCenterWhenLocked = true;
        [SerializeField] private KeyCode secondaryKey = KeyCode.F;

        public override HandSide Side => HandSide.Mouse;
        public override bool IsActive => !XRModeManager.IsXR && viewCamera != null;

        /// <summary>Set by DesktopLocomotion while the user drags to look around, so clicks don't select.</summary>
        public bool IsLookDragging { get; set; }

        private void Awake()
        {
            if (viewCamera == null) viewCamera = Camera.main;
        }

        public override Ray GetRay()
        {
            if (useScreenCenterWhenLocked && Cursor.lockState == CursorLockMode.Locked)
                return viewCamera.ViewportPointToRay(new Vector3(0.5f, 0.5f, 0f));
            return viewCamera.ScreenPointToRay(DesktopInput.MousePosition);
        }

        public override bool GetButton(PointerButton button)
        {
            if (IsLookDragging && !IsSelecting) return false;
            if (!IsSelecting && IsOverScreenUI) return false; // clicks on HUD buttons are not world clicks
            return button == PointerButton.Primary
                ? DesktopInput.GetMouseButton(DesktopInput.LeftMouse)
                : DesktopInput.GetKey(secondaryKey);
        }

        public override bool GetButtonDown(PointerButton button)
        {
            if (IsLookDragging || IsOverScreenUI) return false;
            return button == PointerButton.Primary
                ? DesktopInput.GetMouseButtonDown(DesktopInput.LeftMouse)
                : DesktopInput.GetKeyDown(secondaryKey);
        }

        public override float PushPullInput => DesktopInput.Scroll;

        public static bool IsOverScreenUI => EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
    }
}
