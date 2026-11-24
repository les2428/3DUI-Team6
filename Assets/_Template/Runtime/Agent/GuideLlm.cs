using System;
using System.Collections;
using System.Collections.Generic;
using System.Text;
using UnityEngine;
using UnityEngine.Networking;

namespace ThreeDUI
{
    public enum GuideLlmProvider { Gemini, Groq, OpenAI }

    /// <summary>
    /// Tutorial 4 · step 2 of the pipeline with a real LLM (EDITOR ONLY).
    /// Any "OpenAI-compatible" chat API: POST {base}/chat/completions. Gemini, Groq and OpenAI accept the same request.
    ///
    /// The API key is NEVER in the scene, the project or a build. It is read from this computer only:
    ///   3DUI > Guide Agent > LLM key (this computer)     (saved in EditorPrefs)
    ///   or an environment variable: GEMINI_API_KEY / GROQ_API_KEY / OPENAI_API_KEY
    /// In a build GetKey() always returns "": everything in a web build can be downloaded, so the guide uses its rules.
    /// </summary>
    public static class GuideLlm
    {
        public static string BaseUrl(GuideLlmProvider p) => p switch
        {
            GuideLlmProvider.Gemini => "https://generativelanguage.googleapis.com/v1beta/openai",
            GuideLlmProvider.Groq => "https://api.groq.com/openai/v1",
            _ => "https://api.openai.com/v1",
        };

        public static string DefaultModel(GuideLlmProvider p) => p switch
        {
            GuideLlmProvider.Gemini => "gemini-3.5-flash-lite",
            GuideLlmProvider.Groq => "llama-3.3-70b-versatile",
            _ => "gpt-4o-mini",
        };

        public static string EnvVar(GuideLlmProvider p) => p switch
        {
            GuideLlmProvider.Gemini => "GEMINI_API_KEY",
            GuideLlmProvider.Groq => "GROQ_API_KEY",
            _ => "OPENAI_API_KEY",
        };

        public static string PrefsKey(GuideLlmProvider p) => "3DUI.GuideAgent.LlmKey." + p;

        /// <summary>The key on THIS computer (editor only). In a build: always "".</summary>
        public static string GetKey(GuideLlmProvider p)
        {
#if UNITY_EDITOR
            string key = UnityEditor.EditorPrefs.GetString(PrefsKey(p), "");
            if (string.IsNullOrEmpty(key)) key = Environment.GetEnvironmentVariable(EnvVar(p)) ?? "";
            return key.Trim();
#else
            return "";
#endif
        }

        /// <summary>The instructions: who the agent is, and the ONE JSON tool call it must answer with.</summary>
        public static string SystemPrompt(string agentName) =>
            $"You are {agentName}, a small guide robot in a 3D exhibition (desktop or VR headset). You have a body: you can walk, look, gesture and point.\n" +
            "Answer with ONE JSON object and nothing else:\n" +
            "{\"say\": \"one short sentence, at most 20 words\", \"gesture\": \"" + string.Join("|", GuideVocabulary.Gestures) + "\", " +
            "\"gaze\": \"visitor or a thing id\", \"action\": \"" + string.Join("|", GuideVocabulary.Actions) + "\", \"target\": \"a thing id or empty\"}\n" +
            "Rules: use only thing ids from the list. To show or explain a thing, use walk_to with its id (you point at it when you arrive). " +
            "If the visitor asks you to come, use come (you stop about 1.2 m away). When you talk about a thing, look at it (gaze = its id). Be friendly and brief.";

        /// <summary>What the LLM reads each turn: the scene as text, then what the visitor said.</summary>
        public static string UserMessage(string scene, string visitor) => scene + "\nThe visitor says: " + visitor;

        public static string BuildRequestJson(string model, string system, string user, bool jsonMode) =>
            "{\"model\":" + Quote(model) + ",\"messages\":[{\"role\":\"system\",\"content\":" + Quote(system) +
            "},{\"role\":\"user\",\"content\":" + Quote(user) + "}],\"temperature\":0.3" +
            (jsonMode ? ",\"response_format\":{\"type\":\"json_object\"}" : "") + "}";

        [Serializable] private class Msg { public string content; }
        [Serializable] private class Choice { public Msg message; }
        [Serializable] private class Completion { public Choice[] choices; }
        [Serializable] private class ErrInfo { public string message; }
        [Serializable] private class ErrBody { public ErrInfo error; }
        [Serializable] private class ModelInfo { public string id; }
        [Serializable] private class ModelList { public ModelInfo[] data; }

