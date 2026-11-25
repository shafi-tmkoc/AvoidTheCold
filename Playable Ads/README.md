# Avoid The Cold: Meta Playable Ad (portrait)

The upload file is **`AvoidTheCold_Playable.html`**: one self-contained HTML5 file of about **1.11 MB**, with no external requests.
Everything else in this folder is source, assets and tooling to rebuild it.

```
Playable Ads/
├── AvoidTheCold_Playable.html   ← the upload file (single file, every asset inlined as base64)
├── README.md                    ← this document
├── src/
│   ├── game.js                  ← game code (canvas 2D, no libraries)
│   ├── template.html            ← HTML shell
│   └── levels.json              ← generated from Assets/ScriptableObjects/Levels/Level_01..05.asset
├── assets/
│   ├── img/*.webp               ← compressed copies of the Unity sprites (+ manifest.json)
│   ├── audio/*.mp3              ← 22 kHz mono re-encodes of the Unity audio
│   └── ref/*.webp               ← Playschool end-card kit (same assets as ZombieEscape.html)
└── tools/
    ├── build_assets.py          ← Unity Assets/ → assets/img + src/levels.json
    ├── transcode.ps1            ← MP3 re-encoder (Windows Media Foundation, no ffmpeg needed)
    └── build.py                 ← inlines everything → AvoidTheCold_Playable.html, fails if > 2 MB
```

## Rebuild

```bash
python "Playable Ads/tools/build_assets.py"
```

```bash
python "Playable Ads/tools/build.py"
```

You only need `build_assets.py` when the Unity art or level data changes. `build.py` alone is enough after code edits.
Audio re-encode example (PowerShell):
`& "Playable Ads\tools\transcode.ps1" -In "Assets\Audio\SFX\BGSound.mp3" -Out "Playable Ads\assets\audio\bgm.mp3" -Bitrate 48000 -Rate 22050`

## Flow

The ad opens directly on the core gameplay (no intro story).

1. **Level 1** (`Level_01`, 4 pieces, 14 s freeze time). A hand hint and a one-line instruction show what to do. The freeze timer starts on the viewer's first touch.
2. **Win** (all slots filled): win SFX, confetti, the window crossfades winter→normal, the curtain settles, Daya and Tapu stop shivering, and the tray and wind hide. The **WELL DONE** banner pops up (panel slides up, Tapu and Daya slide in), holds briefly, and the game moves on by itself to **Level 2** — Unity's `Level_04` (5 pieces, 13 s freeze time). After the last level the **Install screen** opens automatically.
3. **Fail** (freeze meter or timer reaches 0): the frost overlay and snowflakes build up and the meter turns red. The **TRY AGAIN** banner (shivering Daya and Tapu) pops up, holds briefly, and the same level restarts by itself. The fail path never opens the install screen.
4. **Install screen**: the Playschool end slate from the reference playable (icon, "Start your FREE TRIAL now!", store badges). A tap anywhere fires the CTA.

A **DOWNLOAD NOW** button (top-right) is visible during gameplay. It fires the same CTA.

## What was carried over from Unity (script → HTML5)

| Unity | HTML5 |
|---|---|
| `LevelLoader` / `LevelData` | Slot rects use the exact `localPosition`/`localScale` maths relative to `OutsideEnvironment` (11.41×8.45 units). The tray shows 3 pieces (`trayVisibleCount`), the queue reveals the next piece into the freed cell, and each cell fits and clamps its own piece. |
| `DraggableShape` / `ShapeDropSlot` | Drag from the tray. On drop, among the slots the piece's bounds touch, the one with the nearest centre wins. A correct drop snaps with a 0.35 s OutBack tween, `connected.mp3` and a StarBurst. A wrong drop returns the piece to the tray. |
| `CountdownTimer` + `FreezeMeterValue` + `MissionResolver` | Meter −= dt/duration. Whichever of meter or timer hits 0 first fails the level. Filling every slot first wins. |
| `FreezeMeterUI` / `DangerWarningEffect` | Horizontal fill bar. Fades to red (0.9, 0.2, 0.2) at ≤ 30 % over 0.4 s. |
| `FrostOverlayEffect` / `CanvasGroupFader` | Frost and ColdFlakes ramp from meter 0.5 to 0, moving at 1 alpha/s. |
| `OutsideEnvironmentSeasonSwitcher` | Winter (darkened) → normal + glass shades, 0.6 s crossfade. |
| `AnimatorStateOnPuzzleComplete` | Curtain `CurtainSway` → `CurtainSwayToNormal`. Daya/Tappu Shivering → SittingIdle (sprite swap, as in the anim). |
| `HideOnPuzzleComplete` / `ShowOnPuzzleComplete` | Tray and wind lines fade out. Confetti plays. |
| `LevelTutorialSequencer` / `IdleHandTutorial` / `HandTutorialUI` | Hand hint for the first 2 pieces of level 1, then an idle hint after 7 s. The hand moves 0.9 s with a 0.3 s pause at each end. |
| `ResultScreenUI` / `ResultBannerIntroAnimation` | Panel slides up 0.45 s OutCubic, sides slide in 0.4 s OutBack, then the buttons appear. |
| `AudioManager` | BG music loop (`BGSound`), wind loop (`Winters Wind Sfx`, vol 0.29), `connected`, `win sfx 03`, `whooh`, `bottle-pop`. |

### Animation implementation

All animation is plain Canvas 2D in a ~50 KB script. **No external libraries** (no DOTween equivalent, no PixiJS or Spine needed):

