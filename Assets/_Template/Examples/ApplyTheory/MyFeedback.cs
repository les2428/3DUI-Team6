using UnityEngine;

namespace ThreeDUI.ApplyTheory
{
    // Same code as Tutorials/Undergrad_T4/ApplyTheory/MyFeedback.cs.
    // Tutorial 4 · applying the theory: every action gets an answer, in more than one channel.
    //   audio    a short tone: 1175 Hz for 0.1 s (high = "done")
    //   touch    a buzz in the hand that clicked: strength 0.6 for 60 ms (only in the headset)
    //   vision   the object grows by 15 % for 0.12 s
    // Subscribes to the object's Activated event (a click: pressed and released on it). Add it next to any Interactable.
    [RequireComponent(typeof(Interactable))]
    public class MyFeedback : MonoBehaviour
    {
        public float pitch = 1175f;           // Hz
        public float buzz = 0.6f;             // 0..1
        public float buzzMs = 60f;            // milliseconds
        Interactable item;
        Vector3 baseScale;

        void Awake()
        {
            item = GetComponent<Interactable>();
            baseScale = transform.localScale;
        }

        void OnEnable() => item.Activated += OnActivated;
        void OnDisable() => item.Activated -= OnActivated;

        void OnActivated(Interactable i, InteractionContext ctx)
        {
            ToneLibrary.Play(ToneLibrary.Blip(pitch, 0.1f), transform.position, 0.7f);   // audio
            ctx?.Pointer?.Haptic(buzz, buzzMs);                                           // touch (headset only)
            StopAllCoroutines();
            StartCoroutine(Pulse());                                                       // vision
        }

        System.Collections.IEnumerator Pulse()
        {
            transform.localScale = baseScale * 1.15f;
            yield return new WaitForSeconds(0.12f);
            transform.localScale = baseScale;
        }
    }
}
