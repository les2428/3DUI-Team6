using ThreeDUI;
using UnityEngine;

namespace TeamTemplate // your team namespace
{
    /// <summary>
    /// Tutorial 1, Lab 4 (reference solution).
    /// Click / trigger the object and a world panel counts how many times it was used.
    /// Put it on any object with a collider, then drag a World Panel into 'panel'.
    /// </summary>
    public class CountOnActivate : Interactable
    {
        [SerializeField] private WorldPanel panel;

        private int count;

        protected override void OnActivate(InteractionContext ctx)
        {
            count++;
            if (panel != null)
                panel.SetText("Pressed", $"{count} time(s)");

            // Different feedback for the two platforms: a buzz only makes sense in a hand.
            if (ctx != null && ctx.Mode != ExperienceMode.Desktop)
                ctx.Pointer.Haptic(0.5f, 60f);
        }
    }
}
