
using UnityEngine;

namespace ThreeDUI
{
    /// <summary>
    /// The hover label of an Interactable ("Click for details" / "눌러서 자세히") in both languages.
    /// </summary>
    public class LocalizedLabel : MonoBehaviour
    {
        [SerializeField] private Interactable target;
        [SerializeField] private string nameEn = "", hintEn = "";
        [SerializeField] private string nameKo = "", hintKo = "";

        public void Set(Interactable interactable, string englishName, string englishHint,
                        string koreanName, string koreanHint)
        {
            target = interactable;
            nameEn = englishName; hintEn = englishHint;
            nameKo = koreanName; hintKo = koreanHint;
            Apply();
        }

        private void Reset() => target = GetComponent<Interactable>();

        private void Awake()
        {
            if (target == null) target = GetComponent<Interactable>();
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
            if (target == null) return;
            target.DisplayName = LabLanguage.Pick(nameEn, nameKo);
            target.Hint = LabLanguage.Pick(hintEn, hintKo);
        }
    }
}
