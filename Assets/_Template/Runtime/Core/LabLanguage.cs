using System;
using UnityEngine;

namespace ThreeDUI
{
    public enum LabLang { English, Korean }

    /// <summary>
    /// The language the visitor picked at the entrance. Everything shown after that follows it:
    /// signs and panels (LocalizedPanel), hover labels (LocalizedLabel), the on-screen help and chat,
    /// and the language the agents speak and reply in.
    ///
    /// The choice is remembered per browser, so a returning visitor is not asked again.
    /// </summary>
    public static class LabLanguage
    {
        private const string PrefKey = "3dui.language";

        private static LabLang current = LabLang.English;

        /// <summary>Raised after the language changes; listeners re-apply their text.</summary>
        public static event Action<LabLang> Changed;

        /// <summary>True once the visitor has picked (or a remembered choice was loaded).</summary>
        public static bool Chosen { get; private set; }

        public static LabLang Current => current;
        public static bool IsKorean => current == LabLang.Korean;

        /// <summary>Two-letter code for the agent server ("en" / "ko").</summary>
        public static string Code => current == LabLang.Korean ? "ko" : "en";

        /// <summary>BCP-47 tag for speech synthesis and recognition.</summary>
        public static string SpeechCode => current == LabLang.Korean ? "ko-KR" : "en-US";

        /// <summary>Pick one of two strings. The one call every piece of localized text goes through.</summary>
        public static string Pick(string english, string korean)
        {
            if (current == LabLang.Korean) return string.IsNullOrEmpty(korean) ? english : korean;
            return string.IsNullOrEmpty(english) ? korean : english;
        }

        public static void Set(LabLang lang, bool remember = true)
        {
            bool changed = lang != current || !Chosen;
            current = lang;
            Chosen = true;
            if (remember)
            {
                try
                {
                    PlayerPrefs.SetString(PrefKey, lang.ToString());
                    PlayerPrefs.Save();
                }
                catch (Exception e) { Debug.LogWarning("[3DUI] Could not remember the language: " + e.Message); }
            }
            if (changed) Changed?.Invoke(current);
        }

        /// <summary>Forget the choice, so the entrance asks again (used by the "change language" sign).</summary>
        public static void Reset()
        {
            Chosen = false;
            try { PlayerPrefs.DeleteKey(PrefKey); PlayerPrefs.Save(); } catch { /* private mode */ }
        }

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Load()
        {
            Chosen = false;
            current = LabLang.English;
            string saved = null;
            try { saved = PlayerPrefs.GetString(PrefKey, ""); } catch { /* private mode */ }
            if (!string.IsNullOrEmpty(saved) && Enum.TryParse(saved, out LabLang lang))
            {
                current = lang;
                Chosen = true;
            }
            else if (SystemLanguage.Korean == Application.systemLanguage)
            {
                // Only a starting suggestion - the visitor is still asked.
                current = LabLang.Korean;
            }
        }
    }
}
