using System;

namespace ThreeDUI
{
    /// <summary>
    /// One decision of the guide agent: a "tool call" in JSON, the same shape an LLM agent would answer with:
    ///   { "say": "...", "gesture": "point", "gaze": "statue", "action": "walk_to", "target": "statue" }
    /// The brain decides WHAT to do; the body (GuideBody) knows HOW to do it.
    /// </summary>
    [Serializable]
    public class GuideAct
    {
        public string say = "";
        public string gesture = "none";
        public string gaze = "visitor";   // "visitor" | "away" | a thing id
        public string action = "none";
        public string target = "";        // a thing id, for walk_to / point_at / look_at

        public static GuideAct Say(string text, string gesture = "none") => new GuideAct { say = text, gesture = gesture };
    }

    /// <summary>The fixed vocabulary of the body (the "tools"). The parser rejects every other word.</summary>
    public static class GuideVocabulary
    {
        public static readonly string[] Gestures = { "none", "wave", "nod", "shake", "point", "think", "shrug", "bow" };
        public static readonly string[] Actions = { "none", "walk_to", "point_at", "look_at", "come" };

        public static bool Contains(string[] list, string value) => Array.IndexOf(list, value) >= 0;
    }
}
