using System;
using UnityEngine;
using UnityEngine.Events;

namespace ThreeDUI
{
    /// <summary>
    /// Scaffold for your team's Signature Interaction: the one core interaction that represents your space.
    /// It gives every team the same small state machine so the experience has a clear
    /// beginning, progress, and ending (and so peer testers can see where they are).
    ///
    ///   Locked -> Ready -> InProgress -> Completed
    ///
    /// How to use:
    ///   1. Create a class that inherits SignatureInteraction (see Assets/Teams/_TeamTemplate/Scripts).
    ///   2. Call Begin() when the visitor starts, ReportProgress(0..1) as they go, Complete() at the end.
    ///   3. Put the reward / story / feedback in OnStateChanged or wire the UnityEvents in the Inspector.
    /// Everything else (what the visitor actually does) is up to you.
    /// </summary>
    public abstract class SignatureInteraction : MonoBehaviour
    {
        public enum State
        {
            Locked,
            Ready,
            InProgress,
            Completed
        }

        [Header("Signature Interaction")]
        [SerializeField] private string title = "Signature Interaction";
        [TextArea(2, 5)]
        [SerializeField] private string description = "Describe what the visitor does here and why it matters for your space.";
        [Tooltip("Start Locked (e.g. until the visitor finishes an intro) instead of Ready.")]
        [SerializeField] private bool startLocked = false;

        // The brief asks three questions of the signature interaction. Write your answers here, next to
        // the thing they describe, and check them against what peer testers actually did.
        [Header("Design notes (DISCOVER / ACT / FEEDBACK)")]
        [Tooltip("DISCOVER: what tells a visitor this is interactive before they touch it? " +
                 "(an outline, a light, a sound, a label, its placement ...)")]
        [TextArea(2, 4)] [SerializeField] private string discoverability = "";
        [Tooltip("ACT: what the visitor does, on desktop and in the headset, and where the two differ.")]
        [TextArea(2, 4)] [SerializeField] private string action = "";
        [Tooltip("FEEDBACK: how the visitor knows exactly what their action did.")]
        [TextArea(2, 4)] [SerializeField] private string feedback = "";

        [Header("Signature events")]
        public UnityEvent onUnlocked = new UnityEvent();
        public UnityEvent onStarted = new UnityEvent();
        public UnityEvent<float> onProgress = new UnityEvent<float>();
        public UnityEvent onCompleted = new UnityEvent();
        public UnityEvent onReset = new UnityEvent();

        public event Action<SignatureInteraction, State> StateChanged;

        public string Title => title;
        public string Description => description;
        public string Discoverability => discoverability;
        public string Action => action;
        public string Feedback => feedback;
        public State CurrentState { get; private set; }
        public float Progress { get; private set; }

        protected virtual void Start()
        {
            CurrentState = startLocked ? State.Locked : State.Ready;
            OnStateChanged(State.Locked, CurrentState);
        }

        /// <summary>Unlock from another object (e.g. a PushButton3D's onPressed).</summary>
        public void Unlock()
        {
            if (CurrentState != State.Locked) return;
            SetState(State.Ready);
            onUnlocked.Invoke();
        }

        /// <summary>Call when the visitor starts the interaction.</summary>
        public void Begin()
        {
            if (CurrentState != State.Ready) return;
            SetState(State.InProgress);
            onStarted.Invoke();
        }

        /// <summary>Report progress 0..1. Starts the interaction automatically if needed.</summary>
        public void ReportProgress(float value)
        {
            if (CurrentState == State.Ready) Begin();
            if (CurrentState != State.InProgress) return;
            Progress = Mathf.Clamp01(value);
            OnProgress(Progress);
            onProgress.Invoke(Progress);
            if (Progress >= 1f) Complete();
        }

        /// <summary>Call when the visitor has achieved the goal.</summary>
        public void Complete()
        {
            if (CurrentState == State.Completed || CurrentState == State.Locked) return;
            Progress = 1f;
            SetState(State.Completed);
            onCompleted.Invoke();
        }

        /// <summary>Return to Ready so the next visitor can try again.</summary>
        public virtual void ResetInteraction()
        {
            Progress = 0f;
            SetState(startLocked ? State.Locked : State.Ready);
            onReset.Invoke();
        }

        private void SetState(State next)
        {
            if (next == CurrentState) return;
            var prev = CurrentState;
            CurrentState = next;
            Debug.Log($"[3DUI] Signature '{title}': {prev} -> {next}");
            OnStateChanged(prev, next);
            StateChanged?.Invoke(this, next);
        }

        // ---- Hooks for your subclass ----
        protected virtual void OnStateChanged(State previous, State current) { }
        protected virtual void OnProgress(float progress) { }
    }
}
