using UnityEngine;

namespace ThreeDUI
{
    /// <summary>
    /// Shows a small floating label (Interactable.DisplayName + Hint) above whatever the visitor points at.
    /// Helps visitors discover what is interactive. One per scene (lives on the PlayerRig).
    /// </summary>
    [DefaultExecutionOrder(30)]
    public class HoverLabel : MonoBehaviour
    {
        [SerializeField] private WorldPanel label;
        [SerializeField] private float heightAboveObject = 0.12f;
        [Tooltip("Keeps the label the same apparent size at any distance.")]
        [SerializeField] private float sizePerMeter = 0.35f;

        private PlayerRig rig;
        private Interactable current;
        private Vector3 baseScale;

        private void Awake()
        {
            rig = GetComponentInParent<PlayerRig>();
            if (label != null)
            {
                baseScale = label.transform.localScale;
                label.transform.SetParent(null, true); // world-space, not moving with the rig
                label.Show(false);
            }
        }

        private void OnDestroy()
        {
            if (label != null) Destroy(label.gameObject);
        }

        private void LateUpdate()
        {
            if (label == null || rig == null) return;
            var target = FindHovered();

            if (target != current)
            {
                current = target;
                bool show = current != null && !string.IsNullOrEmpty(current.DisplayName);
                label.Show(show);
                if (show) label.SetText(current.DisplayName, current.Hint);
            }
            if (current == null || !label.gameObject.activeSelf) return;

            var head = rig.Head.position;
            var top = TopOf(current);
            var toHead = head - top;
            float dist = toHead.magnitude;
            label.transform.position = top + Vector3.up * heightAboveObject + toHead.normalized * 0.05f;
            label.transform.rotation = Quaternion.LookRotation(-new Vector3(toHead.x, 0f, toHead.z).normalized, Vector3.up);
            label.transform.localScale = baseScale * Mathf.Clamp(dist * sizePerMeter, 0.3f, 3f);
        }

        private Interactable FindHovered()
        {
            Pointer[] pointers = { rig.DesktopPointer, rig.RightController, rig.LeftController };
            foreach (var p in pointers)
            {
                if (p != null && p.isActiveAndEnabled && p.IsActive && p.Hovered != null && !p.IsSelecting) return p.Hovered;
            }
            return null;
        }

        private static Vector3 TopOf(Interactable i)
        {
            var renderers = i.GetComponentsInChildren<Renderer>();
            if (renderers.Length == 0) return i.transform.position;
            var b = renderers[0].bounds;
            for (int k = 1; k < renderers.Length; k++) b.Encapsulate(renderers[k].bounds);
            return new Vector3(b.center.x, b.max.y, b.center.z);
        }
    }
}
