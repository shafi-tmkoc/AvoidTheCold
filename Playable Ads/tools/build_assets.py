"""
Avoid The Cold - playable ad asset pipeline.

Reads the ORIGINAL Unity art/audio/level data from ../../Assets and writes compressed,
portrait-ready copies into ../assets plus a manifest (../assets/manifest.json) and the
level layout table (../src/levels.json) used by the HTML5 game.

Run:  python "Playable Ads/tools/build_assets.py"      (needs Pillow with WebP support)

Audio is NOT converted here (no ffmpeg on the build machine) - see tools/transcode.ps1, which
uses the Windows built-in Media Foundation MP3 encoder. Already-converted clips live in
../assets/audio.
"""
import json, os, re, glob, shutil
from PIL import Image

HERE = os.path.dirname(os.path.abspath(__file__))
PA = os.path.dirname(HERE)                       # Playable Ads/
ROOT = os.path.join(os.path.dirname(PA), "Assets")
OUT_IMG = os.path.join(PA, "assets", "img")
REF = os.path.join(PA, "assets", "ref")           # Playschool end-card kit (extracted from the reference playable)
os.makedirs(OUT_IMG, exist_ok=True)

manifest = {}


def src(p):
    return os.path.join(ROOT, p)


def save(key, im, q=78, lossless=False, note=""):
    path = os.path.join(OUT_IMG, key + ".webp")
    if lossless:
        im.save(path, "WEBP", lossless=True, quality=100, method=6)
    else:
        im.save(path, "WEBP", quality=q, method=6, alpha_quality=85)
    manifest[key] = {"file": "img/" + key + ".webp", "w": im.width, "h": im.height, "bytes": os.path.getsize(path), "note": note}


def scaled(p, s, crop=None, rot=0):
    im = Image.open(src(p)).convert("RGBA")
    if crop:
        im = im.crop(crop)
    if rot:
        im = im.rotate(rot, expand=True)
    if s != 1:
        im = im.resize((max(1, round(im.width * s)), max(1, round(im.height * s))), Image.LANCZOS)
    return im


def rgb(im):
    return im.convert("RGB")


# ---------------- environment (Board2D) ----------------
# BG = Room_2.png. Portrait: only the wallpaper part is shipped; skirting/floor colours are drawn in code
# (sampled: skirting #F96979 y805-855, floor #FEC479).
save("wall", rgb(scaled("Sprites/NewArt/Environment/Room_2.png", 0.75, crop=(0, 0, 960, 805))), q=70, note="Room_2 wallpaper (BG)")
save("frame", scaled("Sprites/NewArt/Environment/Window_3.png", 0.62), q=82, note="WindowFrame, 9-slice L190 T75 R195 B75 (x0.62)")
save("outside", rgb(scaled("Sprites/NewArt/Environment/Donkey_BG.jpg", 0.72)), q=72, note="OutsideEnvironment Winter/Normal")
save("shades", scaled("Sprites/NewArt/outline/Shadow/Level2_Shades.png", 1), q=80, note="OutsideEnvironmentNormal/Window glass shading")
save("sofa", scaled("Sprites/NewArt/Environment/SOfa.png", 0.9), q=80, note="Sofa")
save("mat", scaled("Sprites/NewArt/Environment/MAt.png", 0.7), q=78, note="Sofa/Curtains (mat)")
save("pot", scaled("Sprites/NewArt/Environment/Pot.png", 1).transpose(Image.FLIP_LEFT_RIGHT), q=80, note="Cupboard (flipX in scene)")
save("curtain", scaled("Characters/Prefabs/Curtains/Curtain.png", 1), q=82, note="Curtain prefab (bone-animated in Unity)")
save("tray", scaled("Sprites/Rectangle 3384.png", 1), lossless=True, note="PieceTray, 9-slice 60px")

# characters: Unity uses PSB bone rigs (DayaShivering/TappuShivering prefabs); their idle state swaps to the
# flat 'on sofa' sprites. We ship the flat shivering + idle sprites and animate the shiver in code.
save("daya_s", scaled("Sprites/NewArt/Character/Daya on Sofa_Shivering.png", 0.8), q=80, note="DayaShivering")
save("daya_i", scaled("Sprites/NewArt/Character/Daya on Fofa.png", 0.8), q=80, note="DayaSittingIdle")
save("tappu_s", scaled("Sprites/NewArt/Character/Tappu on sofa_Shivering.png", 0.8), q=80, note="TappuShivering")
save("tappu_i", scaled("Sprites/NewArt/Character/Tappu on sofa.png", 0.8), q=80, note="TappuSittingIdle")

# ---------------- overlays / HUD ----------------
save("frost", scaled("Sprites/NewArt/Avoid the cold.png", 0.25), q=70, note="FrostOverlay (tinted 0.396,0.842,1)")
# ColdFlakes is a landscape snow border: rotate 90deg for portrait so the flakes are not stretched 3x.
save("flakes", scaled("Sprites/NewArt/Environment/ColdFlakes.png", 0.5, rot=90), q=70, note="ColdFlakes (rotated for portrait)")
save("hand", scaled("Sprites/NewUI/Hand 2.png", 0.25), q=82, note="HandTutorialHint")
save("snow", scaled("Sprites/snowflake.png", 0.4), lossless=True, note="Freeze meter icon")
save("border", scaled("Sprites/NewUI/border.png", 1), lossless=True, note="FreezeBG 9-slice")
save("bar", scaled("Sprites/NewUI/Rectangle 3396.png", 1), lossless=True, note="FreezeMeterBar")
# (Retry / Next buttons not shipped - the ad's result banners advance automatically)

