using UnityEngine;

namespace ThreeDUI
{
    /// <summary>
    /// Shows something (a panel, a label, an arrow) only when the visitor is close, and hides it again when
    /// they walk away. Information appears where and when it is needed, instead of filling the room.
    /// Works with the desktop camera and the headset alike (it uses the visitor's head position).
    /// </summary>
    public class ProximityReveal : MonoBehaviour
    {
        [Tooltip("What to show. Leave empty to use the first child.")]
        [SerializeField] private GameObject target;
        [Tooltip("Shown when the visitor's head is closer than this (metres).")]
        [SerializeField] private float showWithin = 2.5f;
        [Tooltip("Hidden again beyond this distance (a little more than Show Within, so it does not flicker at the edge).")]
        [SerializeField] private float hideBeyond = 3.2f;
        [Tooltip("Grow from 0 to full size when it appears.")]
        [SerializeField] private bool animate = true;

        private Vector3 fullScale;
        private float shown;      // 0..1
        private bool visible;

        private void Awake()
        {
            if (target == null && transform.childCount > 0) target = transform.GetChild(0).gameObject;
            if (target == null) return;
            fullScale = target.transform.localScale;
            target.SetActive(false);
        }

        private void Update()
        {
            var rig = PlayerRig.Instance;
            if (target == null || rig == null) return;
            float d = Vector3.Distance(rig.Head.position, transform.position);
            if (!visible && d < showWithin) visible = true;
            else if (visible && d > hideBeyond) visible = false;

            shown = animate ? Mathf.MoveTowards(shown, visible ? 1f : 0f, Time.deltaTime * 5f) : (visible ? 1f : 0f);
            if (target.activeSelf != shown > 0f) target.SetActive(shown > 0f);
            if (shown > 0f) target.transform.localScale = fullScale * Mathf.SmoothStep(0.6f, 1f, shown);
        }
    }
}
