using UnityEngine;

namespace ThreeDUI
{
    /// <summary>
    /// Unity calls OnAnimatorIK only on scripts next to the Animator. The Animator is on the character model
    /// (a child of the agent), so this forwards the IK pass to GuideBody (gaze and pointing).
    /// </summary>
    [RequireComponent(typeof(Animator))]
    public class GuideIKRelay : MonoBehaviour
    {
        [SerializeField] private GuideBody body;

        private void Awake() { if (body == null) body = GetComponentInParent<GuideBody>(); }

        private void OnAnimatorIK(int layerIndex) { if (body != null) body.ApplyIK(); }
    }
}
