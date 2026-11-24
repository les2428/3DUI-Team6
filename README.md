# 3DUI Term Project Template (Fall 2026)

A Unity + WebXR starter for the 3D User Interface term project.
**The same build runs in a desktop browser (mouse and keyboard) and in an XR headset browser (Vive XR Elite)**,
and everything runs **on your own computer (locally)**. No server or account is needed.

> The WebXR infrastructure (rig, locomotion, grabbing, panels, web build) is already done.
> Your job is to design and build the **space, the interactions, and the experience**.

Schedule and what to do in each tutorial: [`Course/Tutorials.md`](Course/Tutorials.md) · Tutorial 1 slides: `Tutorial1_Slides.pdf`

---

## 1. Setup

| | |
|---|---|
| Unity | **6000.2.10f1** (Unity Hub → Installs → Install Editor → Archive, pick this exact version) |
| Module | Same version → ⚙ → Add modules → **WebGL Build Support** (required) |
| Browser | A recent Chrome or Edge |
| (Optional) Python 3 | Only needed to reopen a finished build with `Tools/serve.cmd` |

1. Unzip to a **short path** without spaces or Korean characters, e.g. `C:\Unity\3DUI_Undergrad_Template`.
   A long or deep path makes the first import fail with `Could not find a part of the path ...` (Windows path length limit).
2. Unity Hub → **Add → Add project from disk** → select this folder. The first import takes 5–10 minutes.
3. The **Sample Playground** opens by itself the first time, and a **3DUI** menu appears in the top menu bar.

## 2. When you first open it

A **3DUI** menu appears in the top menu bar. (If it does not, open the Console and fix the first red error.)

| Menu | What it does |
|---|---|
| `3DUI > Open Sample Playground` | Back to the example scene (it also opens by itself the first time) |
| `3DUI > Term Project > New Term Project Scene…` | **Creates your term-project scene.** Save it as `Assets/Teams/<TeamName>/<TeamName>.unity` |
| `3DUI > Create > …` | Places a grabbable object / 3D button / **lamp** / info point / teleport anchor / snap zone / world panel |
| `3DUI > Create > Greybox > …` | Wall (4 m × 3 m), short wall (2 m), table (top at 0.75 m), 1 m block — for blocking out your layout |
| `3DUI > Build > Build and Run locally (current scene)` | Builds the open scene for the web and opens it in your browser at **http://localhost** |
| `3DUI > Build > Build WebGL (current scene only)` | Builds to `Builds/WebGL` only (open it later with `Tools/serve.cmd`) |
| `3DUI > Setup > Apply All Project Settings` | Re-applies the settings if materials turn pink or WebXR looks wrong |
| `3DUI > Rebuild Template (prefabs + scenes)` | Regenerates the template prefabs and the sample scene (your team scene is not touched) |

Scenes:
- `Assets/_Template/Scenes/SamplePlayground.unity` — an example with four stations (Grab · Button · Info · Signature)
- `Assets/Teams/<TeamName>/` — **your scene** (created with New Term Project Scene)

The new term-project scene already contains every requirement as a **labelled placeholder**, laid out in the order a visitor meets them:

```
1. ENTRY  →  2. EXPLORE  →  3. CORE (signature interaction)  →  4. EXIT
```

The first object in the Hierarchy is **★ MY SPACE**: type your team name, the name of your space and one sentence
in its Inspector, and the welcome panel follows (in the editor and in Play).

Then start with the objects whose names begin with `TODO`. Delete the Checklist panel before you submit.

## 3. Controls

| Action | Desktop | XR (controller) |
|---|---|---|
| Move | WASD / arrow keys, Shift to run | Right stick forward → teleport arc → release to go |
| Turn | Drag empty space / right-drag, Q · E | Right stick left / right (45° snap turn) |
| Select / click | Left click | Trigger (point with the ray or touch directly) |
| Grab | Hold left click + move the mouse | Hold trigger or grip |
| Push / pull | Scroll while holding | Stick up / down on the holding hand |
| Jump to a viewpoint | Click a blue circle on the floor | Same |
| Help / debug | H / **F1** | – |

## 4. Structure

```
Assets/
  _Template/            ← provided code (do not edit — updates overwrite it)
    Runtime/
      Core/             XRModeManager (Desktop/VR/AR), PlayerRig, DesktopInput
      Input/            Pointer (the shared interaction loop), DesktopPointer, XRControllerPointer
      Interaction/      Interactable (base class), Grabbable, PushButton3D, SnapZone, TeleportAnchor, TeleportArea
      Feedback/         InteractableFeedback (highlight · sound · haptics), ToneLibrary
      Locomotion/       DesktopLocomotion, XRLocomotion
      UI/               MySpace (★ MY SPACE), WorldPanel, InfoPoint, HoverLabel, Billboard, ModeVisibility, DebugOverlay
      Signature/        SignatureInteraction (state machine), FillSnapZonesSignature (example)
    Editor/             the 3DUI menu (setup, scene creation, build)
  Teams/
    _TeamTemplate/      example scripts (TeamSignatureInteraction, SpinOnActivate, ModeAwareMenuExample)
    <TeamName>/         ← your scenes and scripts
Course/                 tutorial schedule, reference code for the labs
Tools/                  serve.cmd / serve.py (local web server)
```