        /// <summary>Send one turn; calls done(reply, error) exactly once.</summary>
        public static IEnumerator Ask(GuideLlmProvider p, string model, string system, string user, Action<string, string> done)
        {
            string key = GetKey(p);
            if (key == "") { done("", "no API key on this computer (3DUI > Guide Agent > LLM key)"); yield break; }
            if (string.IsNullOrEmpty(model)) model = DefaultModel(p);
            bool jsonMode = true;
            for (int attempt = 0; attempt < 2; attempt++)
            {
                using var req = new UnityWebRequest(BaseUrl(p) + "/chat/completions", UnityWebRequest.kHttpVerbPOST)
                {
                    uploadHandler = new UploadHandlerRaw(Encoding.UTF8.GetBytes(BuildRequestJson(model, system, user, jsonMode))),
                    downloadHandler = new DownloadHandlerBuffer(),
                    timeout = 20,
                };
                req.SetRequestHeader("Content-Type", "application/json");
                req.SetRequestHeader("Authorization", "Bearer " + key);   // the key travels only in this header
                yield return req.SendWebRequest();
                string text = req.downloadHandler?.text ?? "";
                if (req.result == UnityWebRequest.Result.Success)
                {
                    string reply = "";
                    try { var c = JsonUtility.FromJson<Completion>(text); reply = c?.choices?.Length > 0 ? c.choices[0].message?.content ?? "" : ""; }
                    catch (Exception e) { done("", "unreadable reply: " + e.Message); yield break; }
                    done(reply, reply == "" ? "empty reply" : "");
                    yield break;
                }
                // Some models refuse response_format: retry once without it (the prompt still asks for JSON).
                if (jsonMode && req.responseCode == 400 && text.IndexOf("response_format", StringComparison.OrdinalIgnoreCase) >= 0) { jsonMode = false; continue; }
                done("", Explain(req.responseCode, ErrorMessage(text) ?? req.error));
                yield break;
            }
        }

        /// <summary>GET {base}/models: checks the key and lists the models it may use.</summary>
        public static IEnumerator ListModels(GuideLlmProvider p, string key, Action<List<string>, string> done)
        {
            using var req = UnityWebRequest.Get(BaseUrl(p) + "/models");
            req.SetRequestHeader("Authorization", "Bearer " + key);
            req.timeout = 15;
            yield return req.SendWebRequest();
            if (req.result != UnityWebRequest.Result.Success) { done(null, Explain(req.responseCode, ErrorMessage(req.downloadHandler?.text) ?? req.error)); yield break; }
            var list = new List<string>();
            try
            {
                var m = JsonUtility.FromJson<ModelList>(req.downloadHandler.text);
                if (m?.data != null) foreach (var d in m.data) list.Add(d.id.Replace("models/", ""));
            }
            catch (Exception e) { done(null, e.Message); yield break; }
            list.Sort();
            done(list, "");
        }

        public static string Explain(long code, string message)
        {
            string hint = code switch
            {
                400 => "bad request (check the model name)",
                401 or 403 => "the key was refused (check it, or the provider)",
                404 => "model not found (check the model name)",
                429 => "free-tier rate limit: wait a minute",
                0 => "no connection (internet? firewall?)",
                _ => code >= 500 ? "the provider has a problem: try again" : "",
            };
            return $"HTTP {code}{(hint != "" ? " - " + hint : "")}: {message}";
        }

        private static string ErrorMessage(string body)
        {
            if (string.IsNullOrEmpty(body)) return null;
            string b = body.Trim();
            if (b.StartsWith("[")) b = b.TrimStart('[').TrimEnd(']');   // Gemini sometimes wraps errors in an array
            try { var e = JsonUtility.FromJson<ErrBody>(b); if (!string.IsNullOrEmpty(e?.error?.message)) return e.error.message; } catch { }
            return body.Length > 200 ? body.Substring(0, 200) + "..." : body;
        }

        public static string Quote(string s)
        {
            if (s == null) return "\"\"";
            var sb = new StringBuilder("\"");
            foreach (char c in s)
            {
                switch (c)
                {
                    case '"': sb.Append("\\\""); break;
                    case '\\': sb.Append("\\\\"); break;
                    case '\n': sb.Append("\\n"); break;
                    case '\r': sb.Append("\\r"); break;
                    case '\t': sb.Append("\\t"); break;
                    default: if (c < 0x20) sb.Append("\\u").Append(((int)c).ToString("x4")); else sb.Append(c); break;
                }
            }
            return sb.Append('"').ToString();
        }
    }
}
