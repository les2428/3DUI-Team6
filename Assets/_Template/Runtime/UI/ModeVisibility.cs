using UnityEngine;

namespace ThreeDUI
{
    /// <summary>
    /// Shows this object only in certain modes. Use it to give desktop and XR visitors
    /// different UI, hints, or even different interactions for the same experience.
    /// (Put it on a parent: it toggles its children, so it keeps receiving mode changes itself.)
    /// </summary>
    public class ModeVisibility : MonoBehaviour
    {
        [SerializeField] private bool showInDesktop = true;
        [SerializeField] private bool showInVR = true;
        [SerializeField] private bool showInAR = true;

        private void OnEnable()
        {
            XRModeManager.ModeChanged += Apply;
            Apply(XRModeManager.Mode);
        }

        private void OnDisable()
        {
            XRModeManager.ModeChanged -= Apply;
        }

        private void Apply(ExperienceMode mode)
        {
            bool visible = mode switch
            {
                ExperienceMode.VR => showInVR,
                ExperienceMode.AR => showInAR,
                _ => showInDesktop
            };
            foreach (Transform child in transform) child.gameObject.SetActive(visible);
        }
    }
}
