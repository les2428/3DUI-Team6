using UnityEngine;

namespace ThreeDUI
{
    /// <summary>
    /// A viewpoint the visitor can jump to by clicking it (Navigation / wayfinding).
    /// Works the same on desktop and XR. The visitor lands on this transform's position and,
    /// if alignRotation is on, faces this transform's forward (blue arrow).
    /// </summary>
    public class TeleportAnchor : Interactable
    {
        [Header("Teleport")]
        [SerializeField] private bool alignRotation = true;
        [Tooltip("Optional: land somewhere other than this object (e.g. in front of an exhibit).")]
        [SerializeField] private Transform destination;

        protected override void OnActivate(InteractionContext ctx)
        {
            TeleportHere();
        }

        public void TeleportHere()
        {
            var rig = PlayerRig.Instance;
            if (rig == null) return;
            var t = destination != null ? destination : transform;
            rig.TeleportTo(t.position, alignRotation ? t.forward : (Vector3?)null);
        }
    }
}
