using UnityEngine;

namespace ThreeDUI
{
    /// <summary>
    /// Turns a 0..1 value (from a lever, dial, slider or drawer) into light. Wire the value event to
    /// LightDimmer.SetLevel in the Inspector ("Dynamic float" section of the function list).
    /// </summary>
    public class LightDimmer : MonoBehaviour
    {
        [SerializeField] private Light target;
        [SerializeField] private float maxIntensity = 3f;
        [Tooltip("Optional: shown while the level is above zero (e.g. a glowing bulb).")]
        [SerializeField] private GameObject glow;

        public void SetLevel(float value01)
        {
            float v = Mathf.Clamp01(value01);
            if (target != null)
            {
                target.intensity = v * maxIntensity;
                target.enabled = v > 0.01f;
            }
            if (glow != null) glow.SetActive(v > 0.05f);
        }
    }
}
