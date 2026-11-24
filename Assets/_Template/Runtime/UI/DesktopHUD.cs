using UnityEngine;
using TMPro;
using UnityEngine.UI;

namespace ThreeDUI
{
    /// <summary>
    /// Screen-space overlay for desktop visitors: control help, an "Enter VR" button (only when the
    /// browser supports WebXR VR), and a small crosshair. Hidden automatically in XR.
    /// Press H to show/hide the help text.
    /// </summary>
    public class DesktopHUD : MonoBehaviour
    {
        [SerializeField] private Canvas canvas;
        [SerializeField] private GameObject helpPanel;
        [SerializeField] private Button enterVRButton;
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private KeyCode toggleHelpKey = KeyCode.H;
        [Tooltip("Shown at the top of the help panel. Put your space's name here.")]
        [SerializeField] private string spaceTitle = "My WebXR Space";
        [Tooltip("Korean version of the title and of the control help (used when the visitor picks Korean).")]
        [SerializeField] private string spaceTitleKo = "";
        [SerializeField] private TMP_Text controlsText;
        [TextArea(4, 10)]
        [SerializeField] private string controlsEn = "";
        [TextArea(4, 10)]
        [SerializeField] private string controlsKo = "";
        [SerializeField] private KeyCode toggleCreditsKey = KeyCode.C;

        private TMP_Text creditsText;
        private bool creditsExpanded;

        private void Awake()
        {
            if (canvas == null) canvas = GetComponent<Canvas>();
            ApplyLanguage();
            if (enterVRButton != null) enterVRButton.onClick.AddListener(XRModeManager.ToggleVR);
            BuildCredits();
        }

        // Attribution for third-party assets (AssetCredit components), bottom-right. Press C to expand.
        private void BuildCredits()
        {
            if (canvas == null) return;
            creditsText = WorldPanel.CreateText(canvas.transform, "Credits", 18, FontStyles.Normal, new Color(1f, 1f, 1f, 0.8f));
            var rt = creditsText.rectTransform;
            rt.anchorMin = rt.anchorMax = rt.pivot = new Vector2(1f, 0f);
            rt.anchoredPosition = new Vector2(-24f, 24f);
            rt.sizeDelta = new Vector2(760f, 300f);
            creditsText.alignment = TextAlignmentOptions.BottomRight;
            AssetCredit.Changed += RefreshCredits;
            RefreshCredits();
        }

        private void OnDestroy()
        {
            AssetCredit.Changed -= RefreshCredits;
        }

        private void RefreshCredits()
        {
            if (creditsText == null) return;
            string all = AssetCredit.Compose();
            if (string.IsNullOrEmpty(all)) { creditsText.text = ""; return; }
            creditsText.text = creditsExpanded ? all + $"\n<size=80%>[{toggleCreditsKey}] hide</size>" : $"<size=80%>[{toggleCreditsKey}] Credits</size>";
        }

        private void OnEnable()
        {
            XRModeManager.ModeChanged += OnModeChanged;
            LabLanguage.Changed += OnLanguageChanged;
            OnModeChanged(XRModeManager.Mode);
            ApplyLanguage();
        }

        private void OnDisable()
        {
            XRModeManager.ModeChanged -= OnModeChanged;
            LabLanguage.Changed -= OnLanguageChanged;
        }

        private void OnLanguageChanged(LabLang lang) => ApplyLanguage();

        /// <summary>Title and control help in the language the visitor picked at the entrance.</summary>
        private void ApplyLanguage()
        {
            if (titleText != null) titleText.text = LabLanguage.Pick(spaceTitle, spaceTitleKo);
            if (controlsText != null && !string.IsNullOrEmpty(controlsEn))
                controlsText.text = LabLanguage.Pick(controlsEn, controlsKo);
        }

        /// <summary>Change the help panel's title from code (MySpace uses this).</summary>
        public void SetTitle(string title, string titleKo = "")
        {
            spaceTitle = title;
            spaceTitleKo = titleKo;
            ApplyLanguage();
        }

        private void OnModeChanged(ExperienceMode mode)
        {
            if (canvas != null) canvas.enabled = mode == ExperienceMode.Desktop;
        }

        private void Update()
        {
            if (enterVRButton != null)
            {
                bool show = XRModeManager.IsVRSupported;
                if (enterVRButton.gameObject.activeSelf != show) enterVRButton.gameObject.SetActive(show);
            }
            if (helpPanel != null && DesktopInput.GetKeyDown(toggleHelpKey))
                helpPanel.SetActive(!helpPanel.activeSelf);
            if (DesktopInput.GetKeyDown(toggleCreditsKey))
            {
                creditsExpanded = !creditsExpanded;
                RefreshCredits();
            }
        }
    }
}
