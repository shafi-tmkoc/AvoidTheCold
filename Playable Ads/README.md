# Avoid The Cold: Meta Playable Ad (portrait)

The upload file is **`AvoidTheCold_Playable.html`**: one self-contained HTML5 file of about **1.21 MB**, with no external requests.
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

## Flow (same scene order as the Unity game)

1. **Intro story** (`StoryCanvas_Root` + `SampleStory.asset`): 3 storyboard slides with the original captions, `whooh` slide SFX, fade / slide-from-right transitions, and SKIP. Tapping moves to the next slide.
2. **Level 1** (`Level_01`, 4 pieces, 30 s), followed by the **WELL DONE** banner, then **Level 2** (`Level_02`, 3 triangular pieces, 27 s).
3. **Win** (all slots filled): win SFX, confetti, the window crossfades winter→normal, the curtain settles, Daya and Tapu stop shivering, and the tray and wind hide. The WELL DONE banner follows (panel slides up, Tapu and Daya slide in, Retry/Next appear). After the last level it goes to the **Install screen**.
4. **Fail** (freeze meter or timer reaches 0): the frost overlay and snowflakes build up and the meter turns red. The **TRY AGAIN** banner (shivering Daya and Tapu, Retry) follows, then the **Install screen**. Retry also opens the install screen.
5. **Install screen**: the Playschool end slate from the reference playable (icon, "Start your FREE TRIAL now!", store badges). A tap anywhere fires the CTA.

A **DOWNLOAD NOW** button (top-right) is visible during the story and gameplay. It fires the same CTA.

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

- **Portrait layout**: the window is full-width at the top, the sofa with Daya, Tapu and the cupboard is in the middle, and the piece tray is at the bottom. The design space is 1080 wide; taller phones extend vertically and wider screens (tablets) extend horizontally.
- The **story is shortened** (9/4/6 s → 4.2/2.8/3.4 s). Tapping advances and SKIP is kept.
- **Result delays are shortened** (win banner 5 s → 2.4 s, lose 2.5 s → 1.4 s). The win banner auto-advances to the next level.
- **2 levels** are played (`CFG.adLevels = [0, 1]`). All 5 Unity levels are bundled, so playing more only means adding indices.
- Loose tray pieces use a stronger glass tint (0.62 instead of 0.31) so they read on small screens. Placed panes keep the original 0.31.
- A one-line on-screen instruction stands in for the tutorial voice-over.
- The DOWNLOAD NOW button, the install end screen, and a 100 s fallback that opens the install screen were added.
- The wind loop gets quieter once the window is sealed.

## Meta compatibility

| Requirement (Meta playable ads docs) | Status |
|---|---|
| Single HTML file ≤ **2 MB** (zip ≤ 5 MB, `index.html` ≤ 2 MB) | ✅ 1.21 MB single file (images 867 KB, audio 255 KB, font 65 KB, code ~50 KB, all as base64) |
| No external network requests / dynamic loading | ✅ All assets are inlined data URIs. Verified in the browser: the only request is the HTML file itself |
| CTA must call `FbPlayableAd.onCTAClick()` (no parameters) | ✅ `cta()` calls it when present (install screen and DOWNLOAD button). Tested with a mocked `FbPlayableAd` |
| No JavaScript redirects (`window.open`, `location.href = …`) | ✅ None. Outside Meta (standalone preview) a temporary `<a target=_blank>` opens the install link |
| Portrait orientation | ✅ `<meta name="ad.orientation" content="portrait">`. The layout adapts from 16:9 to 19.5:9 phones and 3:4 tablets |
| Audio only after user interaction | ✅ WebAudio unlocks on the first tap. Audio is suspended when the page is hidden (`visibilitychange`) |
| Touch input | ✅ Pointer Events (touch + mouse) with `touch-action: none` |

**Install destination:** inside Meta the click goes to the URL set on the ad in Ads Manager. Set that to the Playschool link `https://playschool.page.link/X2cE` (the same `INSTALL_LINK` the reference ZombieEscape playable uses).

**Before upload:** run the file through Meta's **Playable Preview Tool** (Ads Manager → playable asset preview) to confirm the CTA is detected on a real device.

## Known limitations

- **No voice-over.** The Unity build streams its VO (Intro, Storyboard1-3, Tutorial, Encouragement, Success, Fail…) from a server through `RuntimeAudioLoader`. No local VO clips exist in the project, so the story captions and the instruction line carry the narration.
- **Character rigs are approximated.** The PSB bone animations are not exported. The shiver and idle use the project's flat sprites with procedural motion instead of the exact bone curves.
- **The wind loop is trimmed** to a 12 s seamless-ish section of the 110 s `Winters Wind Sfx.mp3` to save space. All audio is re-encoded to 22 kHz mono at 40-48 kbps.
- The **Back button** (exits to the Playschool menu) and the Playschool `WinLosePanel` / `GameCompleteUI` paths are not used in the ad.
- Timing is frame-based (`requestAnimationFrame`, dt capped at 1/30 s), so if the ad runs in a background or throttled tab the game simply runs slower instead of skipping ahead.

## QA hooks

`window.__ATC` exposes `skipStory()`, `solveOne()`, `wrongOne()`, `freeze()` (forces a fail), `advance(seconds)` (a deterministic clock for throttled tabs), plus `G` / `CFG` state. They are harmless in production.
