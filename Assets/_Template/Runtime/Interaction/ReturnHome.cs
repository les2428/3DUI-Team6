using UnityEngine;
using UnityEngine.Events;

namespace ThreeDUI
{
    /// <summary>
    /// Put this next to a Grabbable so it cannot get lost: if it ends up too far from where it started
    /// (thrown over a wall, through the floor, out of the room) it goes back after a moment, with a sound.
    /// </summary>
    [RequireComponent(typeof(Grabbable))]
    public class ReturnHome : MonoBehaviour
    {
        [Tooltip("Return if it is further than this from its start position (metres).")]
        [SerializeField] private float maxDistance = 10f;
        [Tooltip("Return if it falls this far below its start height (metres).")]
        [SerializeField] private float maxDrop = 2f;
        [Tooltip("How long it must stay lost before it returns (seconds).")]
        [SerializeField] private float delay = 1.5f;
        [SerializeField] private bool playSound = true;
        public UnityEvent onReturned = new UnityEvent();

        private Grabbable grabbable;
        private Vector3 home;
        private float lostSince = -1f;

        private void Awake()
        {
            grabbable = GetComponent<Grabbable>();
            home = transform.position;
        }

        private void Update()
        {
            bool lost = !grabbable.IsGrabbed &&
                        (Vector3.Distance(transform.position, home) > maxDistance || transform.position.y < home.y - maxDrop);
            if (!lost) { lostSince = -1f; return; }
            if (lostSince < 0f) { lostSince = Time.time; return; }
            if (Time.time - lostSince < delay) return;
            lostSince = -1f;
            grabbable.ResetToStart();
            if (playSound) ToneLibrary.Play(ToneLibrary.Blip(660f, 0.12f), transform.position, 0.5f);
            onReturned.Invoke();
        }
    }
}
