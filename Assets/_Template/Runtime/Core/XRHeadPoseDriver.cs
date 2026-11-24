using UnityEngine;
using UnityEngine.XR;

namespace ThreeDUI
{
    /// <summary>
    /// Applies the tracked headset pose to this transform while in XR.
    /// (A lightweight replacement for TrackedPoseDriver, so the template has no extra package dependency.)
    /// PlayerRig enables it only in VR/AR.
    /// </summary>
    [DefaultExecutionOrder(-80)]
    public class XRHeadPoseDriver : MonoBehaviour
    {
        private InputDevice device;

        private void OnEnable()
        {
            Application.onBeforeRender += UpdatePose;
        }

        private void OnDisable()
        {
            Application.onBeforeRender -= UpdatePose;
        }

        private void Update()
        {
            UpdatePose();
        }

        [BeforeRenderOrder(-30000)]
        private void UpdatePose()
        {
            if (!device.isValid)
            {
                device = InputDevices.GetDeviceAtXRNode(XRNode.CenterEye);
                if (!device.isValid) device = InputDevices.GetDeviceAtXRNode(XRNode.Head);
                if (!device.isValid) return;
            }

            if (device.TryGetFeatureValue(CommonUsages.centerEyePosition, out Vector3 position) ||
                device.TryGetFeatureValue(CommonUsages.devicePosition, out position))
            {
                transform.localPosition = position;
            }

            if (device.TryGetFeatureValue(CommonUsages.centerEyeRotation, out Quaternion rotation) ||
                device.TryGetFeatureValue(CommonUsages.deviceRotation, out rotation))
            {
                transform.localRotation = rotation;
            }
        }
    }
}
