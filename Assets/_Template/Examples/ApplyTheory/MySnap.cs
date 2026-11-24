using UnityEngine;

namespace ThreeDUI.ApplyTheory
{
    // Same code as Tutorials/Undergrad_T3/ApplyTheory/MySnap.cs (own namespace, so a typed copy does not clash).
    // Tutorial 3 · live coding 3: put it in place. The hand offers 6 DOF; inside the capture radius the socket takes all of them.
    //   distance       d = |p − s| = √(dx² + dy² + dz²)     how far the key is from the socket
    //   capture        d < r                                 close enough? (r = 0.15 m)
    //   6 DOF → 0      p′ = s,  q′ = q_s                     position and rotation both come from the socket
    //   stay           kinematic + new home on release       it does not fall out
    // d is measured from where the hand WANTS the key (the free pose), so pulling it away frees it again.
    // Add it to "Key" inside "Bare Key and Lock" (3DUI_T3_Starter); drag "Socket" into the Socket field.
    // Compare with SnapZone + SnapKey in the template (the zone owns the radius, the key only says which zone fits).
    public class MySnap : Grabbable
    {
        public Transform socket;                          // where the key belongs
        public float radius = 0.15f;                      // capture radius, in metres
        bool inRange;

        protected override void ComputeTargetPose(InteractionContext ctx, out Vector3 position, out Quaternion rotation)
        {
            base.ComputeTargetPose(ctx, out position, out rotation);       // free: 6 DOF
            if (socket == null) return;
            float d = Vector3.Distance(position, socket.position);         // 1. distance
            inRange = d < radius;                                          // 2. capture
            if (inRange)
            {
                position = socket.position;                                // 3. 6 DOF → 0
                rotation = socket.rotation;
            }
        }

        protected override void OnReleased(InteractionContext ctx)
        {
            if (!inRange) return;                                          // 4. stay in the socket
            if (Body != null) Body.isKinematic = true;
            transform.SetPositionAndRotation(socket.position, socket.rotation);
            SetStartPose(socket.position, socket.rotation);
        }
    }
}
