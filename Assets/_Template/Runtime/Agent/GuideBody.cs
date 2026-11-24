using System;
using System.Collections;
using UnityEngine;

namespace ThreeDUI
{
    /// <summary>
    /// The guide agent's body: the "realisation" layer. It knows HOW to do things; it never decides WHAT to do.
    /// (Ported from the graduate Agent Lab.)
    ///
    /// Hybrid animation (what most embodied-agent systems do):
    ///   * prepared clips for whole-body motion   Idle, Talk, Walk                 (Quaternius, CC0)
    ///   * procedural motion on top, every frame   gaze (look-at IK), pointing/waving/thinking (hand IK),
    ///                                             nod / shake / shrug / bow / emotion posture (bone offsets)
    /// A clip cannot know where the user stands or which mug to point at; IK can. So the parts that depend
    /// on the situation are procedural, and everything else is a clip.
    /// </summary>
    public class GuideBody : MonoBehaviour
    {
        public enum GazeMode { User, Away, Point, Forward }

        [Header("Model")]
        [SerializeField] private Animator animator;
        [SerializeField] private Transform bubbleAnchor;
        [SerializeField] private WorldPanel bubble;

        [Header("Motion")]
        [SerializeField] private float walkSpeed = 1.1f;
        [SerializeField] private float turnSpeed = 220f;     // degrees per second
        [Tooltip("Ground speed of the walk clip itself; playback speed is scaled to the real speed (less foot sliding).")]
        [SerializeField] private float walkClipSpeed = 1.25f;
        [SerializeField] private float charactersPerSecond = 14f;

        [Header("Gaze")]
        [SerializeField, Range(0f, 1f)] private float lookWeight = 0.85f;

        [Header("Non-humanoid rigs (e.g. the robot)")]
        [Tooltip("Without a humanoid avatar, gaze and pointing turn these bones directly after the animation.")]
        [SerializeField] private Transform genericHead;
        [SerializeField] private Transform genericUpperArm, genericLowerArm;

        public string Emotion { get; private set; } = "neutral";
        public string Gesture { get; private set; } = "none";
        public bool IsTalking => Time.time < talkingUntil;
        public bool IsWalking => walking;
        public GazeMode Gaze { get; private set; } = GazeMode.User;
        public Vector3 GazePoint { get; private set; }
        public float Speed { get; private set; }
        public Vector3 HeadPosition => head != null ? head.position : transform.position + Vector3.up * 1.6f;
        public Animator Animator => animator;

        /// <summary>Where the visitor's eyes are (the camera); set by GuideAgent every frame.</summary>
        public Vector3 UserHead { get; set; }

        private Transform head, neck, spine, rightShoulder, leftShoulder;
        private float talkingUntil, gestureUntil, gestureStart;
        private Vector3 pointTarget;
        private bool walking;
        private Vector3 walkGoal;
        private float stopDistance;
        private Action onArrive;
        private Coroutine typing;
        private string state = "";
        private float lookBlend;

        private void Awake()
        {
            if (animator == null) animator = GetComponentInChildren<Animator>();
            if (animator != null && animator.isHuman)
            {
                head = animator.GetBoneTransform(HumanBodyBones.Head);
                neck = animator.GetBoneTransform(HumanBodyBones.Neck);
                spine = animator.GetBoneTransform(HumanBodyBones.Chest) ?? animator.GetBoneTransform(HumanBodyBones.Spine);
                rightShoulder = animator.GetBoneTransform(HumanBodyBones.RightShoulder);
                leftShoulder = animator.GetBoneTransform(HumanBodyBones.LeftShoulder);
                animator.applyRootMotion = false;
            }
            else head = genericHead;
            if (bubble != null) bubble.gameObject.SetActive(false);
        }

        // ================================================================== API used by the realiser and the actions

        /// <summary>Show the text in the speech bubble and play the talking clip for as long as it takes to read.</summary>
        public float Say(string text)
        {
            if (string.IsNullOrEmpty(text)) return 0f;
            float duration = Mathf.Clamp(text.Length / charactersPerSecond, 1.2f, 12f);
            talkingUntil = Time.time + duration;
            if (bubble != null)
            {
                if (typing != null) StopCoroutine(typing);
                typing = StartCoroutine(TypeOut(text, duration));
            }
            return duration;
        }

        public void SetEmotion(string emotion) => Emotion = string.IsNullOrEmpty(emotion) ? "neutral" : emotion;

