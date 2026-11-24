using UnityEngine;

namespace ThreeDUI
{
    /// <summary>
    /// Holds both language versions of a panel's text and writes the right one into the WorldPanel
    /// whenever the visitor's choice changes. Put it on the same object as the WorldPanel.
    /// </summary>
    [RequireComponent(typeof(WorldPanel))]
    public class LocalizedPanel : MonoBehaviour
    {
        [SerializeField] private string titleEn = "";
        [TextArea(2, 8)]
        [SerializeField] private string bodyEn = "";
        [SerializeField] private string titleKo = "";
        [TextArea(2, 8)]
        [SerializeField] private string bodyKo = "";

        private WorldPanel panel;

        public void Set(string englishTitle, string englishBody, string koreanTitle, string koreanBody)
        {
            titleEn = englishTitle; bodyEn = englishBody;
            titleKo = koreanTitle; bodyKo = koreanBody;
            Apply();
        }

        private void Awake()
        {
            panel = GetComponent<WorldPanel>();
            Apply();
        }

        private void OnEnable()
        {
            LabLanguage.Changed += OnLanguageChanged;
            Apply();
        }

        private void OnDisable() => LabLanguage.Changed -= OnLanguageChanged;

        private void OnLanguageChanged(LabLang lang) => Apply();

        private void Apply()
        {
            if (panel == null) panel = GetComponent<WorldPanel>();
            if (panel == null) return;
            panel.SetText(LabLanguage.Pick(titleEn, titleKo), LabLanguage.Pick(bodyEn, bodyKo));
        }
    }
}
