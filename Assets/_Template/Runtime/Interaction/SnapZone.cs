using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

namespace ThreeDUI
{
    /// <summary>
    /// A place where a Grabbable "clicks into" when released nearby (a socket / pedestal / slot).
    /// Useful building block for puzzles, collections, assembling, sorting, exhibitions...
    ///
    /// Filter what fits with acceptedKey: only Grabbables with a matching SnapKey component are accepted
    /// (leave empty to accept any Grabbable).
    /// </summary>
    public class SnapZone : MonoBehaviour
    {
        private static readonly List<SnapZone> activeZones = new List<SnapZone>();

        [SerializeField] private float captureRadius = 0.25f;
        [Tooltip("Only accept Grabbables whose SnapKey.key equals this. Empty = accept anything.")]
        [SerializeField] private string acceptedKey = "";
        [Tooltip("Where the snapped object is placed (defaults to this transform).")]
        [SerializeField] private Transform attachPoint;
        [SerializeField] private bool matchRotation = true;
        [Tooltip("Lock the object in place once snapped.")]
        [SerializeField] private bool lockWhenSnapped = false;
        [Tooltip("Optional object shown while a matching object is held nearby (preview / highlight).")]
        [SerializeField] private GameObject previewIndicator;

        public UnityEvent onSnapped = new UnityEvent();
        public UnityEvent onUnsnapped = new UnityEvent();
        public event Action<SnapZone, Grabbable> Snapped;
        public event Action<SnapZone, Grabbable> Unsnapped;

        public Grabbable Current { get; private set; }
        public bool IsOccupied => Current != null;
        public string AcceptedKey => acceptedKey;

        private Transform Attach => attachPoint != null ? attachPoint : transform;

        private void OnEnable()
        {
            activeZones.Add(this);
        }

        private void OnDisable()
        {
            activeZones.Remove(this);
        }

        private void Update()
        {
            if (previewIndicator == null) return;
            bool show = false;
            if (!IsOccupied)
            {
                foreach (var p in PointersHoldingSomething())
                {
                    if (p.Selected is Grabbable g && Accepts(g) && InRange(g)) { show = true; break; }
                }
            }
            if (previewIndicator.activeSelf != show) previewIndicator.SetActive(show);
        }

        private static IEnumerable<Pointer> PointersHoldingSomething()
        {
            var rig = PlayerRig.Instance;
            if (rig == null) yield break;
            if (rig.DesktopPointer != null && rig.DesktopPointer.IsSelecting) yield return rig.DesktopPointer;
            if (rig.LeftController != null && rig.LeftController.IsSelecting) yield return rig.LeftController;
            if (rig.RightController != null && rig.RightController.IsSelecting) yield return rig.RightController;
        }

        public bool Accepts(Grabbable g)
        {
            if (g == null) return false;
            if (string.IsNullOrEmpty(acceptedKey)) return true;
            var key = g.GetComponent<SnapKey>();
            return key != null && key.Key == acceptedKey;
        }

        private bool InRange(Grabbable g) => (g.transform.position - Attach.position).sqrMagnitude <= captureRadius * captureRadius;

        /// <summary>Called by SnapKey/Grabbable on release. Snaps into the closest free matching zone.</summary>
        internal static void TryCapture(Grabbable g)
        {
            SnapZone best = null;
            float bestDist = float.MaxValue;
            foreach (var z in activeZones)
            {
                if (z.IsOccupied || !z.Accepts(g) || !z.InRange(g)) continue;
                float d = (g.transform.position - z.Attach.position).sqrMagnitude;
                if (d < bestDist) { bestDist = d; best = z; }
            }
            if (best != null) best.Capture(g);
        }

        /// <summary>Place a Grabbable into this zone from code.</summary>
        public void Capture(Grabbable g)
        {
            if (IsOccupied || g == null) return;
            Current = g;
            if (g.Body != null)
            {
                g.Body.isKinematic = true;
            }
            var rot = matchRotation ? Attach.rotation : g.transform.rotation;
            g.transform.SetPositionAndRotation(Attach.position, rot);
            g.SetStartPose(Attach.position, rot);
            if (lockWhenSnapped) g.IsInteractable = false;
            g.Grabbed += HandleGrabbedOut;
            if (previewIndicator != null) previewIndicator.SetActive(false);
            onSnapped.Invoke();
            Snapped?.Invoke(this, g);
        }

        /// <summary>Remove the current object from this zone (it stays where it is).</summary>
        public void Release()
        {
            if (!IsOccupied) return;
            var g = Current;
            Current = null;
            g.Grabbed -= HandleGrabbedOut;
            onUnsnapped.Invoke();
            Unsnapped?.Invoke(this, g);
        }

        private void HandleGrabbedOut(Grabbable g)
        {
            if (g == Current) Release();
        }

        private void OnDrawGizmos()
        {
            Gizmos.color = new Color(0.2f, 1f, 0.6f, 0.6f);
            Gizmos.DrawWireSphere(Attach.position, captureRadius);
        }
    }
}