        /// <summary>Play a gesture from the fixed vocabulary (GuideVocabulary.Gestures). Unknown names are ignored.</summary>
        public void PlayGesture(string gesture, float seconds = 1.8f)
        {
            if (string.IsNullOrEmpty(gesture) || gesture == "none" || !GuideVocabulary.Contains(GuideVocabulary.Gestures, gesture)) return;
            Gesture = gesture;
            gestureStart = Time.time;
            gestureUntil = Time.time + seconds;
        }

        public void LookAtUser() => Gaze = GazeMode.User;
        public void LookForward() => Gaze = GazeMode.Forward;
        public void LookAt(Vector3 point) { Gaze = GazeMode.Point; GazePoint = point; }

        /// <summary>Look up and to the side - what people do while they think of what to say (gaze aversion).</summary>
        public void LookAway()
        {
            Gaze = GazeMode.Away;
            Vector3 side = transform.right * -0.6f + transform.up * 0.35f + transform.forward;
            GazePoint = HeadPosition + side.normalized * 2f;
        }

        public void PointAt(Vector3 point, float seconds = 2.5f)
        {
            pointTarget = point;
            PlayGesture("point", seconds);
        }

        /// <summary>Walk in a straight line to a floor point; onArrive runs once when there.</summary>
        public void WalkTo(Vector3 floorPoint, float stopAt = 0.05f, Action arrived = null)
        {
            floorPoint.y = transform.position.y;
            walkGoal = floorPoint;
            stopDistance = stopAt;
            onArrive = arrived;
            walking = true;
        }

        public void Stop() { walking = false; onArrive = null; }

        /// <summary>End the current gesture now (e.g. the "think" pose held while waiting for the LLM).</summary>
        public void StopGesture() { if (Gesture != "none") gestureUntil = Mathf.Min(gestureUntil, Time.time + 0.3f); }

        /// <summary>A small step without turning (e.g. backing away while still facing the user).</summary>
        public void Step(Vector3 worldDelta, float seconds = 0.6f)
        {
            worldDelta.y = 0f;
            if (stepping != null) StopCoroutine(stepping);
            stepping = StartCoroutine(StepRoutine(worldDelta, seconds));
        }

        public bool IsStepping => stepping != null;
        private Coroutine stepping;

        private IEnumerator StepRoutine(Vector3 delta, float seconds)
        {
            Vector3 start = transform.position;
            for (float t = 0f; t < seconds; t += Time.deltaTime)
            {
                float k = Mathf.SmoothStep(0f, 1f, t / seconds);
                transform.position = start + delta * k;
                yield return null;
            }
            transform.position = start + delta;
            stepping = null;
        }

        /// <summary>Turn the whole body toward a point (yaw only).</summary>
        public bool TurnTowards(Vector3 point, float degPerSec = -1f)
        {
            Vector3 d = point - transform.position; d.y = 0f;
            if (d.sqrMagnitude < 0.0001f) return true;
            var goal = Quaternion.LookRotation(d.normalized, Vector3.up);
            transform.rotation = Quaternion.RotateTowards(transform.rotation, goal, (degPerSec > 0 ? degPerSec : turnSpeed) * Time.deltaTime);
            return Quaternion.Angle(transform.rotation, goal) < 3f;
        }

        // ================================================================== per frame

        private void Update()
        {
            if (Gesture != "none" && Time.time > gestureUntil) Gesture = "none";

            // locomotion: turn first, then walk (people do not walk sideways)
            Speed = 0f;
            if (walking)
            {
                Vector3 to = walkGoal - transform.position; to.y = 0f;
                float dist = to.magnitude;
                if (dist <= Mathf.Max(0.05f, stopDistance))
                {
                    walking = false;
                    var cb = onArrive; onArrive = null; cb?.Invoke();
                }
                else
                {
                    bool facing = TurnTowards(walkGoal);
                    float angle = Vector3.Angle(transform.forward, to);
                    if (facing || angle < 60f)
                    {
                        Speed = Mathf.Min(walkSpeed, dist * 2f + 0.3f);
                        transform.position += transform.forward * Speed * Time.deltaTime * Mathf.Clamp01(1.2f - angle / 60f);
                    }
                }
            }

            if (animator == null || animator.runtimeAnimatorController == null) return;
            // A rig with its own gesture clips (the robot: Wave, Yes, No) plays them; otherwise gestures are procedural.
            string clip = Gesture == "wave" ? "Wave" : Gesture == "nod" ? "Yes" : Gesture == "shake" ? "No" : null;
            bool hasClip = clip != null && animator.HasState(0, Animator.StringToHash(clip));
            string want = Speed > 0.05f ? "Walk" : hasClip ? clip : IsTalking ? "Talk" : "Idle";
            if (want != state) { state = want; animator.CrossFadeInFixedTime(want, 0.25f, 0); }
            animator.speed = state == "Walk" ? Mathf.Clamp(Speed / walkClipSpeed, 0.4f, 1.5f) : 1f;
        }

