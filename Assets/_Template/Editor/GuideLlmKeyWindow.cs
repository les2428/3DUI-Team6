using System.Collections.Generic;
using UnityEditor;
using UnityEngine;
using UnityEngine.Networking;

namespace ThreeDUI.Editor
{
    /// <summary>
    /// 3DUI > Guide Agent > LLM key (this computer)   — Tutorial 4, optional.
    /// Saves a FREE test key in EditorPrefs on THIS computer only: never in the scene, the project or a build,
    /// so it is not shared when you zip, upload or push the project. Then set the Guide Agent's Brain to LLM.
    /// </summary>
    public class GuideLlmKeyWindow : EditorWindow
    {
        private GuideLlmProvider provider = GuideLlmProvider.Gemini;
        private string key = "";
        private string status = "";
        private List<string> models;
        private Vector2 scroll;
        private UnityWebRequest pending;

        [MenuItem("3DUI/Guide Agent/LLM key (this computer)", priority = 172)]
        public static void ShowWindow()
        {
            var w = GetWindow<GuideLlmKeyWindow>(true, "Guide Agent - LLM key", true);
            w.minSize = new Vector2(500f, 380f);
            var agent = Object.FindAnyObjectByType<GuideAgent>();
            if (agent != null) w.provider = agent.provider;
            w.Load();
        }

        [MenuItem("3DUI/Guide Agent/Use the LLM brain (selected or first agent)", priority = 173)]
        private static void UseLlm() => SetBrain(GuideAgent.BrainKind.LLM);

        [MenuItem("3DUI/Guide Agent/Use the rules brain", priority = 174)]
        private static void UseRules() => SetBrain(GuideAgent.BrainKind.Rules);

        private static void SetBrain(GuideAgent.BrainKind b)
        {
            var agent = Selection.activeGameObject != null ? Selection.activeGameObject.GetComponentInParent<GuideAgent>() : null;
            if (agent == null) agent = Object.FindAnyObjectByType<GuideAgent>();
            if (agent == null) { EditorUtility.DisplayDialog("3DUI", "No Guide Agent in this scene. Open 3DUI > Examples > T4, or 3DUI > Create > Guide Agent.", "OK"); return; }
            Undo.RecordObject(agent, "Guide brain");
            agent.brain = b;
            EditorUtility.SetDirty(agent);
            Selection.activeGameObject = agent.gameObject;
            Debug.Log($"[3DUI] {agent.name}: Brain = {b}" + (b == GuideAgent.BrainKind.LLM && GuideLlm.GetKey(agent.provider) == "" ? " (no key yet: 3DUI > Guide Agent > LLM key)" : ""));
        }

        private void Load() { key = EditorPrefs.GetString(GuideLlm.PrefsKey(provider), ""); models = null; status = ""; }

        private void OnGUI()
        {
            EditorGUILayout.HelpBox(
                "Optional. Use a FREE test key. It is saved on this computer only (not in the project, the scene or a build).\n" +
                "Free tiers may use your prompts: do not type personal or confidential information.\n" +
                "Your web build never uses the LLM: it always runs the rules.", MessageType.Info);
            EditorGUI.BeginChangeCheck();
            provider = (GuideLlmProvider)EditorGUILayout.EnumPopup("Provider", provider);
            if (EditorGUI.EndChangeCheck()) Load();

            using (new EditorGUILayout.HorizontalScope())
            {
                if (provider == GuideLlmProvider.Gemini && GUILayout.Button("Get a free Gemini key (Google AI Studio)")) Application.OpenURL("https://aistudio.google.com/apikey");
                if (provider == GuideLlmProvider.Groq && GUILayout.Button("Get a free Groq key (GroqCloud)")) Application.OpenURL("https://console.groq.com/keys");
            }
            key = EditorGUILayout.PasswordField("API key", key);
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Save on this computer"))
                {
                    EditorPrefs.SetString(GuideLlm.PrefsKey(provider), key.Trim());
                    status = "Saved. Now: 3DUI > Guide Agent > Use the LLM brain, then Play.";
                    foreach (var a in Object.FindObjectsByType<GuideAgent>(FindObjectsSortMode.None))
                        if (a.provider != provider) { Undo.RecordObject(a, "Provider"); a.provider = provider; EditorUtility.SetDirty(a); }
                }
                if (GUILayout.Button("Remove")) { EditorPrefs.DeleteKey(GuideLlm.PrefsKey(provider)); key = ""; status = "Removed."; }
                GUI.enabled = pending == null && key.Trim() != "";
                if (GUILayout.Button("Test (list models)")) Test();
                GUI.enabled = true;
            }
            EditorGUILayout.LabelField("Or an environment variable:", GuideLlm.EnvVar(provider), EditorStyles.miniLabel);
            if (status != "") EditorGUILayout.HelpBox(status, status.StartsWith("HTTP") ? MessageType.Warning : MessageType.None);
            if (models == null) return;
            EditorGUILayout.LabelField($"Models this key can use ({models.Count}): click one to use it", EditorStyles.boldLabel);
            scroll = EditorGUILayout.BeginScrollView(scroll);
            foreach (var m in models)
                if (GUILayout.Button(m, EditorStyles.miniButton)) UseModel(m);
            EditorGUILayout.EndScrollView();
        }

        private void UseModel(string m)
        {
            var agents = Object.FindObjectsByType<GuideAgent>(FindObjectsSortMode.None);
            if (agents.Length == 0) { status = "No Guide Agent in the open scene."; return; }
            foreach (var a in agents) { Undo.RecordObject(a, "Model"); a.provider = provider; a.model = m; EditorUtility.SetDirty(a); }
            status = $"Guide Agent: {provider} · {m}";
        }

        private void Test()
        {
            models = null; status = "Testing...";
            pending = UnityWebRequest.Get(GuideLlm.BaseUrl(provider) + "/models");
            pending.SetRequestHeader("Authorization", "Bearer " + key.Trim());
            pending.timeout = 15;
            pending.SendWebRequest();
            EditorApplication.update += Poll;
        }

        private void Poll()
        {
            if (pending == null || !pending.isDone) return;
            EditorApplication.update -= Poll;
            if (pending.result == UnityWebRequest.Result.Success)
            {
                models = new List<string>();
                foreach (System.Text.RegularExpressions.Match m in System.Text.RegularExpressions.Regex.Matches(pending.downloadHandler.text, "\"id\"\\s*:\\s*\"([^\"]+)\""))
                    models.Add(m.Groups[1].Value.Replace("models/", ""));
                models.Sort();
                status = $"The key works. {models.Count} models.";
            }
            else status = GuideLlm.Explain(pending.responseCode, pending.error);
            pending.Dispose(); pending = null;
            Repaint();
        }
    }
}
