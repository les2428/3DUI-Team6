using ThreeDUI;
using UnityEngine;

namespace TeamTemplate // TODO: rename to your team
{
    /// <summary>
    /// EXAMPLE of custom input that works on both platforms:
    ///   Desktop: press M       XR: press A/X on either controller
    /// toggles a menu object. In XR the menu appears in front of the visitor's head;
    /// on desktop it appears in front of the camera. Shows how to read raw XR buttons
    /// without touching the WebXR API.
    /// </summary>
    public class ModeAwareMenuExample : MonoBehaviour
    {
        [SerializeField] private GameObject menu;
        [SerializeField] private KeyCode desktopKey = KeyCode.M;
        [SerializeField] private float distance = 1.2f;

        private void Update()
        {
            var rig = PlayerRig.Instance;
            if (rig == null || menu == null) return;

            bool pressed = XRModeManager.IsXR
                ? (rig.LeftController != null && rig.LeftController.GetButtonDown(XRButton.ButtonA)) ||
                  (rig.RightController != null && rig.RightController.GetButtonDown(XRButton.ButtonA))
                : DesktopInput.GetKeyDown(desktopKey);

            if (!pressed) return;

            bool show = !menu.activeSelf;
            menu.SetActive(show);
            if (show)
            {
                var head = rig.Head;
                menu.transform.position = head.position + rig.HeadForwardFlat * distance + Vector3.down * 0.2f;
                menu.transform.rotation = Quaternion.LookRotation(rig.HeadForwardFlat, Vector3.up);
            }
        }
    }
}
