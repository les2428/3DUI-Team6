using ThreeDUI;
using UnityEngine;

namespace TeamTemplate // TODO: rename to your team, e.g. Team07
{
    /// <summary>
    /// TODO: Your team's Signature Interaction.
    ///
    /// Answer these first (from the Term Project brief):
    ///   - Why does this space exist?          (Purpose)
    ///   - Who will visit it?                  (Target visitor)
    ///   - What will visitors do here?         (Activity / interaction)
    ///   - Why 3D instead of a 2D webpage?     (Value of 3D)
    ///
    /// Then design ONE interaction that expresses that purpose, and drive it through
    /// Begin() / ReportProgress(0..1) / Complete().
    ///
    /// The placeholder below already works end to end, so you can see the pattern before you replace it:
    ///   ACT       the visitor activates every object in 'steps' (in the starter scene: one button)
    ///   FEEDBACK  the panel says what happened, 'revealOnComplete' appears, a tone plays
    /// Tip: listen to Interactable / Grabbable / SnapZone events to know what the visitor did.
    /// </summary>
    public class TeamSignatureInteraction : SignatureInteraction
    {
        [Header("Team setup")]
        [Tooltip("The objects the visitor has to use. Replace with your own mechanic.")]
        [SerializeField] private Interactable[] steps = new Interactable[0];
        [Tooltip("Panel that tells the visitor what their action did (FEEDBACK).")]
        [SerializeField] private WorldPanel feedbackPanel;
        [Tooltip("Hidden until the interaction is completed.")]
        [SerializeField] private GameObject revealOnComplete;

        private int completedSteps;

        private void OnEnable()
        {
            foreach (var s in steps)
                if (s != null) s.Activated += OnStepActivated;
        }

        private void OnDisable()
        {
            foreach (var s in steps)
                if (s != null) s.Activated -= OnStepActivated;
        }

        protected override void Start()
        {
            base.Start();
            if (revealOnComplete != null) revealOnComplete.SetActive(false);
        }

        // Placeholder logic: activate every step object once. Replace with your own mechanic.
        private void OnStepActivated(Interactable step, InteractionContext ctx)
        {
            completedSteps++;
            ReportProgress(steps.Length == 0 ? 1f : completedSteps / (float)steps.Length);
        }

        protected override void OnStateChanged(State previous, State current)
        {
            // TODO: feedback for each state (sound, light, animation, story text, ...)
            if (current == State.Completed)
            {
                if (revealOnComplete != null) revealOnComplete.SetActive(true);
                ToneLibrary.Play(ToneLibrary.Blip(1568f, 0.25f), transform.position, 0.8f);
                XRModeManager.Haptic(HandSide.Right, 0.8f, 150f);
            }
            if (feedbackPanel == null) return;
            switch (current)
            {
                case State.Ready:
                    feedbackPanel.SetText(Title, Description);
                    break;
                case State.InProgress:
                    feedbackPanel.SetText(Title, $"{Description}\n\nProgress: {completedSteps} / {steps.Length}");
                    break;
                case State.Completed:
                    feedbackPanel.SetText("Done!", "TODO: say what changed, in the visitor's words, not the system's.");
                    break;
            }
        }

        public override void ResetInteraction()
        {
            completedSteps = 0;
            if (revealOnComplete != null) revealOnComplete.SetActive(false);
            base.ResetInteraction();
        }
    }
}