- Tweens and easing (OutBack, OutCubic, OutElastic, SmoothStep) come from a small built-in tween runner that replaces DOTween.
- The curtain bone animation (4-bone chain) is a strip-based mesh deformation plus the `CurtainWaveEffect` sway, flutter and billow values.
- The character bone rigs (PSB + 2D Animation) use the flat shivering and idle sprites from the project, with a procedural shiver jitter (0.75 s / 1 s loop) and an idle breathing loop (1.25 s).
- The particle effects (Epic Toon FX StarBurst2D, WindlinesStormy, ParticleImage ConfettiFullscreen) are reproduced with a lightweight particle system.

## Ad-only changes (marked `AD:` in `src/game.js`)

- **Portrait layout**: a wide window (opening ~1.42:1, close to the scene's 1.49:1) on the left of the wall, with the curtain attached at its top-right corner in its natural proportions, flying slightly past the screen edge, the sofa with Daya, Tapu and the cupboard in the middle, and the piece tray at the bottom. The design space is 1080 wide; taller phones extend vertically and wider screens (tablets) extend horizontally.
- **No intro story**: the storyboard slides are not shipped; the ad opens on Level 1 and the freeze timer starts on the first touch.
- **No buttons on the result banners**: WELL DONE / TRY AGAIN pop up, hold for 2.6 s after their intro animation, then continue automatically (win → next level or install screen, lose → same level again). The only interaction is in gameplay (plus the DOWNLOAD button and the install screen tap).
- **Softer motion**: a gentler character shiver and lighter wind streaks than the stormy scene. Characters get a floor contact shadow and a soft drop shadow.
- Result banners: the art sits higher and the characters stand lower, with the edges blended into the extended blue.
- **Result delays are shortened** (win banner 5 s → 2.4 s, lose 2.5 s → 1.4 s). 
- **2 levels** are played: Unity `Level_01` then `Level_04` (`CFG.adLevels = [0, 3]`). All 5 Unity levels are bundled, so swapping or adding levels only means changing indices.
- **Freeze time 10-15 s** per level (`CFG.levelTime = [14, 13]`; Unity: Level_01 30 s, Level_04 24 s).
- Loose tray pieces use a stronger glass tint (0.62 instead of 0.31) so they read on small screens. Placed panes keep the original 0.31.
- A one-line on-screen instruction stands in for the tutorial voice-over.
- The DOWNLOAD NOW button and the install end screen were added.
- The wind loop gets quieter once the window is sealed.

## Meta compatibility

| Requirement (Meta playable ads docs) | Status |
|---|---|
| Single HTML file ≤ **2 MB** (zip ≤ 5 MB, `index.html` ≤ 2 MB) | ✅ 1.11 MB single file (images 768 KB, audio 255 KB, font 65 KB, code ~45 KB, all as base64) |
| No external network requests / dynamic loading | ✅ All assets are inlined data URIs. Verified in the browser: the only request is the HTML file itself |
| CTA must call `FbPlayableAd.onCTAClick()` (no parameters) | ✅ `cta()` calls it when present (install screen and DOWNLOAD button). Tested with a mocked `FbPlayableAd` |
| No JavaScript redirects (`window.open`, `location.href = …`) | ✅ None. Outside Meta (standalone preview) a temporary `<a target=_blank>` opens the install link |
| Portrait orientation | ✅ `<meta name="ad.orientation" content="portrait">`. The layout adapts from 16:9 to 19.5:9 phones and 3:4 tablets |
| Audio only after user interaction | ✅ WebAudio unlocks on the first tap. Audio is suspended when the page is hidden (`visibilitychange`) |
| Touch input | ✅ Pointer Events (touch + mouse) with `touch-action: none` |

**Install destination:** inside Meta the click goes to the URL set on the ad in Ads Manager. Set that to the Playschool link `https://playschool.page.link/X2cE` (the same `INSTALL_LINK` the reference ZombieEscape playable uses).

**Before upload:** run the file through Meta's **Playable Preview Tool** (Ads Manager → playable asset preview) to confirm the CTA is detected on a real device.

## Known limitations

- **No voice-over.** The Unity build streams its VO (Tutorial, Encouragement, Success, Fail…) from a server through `RuntimeAudioLoader`. No local VO clips exist in the project, so a one-line on-screen instruction stands in for the tutorial VO.
- **Character rigs are approximated.** The PSB bone animations are not exported. The shiver and idle use the project's flat sprites with procedural motion instead of the exact bone curves.
- **The wind loop is trimmed** to a 12 s seamless-ish section of the 110 s `Winters Wind Sfx.mp3` to save space. All audio is re-encoded to 22 kHz mono at 40-48 kbps.
- The **Back button** (exits to the Playschool menu) and the Playschool `WinLosePanel` / `GameCompleteUI` paths are not used in the ad.
- Timing is **real-time**: each display frame runs as many 1/30 s sub-steps as needed, so the freeze meter empties in its set time even on a slow phone (verified at a simulated 12 fps and at 60 fps). Gaps over 0.25 s (tab switch) are capped so nothing jumps. Drop shadows are pre-rendered once, not blurred every frame.

## QA hooks

`window.__ATC` exposes `solveOne()`, `wrongOne()`, `freeze()` (forces a fail), `advance(seconds)` (a deterministic clock for throttled tabs), `step(ms)` (drive one display frame), plus `G` / `CFG` state. They are harmless in production.
