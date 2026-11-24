using System.Collections;
using System.Linq;
using System.Text;
using UnityEngine;

namespace ThreeDUI
{
    /// <summary>
    /// Tutorial 4 · an AI agent with a body (lecture: Avatars, Embodiment and AI Agents).
    /// The pipeline is the one from the lecture, "from words to actions":
    ///   1 DescribeScene   the 3D scene -> text the brain can read (distance zone, what was clicked, the things)
    ///   2 Brain           text -> ONE tool call in JSON (GuideBrain: rules; or an LLM in the editor, see GuideLlm)
    ///   3 ParseReply      the engine checks the JSON: only known words, only things that exist (never trust the brain)
    ///   4 Realize         gaze, gesture and speech            (mutual gaze, joint attention)
    ///   5 Execute         the tools: walk_to, point_at, look_at, come
    ///   + KeepDistance    every frame: proxemics (Hall 1966), a stranger keeps the social zone (about 1.2 m)
    /// Click the agent to say hello; click a GuideThing to ask "what is this?".
    /// Brain = LLM (editor only, with a key on this computer): a box at the bottom of the Game view lets you type to it.
    /// Events (walked up, arrived) always use the rules, so the body behaves the same with either brain.
    /// </summary>
    [RequireComponent(typeof(GuideBody))]
    public class GuideAgent : Interactable
    {
        [Header("Agent")]
        public string agentName = "Nova";
        [Tooltip("How long the brain 'thinks' (an LLM needs about 1 s too). The agent looks away meanwhile.")]
        [Range(0f, 3f)] public float thinkSeconds = 0.8f;

        public enum BrainKind { Rules, LLM }
        [Header("Brain (step 2)")]
        [Tooltip("Rules: works everywhere, no key. LLM: editor only, needs a key on this computer (3DUI > Guide Agent > LLM key). A build always uses the rules.")]
        public BrainKind brain = BrainKind.Rules;
        public GuideLlmProvider provider = GuideLlmProvider.Gemini;
        [Tooltip("Empty = the provider's default model.")]
        public string model = "";

        [Header("Proxemics (Hall 1966)")]
        [Tooltip("A stranger keeps the social zone: stop this far from the visitor, and step back if they come closer.")]
        public float keepDistance = 1.2f;
        [Tooltip("Within this distance the visitor is 'with' the agent: it greets once.")]
        public float greetDistance = 3.6f;

        [Header("Gaze")]
        [Tooltip("Look at the visitor while listening and speaking.")]
        public bool mutualGaze = true;
        [Tooltip("Look at the thing being talked about, so the visitor sees what was understood.")]
        public bool jointAttention = true;

        [Header("Show the pipeline")]
        [Tooltip("A small panel under the speech bubble shows the last tool call (JSON).")]
        public bool showToolCall = true;
        [SerializeField] private WorldPanel toolCallPanel;

        public string LastReply { get; private set; } = "";
        public GuideAct LastAct { get; private set; }
        public bool Busy { get; private set; }
        /// <summary>"rules" or "LLM · model", or why the LLM was not used.</summary>
        public string LastBrain { get; private set; } = "";

        private GuideBody body;
        private GuideThing presented;
        private bool greeted;
        private float lookBackAt = float.MaxValue;

        private Vector3 Visitor => PlayerRig.Instance != null ? PlayerRig.Instance.FeetPosition : Vector3.zero;

        private void Awake()
        {
            body = GetComponent<GuideBody>();
            body.SetName(agentName);
            if (toolCallPanel != null) toolCallPanel.gameObject.SetActive(false);
        }

        private void OnEnable() => GuideThing.Clicked += OnThingClicked;

        protected override void OnDisable()
        {
            base.OnDisable();
            GuideThing.Clicked -= OnThingClicked;
        }

        protected override void OnActivate(InteractionContext ctx) => Ask("hello");
        private void OnThingClicked(GuideThing thing) => Ask("what is this?");

        private void Update()
        {
            var rig = PlayerRig.Instance;
            if (rig == null) return;
            body.UserHead = rig.Head.position;
            if (!greeted && Flat(transform.position, Visitor) < greetDistance) { greeted = true; Ask("[event] walked up"); }
            if (!body.IsWalking) KeepDistance();
            if (mutualGaze && !Busy && !body.IsWalking && Time.time > lookBackAt) { body.LookAtUser(); lookBackAt = float.MaxValue; }
        }

