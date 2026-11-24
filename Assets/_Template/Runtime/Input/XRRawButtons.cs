using System.Runtime.InteropServices;

namespace ThreeDUI
{
    /// <summary>
    /// The controller buttons as the browser reports them (XRRawButtons.jslib), next to the WebXR package's own reading.
    /// Some headset browsers report a click (the thumbstick press) as "pressed" with value 0, which the package misses.
    /// Index (xr-standard): 0 trigger, 1 grip, 2 touchpad, 3 thumbstick press, 4 A / X, 5 B / Y.
    /// Outside a WebGL build every call answers -1 (unknown).
    /// </summary>
    public static class XRRawButtons
    {
#if UNITY_WEBGL && !UNITY_EDITOR
        [DllImport("__Internal")] private static extern int ThreeDUI_XRButtonPressed(int hand, int index);
        [DllImport("__Internal")] private static extern int ThreeDUI_XRButtonCount(int hand);
        [DllImport("__Internal")] private static extern string ThreeDUI_XRProfile(int hand);
#else
        private static int ThreeDUI_XRButtonPressed(int hand, int index) => -1;
        private static int ThreeDUI_XRButtonCount(int hand) => -1;
        private static string ThreeDUI_XRProfile(int hand) => "";
#endif

        /// <summary>1 pressed, 0 released, -1 unknown.</summary>
        public static int Pressed(HandSide side, int index) => ThreeDUI_XRButtonPressed(side == HandSide.Left ? 1 : 2, index);
        public static int Count(HandSide side) => ThreeDUI_XRButtonCount(side == HandSide.Left ? 1 : 2);
        public static string Profile(HandSide side) => ThreeDUI_XRProfile(side == HandSide.Left ? 1 : 2);

        public static int IndexOf(XRButton b) => b switch
        {
            XRButton.Trigger => 0,
            XRButton.Grip => 1,
            XRButton.Touchpad => 2,
            XRButton.Thumbstick => 3,
            XRButton.ButtonA => 4,
            _ => 5
        };
    }
}
