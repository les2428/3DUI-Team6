using System;
using UnityEngine;

namespace ThreeDUI
{
    /// <summary>
    /// The visitor's body in the scene. The rig's origin is the point on the floor under the visitor.
    ///
    /// Hierarchy (created by 3DUI > Create menu):
    ///   PlayerRig                (this, moved by locomotion)
    ///     Head / Main Camera     (desktop: fixed eye height + mouse pitch, XR: tracked headset)
    ///     LeftController         (XRControllerPointer, tracked by WebXR)
    ///     RightController        (XRControllerPointer, tracked by WebXR)
    ///     DesktopPointer         (mouse ray)
    ///
    /// Team scripts can find it with PlayerRig.Instance.
    /// </summary>
    [DefaultExecutionOrder(-90)]
    public class PlayerRig : MonoBehaviour
    {
        public static PlayerRig Instance { get; private set; }

        [Header("References")]
        [SerializeField] private Camera headCamera;
        [SerializeField] private XRHeadPoseDriver headPoseDriver;
        [SerializeField] private DesktopPointer desktopPointer;
        [SerializeField] private XRControllerPointer leftController;
        [SerializeField] private XRControllerPointer rightController;

        [Header("Desktop")]
        [Tooltip("Camera height above the rig origin in desktop mode (meters).")]
        [SerializeField] private float desktopEyeHeight = 1.6f;
        [SerializeField, Range(-89f, 0f)] private float minPitch = -80f;
        [SerializeField, Range(0f, 89f)] private float maxPitch = 80f;

        [Header("Ground")]
        [Tooltip("Keep the rig standing on colliders below it (stairs, ramps).")]
        [SerializeField] private bool snapToGround = true;
        [SerializeField] private LayerMask groundLayers = ~0;
        [SerializeField] private float maxStepHeight = 0.4f;

        /// <summary>Raised after every teleport (new feet position).</summary>
        public event Action<Vector3> Teleported;

        public Camera HeadCamera => headCamera;
        public Transform Head => headCamera != null ? headCamera.transform : transform;
        public DesktopPointer DesktopPointer => desktopPointer;
        public XRControllerPointer LeftController => leftController;
        public XRControllerPointer RightController => rightController;

        /// <summary>Head forward direction projected on the floor.</summary>
        public Vector3 HeadForwardFlat
        {
            get
            {
                var f = Vector3.ProjectOnPlane(Head.forward, Vector3.up);
                return f.sqrMagnitude < 1e-4f ? transform.forward : f.normalized;
            }
        }

        /// <summary>Point on the floor directly under the head (differs from rig origin in room-scale XR).</summary>
        public Vector3 FeetPosition
        {
            get
            {
                var p = Head.position;
                p.y = transform.position.y;
                return p;
            }
        }

        private float desktopPitch;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[3DUI] More than one PlayerRig in the scene.", this);
            }
            Instance = this;
            if (headCamera == null) headCamera = GetComponentInChildren<Camera>();
            if (headPoseDriver == null && headCamera != null) headPoseDriver = headCamera.GetComponent<XRHeadPoseDriver>();
        }

        private void OnEnable()
        {
            XRModeManager.ModeChanged += ApplyMode;
            ApplyMode(XRModeManager.Mode);
        }

        private void OnDisable()
        {
            XRModeManager.ModeChanged -= ApplyMode;
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        private void ApplyMode(ExperienceMode mode)
        {
            bool xr = mode != ExperienceMode.Desktop;
            if (headPoseDriver != null) headPoseDriver.enabled = xr;
            if (desktopPointer != null) desktopPointer.gameObject.SetActive(!xr);

            if (!xr && headCamera != null)
            {
                headCamera.transform.localPosition = new Vector3(0f, desktopEyeHeight, 0f);
                ApplyDesktopPitch();
            }
        }

        private void LateUpdate()
        {
            if (snapToGround) SnapToGround();
        }

        // ---------------------------------------------------------------- Movement API

        /// <summary>Rotate the view up/down in desktop mode (degrees).</summary>
        public void AddDesktopPitch(float deltaDegrees)
        {
            desktopPitch = Mathf.Clamp(desktopPitch + deltaDegrees, minPitch, maxPitch);
            ApplyDesktopPitch();
        }

        private void ApplyDesktopPitch()
        {
            if (headCamera == null || XRModeManager.IsXR) return;
            headCamera.transform.localRotation = Quaternion.Euler(-desktopPitch, 0f, 0f);
        }

        /// <summary>Turn the visitor around their head position (so room-scale users don't swing around).</summary>
        public void RotateAroundHead(float yawDegrees)
        {
            transform.RotateAround(Head.position, Vector3.up, yawDegrees);
        }

        /// <summary>Move by a world-space offset (y is ignored; ground snapping handles height).</summary>
        public void Move(Vector3 worldDelta)
        {
            worldDelta.y = 0f;
            transform.position += worldDelta;
        }

        /// <summary>
        /// Place the visitor's feet at a world position.
        /// If faceDirection is given, the visitor will look that way after teleporting.
        /// </summary>
        public void TeleportTo(Vector3 feetPosition, Vector3? faceDirection = null)
        {
            if (faceDirection.HasValue)
            {
                var target = Vector3.ProjectOnPlane(faceDirection.Value, Vector3.up);
                if (target.sqrMagnitude > 1e-4f)
                {
                    float yaw = Vector3.SignedAngle(HeadForwardFlat, target.normalized, Vector3.up);
                    RotateAroundHead(yaw);
                }
            }

            // Head offset from the rig origin in the floor plane (room-scale XR).
            var headOffset = FeetPosition - transform.position;
            transform.position = feetPosition - headOffset;
            Teleported?.Invoke(feetPosition);
        }

        private void SnapToGround()
        {
            var origin = FeetPosition + Vector3.up * maxStepHeight;
            if (Physics.Raycast(origin, Vector3.down, out var hit, maxStepHeight * 2f + 2f, groundLayers, QueryTriggerInteraction.Ignore))
            {
                if (hit.collider.GetComponentInParent<Interactable>() != null) return; // don't stand on props
                var p = transform.position;
                p.y = hit.point.y;
                transform.position = p;
            }
        }
    }
}
