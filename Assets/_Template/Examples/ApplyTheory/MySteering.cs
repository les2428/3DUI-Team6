using UnityEngine;

namespace ThreeDUI.ApplyTheory
{
    // Tutorial 2 · applying the theory. Same code as Tutorials/Undergrad_T2/ApplyTheory/MySteering.cs.
    // Steering travel (Bowman, Koller & Hodges 1997): v = speed · d
    //   input condition: hold T · direction: pointing (mouse ray / hand) or gaze (head) · velocity: speed · d
    public class MySteering : MonoBehaviour
    {
        public bool usePointing = true;    // pointing-directed or gaze-directed
        public float speed = 2f;           // m/s

        void Update()
        {
            var rig = PlayerRig.Instance;
            if (rig == null || !DesktopInput.GetKey(KeyCode.T)) return;      // 1. input condition

            Vector3 d = usePointing ? rig.DesktopPointer.GetRay().direction   // 2. direction
                                    : rig.Head.forward;
            d = Vector3.ProjectOnPlane(d, Vector3.up).normalized;             //    stay on the floor
            rig.Move(d * speed * Time.deltaTime);                             // 3. v = speed · d
        }
    }
}
