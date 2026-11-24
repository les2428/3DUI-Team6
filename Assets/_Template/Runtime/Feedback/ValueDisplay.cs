using UnityEngine;

namespace ThreeDUI
{
    /// <summary>
    /// Shows a 0..1 value on a WorldPanel, e.g. "Volume 70 %". Wire the value event to ValueDisplay.Show.
    /// </summary>
    public class ValueDisplay : MonoBehaviour
    {
        [SerializeField] private WorldPanel panel;
        [SerializeField] private string title = "Value";
        [Tooltip("{0} is the value 0..100.")]
        [SerializeField] private string format = "{0:0} %";

        public void Show(float value01)
        {
            if (panel != null) panel.SetText(title, string.Format(format, Mathf.Clamp01(value01) * 100f));
        }
    }
}