        // Animator IK pass (forwarded by GuideIKRelay, because the Animator sits on the model child).
        public void ApplyIK()
        {
            if (animator == null) return;
            Vector3? look = Gaze switch
            {
                GazeMode.User => UserHead,
                GazeMode.Away or GazeMode.Point => GazePoint,
                _ => (Vector3?)null,
            };
            lookBlend = Mathf.MoveTowards(lookBlend, look.HasValue ? 1f : 0f, Time.deltaTime * 3f);
            if (look.HasValue)
            {
                animator.SetLookAtWeight(lookWeight * lookBlend, 0.2f, 0.85f, 1f, 0.5f);
                animator.SetLookAtPosition(look.Value);
            }
            else animator.SetLookAtWeight(0f);

            var shoulder = rightShoulder != null ? rightShoulder.position : transform.position + Vector3.up * 1.4f;
            Vector3 goal = Vector3.zero;
            float w = 0f;
            float t = Time.time - gestureStart;
            float fade = Mathf.Clamp01(t / 0.3f) * Mathf.Clamp01((gestureUntil - Time.time) / 0.3f);
            switch (Gesture)
            {
                case "point":
                {
                    // keep the hand in front of the body: clamp the direction to a cone around 'forward'
                    Vector3 dir = pointTarget - shoulder;
                    Vector3 flat = Vector3.ProjectOnPlane(dir, Vector3.up);
                    float yaw = Vector3.SignedAngle(transform.forward, flat, Vector3.up);
                    float pitch = Mathf.Atan2(dir.y, flat.magnitude) * Mathf.Rad2Deg;
                    Vector3 d = Quaternion.AngleAxis(Mathf.Clamp(yaw, -25f, 75f), Vector3.up) * transform.forward;
                    d = Quaternion.AngleAxis(-Mathf.Clamp(pitch, -40f, 40f), Vector3.Cross(Vector3.up, d)) * d;
                    goal = shoulder + d.normalized * 0.6f;
                    w = fade * Mathf.Clamp01(Mathf.InverseLerp(110f, 80f, Mathf.Abs(yaw)));
                    break;
                }
                case "wave":
                    goal = shoulder + transform.up * 0.42f + transform.right * (0.28f + 0.1f * Mathf.Sin(t * 11f)) + transform.forward * 0.12f;
                    w = fade;
                    break;
                case "think":   // hand to the chin
                    goal = HeadPosition - transform.up * 0.13f + transform.forward * 0.12f + transform.right * 0.02f;
                    w = fade;
                    break;
            }
            animator.SetIKPositionWeight(AvatarIKGoal.RightHand, w);
            if (w > 0f) animator.SetIKPosition(AvatarIKGoal.RightHand, goal);
        }

        // Bone offsets after the animation: head gestures and emotional posture.
        private void LateUpdate()
        {
            if (head == null) return;
            float t = Time.time - gestureStart;
            float fade = Mathf.Clamp01(t / 0.2f) * Mathf.Clamp01((gestureUntil - Time.time) / 0.3f);

            Vector3 headEuler = Vector3.zero;   // x = down, y = right, z = tilt
            Vector3 spineEuler = Vector3.zero;
            float shoulderUp = 0f;
            switch (Emotion)
            {
                case "happy": headEuler += new Vector3(-4f, 0f, 3f); break;
                case "sad": headEuler += new Vector3(14f, 0f, 0f); spineEuler += new Vector3(6f, 0f, 0f); break;
                case "surprised": headEuler += new Vector3(-8f, 0f, 0f); spineEuler += new Vector3(-4f, 0f, 0f); break;
                case "thinking": headEuler += new Vector3(-3f, 0f, 8f); break;
            }
            bool generic = animator == null || !animator.isHuman;
            if (generic) GenericGazeAndPoint(t);
            switch (Gesture)
            {
                case "nod" when !generic: headEuler += new Vector3(Mathf.Sin(t * 11f) * 13f * fade, 0f, 0f); break;
                case "shake" when !generic: headEuler += new Vector3(0f, Mathf.Sin(t * 11f) * 18f * fade, 0f); break;
                case "bow": spineEuler += new Vector3(28f * fade, 0f, 0f); headEuler += new Vector3(12f * fade, 0f, 0f); break;
                case "shrug": shoulderUp = 14f * fade * Mathf.Clamp01(Mathf.Sin(Mathf.Min(t, 1.4f) / 1.4f * Mathf.PI) * 1.5f); headEuler += new Vector3(0f, 0f, 6f * fade); break;
            }
            Apply(head, headEuler);
            if (spine != null) Apply(spine, spineEuler);
            if (shoulderUp != 0f)
            {
                if (rightShoulder != null) rightShoulder.rotation = Quaternion.AngleAxis(-shoulderUp, transform.forward) * rightShoulder.rotation;
                if (leftShoulder != null) leftShoulder.rotation = Quaternion.AngleAxis(shoulderUp, transform.forward) * leftShoulder.rotation;
            }

            if (bubble != null && bubble.gameObject.activeSelf)
            {
                if (bubbleAnchor != null) bubble.transform.position = bubbleAnchor.position;
                Vector3 away = bubble.transform.position - UserHead; away.y = 0f;
                if (away.sqrMagnitude > 0.01f) bubble.transform.rotation = Quaternion.LookRotation(away.normalized, Vector3.up);
            }
        }

