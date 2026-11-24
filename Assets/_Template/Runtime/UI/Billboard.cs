using UnityEngine;

namespace ThreeDUI
{
    /// <summary>Keeps an object (usually a WorldPanel) facing the visitor.</summary>
    public class Billboard : MonoBehaviour
    {
        [Tooltip("Only rotate around the vertical axis (keeps text upright).")]
        [SerializeField] private bool yawOnly = true;
        [Tooltip("Smoothly follow instead of snapping (0 = instant).")]
        [SerializeField, Range(0f, 20f)] private float smoothing = 0f;

        private void LateUpdate()
        {
            var rig = PlayerRig.Instance;
            var head = rig != null ? rig.Head : (Camera.main != null ? Camera.main.transform : null);
            if (head == null) return;

            // UI canvases face -forward, so look *away* from the viewer.
            var dir = transform.position - head.position;
            if (yawOnly) dir.y = 0f;
            if (dir.sqrMagnitude < 1e-4f) return;
            var target = Quaternion.LookRotation(dir.normalized, Vector3.up);
            transform.rotation = smoothing > 0f
                ? Quaternion.Slerp(transform.rotation, target, 1f - Mathf.Exp(-smoothing * Time.deltaTime))
                : target;
        }
    }
}
