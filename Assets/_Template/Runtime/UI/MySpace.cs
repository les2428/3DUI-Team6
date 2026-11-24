using UnityEngine;

namespace ThreeDUI
{
    /// <summary>
    /// ★ MY SPACE: the few facts about your space, in one place at the top of the Hierarchy.
    /// Change them in the Inspector and the welcome panel (and the desktop help title) follow,
    /// in the editor as well as in Play mode.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    public class MySpace : MonoBehaviour
    {
        public enum TeamSize { Solo = 1, Pair = 2, Trio = 3 }

        [Header("Who you are")]
        [Tooltip("Your team name, e.g. Team 07.")]
        [SerializeField] private string teamName = "Team ??";
        [Tooltip("Sets how many viewpoints, interactive objects and panels the requirements ask for " +
                 "(3DUI > Check My Scene).")]
        [SerializeField] private TeamSize teamSize = TeamSize.Pair;

        [Header("What this space is")]
        [Tooltip("A short name for your space, e.g. The Listening Room.")]
        [SerializeField] private string spaceTitle = "TODO: name your space";
        [Tooltip("One or two sentences: what this place is and what a visitor can do here.")]
        [TextArea(2, 4)]
        [SerializeField] private string oneSentence =
            "TODO: one or two sentences telling a visitor what this place is and what they can do here.";

        [Header("Where it is shown")]
        [Tooltip("The panel a visitor reads first (1. ENTRY > Welcome panel).")]
        [SerializeField] private WorldPanel welcomePanel;

        public string TeamName => teamName;
        public TeamSize Size => teamSize;

        /// <summary>The team name as a folder / file name: "Team 07" -> "Team07".</summary>
        public string TeamId
        {
            get
            {
                var sb = new System.Text.StringBuilder();
                foreach (char c in teamName) if (char.IsLetterOrDigit(c) && c < 128) sb.Append(c);
                return sb.Length > 0 ? sb.ToString() : "MyTeam";
            }
        }
        public string SpaceTitle => spaceTitle;
        public string OneSentence => oneSentence;

        public void Set(string team, string title, string sentence)
        {
            teamName = team;
            spaceTitle = title;
            oneSentence = sentence;
            Apply();
        }

        private void OnEnable() => Apply();
        // Unity does not allow changing other objects inside OnValidate, so apply right after it.
        private void OnValidate()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.delayCall += () => { if (this != null) Apply(); };
#endif
        }

        private void Start()
        {
            if (!Application.isPlaying) return;
            var hud = FindAnyObjectByType<DesktopHUD>();
            if (hud != null) hud.SetTitle($"{teamName} · {spaceTitle}");
        }

        public void Apply()
        {
            if (welcomePanel == null) return;
            welcomePanel.SetText($"{teamName} - {spaceTitle}", oneSentence);
        }
    }
}
