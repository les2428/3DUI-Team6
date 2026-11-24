using System;
using System.IO;
using System.Net.Http;
using UnityEditor;
using UnityEditor.SceneManagement;
using UnityEngine;

namespace ThreeDUI.Editor
{
    /// <summary>
    /// 3DUI > Build > Upload to course server: builds the open scene for the web (as Build for headset does) and uploads it
    /// to the course server, where it appears at https://&lt;server&gt;/teams/&lt;team number&gt;/ and on the gallery page.
    /// Upload again whenever you like: the newest upload replaces your space, the address stays the same.
    ///
    /// 3DUI > Build > Course server settings…: the server address, your team number and your team code (from the instructor).
    /// The code is kept in this computer's Unity preferences only — never in the scene, the project or the build.
    /// </summary>
    public static class CourseServerUpload
    {
        [Serializable]
        class Answer
        {
            public bool ok;
            public string error, url, team, name;
            public float mb;
            public bool locked;
            public string[] warnings = new string[0];
        }

        static string Key(string k) => "3DUI.CourseServer." + k + "." + Application.dataPath.GetHashCode().ToString("x8");
        public static string Server { get => EditorPrefs.GetString(Key("server"), ""); set => EditorPrefs.SetString(Key("server"), value.Trim().TrimEnd('/')); }
        public static string Team { get => EditorPrefs.GetString(Key("team"), ""); set => EditorPrefs.SetString(Key("team"), value.Trim()); }
        public static string Code { get => EditorPrefs.GetString(Key("code"), ""); set => EditorPrefs.SetString(Key("code"), value.Trim()); }
        public static bool Configured => Server.StartsWith("http") && Team.Length > 0 && Code.Length > 0;
        public static string SpaceUrl => $"{Server}/teams/{Team}/";

        [MenuItem("3DUI/Build/Upload to course server", priority = 204)]
        public static void UploadMenu()
        {
            if (!Configured) { CourseServerWindow.Open(); return; }
            var scene = EditorSceneManager.GetActiveScene();
            if (string.IsNullOrEmpty(scene.path)) { EditorUtility.DisplayDialog("3DUI", "Save your scene first (Ctrl+S).", "OK"); return; }
            if (!EditorSceneManager.SaveCurrentModifiedScenesIfUserWantsTo()) return;

            string zip = HeadsetUpload.Build(scene.path, out var info, out string error);
            if (zip == null) { EditorUtility.DisplayDialog("3DUI", "The build failed:\n" + error + "\n\nSee the Console.", "OK"); return; }
            try
            {
                EditorUtility.DisplayProgressBar("3DUI", $"Uploading {info.downloadMB:0.0} MB to {Server} …", 0.5f);
                var a = Upload(Server, Team, Code, zip);
                EditorUtility.ClearProgressBar();
                if (!a.ok) { EditorUtility.DisplayDialog("3DUI · Upload refused", a.error, "OK"); return; }
                string warn = a.warnings != null && a.warnings.Length > 0 ? "\n\nWarnings: " + string.Join("; ", a.warnings) : "";
                if (EditorUtility.DisplayDialog("3DUI · Your space is online",
                        $"Team {Team}: {Server}{a.url}\n({a.mb:0.0} MB){warn}\n\nThe same address works in the headset: open it there and press VR.",
                        "Open in browser", "Close"))
                    Application.OpenURL(Server + a.url);
            }
            finally { EditorUtility.ClearProgressBar(); }
        }

        [MenuItem("3DUI/Build/Course server settings…", priority = 205)]
        static void SettingsMenu() => CourseServerWindow.Open();

        [MenuItem("3DUI/Build/Open my space online", priority = 206)]
        static void OpenOnline()
        {
            if (!Configured) { CourseServerWindow.Open(); return; }
            Application.OpenURL(SpaceUrl);
        }

        static Answer Read(HttpResponseMessage r)
        {
            string body = r.Content.ReadAsStringAsync().GetAwaiter().GetResult();
            try { return JsonUtility.FromJson<Answer>(body) ?? new Answer { error = body }; }
            catch (ArgumentException) { return new Answer { error = $"{(int)r.StatusCode} {r.ReasonPhrase}" }; }
        }

        static HttpClient Client(int seconds) => new HttpClient { Timeout = TimeSpan.FromSeconds(seconds) };

