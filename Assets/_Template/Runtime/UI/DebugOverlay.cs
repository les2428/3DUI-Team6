using UnityEngine;

namespace ThreeDUI
{
    /// <summary>
    /// On-screen interaction debugger. Toggle with F1, or open the page with "?debug" in the URL.
    /// Shows the current mode and what each pointer is hovering / holding, and logs select events.
    /// Created automatically; nothing to add to the scene.
    /// </summary>
    public class DebugOverlay : MonoBehaviour
    {
        public static bool Visible { get; private set; }

        private GUIStyle style;
        private string lastEvent = "";

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Create()
        {
            var go = new GameObject("[3DUI DebugOverlay]");
            DontDestroyOnLoad(go);
            go.AddComponent<DebugOverlay>();
            Visible = Application.absoluteURL.Contains("debug");
        }

        private void Update()
        {
            if (DesktopInput.GetKeyDown(KeyCode.F1)) Visible = !Visible;
            if (!Visible || PlayerRig.Instance == null) return;
            var p = PlayerRig.Instance.DesktopPointer;
            if (p != null && DesktopInput.GetMouseButtonDown(DesktopInput.LeftMouse))
                lastEvent = $"LMB down @frame {Time.frameCount}: hovered={Name(p.Hovered)} selected={Name(p.Selected)} lookDrag={p.IsLookDragging}";
        }

        private void LateUpdate()
        {
            if (!Visible || PlayerRig.Instance == null) return;
            var p = PlayerRig.Instance.DesktopPointer;
            if (p != null && DesktopInput.GetMouseButtonDown(DesktopInput.LeftMouse))
            {
                lastEvent += $"\n  after frame: hovered={Name(p.Hovered)} selected={Name(p.Selected)} lookDrag={p.IsLookDragging}";
                Debug.Log("[3DUI debug] " + lastEvent);
            }
        }

        private void OnGUI()
        {
            if (!Visible) return;
            var rig = PlayerRig.Instance;
            if (style == null)
            {
                style = new GUIStyle(GUI.skin.box) { alignment = TextAnchor.UpperLeft, fontSize = 14, wordWrap = true };
                style.normal.textColor = Color.white;
            }

            string text = $"3DUI debug (F1)   mode={XRModeManager.Mode}   fps={1f / Mathf.Max(Time.smoothDeltaTime, 1e-4f):F0}\n";
            if (rig != null)
            {
                text += Line("Mouse", rig.DesktopPointer);
                text += Line("Left", rig.LeftController);
                text += Line("Right", rig.RightController);
                text += $"mouse={DesktopInput.MousePosition}  delta={DesktopInput.MouseDelta}\n";
                text += $"rig={rig.transform.position:F2}  head={rig.Head.position:F2}  facing={rig.HeadForwardFlat:F2}\n";
            }
            text += lastEvent;
            GUI.Box(new Rect(10, 10, 620, 190), text, style);
        }

        private static string Line(string label, Pointer p)
        {
            if (p == null) return $"{label}: -\n";
            string look = p is DesktopPointer d ? $" lookDrag={d.IsLookDragging}" : "";
            return $"{label}: active={p.IsActive} hovered={Name(p.Hovered)} selected={Name(p.Selected)}{look}\n";
        }

        private static string Name(Object o) => o != null ? o.name : "-";
    }
}
