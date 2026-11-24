// Reads the controller buttons straight from the browser's WebXR session (gamepad.buttons[i].pressed).
// The WebXR package reads only button.value; some headset browsers report a click (e.g. the thumbstick) as pressed with value 0.
mergeInto(LibraryManager.library, {
  // hand: 1 = left, 2 = right. Returns 1 pressed, 0 released, -1 no such controller or button.
  ThreeDUI_XRButtonPressed: function (hand, index) {
    var s = Module.WebXR && Module.WebXR.xrSession;
    if (!s || !s.inputSources) return -1;
    var want = hand == 1 ? "left" : "right";
    for (var i = 0; i < s.inputSources.length; i++) {
      var src = s.inputSources[i];
      if (src.handedness !== want || !src.gamepad) continue;
      var b = src.gamepad.buttons[index];
      if (!b) return -1;
      return (b.pressed || b.value > 0.5) ? 1 : 0;
    }
    return -1;
  },

  // Number of buttons the controller reports (-1 if none).
  ThreeDUI_XRButtonCount: function (hand) {
    var s = Module.WebXR && Module.WebXR.xrSession;
    if (!s || !s.inputSources) return -1;
    var want = hand == 1 ? "left" : "right";
    for (var i = 0; i < s.inputSources.length; i++) {
      var src = s.inputSources[i];
      if (src.handedness === want && src.gamepad) return src.gamepad.buttons.length;
    }
    return -1;
  },

  // The controller's first input profile (e.g. "htc-vive-focus-3"), for diagnostics.
  ThreeDUI_XRProfile: function (hand) {
    var s = Module.WebXR && Module.WebXR.xrSession;
    var name = "";
    if (s && s.inputSources) {
      var want = hand == 1 ? "left" : "right";
      for (var i = 0; i < s.inputSources.length; i++) {
        var src = s.inputSources[i];
        if (src.handedness === want) { name = (src.profiles && src.profiles.length ? src.profiles[0] : "?") + (src.gamepad ? "" : " (no gamepad)"); break; }
      }
    }
    return stringToNewUTF8(name);
  }
});