        static Answer Upload(string server, string team, string code, string zipPath)
        {
            try
            {
                using (var http = Client(600))
                using (var file = File.OpenRead(zipPath))
                using (var req = new HttpRequestMessage(HttpMethod.Post, $"{server}/api/upload/{Uri.EscapeDataString(team)}"))
                {
                    req.Headers.Add("X-Team-Code", code);
                    req.Content = new StreamContent(file);
                    req.Content.Headers.ContentType = new System.Net.Http.Headers.MediaTypeHeaderValue("application/zip");
                    using (var r = http.SendAsync(req).GetAwaiter().GetResult()) return Read(r);
                }
            }
            catch (Exception e) { return new Answer { error = "Could not reach the server: " + (e.InnerException ?? e).Message }; }
        }

        /// <summary>Checks the server address, team number and code without uploading anything.</summary>
        public static bool Test(string server, string team, string code, out string message)
        {
            try
            {
                using (var http = Client(15))
                using (var req = new HttpRequestMessage(HttpMethod.Get, $"{server.TrimEnd('/')}/api/check/{Uri.EscapeDataString(team)}"))
                {
                    req.Headers.Add("X-Team-Code", code);
                    using (var r = http.SendAsync(req).GetAwaiter().GetResult())
                    {
                        var a = Read(r);
                        message = a.ok ? $"OK: team {a.team} ({a.name}).{(a.locked ? " Uploads are closed at the moment." : "")}" : a.error;
                        return a.ok;
                    }
                }
            }
            catch (Exception e) { message = "Could not reach the server: " + (e.InnerException ?? e).Message; return false; }
        }

        /// <summary>Command line: -executeMethod ThreeDUI.Editor.CourseServerUpload.UploadFromCommandLine
        /// -scene Assets/... -3duiServer https://... -3duiTeam 07 -3duiCode xxxx  (not -server: Unity reserves it)</summary>
        public static void UploadFromCommandLine()
        {
            var argv = Environment.GetCommandLineArgs();
            string Arg(string n) { int i = Array.IndexOf(argv, n); return i >= 0 && i + 1 < argv.Length ? argv[i + 1] : null; }
            string zip = HeadsetUpload.Build(Arg("-scene"), out _, out string error);
            if (zip == null) { Debug.LogError("[3DUI] " + error); EditorApplication.Exit(1); return; }
            var a = Upload(Arg("-3duiServer").TrimEnd('/'), Arg("-3duiTeam"), Arg("-3duiCode"), zip);
            if (a.ok) Debug.Log($"[3DUI] Uploaded to course server: {Arg("-3duiServer")}{a.url} ({a.mb:0.0} MB)");
            else Debug.LogError("[3DUI] Upload refused: " + a.error);
            EditorApplication.Exit(a.ok ? 0 : 1);
        }
    }

    public class CourseServerWindow : EditorWindow
    {
        string server, team, code, result;
        bool ok;

        public static void Open()
        {
            var w = GetWindow<CourseServerWindow>(true, "Course server", true);
            w.minSize = new Vector2(460, 250);
            w.server = CourseServerUpload.Server;
            w.team = CourseServerUpload.Team;
            w.code = CourseServerUpload.Code;
            if (string.IsNullOrEmpty(w.team))
            {
                var my = FindAnyObjectByType<MySpace>();
                var digits = my != null ? System.Text.RegularExpressions.Regex.Match(my.TeamId, @"\d+").Value : "";
                w.team = digits.Length > 0 ? digits.PadLeft(2, '0') : "";
            }
        }

        void OnGUI()
        {
            EditorGUILayout.HelpBox("Ask the instructor for the server address and your team code. " +
                                    "The code is saved on this computer only (not in the project or the build). Do not share it.", MessageType.Info);
            server = EditorGUILayout.TextField("Server address", server);
            team = EditorGUILayout.TextField("Team number", team);
            code = EditorGUILayout.PasswordField("Team code", code);
            EditorGUILayout.Space();
            using (new EditorGUILayout.HorizontalScope())
            {
                if (GUILayout.Button("Test connection"))
                    ok = CourseServerUpload.Test(server ?? "", team ?? "", code ?? "", out result);
                if (GUILayout.Button("Save"))
                {
                    CourseServerUpload.Server = server ?? ""; CourseServerUpload.Team = team ?? ""; CourseServerUpload.Code = code ?? "";
                    result = "Saved. Now: 3DUI > Build > Upload to course server."; ok = true;
                }
            }
            if (!string.IsNullOrEmpty(result)) EditorGUILayout.HelpBox(result, ok ? MessageType.None : MessageType.Error);
            if (CourseServerUpload.Configured)
                EditorGUILayout.LabelField("Your space:", CourseServerUpload.SpaceUrl, EditorStyles.wordWrappedLabel);
        }
    }
}