        // Without a humanoid avatar there is no Animator IK: turn the head toward the gaze point, and aim the right
        // upper arm at the pointing target, directly on the bones after the animation has posed them.
        private float genericLook, genericPoint;

        private void GenericGazeAndPoint(float t)
        {
            Vector3? look = Gaze switch
            {
                GazeMode.User => UserHead,
                GazeMode.Away or GazeMode.Point => GazePoint,
                _ => (Vector3?)null,
            };
            genericLook = Mathf.MoveTowards(genericLook, look.HasValue ? 1f : 0f, Time.deltaTime * 3f);
            if (genericHead != null && look.HasValue && genericLook > 0f)
            {
                Vector3 dir = look.Value - genericHead.position;
                // limit to what a neck can do: 70° to the side, 35° up or down
                Vector3 local = transform.InverseTransformDirection(dir.normalized);
                float yaw = Mathf.Clamp(Mathf.Atan2(local.x, local.z) * Mathf.Rad2Deg, -70f, 70f);
                float pitch = Mathf.Clamp(Mathf.Asin(Mathf.Clamp(local.y, -1f, 1f)) * Mathf.Rad2Deg, -35f, 35f);
                var turn = Quaternion.AngleAxis(yaw, transform.up) * Quaternion.AngleAxis(-pitch, transform.right);
                genericHead.rotation = Quaternion.Slerp(Quaternion.identity, turn, lookWeight * genericLook) * genericHead.rotation;
            }

            bool pointing = Gesture == "point";
            genericPoint = Mathf.MoveTowards(genericPoint, pointing ? 1f : 0f, Time.deltaTime * 4f);
            if (genericUpperArm != null && genericLowerArm != null && genericPoint > 0f)
            {
                Vector3 now = genericLowerArm.position - genericUpperArm.position;
                Vector3 want = pointTarget - genericUpperArm.position;
                var aim = Quaternion.FromToRotation(now, want);
                genericUpperArm.rotation = Quaternion.Slerp(Quaternion.identity, aim, genericPoint) * genericUpperArm.rotation;
            }
        }

        // Rotate a bone about the body's own axes, so "down" means down for this character whatever its rig.
        private void Apply(Transform bone, Vector3 euler)
        {
            if (euler == Vector3.zero) return;
            bone.rotation = Quaternion.AngleAxis(euler.y, transform.up) * Quaternion.AngleAxis(euler.x, transform.right)
                            * Quaternion.AngleAxis(euler.z, transform.forward) * bone.rotation;
        }

        private IEnumerator TypeOut(string text, float duration)
        {
            bubble.gameObject.SetActive(true);
            bubble.BodyText.gameObject.SetActive(true);   // WorldPanel hides an empty body
            float t = 0f;
            float typeTime = Mathf.Min(duration * 0.6f, text.Length / 40f);
            while (t < typeTime)
            {
                t += Time.deltaTime;
                int n = Mathf.Clamp(Mathf.CeilToInt(text.Length * t / typeTime), 0, text.Length);
                bubble.BodyText.text = text.Substring(0, n);
                yield return null;
            }
            bubble.BodyText.text = text;
            yield return new WaitForSeconds(Mathf.Max(2.5f, duration - typeTime + 2f));
            bubble.gameObject.SetActive(false);
            typing = null;
        }

        /// <summary>Called by GuideAgent when the name tag should change.</summary>
        public void SetName(string agentName) { if (bubble != null) bubble.TitleText.text = agentName; }
    }
}
