using ThreeDUI;
using UnityEngine;

namespace TeamTemplate // TODO: rename to your team
{
    /// <summary>
    /// EXAMPLE of a custom Interactable made by subclassing.
    /// Click / trigger to spin the object. Shows how to:
    ///   - override the interaction hooks,
    ///   - react differently to desktop vs XR (ctx.Mode),
    ///   - use near (touch) vs ray interaction (ctx.IsNear),
    ///   - give haptic feedback.
    /// Add InteractableFeedback next to it for the standard highlight + sound.
    /// </summary>
    public class SpinOnActivate : Interactable
    {
        [SerializeField] private float spinSpeed = 360f;
        [SerializeField] private float spinDuration = 1f;

        private float spinTimeLeft;

        protected override void OnHoverEnter(InteractionContext ctx)
        {
            // e.g. show a hint only for desktop visitors
            if (ctx.Mode == ExperienceMode.Desktop) Debug.Log($"Hovering {name} with the mouse");
        }

        protected override void OnActivate(InteractionContext ctx)
        {
            // Touching it directly in XR spins it longer than pointing at it.
            spinTimeLeft = ctx != null && ctx.IsNear ? spinDuration * 2f : spinDuration;
            ctx?.Pointer.Haptic(0.5f, 80f);
        }

        private void Update()
        {
            if (spinTimeLeft <= 0f) return;
            spinTimeLeft -= Time.deltaTime;
            transform.Rotate(Vector3.up, spinSpeed * Time.deltaTime, Space.World);
        }
    }
}