        /// <summary>One turn: the visitor said or did something.</summary>
        public void Ask(string visitorText)
        {
            if (Busy) return;
            StartCoroutine(Turn(visitorText));
        }

        private IEnumerator Turn(string visitorText)
        {
            Busy = true;
            string scene = DescribeScene();                                  // 1. perception -> text
            body.LookAway();                                                  //    thinking: look away
            body.PlayGesture("think", thinkSeconds + 3f);
            float t0 = Time.time;
            string reply = null;                                              // 2. text -> one JSON tool call
            if (brain == BrainKind.LLM && !visitorText.StartsWith("[event]"))
            {
                if (GuideLlm.GetKey(provider) == "") LastBrain = "rules (no LLM key on this computer)";
                else
                {
                    string user = GuideLlm.UserMessage(scene, visitorText);
                    Debug.Log($"[{agentName}] LLM prompt:\n{user}");
                    string err = "";
                    yield return GuideLlm.Ask(provider, model, GuideLlm.SystemPrompt(agentName), user, (r, e) => { reply = r; err = e; });
                    if (err != "") { Debug.LogWarning($"[{agentName}] LLM: {err}. Using the rules."); reply = null; LastBrain = "rules (LLM failed)"; }
                    else { Debug.Log($"[{agentName}] LLM reply: {reply}"); LastBrain = "LLM · " + (model != "" ? model : GuideLlm.DefaultModel(provider)); }
                }
            }
            else LastBrain = "rules";
            if (reply == null) reply = GuideBrain.Reply(scene, visitorText, agentName);
            float left = thinkSeconds - (Time.time - t0);
            if (left > 0f) yield return new WaitForSeconds(left);
            var act = ParseReply(reply);                                      // 3. check every word
            LastReply = reply; LastAct = act;
            ShowToolCall(reply);
            Realize(act);                                                     // 4. gaze, gesture, speech
            Execute(act);                                                     // 5. the tool
            Busy = false;
        }

        // ================================================================== 1. the scene as text

        public string DescribeScene()
        {
            var sb = new StringBuilder();
            float d = Flat(transform.position, Visitor);
            sb.AppendLine($"The visitor is {d:0.0} m from you ({Zone(d)} zone).");
            if (GuideThing.Selected != null) sb.AppendLine($"The visitor clicked {GuideThing.Selected.id} (\"this one\").");
            sb.AppendLine("Things you can talk about (id: name | what to say):");
            foreach (var t in GuideThing.All.OrderBy(t => Flat(transform.position, t.Center)))
                sb.AppendLine($"- {t.id}: {t.Name} | {t.description}");
            return sb.ToString();
        }

        /// <summary>Hall (1966): intimate &lt; 0.45 m, personal &lt; 1.2 m, social &lt; 3.6 m, public beyond.</summary>
        public static string Zone(float d) => d < 0.45f ? "intimate" : d < 1.2f ? "personal" : d < 3.6f ? "social" : "public";

        // ================================================================== 3. never trust the brain

        public static GuideAct ParseReply(string raw)
        {
            raw ??= "";
            int a = raw.IndexOf('{'), b = raw.LastIndexOf('}');
            if (a < 0 || b <= a) return GuideAct.Say(raw.Trim(), "shrug");                 // no JSON: just speech
            GuideAct act;
            try { act = JsonUtility.FromJson<GuideAct>(raw.Substring(a, b - a + 1)); }
            catch { return GuideAct.Say("Sorry, could you try again?", "shrug"); }
            if (act == null) return GuideAct.Say("Hmm?", "shrug");

            if (!GuideVocabulary.Contains(GuideVocabulary.Gestures, act.gesture)) act.gesture = "none";   // unknown word
            if (!GuideVocabulary.Contains(GuideVocabulary.Actions, act.action)) act.action = "none";
            if (act.target != "" && GuideThing.Find(act.target) == null) act.target = "";                // invented thing
            if (act.target == "" && act.action != "come") act.action = "none";                             // a tool needs a target
            if (act.gaze != "visitor" && act.gaze != "away" && GuideThing.Find(act.gaze) == null) act.gaze = "visitor";
            return act;
        }

        // ================================================================== 4. gaze, gesture, speech

