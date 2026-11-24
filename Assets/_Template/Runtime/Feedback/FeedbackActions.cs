using System.Collections;
using UnityEngine;

namespace ThreeDUI
{
    /// <summary>
    /// Feedback you can trigger from any UnityEvent, with no code: a drawer's On Opened, a snap zone's
    /// On Snapped, a signature's On Completed... Put it on the object that should answer, then pick
    /// FeedbackActions > Pulse / Flash / Beep / Buzz in the event's function list.
    /// </summary>
    public class FeedbackActions : MonoBehaviour
    {
        [Header("Pulse (size)")]
        [SerializeField] private float pulseScale = 1.15f;
        [SerializeField] private float pulseSeconds = 0.25f;

        [Header("Flash (colour)")]
        [SerializeField] private Color flashColor = new Color(1f, 0.85f, 0.3f);
        [SerializeField] private float flashSeconds = 0.4f;

        [Header("Beep (sound)")]
        [Tooltip("Pitch of the beep in Hz. Higher sounds brighter: 880 for small actions, 1300+ for success.")]
        [SerializeField] private float beepFrequency = 1046f;
        [Range(0f, 1f)] [SerializeField] private float volume = 0.6f;

        [Header("Buzz (controller vibration, XR only)")]
        [Range(0f, 1f)] [SerializeField] private float buzzStrength = 0.6f;
        [SerializeField] private float buzzMilliseconds = 80f;

        private Vector3 baseScale;
        private Coroutine pulsing, flashing;
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");

        private void Awake() => baseScale = transform.localScale;

        /// <summary>Grow briefly and settle back.</summary>
        public void Pulse()
        {
            if (pulsing != null) StopCoroutine(pulsing);
            pulsing = StartCoroutine(PulseRoutine());
        }

        /// <summary>Tint every renderer briefly.</summary>
        public void Flash()
        {
            if (flashing != null) StopCoroutine(flashing);
            flashing = StartCoroutine(FlashRoutine());
        }

        /// <summary>A short tone at this object's position.</summary>
        public void Beep() => ToneLibrary.Play(ToneLibrary.Blip(beepFrequency, 0.12f), transform.position, volume);

        /// <summary>Vibrate both controllers (does nothing on desktop).</summary>
        public void Buzz()
        {
            XRModeManager.Haptic(HandSide.Left, buzzStrength, buzzMilliseconds);
            XRModeManager.Haptic(HandSide.Right, buzzStrength, buzzMilliseconds);
        }

        /// <summary>All four at once: for the moments that matter.</summary>
        public void All() { Pulse(); Flash(); Beep(); Buzz(); }

        private IEnumerator PulseRoutine()
        {
            for (float t = 0f; t < pulseSeconds; t += Time.deltaTime)
            {
                float k = Mathf.Sin(t / pulseSeconds * Mathf.PI);
                transform.localScale = baseScale * Mathf.Lerp(1f, pulseScale, k);
                yield return null;
            }
            transform.localScale = baseScale;
        }

        private IEnumerator FlashRoutine()
        {
            var renderers = GetComponentsInChildren<Renderer>();
            var block = new MaterialPropertyBlock();
            for (float t = 0f; t < flashSeconds; t += Time.deltaTime)
            {
                float k = 1f - t / flashSeconds;
                foreach (var r in renderers)
                {
                    if (r is LineRenderer || r.sharedMaterial == null || !r.sharedMaterial.HasProperty(BaseColorId)) continue;
                    r.GetPropertyBlock(block);
                    block.SetColor(BaseColorId, Color.Lerp(r.sharedMaterial.GetColor(BaseColorId), flashColor, k));
                    r.SetPropertyBlock(block);
                }
                yield return null;
            }
            foreach (var r in renderers) r.SetPropertyBlock(null);
        }
    }
}
