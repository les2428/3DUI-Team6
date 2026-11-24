using UnityEngine;

namespace ThreeDUI
{
    /// <summary>
    /// Marks a collider (floor, platform) as a valid target for the XR teleport arc.
    /// Add it to any object with a collider; surfaces without it are not teleportable.
    /// </summary>
    [RequireComponent(typeof(Collider))]
    public class TeleportArea : MonoBehaviour
    {
        [Tooltip("Reject surfaces steeper than this (degrees).")]
        [SerializeField] private float maxSlope = 30f;

        public bool IsValidHit(RaycastHit hit) => Vector3.Angle(hit.normal, Vector3.up) <= maxSlope;
    }
}