        private void Realize(GuideAct act)
        {
            body.StopGesture();
            var thing = GuideThing.Find(act.gaze);
            if (thing != null && jointAttention) body.LookAt(thing.Center);   // joint attention: look at "this one"
            else if (mutualGaze) body.LookAtUser();                           // mutual gaze: look at the visitor
            else body.LookForward();
            if (act.action == "walk_to") return;                              // speak when there, not on the way
            body.PlayGesture(act.gesture);
            float speaking = body.Say(act.say);
            lookBackAt = Time.time + speaking + 0.5f;                         // then look back at the visitor
        }

        // ================================================================== 5. the tools

        private void Execute(GuideAct act)
        {
            var thing = GuideThing.Find(act.target);
            switch (act.action)
            {
                case "walk_to":                                               // stand beside it, on the visitor's side
                    presented = thing;
                    body.Say(act.say);
                    body.WalkTo(StandPoint(thing), 0.05f, () => Ask("[event] arrived at " + thing.id));
                    break;
                case "point_at":
                    presented = thing;
                    body.LookAt(thing.Center);
                    body.PointAt(thing.Center, 3f);
                    break;
                case "look_at":
                    body.LookAt(thing.Center);
                    break;
                case "come":                                                  // walk_to(visitor, stop = 1.2 m)
                    presented = null;
                    body.WalkTo(Visitor, keepDistance + 0.1f, () => body.LookAtUser());
                    break;
            }
        }

        /// <summary>Beside the thing, 0.9 m from it, turned 70° from the line to the visitor so nobody blocks the view.</summary>
        private Vector3 StandPoint(GuideThing thing)
        {
            Vector3 c = thing.Center; c.y = transform.position.y;
            Vector3 toVisitor = Visitor - c; toVisitor.y = 0f;
            if (toVisitor.sqrMagnitude < 0.01f) toVisitor = -thing.transform.forward;
            Vector3 side = Quaternion.AngleAxis(-70f, Vector3.up) * toVisitor.normalized;   // the thing ends up on its right: the pointing hand
            return c + side * 0.9f;
        }

        // ================================================================== + proxemics, every frame

        private void KeepDistance()
        {
            Vector3 toVisitor = Visitor - transform.position; toVisitor.y = 0f;
            float d = toVisitor.magnitude;
            if (d < 0.01f) return;
            if (d < keepDistance && !body.IsStepping)                         // too close for a stranger: step back
                body.Step(-toVisitor.normalized * (keepDistance - d + 0.2f));
            // Face the visitor; when showing a thing, face between the visitor and the thing (a shared space).
            Vector3 face = Visitor;
            if (presented != null) face = Vector3.Lerp(Visitor, presented.Center, 0.5f);
            body.TurnTowards(face, 120f);
        }

        private void ShowToolCall(string reply)
        {
            if (toolCallPanel == null) return;
            toolCallPanel.gameObject.SetActive(showToolCall);
            if (showToolCall) toolCallPanel.SetText($"Tool call (JSON) · {LastBrain}",reply.Replace(",\"", ",\n\""));
        }

        // ================================================================== editor only: type to the LLM brain

        private string typed = "";

        private void OnGUI()
        {
            if (!Application.isEditor || brain != BrainKind.LLM) return;
            const string id = "guide-ask";
            float w = Mathf.Min(560f, Screen.width - 40f);
            var r = new Rect((Screen.width - w) / 2f, Screen.height - 46f, w, 28f);
            GUI.Box(new Rect(r.x - 6f, r.y - 6f, r.width + 12f, r.height + 12f), GUIContent.none);
            GUI.Label(new Rect(r.x, r.y + 4f, 90f, 22f), $"Ask {agentName}:");
            var e = Event.current;
            bool enter = e.type == EventType.KeyDown && (e.keyCode == KeyCode.Return || e.keyCode == KeyCode.KeypadEnter)
                         && GUI.GetNameOfFocusedControl() == id;
            GUI.SetNextControlName(id);
            typed = GUI.TextField(new Rect(r.x + 90f, r.y, r.width - 90f, r.height), typed, 200);
            if (e.type == EventType.Repaint) DesktopInput.TypingElsewhere = GUI.GetNameOfFocusedControl() == id;   // WASD off while typing
            if (enter && typed.Trim() != "") { Ask(typed.Trim()); typed = ""; GUI.FocusControl(null); e.Use(); }
        }

        public static float Flat(Vector3 a, Vector3 b) { a.y = 0f; b.y = 0f; return Vector3.Distance(a, b); }
    }
}