### The interaction loop

```
[DesktopPointer | XRControllerPointer]
        │  raycast / near (touch) check
        ▼
   Interactable ── HoverEnter → SelectEnter → SelectUpdate → SelectExit → Activate (click) → HoverExit
        ├─ InteractableFeedback (colour · sound · vibration)
        ├─ UnityEvents (wired in the Inspector, no code)
        └─ C# override / event subscription (your code)
```

Every input device drives the same `Interactable`, so **an interaction you build once works on desktop and in XR**.
To make the two behave differently, use `ctx.Mode`, `ctx.IsNear`, `XRModeManager.IsXR`, or `ModeVisibility`.

## 5. Three ways to add an interaction

**① No code:** place an object from `3DUI > Create`, then in the Inspector connect a UnityEvent such as `onActivate`, `onToggledOn` or `onSnapped` to another object.

**② Subclass:** inherit from `Interactable` (or `Grabbable`) and override a hook.
```csharp
public class CountOnActivate : Interactable
{
    [SerializeField] private WorldPanel panel;
    private int count;
    protected override void OnActivate(InteractionContext ctx)
    {
        count++;
        panel.SetText("Pressed", $"{count} time(s)");
    }
}
```

**③ Subscribe:** from another script, listen to `interactable.Activated += ...`, `grabbable.Released += ...`, `snapZone.Snapped += ...`.

Reading XR buttons directly:
```csharp
PlayerRig.Instance.RightController.GetButtonDown(XRButton.ButtonA)
XRModeManager.Haptic(HandSide.Right, 0.5f, 50f)
```

### Signature interaction

`SignatureInteraction` is a `Locked → Ready → InProgress → Completed` state machine.
Edit `TeamSignatureInteraction` and call `Begin()` / `ReportProgress(0..1)` / `Complete()`.
Write your design intent in the component's **DISCOVER / ACT / FEEDBACK** fields and compare it with what peer testers actually do.

## 6. Running it locally

| How | When |
|---|---|
| Unity **Play** ▶ | While editing (desktop mode) |
| `3DUI > Build > Build and Run locally` | To check the real web build. Unity starts a local server and opens your browser |
| `Tools/serve.cmd` (double-click) | To reopen an existing `Builds/WebGL` at http://localhost:8080 (needs Python) |

- Double-clicking `index.html` (`file://`) **does not work**. A web build must be served.
- WebXR only runs over HTTPS or on **localhost**. localhost is allowed, so local testing needs no certificate.
- Opening a local build in the headset: connect the headset to a lab PC by USB, run `adb reverse tcp:8080 tcp:8080`, then open `http://localhost:8080` in the headset browser (we do this together in tutorial 2).
- Web deployment (the URL you submit) happens in tutorial 5: you upload the `Builds/WebGL` folder to the course host.
- **What works in the editor and what works on the web are not the same.** Check the web build at every tutorial.

## 7. Debugging

Press **F1** while running, or add `?debug` to the URL (e.g. `http://localhost:8080/?debug`) to see the mode, FPS, and what each pointer is hovering or holding.

## 8. Furnishing your space with assets

Free, redistributable (CC0) sources: [Kenney](https://kenney.nl/assets) (Furniture Kit, …), [Quaternius](https://quaternius.com), [KayKit](https://kaylousberg.itch.io).
Asset Store assets may go into your build, but their original files must not be pushed to a public repository. Record every asset you use in `CREDITS.md`.

1. Put the files in `Assets/ThirdParty/<PackageName>/`
2. Pink materials → `Window > Rendering > Render Pipeline Converter` → Built-in to URP → Material Upgrade
3. Floors people walk on get a collider + `TeleportArea`; walls get a collider
4. Mark objects that never move as **Static**; keep textures at 1024 px or less

## 9. Performance budget (required)

| | Target |
|---|---|
| Build size | under 60 MB |
| Frame rate | 60 fps desktop · 72 fps headset |
| Textures | 1024 px, compressed |
| Realtime lights | 2 or fewer (bake the rest) |

## 10. Known limits

- **Korean fonts**: `3DUI > Setup > Fonts` registers Noto Sans KR (OFL) as the fallback for all TextMeshPro text.
- **Hand tracking**: works only at the level of pinch = trigger. Joint-based hand interaction is an extension.
- Each team uses **one copy (repository) of the project**. Rename the script namespace from `TeamTemplate` to e.g. `Team07`.
- Tests: `Window > General > Test Runner > PlayMode > Run All` (a virtual pointer operates the sample scene and checks it).
