# Programming Tutorials — Schedule

3D User Interface · Fall 2026 · Hanyang University ERICA

Five sessions, 60–90 minutes each. Each one is a stage of **your own project**, not a separate
exercise: you leave every session with your space further along than you came in with.

> The goal of each tutorial is not to learn many new features, but to finish one more stage of
> your project.

| # | Date | Stage | Template parts covered | You leave with |
| --- | --- | --- | --- | --- |
| 1 | 10/7 | **Get your space running** | project tour · `3DUI` menu · Pointer → Interactable · UnityEvents · local web build | your own scene running in a browser at `http://localhost` |
| — | 10/13–14 | *Initial presentation* | | (use the greybox screenshot from tutorial 1) |
| 2 | 11/4 | **Make it navigable** | room shell & scale · `TeleportArea` / `TeleportAnchor` · desktop vs XR locomotion · importing assets · first headset run | a space a visitor can move through, tested in the headset |
| 3 | 11/11 | **Make it interactive** | `Grabbable` · `PushButton3D` · `SnapZone` · subclassing `Interactable` · `ctx.Mode` / `ctx.IsNear` | the required interactive objects, each with a purpose |
| 4 | 11/18 | **Make it understandable** | `WorldPanel` · `InfoPoint` · `HoverLabel` · `InteractableFeedback` · `ModeVisibility` | information and feedback in the world; a silent user test |
| 5 | 11/25 | **Make it test-ready** | `SignatureInteraction` state machine · DISCOVER / ACT / FEEDBACK notes · performance check · deploying to the course host | the signature interaction working end to end, online |
| — | 12/2 | *Peer XR testing* | | three specific problems, in the testers' words |

**Local first.** The template runs entirely on your own machine: Play mode in the editor, and a
real web build served from `http://localhost` (`3DUI > Build > Build and Run locally`). `localhost`
counts as a secure context, so WebXR works there without HTTPS. You put the space online once,
in tutorial 5, when there is something worth testing.

Tutorial 1 is a **video lecture** (eight short videos on the LMS). Tutorials 2–5 are in the lab and
have the same shape: **~20 min walkthrough, ~60 min building your own project**, a checklist, and an
exit ticket. Come with your project open.

---

## Before 10/7 — setup (at home, ~40 min, mostly downloads)

- [ ] Unity Hub → Installs → **Unity 6000.2.10f1** with **WebGL Build Support**
- [ ] Download `3DUI_Undergrad_Template.zip` and unzip it to a **short path** without spaces or Korean characters, e.g. `C:\Unity\`
- [ ] Open it once in Unity Hub (`Add` → select the folder). The first import takes 5–10 min
- [ ] A recent Chrome or Edge
- [ ] Optional: Python 3 (only for `Tools/serve.cmd`; Unity can serve the build by itself)

Problems here are the only thing that can stop you — post on the LMS Q&A board (with a screenshot of the Console) or mail the TA **before** 10/6.

---

## Tutorial 1 — 10/7 · Get your space running (video lecture)

**Leave with:** your own term-project scene, with your team name on it, running in a browser at
`http://localhost`. Eight short videos on the LMS; each lab video ends with a pause slide — stop
and do it yourself. Reference code for Lab 4: `Course/Tutorial1/CountOnActivate.cs`.

| Video | Part | What you do |
| --- | --- | --- |
| 0 | Welcome | today's goal, the roadmap, what is provided vs. what you design |
| 1 | Setup and the Unity editor | the five editor windows you need, folders, the `3DUI` menu, controls |
| 2 | **Lab 1** — Explore | `3DUI > Open Sample Playground`, Play; the four stations; `F1` debug overlay |
| 3 | How it works | Pointer → Interactable → Feedback; one object works on desktop *and* XR |
| 4 | **Lab 2** — Your scene | `3DUI > Term Project > New Term Project Scene…`; fill in **★ MY SPACE**; greybox your layout with `3DUI > Create > Greybox` |
| 5 | **Lab 3** — No code | `3DUI > Create > Push Button` + `Lamp`; wire On Toggled On / Off to `Bulb (on)` |
| 6 | **Lab 4** — A little code | `CountOnActivate`: a panel that counts clicks |
| 7 | **Lab 5** — Build & run | `3DUI > Build > Build and Run locally`; `?debug`; exit ticket |

### Exit ticket

Upload to the LMS: **one screenshot of your scene in the browser** (address bar showing
`localhost`) with your team name on the welcome panel.

### Checklist

- [ ] Scene saved under `Assets/Teams/<YourTeam>/`
- [ ] ★ MY SPACE filled in: team name, space title, one sentence
- [ ] A greybox of your layout (boxes are fine) — screenshot it for 10/13–14
- [ ] One button that does something, wired without code
- [ ] The `CountOnActivate` counter works
- [ ] Builds without errors and runs at `http://localhost`

