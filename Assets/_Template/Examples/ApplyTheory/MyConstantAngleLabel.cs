using UnityEngine;

namespace ThreeDUI.ApplyTheory
{
    // Tutorial 4 · applying the theory. Same code as Tutorials/Undergrad_T4/ApplyTheory/MyConstantAngleLabel.cs.
    // Size by visual angle. Something h tall at distance d covers  θ = 2·atan(h / 2d)  of the view.
    // To look the same size at any distance, grow with the distance:   h = 2·d·tan(θ/2)
    // (θ = 0.7° → about 1.2 cm per metre: the smallest comfortable capital letter in the headset).
    // Put it on a label (TextMeshPro, or a small WorldPanel); it also turns to face the visitor (a billboard).
    public class MyConstantAngleLabel : MonoBehaviour
    {
        public float degrees = 2f;            // θ: how tall it should look
        public float baseHeight = 0.1f;       // its height at the scale it has in the scene (m)
        public float minDistance = 0.6f;      // closer than this it stops shrinking
        Vector3 scaleAtStart;

        void Awake() => scaleAtStart = transform.localScale;

        void LateUpdate()
        {
            var rig = PlayerRig.Instance;
            if (rig == null) return;
            Vector3 eye = rig.Head.position;
            float d = Mathf.Max(minDistance, Vector3.Distance(eye, transform.position));
            float h = 2f * d * Mathf.Tan(degrees * 0.5f * Mathf.Deg2Rad);   // the formula
            transform.localScale = scaleAtStart * (h / baseHeight);
            transform.rotation = Quaternion.LookRotation(transform.position - eye);   // face the eye
        }
    }
}
