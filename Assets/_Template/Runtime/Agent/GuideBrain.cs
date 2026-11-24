using System.Linq;
using System.Text.RegularExpressions;

namespace ThreeDUI
{
    /// <summary>
    /// A rule-based brain. Like an LLM it sees ONLY text (the scene description and what the visitor asked)
    /// and answers with ONE JSON tool call. That is why it can be swapped for a real LLM without touching the body
    /// (GuideAgent > Brain = LLM, editor only). A build has no API key, so it always uses these rules.
    /// </summary>
    public static class GuideBrain
    {
        public static string Reply(string scene, string visitor, string agentName = "Nova")
        {
            string v = visitor.ToLowerInvariant();
            string clicked = Regex.Match(scene, @"clicked ([a-z0-9_]+)").Groups[1].Value;
            var things = Regex.Matches(scene, @"^- ([a-z0-9_]+): ([^|\n]+)\|([^\n]*)", RegexOptions.Multiline)
                              .Cast<Match>().ToDictionary(m => m.Groups[1].Value, m => m.Groups[3].Value.Trim());
            bool far = scene.Contains("(public zone)");

            if (v.Contains("[event] walked up"))
                return Json($"Hi, I'm {agentName}. Click a thing to ask me.", "wave");
            if (v.Contains("hello"))
                return far ? Json("Hello! I'm coming over.", "wave", "visitor", "come")
                           : Json("Hello! Click a thing to ask me.", "wave");
            if (v.Contains("[event] arrived"))
            {
                string at = Regex.Match(v, @"arrived at ([a-z0-9_]+)").Groups[1].Value;   // where the walk ended
                if (at == "") at = clicked;
                if (things.TryGetValue(at, out var said)) return Json(said, "point", at, "point_at", at);
            }
            if (v.Contains("what is this"))
            {
                if (clicked == "" || !things.ContainsKey(clicked))
                    return Json("Which one do you mean? Click it.", "shrug");
                return Json("Let me show you.", "nod", clicked, "walk_to", clicked);
            }
            return Json("I'm a simple guide. Click a thing and I'll tell you about it.", "think");
        }

        private static string Json(string say, string gesture, string gaze = "visitor", string action = "none", string target = "") =>
            "{\"say\":\"" + say.Replace("\"", "'") + "\",\"gesture\":\"" + gesture + "\",\"gaze\":\"" + gaze +
            "\",\"action\":\"" + action + "\",\"target\":\"" + target + "\"}";
    }
}