### Common problems

| | |
| --- | --- |
| No `3DUI` menu | Scripts did not compile. Open the Console, fix the first red error |
| Pink objects | Run `3DUI > Setup > Apply All Project Settings` |
| Build fails: WebGL not supported | Unity Hub → Installs → ⚙ → Add modules → WebGL Build Support |
| Blank page when you double-click `index.html` | `file://` does not work. Use `Build and Run locally` or `Tools/serve.cmd` |
| Korean text shows as boxes | `3DUI > Setup > Fonts` |

---

## Tutorial 2 — 11/4 · Make it navigable

**Leave with:** a space whose shape is yours, that a visitor can move through without getting lost,
and that you have walked through once in the headset.

| Time | Part | What happens |
| --- | --- | --- |
| 0:00 | Walkthrough | scale (a room reads human-sized at ~3 m ceilings); `TeleportArea` on floors, colliders on walls; viewpoints vs. free movement in XR; landmarks and sightlines |
| 0:20 | Assets | importing a CC0 pack (Kenney, Quaternius); pink materials → Render Pipeline Converter; Static; texture size |
| 0:30 | **Build** | replace the greybox with your real shell; place viewpoints (solo 2 · pair 3 · trio 4) |
| 1:00 | **Headset run** | local build on a lab Vive XR Elite (USB + `adb reverse tcp:8080 tcp:8080`, then `http://localhost:8080` in the headset browser) |
| 1:15 | **Hallway test** | a classmate finds the centre of your space without help; you watch where they hesitate |

- [ ] Shell matches your October layout drawing, or you know why it changed
- [ ] Viewpoints at every place worth standing
- [ ] Someone else reached the centre without being told how
- [ ] `F1` shows 60 fps on desktop; still builds

---

## Tutorial 3 — 11/11 · Make it interactive

**Leave with:** the required interactive objects (solo 3 · pair 5 · trio 8), each doing something
that makes sense in *your* space.

| Time | Part | What happens |
| --- | --- | --- |
| 0:00 | Walkthrough | selection on desktop vs. in a headset; the Interactable lifecycle (Hover → Select → Activate); `Grabbable`, `PushButton3D`, `SnapZone`; subclassing (the `Drawer` example) and `ctx.Mode` / `ctx.IsNear` |
| 0:25 | **Build** | your interactive objects; one line of notes each: *what it is, what a visitor does with it, why it is there* |
| 1:05 | **Headset check** | every object on desktop **and** in the headset |

- [ ] Each object has a purpose you can say in one line
- [ ] Works with the mouse and with the controller (ray and touch)
- [ ] Nothing can be picked up and lost outside the room (`Grabbable.ResetToStart`)

---

## Tutorial 4 — 11/18 · Make it understandable

**Leave with:** a space that explains itself without you standing next to it.

| Time | Part | What happens |
| --- | --- | --- |
| 0:00 | Walkthrough | spatial UI — how big, how far, facing whom (`WorldPanel`, `Billboard`, `InfoPoint`, `HoverLabel`); feedback timing — visual, audio, haptic (`InteractableFeedback`, `ToneLibrary`); affordance; `ModeVisibility` for desktop-only / XR-only hints |
| 0:25 | **Build** | information panels (solo 2 · pair 3 · trio 4); feedback on **every** interaction from tutorial 3 |
| 1:00 | **Silent test** | hand it to someone and say nothing; write down every question they ask out loud |

- [ ] Panels readable standing up, in the headset
- [ ] No silent actions
- [ ] The tester's questions are written down

---

## Tutorial 5 — 11/25 · Make it test-ready

**Leave with:** the signature interaction working end to end, online, and ready for strangers next week.

| Time | Part | What happens |
| --- | --- | --- |
| 0:00 | Walkthrough | `SignatureInteraction`: Locked → Ready → InProgress → Completed; `Begin` / `ReportProgress` / `Complete`; the DISCOVER / ACT / FEEDBACK notes; debugging the web build (console, `?debug`, build size) |
| 0:20 | **Build** | finish the signature interaction; fill in the three notes on the component |
| 0:55 | **Deploy** | upload `Builds/WebGL` to the course host; open the URL on a machine that is not yours and in the headset |
| 1:10 | **Walk it** | the whole space once, as a visitor, entry to exit; fix what you trip over |

- [ ] Signature interaction works on desktop **and** in the headset
- [ ] `discoverability`, `action`, `feedback` filled in on the component
- [ ] Build under 60 MB, holding frame rate
- [ ] Checklist panel deleted
- [ ] URL works on a machine that is not yours

### Before 12/2

Bring: the URL, and **three questions you want testers to answer**. Ask *"did you notice the
turntable?"*, not *"was it good?"*.
