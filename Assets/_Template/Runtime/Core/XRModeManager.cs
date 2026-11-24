using System;
using UnityEngine;
using WebXR;

namespace ThreeDUI
{
    /// <summary>Which way the visitor is currently experiencing the space.</summary>
    public enum ExperienceMode
    {
        Desktop,
        VR,
        AR
    }

    /// <summary>
    /// Single source of truth for "are we in XR right now?".
    /// Wraps WebXR Export so team code never has to reference the WebXR namespace directly.
    ///
    /// Usage from any script:
    ///   if (XRModeManager.IsXR) { ... }
    ///   XRModeManager.ModeChanged += mode => { ... };
    /// </summary>
    [DefaultExecutionOrder(-100)]
    public class XRModeManager : MonoBehaviour
    {
        public static XRModeManager Instance { get; private set; }

        public static ExperienceMode Mode { get; private set; } = ExperienceMode.Desktop;
        public static bool IsXR => Mode != ExperienceMode.Desktop;

        /// <summary>Raised whenever the visitor enters or leaves VR/AR.</summary>
        public static event Action<ExperienceMode> ModeChanged;

        public static bool IsVRSupported => WebXRManager.Instance != null && WebXRManager.Instance.isSupportedVR;
        public static bool IsARSupported => WebXRManager.Instance != null && WebXRManager.Instance.isSupportedAR;

        [Tooltip("Log mode changes to the console (visible in the browser dev tools).")]
        [SerializeField] private bool logModeChanges = true;

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[3DUI] More than one XRModeManager in the scene. Disabling the new one.", this);
                enabled = false;
                return;
            }
            Instance = this;
        }

        private void OnEnable()
        {
            WebXRManager.OnXRChange += HandleXRChange;
            if (WebXRManager.Instance != null)
            {
                var m = WebXRManager.Instance;
                HandleXRChange(m.XRState, m.ViewsCount, m.ViewsLeftRect, m.ViewsRightRect);
            }
        }

        private void OnDisable()
        {
            WebXRManager.OnXRChange -= HandleXRChange;
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
                Mode = ExperienceMode.Desktop;
            }
        }

        private void HandleXRChange(WebXRState state, int viewsCount, Rect leftRect, Rect rightRect)
        {
            var mode = state switch
            {
                WebXRState.VR => ExperienceMode.VR,
                WebXRState.AR => ExperienceMode.AR,
                _ => ExperienceMode.Desktop
            };
            SetMode(mode);
        }

        private void SetMode(ExperienceMode mode)
        {
            if (mode == Mode) return;
            Mode = mode;
            if (logModeChanges) Debug.Log($"[3DUI] Experience mode -> {mode}");
            ModeChanged?.Invoke(mode);
        }

        /// <summary>Enter / exit immersive VR. Must be called from a user gesture (click) in the browser.</summary>
        public static void ToggleVR()
        {
            if (WebXRManager.Instance != null) WebXRManager.Instance.ToggleVR();
        }

        public static void ToggleAR()
        {
            if (WebXRManager.Instance != null) WebXRManager.Instance.ToggleAR();
        }

        /// <summary>Vibrate an XR controller. intensity 0..1, duration in milliseconds.</summary>
        public static void Haptic(HandSide hand, float intensity, float durationMs)
        {
            if (WebXRManager.Instance == null) return;
            var webxrHand = hand switch
            {
                HandSide.Left => WebXRControllerHand.LEFT,
                HandSide.Right => WebXRControllerHand.RIGHT,
                _ => WebXRControllerHand.NONE
            };
            if (webxrHand == WebXRControllerHand.NONE) return;
            WebXRManager.Instance.HapticPulse(webxrHand, intensity, durationMs);
        }
    }
}
