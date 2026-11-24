using UnityEngine;

namespace ThreeDUI
{
    /// <summary>
    /// Wayfinding: an arrow on a pole that always points (horizontally) at a target, with a label.
    /// Drag the place a visitor should find into 'target' (e.g. your 3. CORE group) and write where it leads.
    /// Works in the editor too, so you can see the direction while you lay out the space.
    /// </summary>
    [ExecuteAlways]
    public class Signpost : MonoBehaviour
    {
        [Tooltip("Where the arrow points.")]
        [SerializeField] private Transform target;
        [Tooltip("Text on the sign, e.g. \"Listening booth →\".")]
        [SerializeField] private string label = "TODO: where does this lead?";
        [SerializeField] private Transform arrow;
        [SerializeField] private WorldPanel labelPanel;

        public Transform Target { get => target; set => target = value; }

        private string shownLabel;

        private void OnEnable() => Refresh();

        private void Update() => Refresh();

        private void Refresh()
        {
            if (labelPanel != null && shownLabel != label)
            {
                labelPanel.SetText(label, "");
                shownLabel = label;
            }
            if (arrow == null || target == null) return;
            Vector3 d = target.position - arrow.position;
            d.y = 0f;
            if (d.sqrMagnitude < 0.0001f) return;
            var want = Quaternion.LookRotation(d.normalized, Vector3.up);
            if (Quaternion.Angle(arrow.rotation, want) > 0.1f) arrow.rotation = want;   // don't dirty the scene every frame
        }
    }
}
