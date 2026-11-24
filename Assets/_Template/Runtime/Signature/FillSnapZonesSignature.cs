using System.Linq;
using UnityEngine;

namespace ThreeDUI
{
    /// <summary>
    /// EXAMPLE signature interaction: "place every item on its pedestal".
    /// Progress = filled zones / total zones. On completion it reveals a reward object
    /// and updates a WorldPanel. Copy the idea, not necessarily the mechanic!
    /// </summary>
    public class FillSnapZonesSignature : SignatureInteraction
    {
        [Header("Example setup")]
        [SerializeField] private SnapZone[] zones;
        [Tooltip("Activated when all zones are filled.")]
        [SerializeField] private GameObject reward;
        [SerializeField] private WorldPanel statusPanel;

        private void OnEnable()
        {
            foreach (var z in zones)
            {
                if (z == null) continue;
                z.Snapped += OnZoneChanged;
                z.Unsnapped += OnZoneChanged;
            }
        }

        private void OnDisable()
        {
            foreach (var z in zones)
            {
                if (z == null) continue;
                z.Snapped -= OnZoneChanged;
                z.Unsnapped -= OnZoneChanged;
            }
        }

        protected override void Start()
        {
            base.Start();
            if (reward != null) reward.SetActive(false);
            UpdatePanel();
        }

        private void OnZoneChanged(SnapZone zone, Grabbable item)
        {
            if (CurrentState == State.Completed) return;
            int filled = zones.Count(z => z != null && z.IsOccupied);
            ReportProgress(zones.Length == 0 ? 0f : filled / (float)zones.Length);
            UpdatePanel();
        }

        protected override void OnStateChanged(State previous, State current)
        {
            if (current == State.Completed)
            {
                if (reward != null) reward.SetActive(true);
                ToneLibrary.Play(ToneLibrary.Blip(1568f, 0.25f), transform.position, 0.8f);
                XRModeManager.Haptic(HandSide.Left, 0.8f, 150f);
                XRModeManager.Haptic(HandSide.Right, 0.8f, 150f);
            }
            UpdatePanel();
        }

        private void UpdatePanel()
        {
            if (statusPanel == null) return;
            int filled = zones.Count(z => z != null && z.IsOccupied);
            string body = CurrentState == State.Completed
                ? "Complete! Every item is back where it belongs."
                : $"{Description}\n\nPlaced: {filled} / {zones.Length}";
            statusPanel.SetText(Title, body);
        }
    }
}
