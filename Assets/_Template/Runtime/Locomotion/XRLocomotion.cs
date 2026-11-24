using System.Collections.Generic;
using UnityEngine;

namespace ThreeDUI
{
    /// <summary>
    /// XR navigation (each technique can be switched on/off in the Inspector):
    ///   Teleport     : push the teleport-hand thumbstick forward, aim the arc at a TeleportArea, release.
    ///   Snap turn    : flick the turn-hand thumbstick left/right.
    ///   Smooth move  : move-hand thumbstick (head-relative). Off by default (can cause motion sickness).
    /// A thumbstick is ignored while that hand is holding an object (it is used for push/pull instead),
    /// and the right one while a reach technique (Go-Go, HOMER, Flashlight) is active: it adjusts the arm or the cone.
    /// With the ray, the right stick teleports and turns as usual.
    /// </summary>
    [RequireComponent(typeof(PlayerRig))]
    public class XRLocomotion : MonoBehaviour
    {
        [Header("Teleport")]
        [SerializeField] private bool enableTeleport = true;
        [SerializeField] private HandSide teleportHand = HandSide.Right;
        [SerializeField] private float arcVelocity = 8f;
        [SerializeField] private int arcSegments = 40;
        [SerializeField] private float arcTimeStep = 0.04f;
        [SerializeField] private LayerMask teleportLayers = ~0;
        [SerializeField] private LineRenderer arcLine;
        [SerializeField] private Transform teleportMarker;
        [SerializeField] private Color validColor = new Color(0.3f, 0.9f, 1f);
        [SerializeField] private Color invalidColor = new Color(1f, 0.3f, 0.3f);

        [Header("Snap turn")]
        [SerializeField] private bool enableSnapTurn = true;
        [SerializeField] private HandSide turnHand = HandSide.Right;
        [SerializeField] private float snapAngle = 45f;

        [Header("Smooth move")]
        [SerializeField] private bool enableSmoothMove = false;
        [SerializeField] private HandSide moveHand = HandSide.Left;
        [SerializeField] private float moveSpeed = 2f;

        private const float StickPressThreshold = 0.6f;
        private const float StickReleaseThreshold = 0.3f;

        private PlayerRig rig;
        private bool aiming;
        private bool turnLatched;
        private bool hasValidTarget;
        private Vector3 targetPoint;
        private readonly List<Vector3> arcPoints = new List<Vector3>();

        private void Awake()
        {
            rig = GetComponent<PlayerRig>();
            SetArcVisible(false);
        }

        private void Update()
        {
            if (!XRModeManager.IsXR)
            {
                if (aiming) EndAim(false);
                return;
            }

            if (enableSmoothMove) UpdateSmoothMove();
            if (enableTeleport) UpdateTeleport();
            if (enableSnapTurn) UpdateSnapTurn();
        }

        private XRControllerPointer Hand(HandSide side) =>
            side == HandSide.Left ? rig.LeftController : rig.RightController;

        private static Vector2 Stick(XRControllerPointer c) =>
            c != null && c.IsTracked && !c.IsSelecting && !ReachTechnique.UsesThumbstick(c.Side) ? c.Thumbstick : Vector2.zero;

        // ---------------------------------------------------------------- Smooth move

        private void UpdateSmoothMove()
        {
            var stick = Stick(Hand(moveHand));
            if (moveHand == teleportHand && enableTeleport) return;
            if (stick.sqrMagnitude < 0.04f) return;
            var forward = rig.HeadForwardFlat;
            var right = Vector3.Cross(Vector3.up, forward);
            rig.Move((forward * stick.y + right * stick.x) * moveSpeed * Time.deltaTime);
        }

        // ---------------------------------------------------------------- Snap turn

        private void UpdateSnapTurn()
        {
            var stick = Stick(Hand(turnHand));
            if (aiming && turnHand == teleportHand) return;
            if (!turnLatched && Mathf.Abs(stick.x) > StickPressThreshold && Mathf.Abs(stick.x) > Mathf.Abs(stick.y))
            {
                rig.RotateAroundHead(Mathf.Sign(stick.x) * snapAngle);
                turnLatched = true;
            }
            else if (turnLatched && Mathf.Abs(stick.x) < StickReleaseThreshold)
            {
                turnLatched = false;
            }
        }

        // ---------------------------------------------------------------- Teleport

        private void UpdateTeleport()
        {
            var hand = Hand(teleportHand);
            var stick = Stick(hand);

            if (!aiming && stick.y > StickPressThreshold && Mathf.Abs(stick.y) > Mathf.Abs(stick.x))
            {
                aiming = true;
                if (hand != null) hand.Suspended = true;
            }

            if (!aiming) return;

            if (hand == null || !hand.IsTracked || ReachTechnique.UsesThumbstick(hand.Side))   // switched to a technique mid-aim: cancel
            {
                EndAim(false);
                return;
            }

            ComputeArc(hand.GetRay());
            DrawArc();

            if (stick.y < StickReleaseThreshold)
            {
                EndAim(hasValidTarget);
            }
        }

        private void EndAim(bool teleport)
        {
            aiming = false;
            SetArcVisible(false);
            var hand = Hand(teleportHand);
            if (hand != null) hand.Suspended = false;
            if (teleport)
            {
                rig.TeleportTo(targetPoint);
                if (hand != null) hand.Haptic(0.4f, 50f);
            }
        }

        private void ComputeArc(Ray ray)
        {
            arcPoints.Clear();
            hasValidTarget = false;
            var pos = ray.origin;
            var vel = ray.direction * arcVelocity;
            arcPoints.Add(pos);

            for (int i = 0; i < arcSegments; i++)
            {
                var next = pos + vel * arcTimeStep;
                vel += Physics.gravity * arcTimeStep;
                var seg = next - pos;
                if (Physics.Raycast(pos, seg.normalized, out var hit, seg.magnitude, teleportLayers, QueryTriggerInteraction.Ignore))
                {
                    arcPoints.Add(hit.point);
                    var area = hit.collider.GetComponentInParent<TeleportArea>();
                    hasValidTarget = area != null && area.IsValidHit(hit);
                    targetPoint = hit.point;
                    return;
                }
                arcPoints.Add(next);
                pos = next;
            }
        }

        private void DrawArc()
        {
            SetArcVisible(true);
            var c = hasValidTarget ? validColor : invalidColor;
            if (arcLine != null)
            {
                arcLine.positionCount = arcPoints.Count;
                for (int i = 0; i < arcPoints.Count; i++) arcLine.SetPosition(i, arcPoints[i]);
                arcLine.startColor = c;
                arcLine.endColor = c;
            }
            if (teleportMarker != null)
            {
                teleportMarker.gameObject.SetActive(hasValidTarget);
                teleportMarker.position = targetPoint + Vector3.up * 0.01f;
                teleportMarker.rotation = Quaternion.LookRotation(rig.HeadForwardFlat, Vector3.up);
            }
        }

        private void SetArcVisible(bool visible)
        {
            if (arcLine != null) arcLine.enabled = visible;
            if (teleportMarker != null && !visible) teleportMarker.gameObject.SetActive(false);
        }
    }
}
