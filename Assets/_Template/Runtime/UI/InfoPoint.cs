using UnityEngine;

namespace ThreeDUI
{
    /// <summary>
    /// Click/trigger to show or hide an information panel (Spatial UI / information presentation).
    /// Put it on a marker object (e.g. an "i" sphere) and point 'panel' at a WorldPanel.
    /// </summary>
    public class InfoPoint : Interactable
    {
        [Header("Info")]
        [SerializeField] private WorldPanel panel;
        [Tooltip("A second panel that opens and closes with this one - the board on the wall, " +
                 "so a zone shows nothing until the visitor asks for it.")]
        [SerializeField] private WorldPanel alsoToggle;
        [SerializeField] private bool startVisible = false;
        [Tooltip("Hide the panel automatically when the visitor walks farther than this (0 = never).")]
        [SerializeField] private float autoHideDistance = 6f;

        public bool IsOpen => panel != null && panel.gameObject.activeSelf;

        protected virtual void Awake()
        {
            if (panel != null) panel.Show(startVisible);
            if (alsoToggle != null) alsoToggle.Show(startVisible);
        }

        protected virtual void Update()
        {
            if (!IsOpen || autoHideDistance <= 0f || PlayerRig.Instance == null) return;
            if ((PlayerRig.Instance.Head.position - transform.position).sqrMagnitude > autoHideDistance * autoHideDistance)
                SetOpen(false);
        }

        protected override void OnActivate(InteractionContext ctx)
        {
            Toggle();
        }

        public void Toggle() => SetOpen(!IsOpen);

        public void SetOpen(bool open)
        {
            if (panel != null) panel.Show(open);
            if (alsoToggle != null) alsoToggle.Show(open);
        }
    }
}