# ---------------- result banners (ResultBanners/WinBanner + LoseBanner) ----------------
# WinImage / tryAgain are 1672x941 landscape; the banner art sits in the centre, so a centre crop works for
# portrait - the code extends the remaining area with the sampled edge blue.
for key, p in (("win_bg", "Sprites/Grid Screen/WinImage.png"), ("lose_bg", "Sprites/Grid Screen/tryAgain.png")):
    im = Image.open(src(p)).convert("RGB")
    cx = im.width // 2
    im = im.crop((cx - 380, 0, cx + 380, im.height)).resize((608, 753), Image.LANCZOS)
    save(key, im, q=74, note=p + " centre crop 760px")
save("win_daya", scaled("Sprites/Grid Screen/win_daya.png", 0.8), q=80, note="WinBanner/Daya")
save("win_tappu", scaled("Sprites/Grid Screen/win_tappu.png", 0.8), q=80, note="WinBanner/Tappu")
save("lose_daya", scaled("Sprites/Grid Screen/DayaShivering.png", 0.8), q=80, note="LoseBanner/Daya")
save("lose_tappu", scaled("Sprites/Grid Screen/TappuShivering.png", 0.8), q=80, note="LoseBanner/Tappu")

# (intro story / storyboard intentionally not shipped - the ad opens directly on the core gameplay)

# ---------------- level art + layout (ScriptableObjects/Levels/Level_0N.asset) ----------------
guid = {}
for m in glob.glob(ROOT + "/**/*.meta", recursive=True):
    try:
        t = open(m, encoding="utf-8", errors="ignore").read()
    except OSError:
        continue
    g = re.search(r"^guid: (\w+)", t, re.M)
    if g:
        guid[g.group(1)] = os.path.relpath(m[:-5], ROOT).replace("\\", "/")


def ref_path(s):
    m = re.search(r"guid: (\w+)", s or "")
    return guid.get(m.group(1)) if m else None


OUTSIDE_W, OUTSIDE_H = 11.41, 8.45   # slotAnchor = OutsideEnvironment (Donkey_BG 1141x845 @100ppu)
levels = []
for f in sorted(glob.glob(ROOT + "/ScriptableObjects/Levels/Level_*.asset"))[:5]:   # LevelFlow.levels = Level_01..05
    t = open(f, encoding="utf-8").read()
    n = int(re.search(r"levelNumber: (\d+)", t).group(1))
    lv = {"level": n, "time": float(re.search(r"timeLimitSeconds: ([\d.]+)", t).group(1)), "pieces": []}
    for i, pb in enumerate(t.split("  - shapeId: ")[1:]):
        g = lambda k: re.search(r"^\s*" + k + r": (.*)$", pb, re.M).group(1)
        nums = lambda s: [float(x) for x in re.findall(r"-?[\d.]+(?:e-?\d+)?", s)]
        pos, scl = nums(g("position")), nums(g("scale"))
        slot_p, piece_p, stroke_p = ref_path(g("sprite")), ref_path(g("pieceSprite")), ref_path(g("outlineSprite"))
        slot_im = Image.open(src(slot_p)).convert("RGBA")
        pw, ph = slot_im.size
        k = "L%d_%d" % (n, i + 1)
        save(k + "_slot", slot_im, lossless=True, note=slot_p)
        save(k + "_piece", Image.open(src(piece_p)).convert("RGBA"), lossless=True, note=piece_p)
        save(k + "_stroke", Image.open(src(stroke_p)).convert("RGBA"), lossless=True, note=stroke_p)
        # normalised rect inside the window opening (0..1, y down) - exact LevelLoader transform maths
        w = pw * scl[0] / 100 / OUTSIDE_W
        h = ph * scl[1] / 100 / OUTSIDE_H
        cx = 0.5 + pos[0] / OUTSIDE_W
        cy = 0.5 - pos[1] / OUTSIDE_H
        lv["pieces"].append({"id": pb.split("\n")[0].strip(), "key": k, "cx": round(cx, 5), "cy": round(cy, 5), "w": round(w, 5), "h": round(h, 5), "pw": pw, "ph": ph})
    levels.append(lv)
json.dump(levels, open(os.path.join(PA, "src", "levels.json"), "w"), indent=1)

# ---------------- Playschool end-card kit (same assets as the reference playable) ----------------
for k in ("endbg", "endicon", "endtext", "endbtn", "dlbtn", "shine", "sparkle", "star", "glow"):
    p = os.path.join(REF, k + ".webp")
    shutil.copy(p, os.path.join(OUT_IMG, k + ".webp"))
    im = Image.open(p)
    manifest[k] = {"file": "img/" + k + ".webp", "w": im.width, "h": im.height, "bytes": os.path.getsize(p), "note": "Playschool kit (reference playable)"}

json.dump(manifest, open(os.path.join(PA, "assets", "manifest.json"), "w"), indent=1)
total = sum(v["bytes"] for v in manifest.values())
for k, v in sorted(manifest.items(), key=lambda kv: -kv[1]["bytes"])[:25]:
    print("%-16s %6.1f KB  %dx%d" % (k, v["bytes"] / 1024, v["w"], v["h"]))
print("images: %d files, %.1f KB" % (len(manifest), total / 1024))
