using UnityEngine;

namespace ThreeDUI.ApplyTheory
{
    // Same code as Tutorials/Undergrad_T4/ApplyTheory/MyProximityReveal.cs.
    // Tutorial 4 · applying the theory: details on demand, with hysteresis.
    //   distance     d = |head − p|                        Vector3.Distance
    //   show         d < showWithin (2.5 m)
    //   hysteresis   hide only when d > hideBeyond (3.2 m); in between, keep the last state
    // Untick "Hysteresis" and it hides at 2.5 m too: one number. In the headset your head always sways a little,
    // so standing at the edge the panel flickers. Put two copies side by side (one ticked, one not) and compare them in ONE build.
    // Add it to an empty above an object; its first child (a panel) is what appears.
    public class MyProximityReveal : MonoBehaviour
    {
        public GameObject target;             // what to show; empty = the first child
        public float showWithin = 2.5f;       // show when closer than this (m)
        public float hideBeyond = 3.2f;       // hide only when farther than this (m)
        public bool hysteresis = true;        // untick: one number (it flickers at the edge)
        bool visible;

        void Start()
        {
            if (target == null && transform.childCount > 0) target = transform.GetChild(0).gameObject;
            if (target != null) target.SetActive(false);
        }

        void Update()
        {
            var rig = PlayerRig.Instance;
            if (rig == null || target == null) return;
            float d = Vector3.Distance(rig.Head.position, transform.position);   // d = |head − p|
            float hide = hysteresis ? hideBeyond : showWithin;                   // two numbers, or one
            if (!visible && d < showWithin) visible = true;                      // come close: show
            else if (visible && d > hide) visible = false;                       // walk away: hide
            if (target.activeSelf != visible) target.SetActive(visible);
        }
    }
}
