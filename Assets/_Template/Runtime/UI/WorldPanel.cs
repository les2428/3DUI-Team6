using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ThreeDUI
{
    /// <summary>
    /// A world-space information panel (Spatial UI): title + body text on a background.
    /// Create one with WorldPanel.Create(...) or from the 3DUI menu, then edit the texts in the Inspector.
    /// Uses TextMeshPro (sharp at any distance, which matters in VR). Korean and other scripts work
    /// once a fallback font is set up (see 3DUI > Setup > Fonts).
    /// </summary>
    public class WorldPanel : MonoBehaviour
    {
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text bodyText;
        [SerializeField] private Image background;

        public TMP_Text TitleText => titleText;
        public TMP_Text BodyText => bodyText;
        public Image Background => background;

        public void SetText(string title, string body)
        {
            if (titleText != null)
            {
                titleText.text = title;
                titleText.gameObject.SetActive(!string.IsNullOrEmpty(title));
            }
            if (bodyText != null)
            {
                bodyText.text = body;
                bodyText.gameObject.SetActive(!string.IsNullOrEmpty(body));
            }
        }

        public void Show(bool visible) => gameObject.SetActive(visible);

        /// <summary>Pixels per meter of the world canvas (1000 = 1 canvas unit is 1 mm).</summary>
        public const float PixelsPerMeter = 1000f;

        /// <summary>
        /// Build a panel from code. sizeMeters is the panel size in the world.
        /// </summary>
        public static WorldPanel Create(Transform parent, string name, Vector2 sizeMeters, string title, string body,
            Color? backgroundColor = null)
        {
            var go = new GameObject(name, typeof(RectTransform));
            go.transform.SetParent(parent, false);
            var canvas = go.AddComponent<Canvas>();
            canvas.renderMode = RenderMode.WorldSpace;
            var rt = (RectTransform)go.transform;
            rt.sizeDelta = sizeMeters * PixelsPerMeter;
            rt.localScale = Vector3.one / PixelsPerMeter;

            var panel = go.AddComponent<WorldPanel>();

            var bg = new GameObject("Background", typeof(RectTransform), typeof(Image));
            bg.transform.SetParent(go.transform, false);
            Stretch((RectTransform)bg.transform, 0f);
            panel.background = bg.GetComponent<Image>();
            panel.background.color = backgroundColor ?? new Color(0.08f, 0.1f, 0.14f, 0.88f);
            panel.background.raycastTarget = false;

            var layout = new GameObject("Content", typeof(RectTransform), typeof(VerticalLayoutGroup));
            layout.transform.SetParent(go.transform, false);
            Stretch((RectTransform)layout.transform, 30f);
            var vlg = layout.GetComponent<VerticalLayoutGroup>();
            vlg.spacing = 16f;
            vlg.childControlHeight = true;
            vlg.childControlWidth = true;
            // Give the children the panel's height to share rather than letting them grow past
            // it. A bounded rect is also what makes the auto-sizing in CreateText do anything.
            vlg.childForceExpandHeight = true;
            vlg.childForceExpandWidth = true;

            // Read from a couple of metres away, so the body needs to be closer to the title in size
            // than a screen layout would suggest.
            panel.titleText = CreateText(layout.transform, "Title", 96, FontStyles.Bold, Color.white);
            panel.bodyText = CreateText(layout.transform, "Body", 58, FontStyles.Normal, new Color(0.90f, 0.93f, 0.97f));
            panel.SetText(title, body);
            return panel;
        }

        public static TextMeshProUGUI CreateText(Transform parent, string name, float size, FontStyles style, Color color)
        {
            var go = new GameObject(name, typeof(RectTransform), typeof(TextMeshProUGUI));
            go.transform.SetParent(parent, false);
            var t = go.GetComponent<TextMeshProUGUI>();
            t.fontSize = size;
            t.fontStyle = style;
            t.color = color;
            t.alignment = TextAlignmentOptions.TopLeft;
            t.textWrappingMode = TextWrappingModes.Normal;
            // The same panel carries English and Korean, and English is usually the longer of
            // the two. Rather than cutting the end off, shrink until it fits - down to a floor,
            // below which text would be unreadable across a room anyway.
            t.enableAutoSizing = true;
            t.fontSizeMax = size;
            t.fontSizeMin = Mathf.Max(34f, size * 0.7f);
            t.overflowMode = TextOverflowModes.Truncate;
            t.raycastTarget = false;
            return t;
        }

        private static void Stretch(RectTransform rt, float padding)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.offsetMin = new Vector2(padding, padding);
            rt.offsetMax = new Vector2(-padding, -padding);
        }
    }
}
