using UnityEngine;

namespace ThreeDUI
{
    /// <summary>
    /// Draws the XR laser ray and a reticle where it hits.
    /// Color changes when pointing at something interactable (feedback for "Discover").
    /// </summary>
    [RequireComponent(typeof(LineRenderer))]
    [DefaultExecutionOrder(20)]
    public class PointerRayVisual : MonoBehaviour
    {
        [SerializeField] private Pointer pointer;
        [SerializeField] private Transform reticle;
        [SerializeField] private Color idleColor = new Color(1f, 1f, 1f, 0.35f);
        [SerializeField] private Color hoverColor = new Color(0.25f, 0.8f, 1f, 1f);
        [SerializeField] private Color selectColor = new Color(1f, 0.75f, 0.2f, 1f);
        [Tooltip("Hide the ray when it hits nothing interactable.")]
        [SerializeField] private bool onlyShowOnHover = false;

        private LineRenderer line;

        private void Awake()
        {
            line = GetComponent<LineRenderer>();
            line.positionCount = 2;
            line.useWorldSpace = true;
            if (pointer == null) pointer = GetComponentInParent<Pointer>();
        }

        private void LateUpdate()
        {
            bool active = pointer != null && pointer.IsActive && !pointer.Suspended && !pointer.IsNearInteraction;
            bool interesting = pointer != null && (pointer.Hovered != null || pointer.IsSelecting);
            bool show = active && (!onlyShowOnHover || interesting);

            line.enabled = show;
            if (reticle != null) reticle.gameObject.SetActive(show && (pointer.HasHit || pointer.IsSelecting));
            if (!show) return;

            var ray = pointer.GetRay();
            var end = pointer.GetRayEndPoint();
            line.SetPosition(0, ray.origin);
            line.SetPosition(1, end);

            var c = pointer.IsSelecting ? selectColor : pointer.Hovered != null ? hoverColor : idleColor;
            line.startColor = c;
            line.endColor = new Color(c.r, c.g, c.b, c.a * 0.3f);

            if (reticle != null)
            {
                reticle.position = end;
                var normal = pointer.HasHit ? pointer.LastHit.normal : -ray.direction;
                reticle.rotation = Quaternion.LookRotation(normal);
            }
        }
    }
}
