using System.Collections.Generic;
using UnityEngine;

namespace ThreeDUI
{
    /// <summary>
    /// Attribution for a third-party asset (e.g. "3D avatar created with Avaturn (avaturn.me), modified").
    /// Put it on the asset's object; all active credits are listed on the desktop HUD (press C) and can be
    /// read from code via AssetCredit.All. Also record them in CREDITS.md.
    /// </summary>
    public class AssetCredit : MonoBehaviour
    {
        private static readonly List<AssetCredit> all = new List<AssetCredit>();
        public static IReadOnlyList<AssetCredit> All => all;
        public static event System.Action Changed;

        [TextArea(1, 3)]
        [SerializeField] private string text = "Asset by ... (license)";
        [SerializeField] private string url = "";

        public string Text { get => text; set => text = value; }
        public string Url { get => url; set => url = value; }

        private void OnEnable()
        {
            all.Add(this);
            Changed?.Invoke();
        }

        private void OnDisable()
        {
            all.Remove(this);
            Changed?.Invoke();
        }

        public static string Compose()
        {
            var seen = new HashSet<string>();
            var sb = new System.Text.StringBuilder();
            foreach (var c in all)
            {
                string line = string.IsNullOrEmpty(c.url) ? c.text : $"{c.text}  ({c.url})";
                if (seen.Add(line)) sb.AppendLine(line);
            }
            return sb.ToString().TrimEnd();
        }
    }
}
