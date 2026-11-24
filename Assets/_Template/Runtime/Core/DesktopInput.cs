using UnityEngine;
using UnityEngine.EventSystems;

namespace ThreeDUI
{
    /// <summary>
    /// All keyboard / mouse reads go through here, so the whole template can be moved to the
    /// new Input System later by changing only this file.
    /// Project Settings > Player > Active Input Handling must be "Input Manager (Old)" or "Both".
    /// </summary>
    public static class DesktopInput
    {
        public const int LeftMouse = 0;
        public const int RightMouse = 1;

        public static Vector2 MousePosition => Input.mousePosition;
        public static Vector2 MouseDelta => new Vector2(Input.GetAxisRaw("Mouse X"), Input.GetAxisRaw("Mouse Y"));
        public static float Scroll => Input.mouseScrollDelta.y;

        public static bool GetMouseButton(int button) => Input.GetMouseButton(button);
        public static bool GetMouseButtonDown(int button) => Input.GetMouseButtonDown(button);
        public static bool GetMouseButtonUp(int button) => Input.GetMouseButtonUp(button);

        /// <summary>True while a text field has keyboard focus: keyboard shortcuts and WASD are ignored.</summary>
        /// <summary>Set by an on-screen text box drawn without a TMP field (the guide agent's "Ask" box).</summary>
        public static bool TypingElsewhere;

        public static bool IsTyping
        {
            get
            {
                if (TypingElsewhere) return true;
                var es = EventSystem.current;
                if (es == null || es.currentSelectedGameObject == null) return false;
                var field = es.currentSelectedGameObject.GetComponent<TMPro.TMP_InputField>();
                return field != null && field.isFocused;
            }
        }

        public static bool GetKey(KeyCode key) => !IsTyping && Input.GetKey(key);
        public static bool GetKeyDown(KeyCode key) => !IsTyping && Input.GetKeyDown(key);

        /// <summary>WASD / arrow keys as a 2D vector (x = strafe, y = forward).</summary>
        public static Vector2 MoveAxis
        {
            get
            {
                var v = Vector2.zero;
                if (IsTyping) return v;
                if (Input.GetKey(KeyCode.W) || Input.GetKey(KeyCode.UpArrow)) v.y += 1f;
                if (Input.GetKey(KeyCode.S) || Input.GetKey(KeyCode.DownArrow)) v.y -= 1f;
                if (Input.GetKey(KeyCode.D)) v.x += 1f;
                if (Input.GetKey(KeyCode.A)) v.x -= 1f;
                return Vector2.ClampMagnitude(v, 1f);
            }
        }

        /// <summary>Q/E and left/right arrows for keyboard turning (-1..1).</summary>
        public static float TurnAxis
        {
            get
            {
                float t = 0f;
                if (IsTyping) return t;
                if (Input.GetKey(KeyCode.RightArrow) || Input.GetKey(KeyCode.E)) t += 1f;
                if (Input.GetKey(KeyCode.LeftArrow) || Input.GetKey(KeyCode.Q)) t -= 1f;
                return t;
            }
        }
    }
}
