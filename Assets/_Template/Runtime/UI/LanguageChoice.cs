using UnityEngine;

namespace ThreeDUI
{
    /// <summary>
    /// A language button. Clicking (or triggering) it sets the language for the whole space and,
    /// if it belongs to the entrance picker, closes the picker.
    /// </summary>
    public class LanguageChoice : Interactable
    {
        [Header("Language")]
        [SerializeField] private LabLang language = LabLang.English;
        [Tooltip("The entrance picker to hide after choosing (leave empty for the small switch in the lobby).")]
        [SerializeField] private GameObject closeOnPick;
        [Tooltip("Highlight colour while this language is the active one.")]
        [SerializeField] private Renderer plate;
        [SerializeField] private Color activeColor = new Color(0.17f, 0.65f, 0.72f);
        [SerializeField] private Color inactiveColor = new Color(0.16f, 0.19f, 0.25f);

        protected override void OnActivate(InteractionContext ctx) => Choose();

        public void Choose()
        {
            LabLanguage.Set(language);
            if (closeOnPick != null) closeOnPick.SetActive(false);
        }

        private void OnEnable()
        {
            LabLanguage.Changed += OnLanguageChanged;
            Paint();
        }

        protected override void OnDisable() { base.OnDisable(); LabLanguage.Changed -= OnLanguageChanged; }

        private void OnLanguageChanged(LabLang lang) => Paint();

        private void Paint()
        {
            if (plate == null) return;
            var color = LabLanguage.Current == language ? activeColor : inactiveColor;
            var block = new MaterialPropertyBlock();
            plate.GetPropertyBlock(block);
            block.SetColor("_BaseColor", color);
            block.SetColor("_Color", color);
            plate.SetPropertyBlock(block);
        }
    }
}
